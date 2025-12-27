using Microsoft.EntityFrameworkCore;
using SciSubmit.Data;
using SciSubmit.Models.Review;
using SciSubmit.Models.Enums;
using SciSubmit.Models.Notification;

namespace SciSubmit.Services
{
    public class ReviewService : IReviewService
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly INotificationService _notificationService;

        public ReviewService(ApplicationDbContext context, IEmailService emailService, INotificationService notificationService)
        {
            _context = context;
            _emailService = emailService;
            _notificationService = notificationService;
        }

        public async Task<ReviewerDashboardViewModel> GetReviewerDashboardAsync(int reviewerId)
        {
            var assignments = await _context.ReviewAssignments
                .Include(ra => ra.Submission)
                    .ThenInclude(s => s.SubmissionTopics)
                        .ThenInclude(st => st.Topic)
                .Include(ra => ra.Submission)
                    .ThenInclude(s => s.FullPaperVersions)
                .Where(ra => ra.ReviewerId == reviewerId)
                .ToListAsync();

            var now = DateTime.UtcNow;
            var urgentThreshold = now.AddDays(3);

            var dashboard = new ReviewerDashboardViewModel
            {
                TotalAssignments = assignments.Count,
                PendingAssignments = assignments.Count(ra => ra.Status == ReviewAssignmentStatus.Pending),
                AcceptedAssignments = assignments.Count(ra => ra.Status == ReviewAssignmentStatus.Accepted && ra.CompletedAt == null),
                CompletedAssignments = assignments.Count(ra => ra.Status == ReviewAssignmentStatus.Completed),
                UrgentAssignments = assignments.Count(ra => 
                    ra.Status == ReviewAssignmentStatus.Accepted && 
                    ra.CompletedAt == null && 
                    ra.Deadline <= urgentThreshold && 
                    ra.Deadline > now)
            };

            // Get pending reviews (Accepted but not completed)
            var pendingAssignments = assignments
                .Where(ra => ra.Status == ReviewAssignmentStatus.Accepted && ra.CompletedAt == null)
                .OrderBy(ra => ra.Deadline)
                .ToList();

            var pendingReviews = new List<ReviewAssignmentItemViewModel>();
            foreach (var ra in pendingAssignments)
            {
                var fullPaperVersion = ra.Submission.FullPaperVersions
                    .FirstOrDefault(v => v.IsCurrentVersion);

                pendingReviews.Add(new ReviewAssignmentItemViewModel
                {
                    Id = ra.Id,
                    SubmissionId = ra.SubmissionId,
                    Title = ra.Submission.Title,
                    Abstract = ra.Submission.Abstract ?? string.Empty,
                    Topics = ra.Submission.SubmissionTopics
                        .Where(st => st.Topic != null)
                        .Select(st => st.Topic!.Name)
                        .ToList(),
                    Deadline = ra.Deadline,
                    InvitedAt = ra.InvitedAt,
                    AcceptedAt = ra.AcceptedAt,
                    Status = ra.Status.ToString(),
                    DaysRemaining = (int)Math.Ceiling((ra.Deadline - now).TotalDays),
                    IsUrgent = ra.Deadline <= urgentThreshold && ra.Deadline > now,
                    AbstractFileUrl = ra.Submission.AbstractFileUrl,
                    FullPaperFileUrl = fullPaperVersion?.FileUrl
                });
            }

            // Get completed reviews
            var completedAssignments = assignments
                .Where(ra => ra.Status == ReviewAssignmentStatus.Completed)
                .OrderByDescending(ra => ra.CompletedAt)
                .ToList();

            var completedReviews = new List<ReviewAssignmentItemViewModel>();
            foreach (var ra in completedAssignments)
            {
                var fullPaperVersion = ra.Submission.FullPaperVersions
                    .FirstOrDefault(v => v.IsCurrentVersion);

                completedReviews.Add(new ReviewAssignmentItemViewModel
                {
                    Id = ra.Id,
                    SubmissionId = ra.SubmissionId,
                    Title = ra.Submission.Title,
                    Abstract = ra.Submission.Abstract ?? string.Empty,
                    Topics = ra.Submission.SubmissionTopics
                        .Where(st => st.Topic != null)
                        .Select(st => st.Topic!.Name)
                        .ToList(),
                    Deadline = ra.Deadline,
                    InvitedAt = ra.InvitedAt,
                    AcceptedAt = ra.AcceptedAt,
                    CompletedAt = ra.CompletedAt,
                    Status = ra.Status.ToString(),
                    DaysRemaining = 0,
                    IsUrgent = false,
                    AbstractFileUrl = ra.Submission.AbstractFileUrl,
                    FullPaperFileUrl = fullPaperVersion?.FileUrl
                });
            }

            dashboard.PendingReviews = pendingReviews;
            dashboard.CompletedReviews = completedReviews;

            return dashboard;
        }

        public async Task<ReviewAssignmentItemViewModel?> GetReviewAssignmentAsync(int assignmentId, int reviewerId)
        {
            var assignment = await _context.ReviewAssignments
                .Include(ra => ra.Submission)
                    .ThenInclude(s => s.SubmissionTopics)
                        .ThenInclude(st => st.Topic)
                .Include(ra => ra.Submission)
                    .ThenInclude(s => s.FullPaperVersions)
                .FirstOrDefaultAsync(ra => ra.Id == assignmentId && ra.ReviewerId == reviewerId);

            if (assignment == null)
                return null;

            var now = DateTime.UtcNow;
            var urgentThreshold = now.AddDays(3);

            var fullPaperVersion = assignment.Submission.FullPaperVersions
                .FirstOrDefault(v => v.IsCurrentVersion);

            return new ReviewAssignmentItemViewModel
            {
                Id = assignment.Id,
                SubmissionId = assignment.SubmissionId,
                Title = assignment.Submission.Title,
                Abstract = assignment.Submission.Abstract ?? string.Empty,
                Topics = assignment.Submission.SubmissionTopics
                    .Where(st => st.Topic != null)
                    .Select(st => st.Topic!.Name)
                    .ToList(),
                Deadline = assignment.Deadline,
                InvitedAt = assignment.InvitedAt,
                AcceptedAt = assignment.AcceptedAt,
                CompletedAt = assignment.CompletedAt,
                Status = assignment.Status.ToString(),
                DaysRemaining = assignment.Deadline > now ? (int)Math.Ceiling((assignment.Deadline - now).TotalDays) : 0,
                IsUrgent = assignment.Deadline <= urgentThreshold && assignment.Deadline > now,
                AbstractFileUrl = assignment.Submission.AbstractFileUrl,
                FullPaperFileUrl = fullPaperVersion?.FileUrl
            };
        }

        public async Task<ReviewDetailsViewModel?> GetReviewDetailsAsync(int assignmentId, int reviewerId)
        {
            var assignment = await _context.ReviewAssignments
                .Include(ra => ra.Submission)
                    .ThenInclude(s => s.SubmissionTopics)
                        .ThenInclude(st => st.Topic)
                .Include(ra => ra.Submission)
                    .ThenInclude(s => s.Conference)
                .Include(ra => ra.Submission)
                    .ThenInclude(s => s.FullPaperVersions)
                .Include(ra => ra.Review)
                    .ThenInclude(r => r.ReviewScores)
                .FirstOrDefaultAsync(ra => ra.Id == assignmentId && ra.ReviewerId == reviewerId);

            if (assignment == null)
                return null;

            var now = DateTime.UtcNow;
            var urgentThreshold = now.AddDays(3);

            var fullPaperVersion = assignment.Submission.FullPaperVersions
                .FirstOrDefault(v => v.IsCurrentVersion);

            // Get review criteria for the conference
            var criteria = await _context.ReviewCriterias
                .Where(rc => rc.ConferenceId == assignment.Submission.ConferenceId && rc.IsActive)
                .OrderBy(rc => rc.OrderIndex)
                .ThenBy(rc => rc.Name)
                .Select(rc => new ReviewCriteriaViewModel
                {
                    Id = rc.Id,
                    Name = rc.Name,
                    Description = rc.Description,
                    MaxScore = rc.MaxScore,
                    OrderIndex = rc.OrderIndex
                })
                .ToListAsync();

            // Check if review already exists
            var existingReview = assignment.Review;
            var existingScores = new Dictionary<string, int>();
            if (existingReview != null)
            {
                foreach (var score in existingReview.ReviewScores)
                {
                    existingScores[score.CriteriaName] = score.Score;
                }
            }

            return new ReviewDetailsViewModel
            {
                AssignmentId = assignment.Id,
                SubmissionId = assignment.SubmissionId,
                Title = assignment.Submission.Title,
                Abstract = assignment.Submission.Abstract ?? string.Empty,
                Topics = assignment.Submission.SubmissionTopics
                    .Where(st => st.Topic != null)
                    .Select(st => st.Topic!.Name)
                    .ToList(),
                AbstractFileUrl = assignment.Submission.AbstractFileUrl,
                FullPaperFileUrl = fullPaperVersion?.FileUrl,
                Deadline = assignment.Deadline,
                InvitedAt = assignment.InvitedAt,
                AcceptedAt = assignment.AcceptedAt,
                Status = assignment.Status.ToString(),
                DaysRemaining = assignment.Deadline > now ? (int)Math.Ceiling((assignment.Deadline - now).TotalDays) : 0,
                IsUrgent = assignment.Deadline <= urgentThreshold && assignment.Deadline > now,
                Criteria = criteria,
                HasExistingReview = existingReview != null,
                ReviewId = existingReview?.Id,
                ExistingScores = existingScores,
                ExistingCommentsForAuthor = existingReview?.CommentsForAuthor,
                ExistingCommentsForAdmin = existingReview?.CommentsForAdmin,
                ExistingRecommendation = existingReview?.Recommendation
            };
        }

        public async Task<bool> AcceptInvitationAsync(int assignmentId, int reviewerId)
        {
            var assignment = await _context.ReviewAssignments
                .Include(ra => ra.Submission)
                .FirstOrDefaultAsync(ra => ra.Id == assignmentId && ra.ReviewerId == reviewerId);

            if (assignment == null)
            {
                return false; // Assignment not found or not owned by reviewer
            }

            if (assignment.Status != ReviewAssignmentStatus.Pending)
            {
                return false; // Already accepted or rejected
            }

            // Update assignment status
            assignment.Status = ReviewAssignmentStatus.Accepted;
            assignment.AcceptedAt = DateTime.UtcNow;
            assignment.UpdatedAt = DateTime.UtcNow;

            // Update submission status if needed
            if (assignment.Submission.Status == SubmissionStatus.PendingAbstractReview)
            {
                // Keep as PendingAbstractReview until review is completed
            }
            else if (assignment.Submission.Status == SubmissionStatus.AbstractApproved ||
                     assignment.Submission.Status == SubmissionStatus.FullPaperSubmitted)
            {
                assignment.Submission.Status = SubmissionStatus.UnderReview;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RejectInvitationAsync(int assignmentId, int reviewerId, string? reason = null)
        {
            var assignment = await _context.ReviewAssignments
                .Include(ra => ra.Submission)
                .FirstOrDefaultAsync(ra => ra.Id == assignmentId && ra.ReviewerId == reviewerId);

            if (assignment == null)
            {
                return false; // Assignment not found or not owned by reviewer
            }

            if (assignment.Status != ReviewAssignmentStatus.Pending)
            {
                return false; // Already accepted or rejected
            }

            // Update assignment status
            assignment.Status = ReviewAssignmentStatus.Rejected;
            assignment.RejectedAt = DateTime.UtcNow;
            assignment.RejectionReason = reason;
            assignment.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> SubmitReviewAsync(int assignmentId, int reviewerId, SubmitReviewViewModel model)
        {
            var assignment = await _context.ReviewAssignments
                .Include(ra => ra.Submission)
                    .ThenInclude(s => s.Conference)
                .Include(ra => ra.Review)
                    .ThenInclude(r => r.ReviewScores)
                .FirstOrDefaultAsync(ra => ra.Id == assignmentId && ra.ReviewerId == reviewerId);

            if (assignment == null)
                return false;

            // Check if assignment is accepted
            if (assignment.Status != ReviewAssignmentStatus.Accepted)
                return false;

            // Get review criteria
            var criteria = await _context.ReviewCriterias
                .Where(rc => rc.ConferenceId == assignment.Submission.ConferenceId && rc.IsActive)
                .ToListAsync();

            // Validate all criteria are scored
            foreach (var criterion in criteria)
            {
                if (!model.Scores.ContainsKey(criterion.Name) || model.Scores[criterion.Name] < 1 || model.Scores[criterion.Name] > criterion.MaxScore)
                {
                    return false;
                }
            }

            // Calculate average score
            var averageScore = model.Scores.Values.Average();

            // Create or update review
            Review review;
            bool isNewReview = false;
            if (assignment.Review != null)
            {
                review = assignment.Review;
                review.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                review = new Review
                {
                    ReviewAssignmentId = assignmentId,
                    SubmissionId = assignment.SubmissionId,
                    ReviewerId = reviewerId,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Reviews.Add(review);
                isNewReview = true;
            }

            review.AverageScore = (decimal)averageScore;
            review.CommentsForAuthor = model.CommentsForAuthor;
            review.CommentsForAdmin = model.CommentsForAdmin;
            review.Recommendation = model.Recommendation;
            review.SubmittedAt = DateTime.UtcNow;

            // If new review, save first to get ReviewId
            if (isNewReview)
            {
                await _context.SaveChangesAsync();
            }

            // Update or create review scores
            if (review.ReviewScores.Any())
            {
                _context.ReviewScores.RemoveRange(review.ReviewScores);
            }

            var reviewScores = new List<ReviewScore>();
            foreach (var score in model.Scores)
            {
                reviewScores.Add(new ReviewScore
                {
                    ReviewId = review.Id,
                    CriteriaName = score.Key,
                    Score = score.Value,
                    CreatedAt = DateTime.UtcNow
                });
            }
            _context.ReviewScores.AddRange(reviewScores);

            // Update assignment status
            assignment.Status = ReviewAssignmentStatus.Completed;
            assignment.CompletedAt = DateTime.UtcNow;
            assignment.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            
            // Send email notifications to admin and author
            try
            {
                // Get submission with author
                var submissionWithAuthor = await _context.Submissions
                    .Include(s => s.Author)
                    .FirstOrDefaultAsync(s => s.Id == assignment.SubmissionId);

                if (submissionWithAuthor != null && submissionWithAuthor.Author != null)
                {
                    // Email to Author
                    var authorEmailNotification = new Models.Notification.EmailNotification
                    {
                        ToEmail = submissionWithAuthor.Author.Email ?? "",
                        Subject = $"Review Completed for Your Submission: {submissionWithAuthor.Title}",
                        Body = $"A review has been completed for your submission \"{submissionWithAuthor.Title}\". " +
                               $"Average Score: {review.AverageScore:F2}. " +
                               $"Recommendation: {review.Recommendation}. " +
                               $"Please check the submission details page for full review comments.",
                        Type = "ReviewCompleted",
                        Status = Models.Enums.EmailNotificationStatus.Pending,
                        RelatedSubmissionId = assignment.SubmissionId,
                        RelatedUserId = submissionWithAuthor.AuthorId,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.EmailNotifications.Add(authorEmailNotification);

                    // Email to Admin (get first admin email)
                    var admin = await _context.Users
                        .Where(u => u.Role == Models.Enums.UserRole.Admin && u.IsActive)
                        .FirstOrDefaultAsync();

                    Models.Notification.EmailNotification? adminEmailNotification = null;
                    if (admin != null && !string.IsNullOrEmpty(admin.Email))
                    {
                        adminEmailNotification = new Models.Notification.EmailNotification
                        {
                            ToEmail = admin.Email,
                            Subject = $"Review Completed: {submissionWithAuthor.Title}",
                            Body = $"Reviewer {review.Reviewer?.FullName ?? "N/A"} has completed the review for submission \"{submissionWithAuthor.Title}\". " +
                                   $"Average Score: {review.AverageScore:F2}. " +
                                   $"Recommendation: {review.Recommendation}.",
                            Type = "ReviewCompleted",
                            Status = Models.Enums.EmailNotificationStatus.Pending,
                            RelatedSubmissionId = assignment.SubmissionId,
                            RelatedUserId = admin.Id,
                            CreatedAt = DateTime.UtcNow
                        };
                        _context.EmailNotifications.Add(adminEmailNotification);
                    }

                    await _context.SaveChangesAsync();

                    // Create in-app notifications
                    try
                    {
                        // Notification for author
                        var authorNotification = new Models.Notification.UserNotification
                        {
                            UserId = submissionWithAuthor.AuthorId,
                            Type = Models.Enums.NotificationType.ReviewCompleted,
                            Title = "Review Completed",
                            Message = $"A review has been completed for your submission \"{submissionWithAuthor.Title}\". Average Score: {review.AverageScore:F2}. Recommendation: {review.Recommendation}.",
                            Status = Models.Enums.NotificationStatus.Unread,
                            RelatedSubmissionId = assignment.SubmissionId,
                            CreatedAt = DateTime.UtcNow
                        };
                        await _notificationService.CreateNotificationAsync(authorNotification);

                        // Notification for admin
                        if (admin != null)
                        {
                            var adminNotification = new Models.Notification.UserNotification
                            {
                                UserId = admin.Id,
                                Type = Models.Enums.NotificationType.ReviewCompleted,
                                Title = "Review Completed",
                                Message = $"Reviewer {review.Reviewer?.FullName ?? "N/A"} has completed the review for submission \"{submissionWithAuthor.Title}\".",
                                Status = Models.Enums.NotificationStatus.Unread,
                                RelatedSubmissionId = assignment.SubmissionId,
                                CreatedAt = DateTime.UtcNow
                            };
                            await _notificationService.CreateNotificationAsync(adminNotification);
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error creating review completion notifications: {ex.Message}");
                    }

                    // Send emails
                    if (!string.IsNullOrEmpty(submissionWithAuthor.Author.Email))
                    {
                        await _emailService.SendEmailNotificationAsync(authorEmailNotification);
                    }
                    if (adminEmailNotification != null)
                    {
                        await _emailService.SendEmailNotificationAsync(adminEmailNotification);
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error but don't fail the review submission
                System.Diagnostics.Debug.WriteLine($"Error sending review completion emails: {ex.Message}");
            }
            
            return true;
        }
    }
}

