using Microsoft.EntityFrameworkCore;
using SciSubmit.Data;
using SciSubmit.Models.Submission;
using SciSubmit.Models.Enums;
using SciSubmit.Models.Notification;
using SciSubmit.Models.Content;
using Microsoft.Extensions.Logging;

namespace SciSubmit.Services
{
    public interface ISubmissionService
    {
        Task<Submission?> SaveDraftAsync(AbstractSubmissionViewModel model, int authorId, int conferenceId, string? fileUrl = null);
        Task<Submission?> SubmitAbstractAsync(AbstractSubmissionViewModel model, int authorId, int conferenceId, string? fileUrl);
        Task<AbstractSubmissionViewModel?> GetDraftAsync(int submissionId, int authorId);
        Task<List<Topic>> GetActiveTopicsAsync(int conferenceId);
        Task<List<Keyword>> GetOrCreateKeywordsAsync(List<string> keywordNames, int conferenceId);
        Task<bool> WithdrawSubmissionAsync(int submissionId, int authorId, string? reason = null);
        Task<bool> SubmitFullPaperAsync(int submissionId, int authorId, string fileUrl, string fileName, long fileSize);
    }

    public class SubmissionService : ISubmissionService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SubmissionService> _logger;
        private readonly IEmailService _emailService;
        private readonly INotificationService _notificationService;

        public SubmissionService(
            ApplicationDbContext context, 
            ILogger<SubmissionService> logger,
            IEmailService emailService,
            INotificationService notificationService)
        {
            _context = context;
            _logger = logger;
            _emailService = emailService;
            _notificationService = notificationService;
        }

        public async Task<Submission?> SaveDraftAsync(AbstractSubmissionViewModel model, int authorId, int conferenceId, string? fileUrl = null)
        {
            try
            {
                _logger.LogInformation("=== SAVE DRAFT START ===");
                _logger.LogInformation($"AuthorId: {authorId}, ConferenceId: {conferenceId}");
                _logger.LogInformation($"Title: {model.Title?.Substring(0, Math.Min(50, model.Title?.Length ?? 0))}...");

                Submission submission;

                if (model.Id.HasValue)
                {
                    // Update existing draft
                    submission = await _context.Submissions
                        .Include(s => s.SubmissionAuthors)
                        .Include(s => s.SubmissionKeywords)
                        .Include(s => s.SubmissionTopics)
                        .FirstOrDefaultAsync(s => s.Id == model.Id.Value && s.AuthorId == authorId);

                    if (submission == null)
                    {
                        _logger.LogWarning($"Submission {model.Id.Value} not found or not owned by author {authorId}");
                        return null;
                    }

                    // Check if already submitted (cannot edit after submission)
                    if (submission.Status != SubmissionStatus.Draft)
                    {
                        _logger.LogWarning($"Submission {submission.Id} is not in Draft status, cannot edit");
                        return null;
                    }

                    _logger.LogInformation($"Updating existing submission {submission.Id}");
                }
                else
                {
                    // Create new draft
                    submission = new Submission
                    {
                        ConferenceId = conferenceId,
                        AuthorId = authorId,
                        Status = SubmissionStatus.Draft,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.Submissions.Add(submission);
                    _logger.LogInformation("Creating new submission draft");
                }

                // Update submission data
                submission.Title = model.Title;
                submission.Abstract = model.Abstract;
                submission.UpdatedAt = DateTime.UtcNow;
                submission.LastSavedAt = DateTime.UtcNow;

                // Update file URL if provided
                if (!string.IsNullOrEmpty(fileUrl))
                {
                    submission.AbstractFileUrl = fileUrl;
                    _logger.LogInformation($"Set AbstractFileUrl: {fileUrl}");
                }

                // Remove existing authors
                _context.SubmissionAuthors.RemoveRange(submission.SubmissionAuthors);
                _logger.LogInformation($"Removed {submission.SubmissionAuthors.Count} existing authors");

                // Add new authors
                for (int i = 0; i < model.Authors.Count; i++)
                {
                    var author = model.Authors[i];
                    submission.SubmissionAuthors.Add(new SubmissionAuthor
                    {
                        FullName = author.FullName,
                        Email = author.Email,
                        Affiliation = author.Affiliation,
                        IsCorrespondingAuthor = author.IsCorrespondingAuthor,
                        OrderIndex = i,
                        CreatedAt = DateTime.UtcNow
                    });
                }
                _logger.LogInformation($"Added {model.Authors.Count} authors");

                // Remove existing keywords
                _context.SubmissionKeywords.RemoveRange(submission.SubmissionKeywords);
                _logger.LogInformation($"Removed {submission.SubmissionKeywords.Count} existing keywords");

                // Get or create keywords (all will be linked to the same conferenceId)
                var keywords = await GetOrCreateKeywordsAsync(model.Keywords, conferenceId);
                foreach (var keyword in keywords)
                {
                    // Double-check keyword belongs to the same conference
                    if (keyword.ConferenceId != conferenceId)
                    {
                        _logger.LogWarning($"Keyword {keyword.Id} ({keyword.Name}) belongs to Conference {keyword.ConferenceId}, but submission is for Conference {conferenceId}");
                        continue; // Skip this keyword
                    }
                    submission.SubmissionKeywords.Add(new SubmissionKeyword
                    {
                        KeywordId = keyword.Id,
                        CreatedAt = DateTime.UtcNow
                    });
                }
                _logger.LogInformation($"Added {keywords.Count} keywords for Conference {conferenceId}");

                // Remove existing topics
                _context.SubmissionTopics.RemoveRange(submission.SubmissionTopics);
                _logger.LogInformation($"Removed {submission.SubmissionTopics.Count} existing topics");

                // Validate and add topic
                var topic = await _context.Topics.FirstOrDefaultAsync(t => t.Id == model.TopicId && t.ConferenceId == conferenceId);
                if (topic == null)
                {
                    _logger.LogWarning($"Topic {model.TopicId} does not belong to Conference {conferenceId}");
                    throw new InvalidOperationException($"Topic {model.TopicId} does not belong to the active conference");
                }
                submission.SubmissionTopics.Add(new SubmissionTopic
                {
                    TopicId = model.TopicId,
                    CreatedAt = DateTime.UtcNow
                });
                _logger.LogInformation($"Added topic {model.TopicId} ({topic.Name}) for Conference {conferenceId}");

                await _context.SaveChangesAsync();
                _logger.LogInformation($"=== SAVE DRAFT SUCCESS - SubmissionId: {submission.Id} ===");
                
                return submission;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "=== SAVE DRAFT ERROR ===");
                _logger.LogError($"Error: {ex.Message}");
                _logger.LogError($"Stack: {ex.StackTrace}");
                throw;
            }
        }

        public async Task<Submission?> SubmitAbstractAsync(AbstractSubmissionViewModel model, int authorId, int conferenceId, string? fileUrl)
        {
            try
            {
                _logger.LogInformation("=== SUBMIT ABSTRACT START ===");
                _logger.LogInformation($"AuthorId: {authorId}, ConferenceId: {conferenceId}");

                Submission submission;

                if (model.Id.HasValue)
                {
                    // Update existing draft and submit
                    submission = await _context.Submissions
                        .Include(s => s.SubmissionAuthors)
                        .Include(s => s.SubmissionKeywords)
                        .Include(s => s.SubmissionTopics)
                        .FirstOrDefaultAsync(s => s.Id == model.Id.Value && s.AuthorId == authorId);

                    if (submission == null)
                    {
                        _logger.LogWarning($"Submission {model.Id.Value} not found or not owned by author {authorId}");
                        return null;
                    }

                    if (submission.Status != SubmissionStatus.Draft)
                    {
                        _logger.LogWarning($"Submission {submission.Id} is not in Draft status, cannot submit");
                        return null;
                    }

                    _logger.LogInformation($"Submitting existing submission {submission.Id}");
                }
                else
                {
                    // Create new and submit
                    submission = new Submission
                    {
                        ConferenceId = conferenceId,
                        AuthorId = authorId,
                        Status = SubmissionStatus.PendingAbstractReview,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.Submissions.Add(submission);
                    _logger.LogInformation("Creating and submitting new submission");
                }

                // Update submission data
                submission.Title = model.Title;
                submission.Abstract = model.Abstract;
                submission.Status = SubmissionStatus.PendingAbstractReview;
                submission.AbstractSubmittedAt = DateTime.UtcNow;
                submission.UpdatedAt = DateTime.UtcNow;
                submission.LastSavedAt = DateTime.UtcNow; // Also update LastSavedAt

                if (!string.IsNullOrEmpty(fileUrl))
                {
                    submission.AbstractFileUrl = fileUrl;
                    _logger.LogInformation($"File URL set: {fileUrl}");
                }

                // Remove existing authors
                _context.SubmissionAuthors.RemoveRange(submission.SubmissionAuthors);

                // Add new authors
                for (int i = 0; i < model.Authors.Count; i++)
                {
                    var author = model.Authors[i];
                    submission.SubmissionAuthors.Add(new SubmissionAuthor
                    {
                        FullName = author.FullName,
                        Email = author.Email,
                        Affiliation = author.Affiliation,
                        IsCorrespondingAuthor = author.IsCorrespondingAuthor,
                        OrderIndex = i,
                        CreatedAt = DateTime.UtcNow
                    });
                }
                _logger.LogInformation($"Added {model.Authors.Count} authors");

                // Remove existing keywords
                _context.SubmissionKeywords.RemoveRange(submission.SubmissionKeywords);

                // Get or create keywords (all will be linked to the same conferenceId)
                var keywords = await GetOrCreateKeywordsAsync(model.Keywords, conferenceId);
                foreach (var keyword in keywords)
                {
                    // Double-check keyword belongs to the same conference
                    if (keyword.ConferenceId != conferenceId)
                    {
                        _logger.LogWarning($"Keyword {keyword.Id} ({keyword.Name}) belongs to Conference {keyword.ConferenceId}, but submission is for Conference {conferenceId}");
                        continue; // Skip this keyword
                    }
                    submission.SubmissionKeywords.Add(new SubmissionKeyword
                    {
                        KeywordId = keyword.Id,
                        CreatedAt = DateTime.UtcNow
                    });
                }
                _logger.LogInformation($"Added {keywords.Count} keywords for Conference {conferenceId}");

                // Remove existing topics
                _context.SubmissionTopics.RemoveRange(submission.SubmissionTopics);

                // Validate and add topic
                var topic = await _context.Topics.FirstOrDefaultAsync(t => t.Id == model.TopicId && t.ConferenceId == conferenceId);
                if (topic == null)
                {
                    _logger.LogWarning($"Topic {model.TopicId} does not belong to Conference {conferenceId}");
                    throw new InvalidOperationException($"Topic {model.TopicId} does not belong to the active conference");
                }
                submission.SubmissionTopics.Add(new SubmissionTopic
                {
                    TopicId = model.TopicId,
                    CreatedAt = DateTime.UtcNow
                });
                _logger.LogInformation($"Added topic {model.TopicId} ({topic.Name}) for Conference {conferenceId}");

                await _context.SaveChangesAsync();

                // Create and send email notification
                Console.WriteLine($"[EMAIL DEBUG] Getting author user with ID: {authorId}");
                var authorUser = await _context.Users.FindAsync(authorId);
                if (authorUser == null)
                {
                    Console.WriteLine($"[EMAIL ERROR] Author user with ID {authorId} not found in database!");
                    _logger.LogError($"Author user with ID {authorId} not found in database. Cannot send email notification.");
                }
                else if (string.IsNullOrWhiteSpace(authorUser.Email))
                {
                    Console.WriteLine($"[EMAIL ERROR] Author {authorUser.FullName} (ID: {authorId}) has no email address!");
                    _logger.LogError($"Author {authorUser.FullName} (ID: {authorId}) has no email address. Cannot send email notification.");
                }
                else
                {
                    Console.WriteLine($"[EMAIL DEBUG] Author found: {authorUser.FullName}, Email: {authorUser.Email}");
                    var emailNotification = new EmailNotification
                    {
                        ToEmail = authorUser.Email,
                        Subject = "Abstract Submission Confirmation",
                        Body = $"<p>Dear {authorUser.FullName},</p>" +
                               $"<p>We have received your abstract submission: <strong>\"{submission.Title}\"</strong>.</p>" +
                               $"<p>Current status: <strong>Pending Abstract Review</strong>.</p>" +
                               $"<p>We will notify you of the result as soon as possible.</p>" +
                               $"<p>Thank you for your submission.</p>" +
                               $"<p>Best regards,<br/>SciSubmit Team</p>",
                        Type = "AbstractSubmitted",
                        Status = EmailNotificationStatus.Pending,
                        RelatedSubmissionId = submission.Id,
                        RelatedUserId = authorId,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.EmailNotifications.Add(emailNotification);
                    await _context.SaveChangesAsync();
                    
                    // Create in-app notification
                    try
                    {
                        var userNotification = new UserNotification
                        {
                            UserId = authorId,
                            Type = NotificationType.AbstractSubmitted,
                            Title = "Abstract Submitted Successfully",
                            Message = $"Your abstract \"{submission.Title}\" has been submitted successfully and is now pending review.",
                            Status = NotificationStatus.Unread,
                            RelatedSubmissionId = submission.Id,
                            CreatedAt = DateTime.UtcNow
                        };
                        await _notificationService.CreateNotificationAsync(userNotification);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error creating user notification for submission {submission.Id}");
                        // Don't throw - notification failure shouldn't break submission
                    }
                    
                    // Send email immediately
                    try
                    {
                        var emailSent = await _emailService.SendEmailNotificationAsync(emailNotification);
                        if (emailSent)
                        {
                            _logger.LogInformation($"Email sent successfully to {authorUser.Email} for submission {submission.Id}");
                            Console.WriteLine($"[EMAIL SUCCESS] Email sent successfully to {authorUser.Email} for submission {submission.Id}");
                        }
                        else
                        {
                            _logger.LogWarning($"Failed to send email to {authorUser.Email} for submission {submission.Id}");
                            Console.WriteLine($"[EMAIL WARNING] Failed to send email to {authorUser.Email} for submission {submission.Id}");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error sending email to {authorUser.Email} for submission {submission.Id}");
                        Console.WriteLine($"[EMAIL ERROR] Error sending email to {authorUser.Email} for submission {submission.Id}: {ex.Message}");
                        Console.WriteLine($"[EMAIL ERROR] Stack Trace: {ex.StackTrace}");
                        if (ex.InnerException != null)
                        {
                            Console.WriteLine($"[EMAIL ERROR] Inner Exception: {ex.InnerException.Message}");
                        }
                        // Don't throw - email failure shouldn't break submission
                    }
                }
                
                // Log warning if email notification was skipped
                if (authorUser == null || string.IsNullOrWhiteSpace(authorUser?.Email))
                {
                    Console.WriteLine($"[EMAIL WARNING] Email notification skipped - Author user not found or has no email");
                }
                _logger.LogInformation($"=== SUBMIT ABSTRACT SUCCESS - SubmissionId: {submission.Id} ===");
                
                return submission;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "=== SUBMIT ABSTRACT ERROR ===");
                _logger.LogError($"Error: {ex.Message}");
                _logger.LogError($"Stack: {ex.StackTrace}");
                throw;
            }
        }

        public async Task<AbstractSubmissionViewModel?> GetDraftAsync(int submissionId, int authorId)
        {
            var submission = await _context.Submissions
                .Include(s => s.SubmissionAuthors)
                .Include(s => s.SubmissionKeywords)
                    .ThenInclude(sk => sk.Keyword)
                .Include(s => s.SubmissionTopics)
                    .ThenInclude(st => st.Topic)
                .FirstOrDefaultAsync(s => s.Id == submissionId && s.AuthorId == authorId && s.Status == SubmissionStatus.Draft);

            if (submission == null) return null;

            return new AbstractSubmissionViewModel
            {
                Id = submission.Id,
                Title = submission.Title,
                Abstract = submission.Abstract,
                TopicId = submission.SubmissionTopics.FirstOrDefault()?.TopicId ?? 0,
                Keywords = submission.SubmissionKeywords.Select(sk => sk.Keyword.Name).ToList(),
                Authors = submission.SubmissionAuthors.OrderBy(a => a.OrderIndex).Select(a => new Models.Submission.AuthorViewModel
                {
                    FullName = a.FullName,
                    Email = a.Email,
                    Affiliation = a.Affiliation,
                    IsCorrespondingAuthor = a.IsCorrespondingAuthor
                }).ToList()
            };
        }

        public async Task<List<Topic>> GetActiveTopicsAsync(int conferenceId)
        {
            // Load ALL topics for the conference (same as Admin), not just IsActive
            // This ensures Author sees the same topics as Admin
            return await _context.Topics
                .Where(t => t.ConferenceId == conferenceId)
                .OrderBy(t => t.OrderIndex)
                .ThenBy(t => t.Name)
                .ToListAsync();
        }

        public async Task<List<Keyword>> GetOrCreateKeywordsAsync(List<string> keywordNames, int conferenceId)
        {
            var keywords = new List<Keyword>();
            var activeConference = await _context.Conferences.FirstOrDefaultAsync(c => c.Id == conferenceId);

            foreach (var keywordName in keywordNames.Where(k => !string.IsNullOrWhiteSpace(k)))
            {
                var trimmedName = keywordName.Trim();
                var keyword = await _context.Keywords
                    .FirstOrDefaultAsync(k => k.Name.ToLower() == trimmedName.ToLower() && k.ConferenceId == conferenceId);

                if (keyword == null)
                {
                    keyword = new Keyword
                    {
                        Name = trimmedName,
                        ConferenceId = conferenceId,
                        Status = Models.Enums.KeywordStatus.Pending,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.Keywords.Add(keyword);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation($"Created new keyword: {trimmedName}");
                }

                keywords.Add(keyword);
            }

            return keywords;
        }

        public async Task<bool> WithdrawSubmissionAsync(int submissionId, int authorId, string? reason = null)
        {
            try
            {
                _logger.LogInformation($"=== WITHDRAW SUBMISSION START ===");
                _logger.LogInformation($"SubmissionId: {submissionId}, AuthorId: {authorId}");

                // Get submission with author check
                var submission = await _context.Submissions
                    .Include(s => s.Author)
                    .FirstOrDefaultAsync(s => s.Id == submissionId && s.AuthorId == authorId);

                if (submission == null)
                {
                    _logger.LogWarning($"Submission {submissionId} not found or not owned by author {authorId}");
                    return false;
                }

                // Check if submission can be withdrawn
                // Cannot withdraw if already withdrawn, accepted, or rejected
                if (submission.Status == Models.Enums.SubmissionStatus.Withdrawn)
                {
                    _logger.LogWarning($"Submission {submissionId} is already withdrawn");
                    return false;
                }

                if (submission.Status == Models.Enums.SubmissionStatus.Accepted)
                {
                    _logger.LogWarning($"Submission {submissionId} cannot be withdrawn because it is already accepted");
                    return false;
                }

                if (submission.Status == Models.Enums.SubmissionStatus.Rejected)
                {
                    _logger.LogWarning($"Submission {submissionId} cannot be withdrawn because it is already rejected");
                    return false;
                }

                // Update submission status
                submission.Status = Models.Enums.SubmissionStatus.Withdrawn;
                submission.UpdatedAt = DateTime.UtcNow;
                
                // Store withdrawal reason if provided
                if (!string.IsNullOrWhiteSpace(reason))
                {
                    // You might want to add a WithdrawalReason field to Submission model
                    // For now, we'll log it
                    _logger.LogInformation($"Withdrawal reason: {reason}");
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation($"Submission {submissionId} withdrawn successfully");

                // Send email notification to author
                if (submission.Author != null && !string.IsNullOrWhiteSpace(submission.Author.Email))
                {
                    Console.WriteLine($"[EMAIL DEBUG] Sending withdrawal confirmation to {submission.Author.Email}");
                    
                    var emailNotification = new EmailNotification
                    {
                        ToEmail = submission.Author.Email,
                        Subject = "Submission Withdrawn Confirmation",
                        Body = $"<p>Dear {submission.Author.FullName},</p>" +
                               $"<p>Your submission <strong>\"{submission.Title}\"</strong> has been successfully withdrawn.</p>" +
                               (!string.IsNullOrWhiteSpace(reason) 
                                   ? $"<p>Reason: {reason}</p>" 
                                   : "") +
                               $"<p>If you have any questions, please contact the conference organizers.</p>" +
                               $"<p>Best regards,<br/>SciSubmit Team</p>",
                        Type = "SubmissionWithdrawn",
                        Status = EmailNotificationStatus.Pending,
                        RelatedSubmissionId = submission.Id,
                        RelatedUserId = authorId,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.EmailNotifications.Add(emailNotification);
                    await _context.SaveChangesAsync();

                    // Create in-app notification
                    try
                    {
                        var userNotification = new UserNotification
                        {
                            UserId = authorId,
                            Type = NotificationType.SubmissionStatusChanged,
                            Title = "Submission Withdrawn",
                            Message = $"Your submission \"{submission.Title}\" has been withdrawn successfully.",
                            Status = NotificationStatus.Unread,
                            RelatedSubmissionId = submission.Id,
                            CreatedAt = DateTime.UtcNow
                        };
                        await _notificationService.CreateNotificationAsync(userNotification);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error creating user notification for withdrawn submission {submission.Id}");
                    }

                    // Send email immediately
                    try
                    {
                        var emailSent = await _emailService.SendEmailNotificationAsync(emailNotification);
                        if (emailSent)
                        {
                            _logger.LogInformation($"Withdrawal confirmation email sent successfully to {submission.Author.Email}");
                            Console.WriteLine($"[EMAIL SUCCESS] Withdrawal confirmation email sent to {submission.Author.Email}");
                        }
                        else
                        {
                            _logger.LogWarning($"Failed to send withdrawal confirmation email to {submission.Author.Email}");
                            Console.WriteLine($"[EMAIL WARNING] Failed to send withdrawal confirmation email to {submission.Author.Email}");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error sending withdrawal confirmation email to {submission.Author.Email}");
                        Console.WriteLine($"[EMAIL ERROR] Error sending withdrawal confirmation email: {ex.Message}");
                        // Don't throw - email failure shouldn't break withdrawal
                    }
                }

                _logger.LogInformation($"=== WITHDRAW SUBMISSION SUCCESS - SubmissionId: {submission.Id} ===");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"=== WITHDRAW SUBMISSION ERROR ===");
                _logger.LogError($"Error: {ex.Message}");
                _logger.LogError($"Stack: {ex.StackTrace}");
                throw;
            }
        }

        public async Task<bool> SubmitFullPaperAsync(int submissionId, int authorId, string fileUrl, string fileName, long fileSize)
        {
            try
            {
                _logger.LogInformation($"=== SUBMIT FULL PAPER START ===");
                _logger.LogInformation($"SubmissionId: {submissionId}, AuthorId: {authorId}");

                // Get submission
                var submission = await _context.Submissions
                    .Include(s => s.FullPaperVersions)
                    .FirstOrDefaultAsync(s => s.Id == submissionId && s.AuthorId == authorId);

                if (submission == null)
                {
                    _logger.LogWarning($"Submission {submissionId} not found or not owned by author {authorId}");
                    return false;
                }

                // Check if abstract is approved
                if (submission.Status != SubmissionStatus.AbstractApproved)
                {
                    _logger.LogWarning($"Submission {submissionId} is not in AbstractApproved status. Current status: {submission.Status}");
                    return false;
                }

                // Check if payment is completed
                var payment = await _context.Payments
                    .FirstOrDefaultAsync(p => p.SubmissionId == submissionId && p.Status == Models.Enums.PaymentStatus.Completed);

                if (payment == null)
                {
                    _logger.LogWarning($"Payment not completed for submission {submissionId}");
                    return false;
                }

                // Get next version number
                var maxVersion = submission.FullPaperVersions.Any() 
                    ? submission.FullPaperVersions.Max(v => v.VersionNumber) 
                    : 0;
                var nextVersion = maxVersion + 1;

                // Mark all previous versions as not current
                foreach (var version in submission.FullPaperVersions)
                {
                    version.IsCurrentVersion = false;
                }

                // Create new FullPaperVersion
                var fullPaperVersion = new FullPaperVersion
                {
                    SubmissionId = submissionId,
                    VersionNumber = nextVersion,
                    FileUrl = fileUrl,
                    FileName = fileName,
                    FileSize = fileSize,
                    UploadedBy = authorId,
                    UploadedAt = DateTime.UtcNow,
                    IsCurrentVersion = true
                };

                _context.FullPaperVersions.Add(fullPaperVersion);

                // Update submission status and timestamp
                // Note: Status remains AbstractApproved until admin assigns reviewers
                // When reviewers are assigned, status will change to UnderReview
                submission.FullPaperSubmittedAt = DateTime.UtcNow;
                submission.UpdatedAt = DateTime.UtcNow;
                // Keep status as AbstractApproved - admin will assign reviewers and status will change to UnderReview

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Full paper version {nextVersion} submitted successfully for submission {submissionId}");

                // Send email notification to author
                var authorUser = await _context.Users.FindAsync(authorId);
                if (authorUser != null && !string.IsNullOrWhiteSpace(authorUser.Email))
                {
                    var emailNotification = new EmailNotification
                    {
                        ToEmail = authorUser.Email,
                        Subject = "Full Paper Submission Confirmation",
                        Body = $"<p>Dear {authorUser.FullName},</p>" +
                               $"<p>We have received your full paper submission: <strong>\"{submission.Title}\"</strong>.</p>" +
                               $"<p>Version: <strong>{nextVersion}</strong></p>" +
                               $"<p>Current status: <strong>Full Paper Submitted</strong>.</p>" +
                               $"<p>Your paper will be reviewed by our reviewers. We will notify you of the result as soon as possible.</p>" +
                               $"<p>Thank you for your submission.</p>" +
                               $"<p>Best regards,<br/>SciSubmit Team</p>",
                        Type = "FullPaperSubmitted",
                        Status = EmailNotificationStatus.Pending,
                        RelatedSubmissionId = submissionId,
                        RelatedUserId = authorId,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.EmailNotifications.Add(emailNotification);
                    await _context.SaveChangesAsync();

                    // Create in-app notification
                    try
                    {
                        var userNotification = new UserNotification
                        {
                            UserId = authorId,
                            Type = NotificationType.FullPaperSubmitted,
                            Title = "Full Paper Submitted Successfully",
                            Message = $"Your full paper \"{submission.Title}\" (Version {nextVersion}) has been submitted successfully.",
                            Status = NotificationStatus.Unread,
                            RelatedSubmissionId = submissionId,
                            CreatedAt = DateTime.UtcNow
                        };
                        await _notificationService.CreateNotificationAsync(userNotification);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error creating user notification for full paper submission {submissionId}");
                    }

                    try
                    {
                        await _emailService.SendEmailNotificationAsync(emailNotification);
                        _logger.LogInformation($"Email sent successfully to {authorUser.Email} for full paper submission {submissionId}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error sending email to {authorUser.Email} for full paper submission {submissionId}");
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error submitting full paper for submission {submissionId}");
                throw;
            }
        }
    }
}

