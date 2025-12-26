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
    }

    public class SubmissionService : ISubmissionService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SubmissionService> _logger;

        public SubmissionService(ApplicationDbContext context, ILogger<SubmissionService> logger)
        {
            _context = context;
            _logger = logger;
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

                // Create email notification
                var authorUser = await _context.Users.FindAsync(authorId);
                if (authorUser != null)
                {
                    var emailNotification = new EmailNotification
                    {
                        ToEmail = authorUser.Email,
                        Subject = "Xác nhận nhận được tóm tắt bài báo",
                        Body = $"Chúng tôi đã nhận được tóm tắt bài báo của bạn: \"{submission.Title}\". " +
                               $"Trạng thái hiện tại: Chờ duyệt tóm tắt. " +
                               $"Chúng tôi sẽ thông báo kết quả trong thời gian sớm nhất.",
                        Type = "AbstractSubmitted",
                        Status = EmailNotificationStatus.Pending,
                        RelatedSubmissionId = submission.Id,
                        RelatedUserId = authorId,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.EmailNotifications.Add(emailNotification);
                    _logger.LogInformation($"Email notification created for {authorUser.Email}");
                }

                await _context.SaveChangesAsync();
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
    }
}

