using Microsoft.EntityFrameworkCore;
using SciSubmit.Data;
using SciSubmit.Models.Admin;
using SciSubmit.Models.Enums;
using SciSubmit.Models.Review;
using SciSubmit.Models.Content;
using SciSubmit.Models.Conference;
using SciSubmit.Models.Notification;
using SciSubmit.Models.Identity;
using System;

namespace SciSubmit.Services
{
    public class AdminService : IAdminService
    {
        private readonly ApplicationDbContext _context;

        public AdminService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<DashboardStatsViewModel> GetDashboardStatsAsync()
        {
            var stats = new DashboardStatsViewModel
            {
                // Tổng số bài nộp (tất cả status)
                TotalSubmissions = await _context.Submissions.CountAsync(),

                // Đã duyệt tóm tắt (status = AbstractApproved)
                ApprovedAbstracts = await _context.Submissions
                    .CountAsync(s => s.Status == SubmissionStatus.AbstractApproved),

                // Đang phản biện (status = UnderReview)
                UnderReview = await _context.Submissions
                    .CountAsync(s => s.Status == SubmissionStatus.UnderReview),

                // Tổng số đăng ký (tất cả users)
                TotalRegistrations = await _context.Users.CountAsync()
            };

            return stats;
        }

        public async Task<List<DeadlineViewModel>> GetUpcomingDeadlinesAsync()
        {
            var deadlines = new List<DeadlineViewModel>();

            // Lấy active conference
            var activeConference = await _context.Conferences
                .Where(c => c.IsActive)
                .FirstOrDefaultAsync();

            if (activeConference == null)
            {
                return deadlines;
            }

            // Lấy conference plan
            var plan = await _context.ConferencePlans
                .Where(p => p.ConferenceId == activeConference.Id)
                .OrderByDescending(p => p.CreatedAt)
                .FirstOrDefaultAsync();

            if (plan == null)
            {
                return deadlines;
            }

            var now = DateTime.UtcNow;

            // Deadline nộp tóm tắt
            if (plan.AbstractSubmissionDeadline > now)
            {
                var remainingDays = (int)(plan.AbstractSubmissionDeadline - now).TotalDays;
                deadlines.Add(new DeadlineViewModel
                {
                    Title = "Deadline nộp tóm tắt",
                    DeadlineDate = plan.AbstractSubmissionDeadline,
                    RemainingDays = remainingDays,
                    BadgeClass = remainingDays <= 7 ? "bg-danger" : remainingDays <= 14 ? "bg-warning" : "bg-info"
                });
            }

            // Deadline nộp Full-text
            if (plan.FullPaperSubmissionDeadline.HasValue && plan.FullPaperSubmissionDeadline.Value > now)
            {
                var remainingDays = (int)(plan.FullPaperSubmissionDeadline.Value - now).TotalDays;
                deadlines.Add(new DeadlineViewModel
                {
                    Title = "Deadline nộp Full-text",
                    DeadlineDate = plan.FullPaperSubmissionDeadline.Value,
                    RemainingDays = remainingDays,
                    BadgeClass = remainingDays <= 7 ? "bg-danger" : remainingDays <= 14 ? "bg-warning" : "bg-info"
                });
            }

            // Ngày công bố kết quả
            if (plan.ResultAnnouncementDate.HasValue && plan.ResultAnnouncementDate.Value > now)
            {
                var remainingDays = (int)(plan.ResultAnnouncementDate.Value - now).TotalDays;
                deadlines.Add(new DeadlineViewModel
                {
                    Title = "Ngày công bố kết quả",
                    DeadlineDate = plan.ResultAnnouncementDate.Value,
                    RemainingDays = remainingDays,
                    BadgeClass = remainingDays <= 7 ? "bg-danger" : remainingDays <= 14 ? "bg-warning" : "bg-info"
                });
            }

            // Sắp xếp theo deadline gần nhất
            return deadlines.OrderBy(d => d.DeadlineDate).ToList();
        }

        public async Task<PagedList<SubmissionViewModel>> GetSubmissionsAsync(SubmissionFilterViewModel filter)
        {
            var query = _context.Submissions
                .Include(s => s.Author)
                .Include(s => s.SubmissionTopics)
                    .ThenInclude(st => st.Topic)
                .Include(s => s.SubmissionKeywords)
                    .ThenInclude(sk => sk.Keyword)
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(filter.Status) && Enum.TryParse<SubmissionStatus>(filter.Status, out var status))
            {
                query = query.Where(s => s.Status == status);
            }

            if (filter.TopicId.HasValue)
            {
                query = query.Where(s => s.SubmissionTopics.Any(st => st.TopicId == filter.TopicId.Value));
            }

            if (filter.KeywordId.HasValue)
            {
                query = query.Where(s => s.SubmissionKeywords.Any(sk => sk.KeywordId == filter.KeywordId.Value));
            }

            if (filter.FromDate.HasValue)
            {
                query = query.Where(s => s.CreatedAt >= filter.FromDate.Value);
            }

            if (filter.ToDate.HasValue)
            {
                query = query.Where(s => s.CreatedAt <= filter.ToDate.Value);
            }

            if (!string.IsNullOrEmpty(filter.SearchTerm))
            {
                var searchTerm = filter.SearchTerm.ToLower();
                query = query.Where(s => s.Title.ToLower().Contains(searchTerm) ||
                                       s.Abstract.ToLower().Contains(searchTerm) ||
                                       (s.Author != null && s.Author.FullName.ToLower().Contains(searchTerm)));
            }

            // Get total count before pagination
            var totalCount = await query.CountAsync();

            // Apply pagination
            var submissions = await query
                .OrderByDescending(s => s.CreatedAt)
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            var viewModels = submissions.Select(s => new SubmissionViewModel
            {
                Id = s.Id,
                Title = s.Title,
                AuthorName = s.Author.FullName,
                Status = s.Status.ToString(),
                SubmittedAt = s.AbstractSubmittedAt,
                Topics = s.SubmissionTopics.Select(st => st.Topic.Name).ToList(),
                Keywords = s.SubmissionKeywords.Select(sk => sk.Keyword.Name).ToList()
            }).ToList();

            return new PagedList<SubmissionViewModel>
            {
                Items = viewModels,
                Page = filter.Page,
                PageSize = filter.PageSize,
                TotalCount = totalCount
            };
        }

        public async Task<SubmissionDetailsViewModel?> GetSubmissionDetailsAsync(int id)
        {
            var submission = await _context.Submissions
                .Include(s => s.Author)
                .Include(s => s.SubmissionAuthors)
                .Include(s => s.SubmissionTopics)
                    .ThenInclude(st => st.Topic)
                .Include(s => s.SubmissionKeywords)
                    .ThenInclude(sk => sk.Keyword)
                .Include(s => s.FullPaperVersions)
                .Include(s => s.ReviewAssignments)
                    .ThenInclude(ra => ra.Reviewer)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (submission == null)
            {
                return null;
            }

            if (submission.Author == null)
            {
                return null;
            }

            var viewModel = new SubmissionDetailsViewModel
            {
                Id = submission.Id,
                Title = submission.Title,
                Abstract = submission.Abstract,
                Status = submission.Status.ToString(),
                AuthorName = submission.Author.FullName,
                AuthorEmail = submission.Author.Email,
                AuthorAffiliation = submission.Author.Affiliation,
                AbstractSubmittedAt = submission.AbstractSubmittedAt,
                AbstractReviewedAt = submission.AbstractReviewedAt,
                AbstractRejectionReason = submission.AbstractRejectionReason,
                FullPaperSubmittedAt = submission.FullPaperSubmittedAt,
                AbstractFileUrl = submission.AbstractFileUrl,
                CoAuthors = submission.SubmissionAuthors
                    .OrderBy(sa => sa.OrderIndex)
                    .Select(sa => new AuthorInfoViewModel
                    {
                        FullName = sa.FullName,
                        Email = sa.Email,
                        Affiliation = sa.Affiliation,
                        IsCorrespondingAuthor = sa.IsCorrespondingAuthor
                    }).ToList(),
                Topics = submission.SubmissionTopics.Select(st => st.Topic.Name).ToList(),
                Keywords = submission.SubmissionKeywords.Select(sk => sk.Keyword.Name).ToList(),
                FullPaperVersions = submission.FullPaperVersions
                    .OrderByDescending(v => v.VersionNumber)
                    .Select(v => new FullPaperVersionViewModel
                    {
                        Id = v.Id,
                        VersionNumber = v.VersionNumber,
                        FileName = v.FileName,
                        FileUrl = v.FileUrl,
                        FileSize = v.FileSize,
                        UploadedAt = v.UploadedAt,
                        IsCurrentVersion = v.IsCurrentVersion
                    }).ToList(),
                CanApproveAbstract = submission.Status == SubmissionStatus.PendingAbstractReview,
                CanRejectAbstract = submission.Status == SubmissionStatus.PendingAbstractReview,
                CanAssignReviewer = submission.Status == SubmissionStatus.PendingAbstractReview || // Allow assigning reviewer for abstract review
                                   submission.Status == SubmissionStatus.AbstractApproved ||
                                   submission.Status == SubmissionStatus.FullPaperSubmitted ||
                                   submission.Status == SubmissionStatus.UnderReview
            };

            // Check if can make final decision (has completed reviews and status is UnderReview)
            var hasCompletedReviews = await _context.ReviewAssignments
                .AnyAsync(ra => ra.SubmissionId == id && 
                               ra.Status == Models.Enums.ReviewAssignmentStatus.Completed &&
                               ra.Review != null);
            
            viewModel.CanMakeFinalDecision = hasCompletedReviews && 
                                            submission.Status == SubmissionStatus.UnderReview;

            // Load review assignments
            viewModel.ReviewAssignments = submission.ReviewAssignments
                .OrderByDescending(ra => ra.InvitedAt)
                .Select(ra => new ReviewAssignmentInfoViewModel
                {
                    Id = ra.Id,
                    ReviewerName = ra.Reviewer != null ? ra.Reviewer.FullName : "N/A",
                    ReviewerEmail = ra.Reviewer != null ? ra.Reviewer.Email : "N/A",
                    Status = ra.Status.ToString(),
                    InvitedAt = ra.InvitedAt,
                    Deadline = ra.Deadline,
                    AcceptedAt = ra.AcceptedAt,
                    RejectedAt = ra.RejectedAt,
                    CompletedAt = ra.CompletedAt,
                    RejectionReason = ra.RejectionReason
                }).ToList();

            return viewModel;
        }

        public async Task<bool> ApproveAbstractAsync(int submissionId, int adminId)
        {
            var submission = await _context.Submissions
                .Include(s => s.Author)
                .FirstOrDefaultAsync(s => s.Id == submissionId);
            
            if (submission == null || submission.Status != SubmissionStatus.PendingAbstractReview)
            {
                return false;
            }

            if (submission.Author == null)
            {
                return false;
            }

            submission.Status = SubmissionStatus.AbstractApproved;
            submission.AbstractReviewedAt = DateTime.UtcNow;

            // Create email notification
            var emailNotification = new Models.Notification.EmailNotification
            {
                ToEmail = submission.Author.Email,
                Subject = "Tóm tắt của bạn đã được chấp nhận",
                Body = $"Tóm tắt \"{submission.Title}\" của bạn đã được chấp nhận.",
                Type = "AbstractApproved",
                Status = Models.Enums.EmailNotificationStatus.Pending,
                RelatedSubmissionId = submissionId,
                RelatedUserId = submission.AuthorId,
                CreatedAt = DateTime.UtcNow
            };

            _context.EmailNotifications.Add(emailNotification);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> RejectAbstractAsync(int submissionId, int adminId, string reason)
        {
            var submission = await _context.Submissions
                .Include(s => s.Author)
                .FirstOrDefaultAsync(s => s.Id == submissionId);
            
            if (submission == null || submission.Status != SubmissionStatus.PendingAbstractReview)
            {
                return false;
            }

            if (submission.Author == null)
            {
                return false;
            }

            submission.Status = SubmissionStatus.AbstractRejected;
            submission.AbstractRejectionReason = reason;
            submission.AbstractReviewedAt = DateTime.UtcNow;

            // Create email notification
            var emailNotification = new Models.Notification.EmailNotification
            {
                ToEmail = submission.Author.Email,
                Subject = "Tóm tắt của bạn đã bị từ chối",
                Body = $"Tóm tắt \"{submission.Title}\" của bạn đã bị từ chối. Lý do: {reason}",
                Type = "AbstractRejected",
                Status = Models.Enums.EmailNotificationStatus.Pending,
                RelatedSubmissionId = submissionId,
                RelatedUserId = submission.AuthorId,
                CreatedAt = DateTime.UtcNow
            };

            _context.EmailNotifications.Add(emailNotification);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<PagedList<ReviewAssignmentViewModel>> GetReviewAssignmentsAsync(AssignmentFilterViewModel filter)
        {
            var query = _context.ReviewAssignments
                .Include(ra => ra.Submission)
                .Include(ra => ra.Reviewer)
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(filter.Status) && Enum.TryParse<Models.Enums.ReviewAssignmentStatus>(filter.Status, out var status))
            {
                query = query.Where(ra => ra.Status == status);
            }

            if (filter.SubmissionId.HasValue)
            {
                query = query.Where(ra => ra.SubmissionId == filter.SubmissionId.Value);
            }

            if (filter.ReviewerId.HasValue)
            {
                query = query.Where(ra => ra.ReviewerId == filter.ReviewerId.Value);
            }

            if (filter.FromDate.HasValue)
            {
                query = query.Where(ra => ra.InvitedAt >= filter.FromDate.Value);
            }

            if (filter.ToDate.HasValue)
            {
                query = query.Where(ra => ra.InvitedAt <= filter.ToDate.Value);
            }

            // Get total count
            var totalCount = await query.CountAsync();

            // Apply pagination
            var assignments = await query
                .OrderByDescending(ra => ra.InvitedAt)
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            // Get submission IDs to load titles separately
            var submissionIds = assignments.Select(ra => ra.SubmissionId).Distinct().ToList();
            var submissions = await _context.Submissions
                .Where(s => submissionIds.Contains(s.Id))
                .ToDictionaryAsync(s => s.Id, s => s.Title);

            var reviewerIds = assignments.Select(ra => ra.ReviewerId).Distinct().ToList();
            var reviewers = await _context.Users
                .Where(u => reviewerIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => new { u.FullName, u.Email });

            var viewModels = assignments.Select(ra => new ReviewAssignmentViewModel
            {
                Id = ra.Id,
                SubmissionId = ra.SubmissionId,
                SubmissionTitle = submissions.TryGetValue(ra.SubmissionId, out var title) ? title : "N/A",
                ReviewerName = reviewers.TryGetValue(ra.ReviewerId, out var reviewer) ? reviewer.FullName : "N/A",
                ReviewerEmail = reviewers.TryGetValue(ra.ReviewerId, out var reviewerEmail) ? reviewerEmail.Email : "N/A",
                Status = ra.Status.ToString(),
                InvitedAt = ra.InvitedAt,
                Deadline = ra.Deadline,
                AcceptedAt = ra.AcceptedAt,
                RejectedAt = ra.RejectedAt,
                CompletedAt = ra.CompletedAt,
                RejectionReason = ra.RejectionReason
            }).ToList();

            return new PagedList<ReviewAssignmentViewModel>
            {
                Items = viewModels,
                Page = filter.Page,
                PageSize = filter.PageSize,
                TotalCount = totalCount
            };
        }

        public async Task<List<ReviewerViewModel>> GetAvailableReviewersAsync(int submissionId)
        {
            // Get submission with keywords
            var submission = await _context.Submissions
                .Include(s => s.SubmissionKeywords)
                    .ThenInclude(sk => sk.Keyword)
                .FirstOrDefaultAsync(s => s.Id == submissionId);

            if (submission == null)
            {
                return new List<ReviewerViewModel>();
            }

            // Get submission keyword IDs (can be empty if submission has no keywords)
            var submissionKeywordIds = submission.SubmissionKeywords
                .Where(sk => sk.Keyword != null)
                .Select(sk => sk.KeywordId)
                .ToList();

            // Get all active reviewers (users with Reviewer role and IsActive = true)
            var reviewers = await _context.Users
                .Where(u => u.Role == Models.Enums.UserRole.Reviewer && u.IsActive)
                .Include(u => u.UserKeywords)
                    .ThenInclude(uk => uk.Keyword)
                .ToListAsync();

            if (!reviewers.Any())
            {
                // No reviewers in system
                return new List<ReviewerViewModel>();
            }

            var reviewerViewModels = new List<ReviewerViewModel>();

            foreach (var reviewer in reviewers)
            {
                var reviewerKeywordIds = reviewer.UserKeywords
                    .Where(uk => uk.Keyword != null)
                    .Select(uk => uk.KeywordId)
                    .ToList();
                
                // Calculate match score based on keyword overlap
                int matchScore = 0;
                if (submissionKeywordIds.Any() && reviewerKeywordIds.Any())
                {
                    var matchingKeywords = reviewerKeywordIds.Intersect(submissionKeywordIds).Count();
                    var totalSubmissionKeywords = submissionKeywordIds.Count;
                    matchScore = totalSubmissionKeywords > 0 
                        ? (int)((matchingKeywords / (double)totalSubmissionKeywords) * 100) 
                        : 0;
                }
                // If submission has no keywords, match score is 0 (but reviewer is still available)

                // Count active assignments (Accepted but not Completed)
                var activeAssignments = await _context.ReviewAssignments
                    .CountAsync(ra => ra.ReviewerId == reviewer.Id && 
                                     ra.Status == Models.Enums.ReviewAssignmentStatus.Accepted &&
                                     ra.CompletedAt == null);

                reviewerViewModels.Add(new ReviewerViewModel
                {
                    Id = reviewer.Id,
                    FullName = reviewer.FullName ?? "N/A",
                    Email = reviewer.Email ?? "N/A",
                    Affiliation = reviewer.Affiliation,
                    Keywords = reviewer.UserKeywords
                        .Where(uk => uk.Keyword != null)
                        .Select(uk => uk.Keyword!.Name)
                        .ToList(),
                    MatchScore = matchScore,
                    ActiveAssignments = activeAssignments
                });
            }

            // Sort by match score descending, then by active assignments (fewer is better)
            return reviewerViewModels
                .OrderByDescending(r => r.MatchScore)
                .ThenBy(r => r.ActiveAssignments)
                .ToList();
        }

        public async Task<bool> AssignReviewerAsync(int submissionId, int reviewerId, DateTime deadline, int adminId)
        {
            // Validate deadline is in the future
            if (deadline <= DateTime.UtcNow)
            {
                return false; // Deadline must be in the future
            }

            // Check if submission exists
            var submission = await _context.Submissions.FindAsync(submissionId);
            if (submission == null)
            {
                return false; // Submission not found
            }

            // Check if submission is in valid status for assignment
            // Allow assignment for: PendingAbstractReview (abstract review), AbstractApproved, FullPaperSubmitted, UnderReview
            var validStatuses = new[] 
            { 
                Models.Enums.SubmissionStatus.PendingAbstractReview, // Allow reviewer assignment for abstract review
                Models.Enums.SubmissionStatus.AbstractApproved,
                Models.Enums.SubmissionStatus.FullPaperSubmitted,
                Models.Enums.SubmissionStatus.UnderReview
            };
            
            if (!validStatuses.Contains(submission.Status))
            {
                return false; // Submission status not valid for assignment
            }

            // Check if reviewer exists and is active
            var reviewer = await _context.Users.FindAsync(reviewerId);
            if (reviewer == null)
            {
                return false; // Reviewer not found
            }
            
            if (reviewer.Role != Models.Enums.UserRole.Reviewer)
            {
                return false; // User is not a reviewer
            }
            
            if (!reviewer.IsActive)
            {
                return false; // Reviewer is not active
            }

            // Check if assignment already exists
            var existingAssignment = await _context.ReviewAssignments
                .FirstOrDefaultAsync(ra => ra.SubmissionId == submissionId && ra.ReviewerId == reviewerId);
            
            if (existingAssignment != null)
            {
                return false; // Already assigned
            }

            // Create new assignment
            var assignment = new Models.Review.ReviewAssignment
            {
                SubmissionId = submissionId,
                ReviewerId = reviewerId,
                Status = Models.Enums.ReviewAssignmentStatus.Pending,
                InvitedAt = DateTime.UtcNow,
                InvitedBy = adminId,
                Deadline = deadline,
                CreatedAt = DateTime.UtcNow
            };

            _context.ReviewAssignments.Add(assignment);

            // Update submission status if needed
            // For PendingAbstractReview: Keep status as is (reviewer will review abstract)
            // For AbstractApproved or FullPaperSubmitted: Update to UnderReview
            if (submission.Status == Models.Enums.SubmissionStatus.AbstractApproved ||
                submission.Status == Models.Enums.SubmissionStatus.FullPaperSubmitted)
            {
                submission.Status = Models.Enums.SubmissionStatus.UnderReview;
            }
            // Note: If status is PendingAbstractReview, we keep it as is so admin can still see it needs abstract review

            // Create email notification
            var emailNotification = new Models.Notification.EmailNotification
            {
                ToEmail = reviewer.Email,
                Subject = $"Lời mời phản biện bài báo: {submission.Title}",
                Body = $"Bạn đã được mời phản biện bài báo \"{submission.Title}\". Deadline: {deadline:dd/MM/yyyy HH:mm}",
                Type = "ReviewInvitation",
                Status = Models.Enums.EmailNotificationStatus.Pending,
                RelatedSubmissionId = submissionId,
                RelatedUserId = reviewerId,
                CreatedAt = DateTime.UtcNow
            };

            _context.EmailNotifications.Add(emailNotification);
            
            try
            {
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                // Log error for debugging
                // In production, use proper logging
                System.Diagnostics.Debug.WriteLine($"Error assigning reviewer: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                return false;
            }
        }

        public async Task<ConferenceConfigViewModel?> GetConferenceConfigAsync()
        {
            var conference = await _context.Conferences
                .Where(c => c.IsActive)
                .FirstOrDefaultAsync();

            if (conference == null)
            {
                return null;
            }

            var plan = await _context.ConferencePlans
                .Where(p => p.ConferenceId == conference.Id)
                .OrderByDescending(p => p.CreatedAt)
                .FirstOrDefaultAsync();

            var viewModel = new ConferenceConfigViewModel
            {
                Id = conference.Id,
                Name = conference.Name,
                Description = conference.Description,
                Location = conference.Location,
                StartDate = conference.StartDate,
                EndDate = conference.EndDate,
                IsActive = conference.IsActive,
                Plan = plan != null ? new ConferencePlanViewModel
                {
                    Id = plan.Id,
                    ConferenceId = plan.ConferenceId,
                    // Convert UTC to local time for display in datetime-local input
                    // Only convert if not default DateTime (01/01/0001)
                    AbstractSubmissionOpenDate = plan.AbstractSubmissionOpenDate != default(DateTime)
                        ? (plan.AbstractSubmissionOpenDate.Kind == DateTimeKind.Utc 
                            ? plan.AbstractSubmissionOpenDate.ToLocalTime() 
                            : (plan.AbstractSubmissionOpenDate.Kind == DateTimeKind.Unspecified
                                ? DateTime.SpecifyKind(plan.AbstractSubmissionOpenDate, DateTimeKind.Utc).ToLocalTime()
                                : plan.AbstractSubmissionOpenDate))
                        : default(DateTime),
                    AbstractSubmissionDeadline = plan.AbstractSubmissionDeadline != default(DateTime)
                        ? (plan.AbstractSubmissionDeadline.Kind == DateTimeKind.Utc 
                            ? plan.AbstractSubmissionDeadline.ToLocalTime() 
                            : (plan.AbstractSubmissionDeadline.Kind == DateTimeKind.Unspecified
                                ? DateTime.SpecifyKind(plan.AbstractSubmissionDeadline, DateTimeKind.Utc).ToLocalTime()
                                : plan.AbstractSubmissionDeadline))
                        : default(DateTime),
                    FullPaperSubmissionOpenDate = plan.FullPaperSubmissionOpenDate.HasValue && plan.FullPaperSubmissionOpenDate.Value != default(DateTime)
                        ? (plan.FullPaperSubmissionOpenDate.Value.Kind == DateTimeKind.Utc 
                            ? plan.FullPaperSubmissionOpenDate.Value.ToLocalTime() 
                            : plan.FullPaperSubmissionOpenDate.Value)
                        : null,
                    FullPaperSubmissionDeadline = plan.FullPaperSubmissionDeadline.HasValue && plan.FullPaperSubmissionDeadline.Value != default(DateTime)
                        ? (plan.FullPaperSubmissionDeadline.Value.Kind == DateTimeKind.Utc 
                            ? plan.FullPaperSubmissionDeadline.Value.ToLocalTime() 
                            : plan.FullPaperSubmissionDeadline.Value)
                        : null,
                    ReviewDeadline = plan.ReviewDeadline.HasValue && plan.ReviewDeadline.Value != default(DateTime)
                        ? (plan.ReviewDeadline.Value.Kind == DateTimeKind.Utc 
                            ? plan.ReviewDeadline.Value.ToLocalTime() 
                            : plan.ReviewDeadline.Value)
                        : null,
                    ResultAnnouncementDate = plan.ResultAnnouncementDate.HasValue && plan.ResultAnnouncementDate.Value != default(DateTime)
                        ? (plan.ResultAnnouncementDate.Value.Kind == DateTimeKind.Utc 
                            ? plan.ResultAnnouncementDate.Value.ToLocalTime() 
                            : plan.ResultAnnouncementDate.Value)
                        : null,
                    ConferenceDate = plan.ConferenceDate.HasValue && plan.ConferenceDate.Value != default(DateTime)
                        ? (plan.ConferenceDate.Value.Kind == DateTimeKind.Utc 
                            ? plan.ConferenceDate.Value.ToLocalTime() 
                            : plan.ConferenceDate.Value)
                        : null
                } : null
            };

            return viewModel;
        }

        public async Task<bool> UpdateConferenceAsync(ConferenceConfigViewModel model)
        {
            var conference = await _context.Conferences.FindAsync(model.Id);
            if (conference == null)
            {
                return false;
            }

            conference.Name = model.Name;
            conference.Description = model.Description;
            conference.Location = model.Location;
            conference.StartDate = model.StartDate;
            conference.EndDate = model.EndDate;
            conference.IsActive = model.IsActive;
            conference.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateConferencePlanAsync(ConferencePlanViewModel model)
        {
            var activeConference = await _context.Conferences
                .Where(c => c.IsActive)
                .FirstOrDefaultAsync();

            if (activeConference == null)
            {
                return false;
            }

            // Use active conference ID instead of model.ConferenceId to avoid FK constraint issues
            var conferenceId = activeConference.Id;

            Models.Conference.ConferencePlan? plan = null;

            if (model.Id.HasValue)
            {
                // Update existing plan
                plan = await _context.ConferencePlans
                    .FirstOrDefaultAsync(p => p.Id == model.Id.Value && p.ConferenceId == conferenceId);
                if (plan == null)
                {
                    return false;
                }
                plan.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                // Check if plan already exists for this conference
                plan = await _context.ConferencePlans
                    .Where(p => p.ConferenceId == conferenceId)
                    .OrderByDescending(p => p.CreatedAt)
                    .FirstOrDefaultAsync();

                if (plan == null)
                {
                    // Create new plan
                    plan = new Models.Conference.ConferencePlan
                    {
                        ConferenceId = conferenceId,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.ConferencePlans.Add(plan);
                }
                else
                {
                    // Update existing plan
                    plan.UpdatedAt = DateTime.UtcNow;
                }
            }

            // Only update if not default DateTime
            if (model.AbstractSubmissionOpenDate != default(DateTime))
                plan.AbstractSubmissionOpenDate = model.AbstractSubmissionOpenDate;
            if (model.AbstractSubmissionDeadline != default(DateTime))
                plan.AbstractSubmissionDeadline = model.AbstractSubmissionDeadline;
            
            plan.FullPaperSubmissionOpenDate = model.FullPaperSubmissionOpenDate;
            plan.FullPaperSubmissionDeadline = model.FullPaperSubmissionDeadline;
            plan.ReviewDeadline = model.ReviewDeadline;
            plan.ResultAnnouncementDate = model.ResultAnnouncementDate;
            plan.ConferenceDate = model.ConferenceDate;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<ReportStatisticsViewModel> GetReportStatisticsAsync()
        {
            var stats = new ReportStatisticsViewModel
            {
                SubmissionsByStatus = await GetSubmissionsByStatusAsync(),
                SubmissionsByTopic = await GetSubmissionsByTopicAsync(),
                ReviewStats = await GetReviewStatisticsAsync()
            };

            // Calculate totals
            stats.TotalSubmissions = await _context.Submissions.CountAsync();
            stats.TotalAccepted = await _context.Submissions.CountAsync(s => s.Status == SubmissionStatus.Accepted);
            stats.TotalRejected = await _context.Submissions.CountAsync(s => s.Status == SubmissionStatus.Rejected);
            stats.TotalUnderReview = await _context.Submissions.CountAsync(s => s.Status == SubmissionStatus.UnderReview);

            // Calculate average review score
            var completedReviews = await _context.Reviews
                .Where(r => r.AverageScore.HasValue)
                .ToListAsync();
            
            if (completedReviews.Any())
            {
                stats.AverageReviewScore = (decimal)completedReviews.Average(r => r.AverageScore!.Value);
            }

            // Submissions by month
            var submissionsByMonth = await _context.Submissions
                .Where(s => s.CreatedAt >= DateTime.UtcNow.AddMonths(-12))
                .GroupBy(s => new { s.CreatedAt.Year, s.CreatedAt.Month })
                .Select(g => new { Key = $"{g.Key.Year}-{g.Key.Month:D2}", Count = g.Count() })
                .ToListAsync();

            stats.SubmissionsByMonth = submissionsByMonth.ToDictionary(x => x.Key, x => x.Count);

            return stats;
        }

        public async Task<Dictionary<string, int>> GetSubmissionsByTopicAsync()
        {
            var submissionsByTopic = await _context.SubmissionTopics
                .Include(st => st.Topic)
                .GroupBy(st => st.Topic.Name)
                .Select(g => new { Topic = g.Key, Count = g.Count() })
                .ToListAsync();

            return submissionsByTopic.ToDictionary(x => x.Topic, x => x.Count);
        }

        public async Task<Dictionary<string, int>> GetSubmissionsByStatusAsync()
        {
            var submissionsByStatus = await _context.Submissions
                .GroupBy(s => s.Status)
                .Select(g => new { Status = g.Key.ToString(), Count = g.Count() })
                .ToListAsync();

            return submissionsByStatus.ToDictionary(x => x.Status, x => x.Count);
        }

        public async Task<ReviewStatisticsViewModel> GetReviewStatisticsAsync()
        {
            var stats = new ReviewStatisticsViewModel
            {
                TotalAssignments = await _context.ReviewAssignments.CountAsync(),
                PendingAssignments = await _context.ReviewAssignments.CountAsync(ra => ra.Status == Models.Enums.ReviewAssignmentStatus.Pending),
                AcceptedAssignments = await _context.ReviewAssignments.CountAsync(ra => ra.Status == Models.Enums.ReviewAssignmentStatus.Accepted),
                CompletedAssignments = await _context.ReviewAssignments.CountAsync(ra => ra.Status == Models.Enums.ReviewAssignmentStatus.Completed),
                RejectedAssignments = await _context.ReviewAssignments.CountAsync(ra => ra.Status == Models.Enums.ReviewAssignmentStatus.Rejected)
            };

            // Assignments by status
            var assignmentsByStatus = await _context.ReviewAssignments
                .GroupBy(ra => ra.Status)
                .Select(g => new { Status = g.Key.ToString(), Count = g.Count() })
                .ToListAsync();

            stats.AssignmentsByStatus = assignmentsByStatus.ToDictionary(x => x.Status, x => x.Count);

            // Calculate average completion time
            var completedAssignments = await _context.ReviewAssignments
                .Where(ra => ra.Status == Models.Enums.ReviewAssignmentStatus.Completed && 
                            ra.CompletedAt.HasValue && 
                            ra.AcceptedAt.HasValue)
                .ToListAsync();

            if (completedAssignments.Any())
            {
                var totalDays = completedAssignments
                    .Sum(ra => (ra.CompletedAt!.Value - ra.AcceptedAt!.Value).TotalDays);
                stats.AverageCompletionTime = (decimal)(totalDays / completedAssignments.Count);
            }

            return stats;
        }

        public async Task<PagedList<UserViewModel>> GetUsersAsync(UserFilterViewModel filter)
        {
            var query = _context.Users.AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(filter.Role) && Enum.TryParse<Models.Enums.UserRole>(filter.Role, out var role))
            {
                query = query.Where(u => u.Role == role);
            }

            if (!string.IsNullOrEmpty(filter.SearchTerm))
            {
                var searchTerm = filter.SearchTerm.ToLower();
                query = query.Where(u => u.FullName.ToLower().Contains(searchTerm) ||
                                       u.Email.ToLower().Contains(searchTerm) ||
                                       (u.Affiliation != null && u.Affiliation.ToLower().Contains(searchTerm)));
            }

            if (filter.IsActive.HasValue)
            {
                query = query.Where(u => u.IsActive == filter.IsActive.Value);
            }

            // Get total count
            var totalCount = await query.CountAsync();

            // Apply pagination
            var users = await query
                .OrderByDescending(u => u.CreatedAt)
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            var viewModels = new List<UserViewModel>();
            foreach (var user in users)
            {
                var submissionCount = await _context.Submissions.CountAsync(s => s.AuthorId == user.Id);
                var reviewCount = await _context.ReviewAssignments.CountAsync(ra => ra.ReviewerId == user.Id);

                viewModels.Add(new UserViewModel
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Email = user.Email,
                    Affiliation = user.Affiliation,
                    Role = user.Role.ToString(),
                    IsActive = user.IsActive,
                    CreatedAt = user.CreatedAt,
                    SubmissionCount = submissionCount,
                    ReviewAssignmentCount = reviewCount
                });
            }

            return new PagedList<UserViewModel>
            {
                Items = viewModels,
                Page = filter.Page,
                PageSize = filter.PageSize,
                TotalCount = totalCount
            };
        }

        public async Task<bool> CreateUserAsync(CreateUserViewModel model)
        {
            // Check if email already exists
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == model.Email.ToLower());
            
            if (existingUser != null)
            {
                return false; // Email already exists
            }

            // Hash password
            string HashPassword(string password)
            {
                using (var sha256 = System.Security.Cryptography.SHA256.Create())
                {
                    var hashedBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password + "SciSubmitSalt"));
                    return Convert.ToBase64String(hashedBytes);
                }
            }

            var user = new Models.Identity.User
            {
                Email = model.Email,
                FullName = model.FullName,
                PhoneNumber = model.PhoneNumber,
                Affiliation = model.Affiliation,
                Role = model.Role,
                PasswordHash = HashPassword(model.Password),
                EmailConfirmed = model.EmailConfirmed,
                IsActive = model.IsActive,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> UpdateUserAsync(UpdateUserViewModel model)
        {
            var user = await _context.Users.FindAsync(model.Id);
            if (user == null)
            {
                return false;
            }

            // Check if email is being changed and if new email already exists
            if (user.Email.ToLower() != model.Email.ToLower())
            {
                var emailExists = await _context.Users
                    .AnyAsync(u => u.Email.ToLower() == model.Email.ToLower() && u.Id != model.Id);
                
                if (emailExists)
                {
                    return false; // Email already exists
                }
            }

            // Update user properties
            user.Email = model.Email;
            user.FullName = model.FullName;
            user.PhoneNumber = model.PhoneNumber;
            user.Affiliation = model.Affiliation;
            user.Role = model.Role;
            user.EmailConfirmed = model.EmailConfirmed;
            user.IsActive = model.IsActive;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateUserRoleAsync(int userId, string newRole)
        {
            if (!Enum.TryParse<Models.Enums.UserRole>(newRole, out var role))
            {
                return false;
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return false;
            }

            user.Role = role;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> ActivateUserAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return false;
            }

            user.IsActive = true;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> DeactivateUserAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return false;
            }

            user.IsActive = false;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return true;
        }

        // Final Decision Methods
        public async Task<FinalDecisionViewModel?> GetSubmissionForDecisionAsync(int submissionId)
        {
            var submission = await _context.Submissions
                .Include(s => s.SubmissionTopics)
                    .ThenInclude(st => st.Topic)
                .Include(s => s.ReviewAssignments)
                    .ThenInclude(ra => ra.Review)
                        .ThenInclude(r => r.Reviewer)
                .Include(s => s.ReviewAssignments)
                    .ThenInclude(ra => ra.Review)
                        .ThenInclude(r => r.ReviewScores)
                .FirstOrDefaultAsync(s => s.Id == submissionId);

            if (submission == null)
            {
                return null;
            }

            var completedReviews = submission.ReviewAssignments
                .Where(ra => ra.Review != null && ra.Status == ReviewAssignmentStatus.Completed)
                .Select(ra => ra.Review!)
                .ToList();

            var reviews = completedReviews.Select(r => new ReviewSummaryViewModel
            {
                ReviewId = r.Id,
                ReviewerName = r.Reviewer.FullName,
                Score = r.AverageScore ?? 0,
                Recommendation = r.Recommendation ?? "N/A",
                CommentsForAuthor = r.CommentsForAuthor,
                SubmittedAt = r.SubmittedAt
            }).ToList();

            var averageScore = completedReviews.Any() 
                ? completedReviews.Where(r => r.AverageScore.HasValue).Average(r => r.AverageScore!.Value)
                : 0;

            // Determine system recommendation
            string recommendation = "Reject";
            if (completedReviews.Count >= 2)
            {
                var acceptCount = completedReviews.Count(r => r.Recommendation?.ToLower() == "accept");
                var minorCount = completedReviews.Count(r => r.Recommendation?.ToLower().Contains("minor") == true);
                var majorCount = completedReviews.Count(r => r.Recommendation?.ToLower().Contains("major") == true);
                var rejectCount = completedReviews.Count(r => r.Recommendation?.ToLower() == "reject");

                if (averageScore >= 4.0m && acceptCount >= 2)
                {
                    recommendation = "Accept";
                }
                else if (averageScore >= 3.5m && (acceptCount >= 1 || minorCount >= 1))
                {
                    recommendation = "MinorRevision";
                }
                else if (averageScore >= 3.0m && (minorCount >= 1 || majorCount >= 1))
                {
                    recommendation = "MajorRevision";
                }
                else
                {
                    recommendation = "Reject";
                }
            }
            else if (completedReviews.Count == 1)
            {
                var firstReview = completedReviews.First();
                if (firstReview.AverageScore >= 4.0m && firstReview.Recommendation?.ToLower() == "accept")
                {
                    recommendation = "Accept";
                }
                else if (firstReview.AverageScore >= 3.5m)
                {
                    recommendation = "MinorRevision";
                }
                else if (firstReview.AverageScore >= 3.0m)
                {
                    recommendation = "MajorRevision";
                }
            }

            var viewModel = new FinalDecisionViewModel
            {
                SubmissionId = submission.Id,
                SubmissionTitle = submission.Title,
                Topics = submission.SubmissionTopics.Select(st => st.Topic.Name).ToList(),
                Reviews = reviews,
                AverageScore = averageScore,
                SystemRecommendation = recommendation,
                CanMakeDecision = completedReviews.Count >= 1 && submission.Status == SubmissionStatus.UnderReview,
                TotalReviews = submission.ReviewAssignments.Count,
                CompletedReviews = completedReviews.Count
            };

            return viewModel;
        }

        public async Task<bool> MakeFinalDecisionAsync(int submissionId, FinalDecisionType decision, string? reason, int adminId)
        {
            var submission = await _context.Submissions
                .Include(s => s.ReviewAssignments)
                    .ThenInclude(ra => ra.Review)
                .FirstOrDefaultAsync(s => s.Id == submissionId);

            if (submission == null)
            {
                return false;
            }

            // Check if all reviews are completed
            var completedReviews = submission.ReviewAssignments
                .Where(ra => ra.Review != null && ra.Status == ReviewAssignmentStatus.Completed)
                .Select(ra => ra.Review!)
                .ToList();

            if (!completedReviews.Any())
            {
                return false;
            }

            // Calculate average score
            var averageScore = completedReviews
                .Where(r => r.AverageScore.HasValue)
                .Select(r => r.AverageScore!.Value)
                .DefaultIfEmpty(0)
                .Average();

            // Check if decision already exists
            var existingDecision = await _context.FinalDecisions
                .FirstOrDefaultAsync(fd => fd.SubmissionId == submissionId);

            if (existingDecision != null)
            {
                // Update existing decision
                existingDecision.Decision = decision;
                existingDecision.DecisionReason = reason;
                existingDecision.AverageScore = averageScore;
                existingDecision.DecidedAt = DateTime.UtcNow;
                existingDecision.DecisionBy = adminId;
            }
            else
            {
                // Create new decision
                var finalDecision = new Models.Review.FinalDecision
                {
                    SubmissionId = submissionId,
                    Decision = decision,
                    DecisionBy = adminId,
                    DecisionReason = reason,
                    AverageScore = averageScore,
                    DecidedAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow
                };

                _context.FinalDecisions.Add(finalDecision);
            }

            // Update submission status
            submission.Status = decision switch
            {
                FinalDecisionType.Accepted => SubmissionStatus.Accepted,
                FinalDecisionType.MinorRevision => SubmissionStatus.RevisionRequired,
                FinalDecisionType.MajorRevision => SubmissionStatus.RevisionRequired,
                FinalDecisionType.Rejected => SubmissionStatus.Rejected,
                _ => submission.Status
            };

            // Load author to ensure it's available
            await _context.Entry(submission).Reference(s => s.Author).LoadAsync();
            
            if (submission.Author == null)
            {
                return false;
            }

            // Create email notification
            var emailNotification = new Models.Notification.EmailNotification
            {
                ToEmail = submission.Author.Email,
                Subject = $"Quyết định cuối cùng cho bài báo: {submission.Title}",
                Body = $"Quyết định: {decision}. Lý do: {reason ?? "Không có"}",
                Type = "FinalDecision",
                Status = EmailNotificationStatus.Pending,
                RelatedSubmissionId = submissionId,
                RelatedUserId = submission.AuthorId,
                CreatedAt = DateTime.UtcNow
            };

            _context.EmailNotifications.Add(emailNotification);
            await _context.SaveChangesAsync();

            return true;
        }

        // Topics Management Methods
        public async Task<List<TopicViewModel>> GetTopicsAsync()
        {
            var activeConference = await _context.Conferences
                .Where(c => c.IsActive)
                .FirstOrDefaultAsync();

            if (activeConference == null)
            {
                return new List<TopicViewModel>();
            }

            var topics = await _context.Topics
                .Where(t => t.ConferenceId == activeConference.Id)
                .OrderBy(t => t.OrderIndex)
                .ThenBy(t => t.Name)
                .ToListAsync();

            var viewModels = new List<TopicViewModel>();

            foreach (var topic in topics)
            {
                var submissionCount = await _context.SubmissionTopics
                    .CountAsync(st => st.TopicId == topic.Id);

                viewModels.Add(new TopicViewModel
                {
                    Id = topic.Id,
                    Name = topic.Name,
                    Description = topic.Description,
                    OrderIndex = topic.OrderIndex,
                    IsActive = topic.IsActive,
                    SubmissionCount = submissionCount
                });
            }

            return viewModels;
        }

        public async Task<bool> CreateTopicAsync(TopicViewModel model)
        {
            var activeConference = await _context.Conferences
                .Where(c => c.IsActive)
                .FirstOrDefaultAsync();

            if (activeConference == null)
            {
                return false;
            }

            // Check if topic name already exists in this conference (case-insensitive)
            var topics = await _context.Topics
                .Where(t => t.ConferenceId == activeConference.Id)
                .ToListAsync();
            var exists = topics.Any(t => t.Name.Trim().Equals(model.Name.Trim(), StringComparison.OrdinalIgnoreCase));

            if (exists)
            {
                return false;
            }

            // If OrderIndex is 0 or not specified, calculate the next index
            int orderIndex = model.OrderIndex;
            if (orderIndex <= 0)
            {
                var maxOrderIndex = await _context.Topics
                    .Where(t => t.ConferenceId == activeConference.Id)
                    .Select(t => (int?)t.OrderIndex)
                    .DefaultIfEmpty(0)
                    .MaxAsync() ?? 0;
                orderIndex = maxOrderIndex + 1;
            }

            var topic = new Models.Content.Topic
            {
                ConferenceId = activeConference.Id,
                Name = model.Name.Trim(),
                Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim(),
                OrderIndex = orderIndex,
                IsActive = model.IsActive,
                CreatedAt = DateTime.UtcNow
            };

            _context.Topics.Add(topic);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> UpdateTopicAsync(int id, TopicViewModel model)
        {
            var topic = await _context.Topics.FindAsync(id);
            if (topic == null)
            {
                return false;
            }

            // Check for duplicate name (excluding the current topic itself)
            var topics = await _context.Topics
                .Where(t => t.ConferenceId == topic.ConferenceId && t.Id != id)
                .ToListAsync();
            var exists = topics.Any(t => t.Name.Trim().Equals(model.Name.Trim(), StringComparison.OrdinalIgnoreCase));

            if (exists)
            {
                return false;
            }

            topic.Name = model.Name.Trim();
            topic.Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim();
            topic.OrderIndex = model.OrderIndex;
            topic.IsActive = model.IsActive;
            // Note: UpdatedAt field doesn't exist in Topic model, but we can add it if needed

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteTopicAsync(int id)
        {
            var topic = await _context.Topics.FindAsync(id);
            if (topic == null)
            {
                return false;
            }

            // Check if topic is used in submissions
            var isUsed = await _context.SubmissionTopics
                .AnyAsync(st => st.TopicId == id);

            if (isUsed)
            {
                // Soft delete
                topic.IsActive = false;
            }
            else
            {
                // Hard delete
                _context.Topics.Remove(topic);
            }

            await _context.SaveChangesAsync();
            return true;
        }

        // Keywords Management Methods
        public async Task<PagedList<KeywordViewModel>> GetKeywordsAsync(KeywordFilterViewModel filter)
        {
            var activeConference = await _context.Conferences
                .Where(c => c.IsActive)
                .FirstOrDefaultAsync();

            if (activeConference == null)
            {
                return new PagedList<KeywordViewModel>
                {
                    Items = new List<KeywordViewModel>(),
                    Page = filter.Page,
                    PageSize = filter.PageSize,
                    TotalCount = 0
                };
            }

            var query = _context.Keywords
                .Where(k => k.ConferenceId == activeConference.Id)
                .Include(k => k.Creator)
                .Include(k => k.Approver)
                .AsQueryable();

            // Apply filters
            if (filter.Status.HasValue)
            {
                query = query.Where(k => k.Status == filter.Status.Value);
            }

            if (!string.IsNullOrEmpty(filter.SearchTerm))
            {
                var searchTerm = filter.SearchTerm.ToLower();
                query = query.Where(k => k.Name.ToLower().Contains(searchTerm));
            }

            var totalCount = await query.CountAsync();

            var keywords = await query
                .OrderBy(k => k.Status)
                .ThenBy(k => k.Name)
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            var viewModels = keywords.Select(k => new KeywordViewModel
            {
                Id = k.Id,
                Name = k.Name,
                Status = k.Status,
                CreatedByName = k.Creator?.FullName,
                CreatedAt = k.CreatedAt,
                ApprovedByName = k.Approver?.FullName,
                ApprovedAt = k.ApprovedAt
            }).ToList();

            return new PagedList<KeywordViewModel>
            {
                Items = viewModels,
                Page = filter.Page,
                PageSize = filter.PageSize,
                TotalCount = totalCount
            };
        }

        public async Task<bool> CreateKeywordAsync(string name, int createdBy)
        {
            var activeConference = await _context.Conferences
                .Where(c => c.IsActive)
                .FirstOrDefaultAsync();

            if (activeConference == null)
            {
                return false;
            }

            // Check if keyword already exists
            var exists = await _context.Keywords
                .AnyAsync(k => k.ConferenceId == activeConference.Id && k.Name.ToLower() == name.ToLower());

            if (exists)
            {
                return false;
            }

            var keyword = new Models.Content.Keyword
            {
                ConferenceId = activeConference.Id,
                Name = name,
                Status = KeywordStatus.Pending,
                CreatedBy = createdBy,
                CreatedAt = DateTime.UtcNow
            };

            _context.Keywords.Add(keyword);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> ApproveKeywordAsync(int keywordId, int adminId)
        {
            var keyword = await _context.Keywords.FindAsync(keywordId);
            if (keyword == null)
            {
                return false;
            }

            keyword.Status = KeywordStatus.Approved;
            keyword.ApprovedBy = adminId;
            keyword.ApprovedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RejectKeywordAsync(int keywordId, int adminId)
        {
            var keyword = await _context.Keywords.FindAsync(keywordId);
            if (keyword == null)
            {
                return false;
            }

            // Check if keyword is used in submissions
            var isUsed = await _context.SubmissionKeywords
                .AnyAsync(sk => sk.KeywordId == keywordId);

            if (isUsed)
            {
                // Can't reject if used in submissions
                return false;
            }

            // Mark as rejected instead of deleting
            keyword.Status = KeywordStatus.Rejected;
            keyword.ApprovedBy = adminId;
            keyword.ApprovedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteKeywordAsync(int keywordId)
        {
            var keyword = await _context.Keywords.FindAsync(keywordId);
            if (keyword == null)
            {
                return false;
            }

            // Check if keyword is used in submissions
            var isUsed = await _context.SubmissionKeywords
                .AnyAsync(sk => sk.KeywordId == keywordId);

            if (isUsed)
            {
                return false; // Can't delete if used
            }

            _context.Keywords.Remove(keyword);
            await _context.SaveChangesAsync();
            return true;
        }

        // Settings Methods
        public async Task<SettingsViewModel> GetSettingsAsync()
        {
            var activeConference = await _context.Conferences
                .Where(c => c.IsActive)
                .FirstOrDefaultAsync();

            if (activeConference == null)
            {
                return new SettingsViewModel();
            }

            var settings = await _context.SystemSettings
                .Where(s => s.ConferenceId == activeConference.Id)
                .ToDictionaryAsync(s => s.Key, s => s.Value);

            var viewModel = new SettingsViewModel();

            // Load email settings
            if (settings.TryGetValue("SmtpServer", out var smtpServer))
                viewModel.SmtpServer = smtpServer ?? "smtp.gmail.com";
            if (settings.TryGetValue("SmtpPort", out var smtpPort) && int.TryParse(smtpPort, out var port))
                viewModel.SmtpPort = port;
            if (settings.TryGetValue("SmtpUsername", out var smtpUsername))
                viewModel.SmtpUsername = smtpUsername ?? string.Empty;
            if (settings.TryGetValue("SmtpPassword", out var smtpPassword))
                viewModel.SmtpPassword = smtpPassword ?? string.Empty;
            if (settings.TryGetValue("SmtpUseSsl", out var smtpUseSsl) && bool.TryParse(smtpUseSsl, out var useSsl))
                viewModel.SmtpUseSsl = useSsl;
            if (settings.TryGetValue("FromEmail", out var fromEmail))
                viewModel.FromEmail = fromEmail ?? string.Empty;
            if (settings.TryGetValue("FromName", out var fromName))
                viewModel.FromName = fromName ?? "SciSubmit";

            // Load system settings
            if (settings.TryGetValue("MaxFileSizeMB", out var maxFileSize) && int.TryParse(maxFileSize, out var fileSize))
                viewModel.MaxFileSizeMB = fileSize;
            if (settings.TryGetValue("MaxKeywordsPerSubmission", out var maxKeywords) && int.TryParse(maxKeywords, out var keywords))
                viewModel.MaxKeywordsPerSubmission = keywords;
            if (settings.TryGetValue("MaxAuthorsPerSubmission", out var maxAuthors) && int.TryParse(maxAuthors, out var authors))
                viewModel.MaxAuthorsPerSubmission = authors;
            if (settings.TryGetValue("ReviewDeadlineDays", out var reviewDeadline) && int.TryParse(reviewDeadline, out var days))
                viewModel.ReviewDeadlineDays = days;

            // Load notification settings
            if (settings.TryGetValue("EmailNotificationsEnabled", out var emailEnabled) && bool.TryParse(emailEnabled, out var emailEnabledValue))
                viewModel.EmailNotificationsEnabled = emailEnabledValue;
            if (settings.TryGetValue("AutoAssignReviewers", out var autoAssign) && bool.TryParse(autoAssign, out var autoAssignValue))
                viewModel.AutoAssignReviewers = autoAssignValue;

            return viewModel;
        }

        public async Task<bool> UpdateSettingsAsync(SettingsViewModel model)
        {
            var activeConference = await _context.Conferences
                .Where(c => c.IsActive)
                .FirstOrDefaultAsync();

            if (activeConference == null)
            {
                return false;
            }

            var settingsToUpdate = new Dictionary<string, string?>
            {
                { "SmtpServer", model.SmtpServer },
                { "SmtpPort", model.SmtpPort.ToString() },
                { "SmtpUsername", model.SmtpUsername },
                { "SmtpPassword", model.SmtpPassword },
                { "SmtpUseSsl", model.SmtpUseSsl.ToString() },
                { "FromEmail", model.FromEmail },
                { "FromName", model.FromName },
                { "MaxFileSizeMB", model.MaxFileSizeMB.ToString() },
                { "MaxKeywordsPerSubmission", model.MaxKeywordsPerSubmission.ToString() },
                { "MaxAuthorsPerSubmission", model.MaxAuthorsPerSubmission.ToString() },
                { "ReviewDeadlineDays", model.ReviewDeadlineDays.ToString() },
                { "EmailNotificationsEnabled", model.EmailNotificationsEnabled.ToString() },
                { "AutoAssignReviewers", model.AutoAssignReviewers.ToString() }
            };

            foreach (var kvp in settingsToUpdate)
            {
                var existingSetting = await _context.SystemSettings
                    .FirstOrDefaultAsync(s => s.ConferenceId == activeConference.Id && s.Key == kvp.Key);

                if (existingSetting != null)
                {
                    existingSetting.Value = kvp.Value;
                    existingSetting.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    var newSetting = new Models.Conference.SystemSetting
                    {
                        ConferenceId = activeConference.Id,
                        Key = kvp.Key,
                        Value = kvp.Value,
                        Description = GetSettingDescription(kvp.Key),
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.SystemSettings.Add(newSetting);
                }
            }

            await _context.SaveChangesAsync();
            return true;
        }

        private string GetSettingDescription(string key)
        {
            return key switch
            {
                "SmtpServer" => "SMTP Server address for sending emails",
                "SmtpPort" => "SMTP Server port number",
                "SmtpUsername" => "SMTP authentication username",
                "SmtpPassword" => "SMTP authentication password",
                "SmtpUseSsl" => "Whether to use SSL/TLS for SMTP connection",
                "FromEmail" => "Default sender email address",
                "FromName" => "Default sender name",
                "MaxFileSizeMB" => "Maximum file size in MB for uploads",
                "MaxKeywordsPerSubmission" => "Maximum number of keywords per submission",
                "MaxAuthorsPerSubmission" => "Maximum number of authors per submission",
                "ReviewDeadlineDays" => "Default number of days for review deadline",
                "EmailNotificationsEnabled" => "Enable or disable email notifications",
                "AutoAssignReviewers" => "Automatically assign reviewers based on keywords",
                _ => "System setting"
            };
        }
    }
}






