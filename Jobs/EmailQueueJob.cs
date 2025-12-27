using Hangfire;
using Microsoft.EntityFrameworkCore;
using SciSubmit.Data;
using SciSubmit.Models.Enums;
using SciSubmit.Services;

namespace SciSubmit.Jobs
{
    public class EmailQueueJob
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<EmailQueueJob> _logger;

        public EmailQueueJob(IServiceProvider serviceProvider, ILogger<EmailQueueJob> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        /// <summary>
        /// Process pending email notifications from queue
        /// </summary>
        [AutomaticRetry(Attempts = 3)]
        public async Task ProcessEmailQueueAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

            try
            {
                // Get pending emails (limit to 50 per batch)
                var pendingEmails = await context.EmailNotifications
                    .Where(e => e.Status == EmailNotificationStatus.Pending)
                    .OrderBy(e => e.CreatedAt)
                    .Take(50)
                    .ToListAsync();

                if (!pendingEmails.Any())
                {
                    _logger.LogInformation("No pending emails to process");
                    return;
                }

                _logger.LogInformation($"Processing {pendingEmails.Count} pending emails");

                foreach (var email in pendingEmails)
                {
                    try
                    {
                        await emailService.SendEmailNotificationAsync(email);
                        _logger.LogInformation($"Email {email.Id} sent successfully to {email.ToEmail}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Failed to send email {email.Id} to {email.ToEmail}");
                        // Email status will be updated to Failed by EmailService
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing email queue");
                throw;
            }
        }

        /// <summary>
        /// Send reminder emails for upcoming deadlines
        /// </summary>
        [AutomaticRetry(Attempts = 2)]
        public async Task SendDeadlineRemindersAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

            try
            {
                var threeDaysFromNow = DateTime.UtcNow.AddDays(3);
                var fourDaysFromNow = DateTime.UtcNow.AddDays(4);

                // Find review assignments with deadlines in 3 days
                // Check if reminder was already sent by checking EmailNotifications
                var upcomingDeadlines = await context.ReviewAssignments
                    .Include(ra => ra.Reviewer)
                    .Include(ra => ra.Submission)
                        .ThenInclude(s => s.Author)
                    .Where(ra => ra.Status == ReviewAssignmentStatus.Accepted &&
                                ra.Deadline >= threeDaysFromNow &&
                                ra.Deadline < fourDaysFromNow)
                    .ToListAsync();

                // Filter out assignments that already have reminder sent
                var assignmentsToRemind = new List<Models.Review.ReviewAssignment>();
                foreach (var assignment in upcomingDeadlines)
                {
                    var hasReminder = await context.EmailNotifications
                        .AnyAsync(e => e.RelatedSubmissionId == assignment.SubmissionId &&
                                     e.RelatedUserId == assignment.ReviewerId &&
                                     e.Type == "DeadlineReminder" &&
                                     e.CreatedAt >= threeDaysFromNow.AddDays(-1));
                    
                    if (!hasReminder)
                    {
                        assignmentsToRemind.Add(assignment);
                    }
                }

                foreach (var assignment in upcomingDeadlines)
                {
                    try
                    {
                        var emailNotification = new Models.Notification.EmailNotification
                        {
                            ToEmail = assignment.Reviewer.Email,
                            Subject = $"Nhắc nhở: Deadline phản biện sắp đến - {assignment.Submission.Title}",
                            Body = $@"
                                <h2>Nhắc nhở deadline phản biện</h2>
                                <p>Xin chào {assignment.Reviewer.FullName},</p>
                                <p>Bạn có một deadline phản biện sắp đến trong 3 ngày:</p>
                                <ul>
                                    <li><strong>Bài báo:</strong> {assignment.Submission.Title}</li>
                                    <li><strong>Deadline:</strong> {assignment.Deadline:dd/MM/yyyy HH:mm}</li>
                                </ul>
                                <p>Vui lòng hoàn thành phản biện trước deadline.</p>
                                <p>Trân trọng,<br>Ban tổ chức</p>
                            ",
                            Type = "DeadlineReminder",
                            Status = EmailNotificationStatus.Pending,
                            RelatedSubmissionId = assignment.SubmissionId,
                            RelatedUserId = assignment.ReviewerId,
                            CreatedAt = DateTime.UtcNow
                        };

                        context.EmailNotifications.Add(emailNotification);
                        assignment.UpdatedAt = DateTime.UtcNow;

                        await context.SaveChangesAsync();

                        // Create in-app notification
                        try
                        {
                            var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
                            var userNotification = new Models.Notification.UserNotification
                            {
                                UserId = assignment.ReviewerId,
                                Type = Models.Enums.NotificationType.DeadlineReminder,
                                Title = "Deadline Reminder",
                                Message = $"You have a review deadline approaching in 3 days for submission \"{assignment.Submission.Title}\". Deadline: {assignment.Deadline:dd/MM/yyyy HH:mm}.",
                                Status = Models.Enums.NotificationStatus.Unread,
                                RelatedSubmissionId = assignment.SubmissionId,
                                CreatedAt = DateTime.UtcNow
                            };
                            await notificationService.CreateNotificationAsync(userNotification);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"Error creating deadline reminder notification for assignment {assignment.Id}");
                        }

                        await emailService.SendEmailNotificationAsync(emailNotification);

                        _logger.LogInformation($"Reminder sent for review assignment {assignment.Id}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Failed to send reminder for review assignment {assignment.Id}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending deadline reminders");
                throw;
            }
        }
    }
}

