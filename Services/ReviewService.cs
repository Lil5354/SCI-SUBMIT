using Microsoft.EntityFrameworkCore;
using SciSubmit.Data;
using SciSubmit.Models.Review;
using SciSubmit.Models.Enums;

namespace SciSubmit.Services
{
    public class ReviewService : IReviewService
    {
        private readonly ApplicationDbContext _context;

        public ReviewService(ApplicationDbContext context)
        {
            _context = context;
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
            return true;
        }
    }
}

