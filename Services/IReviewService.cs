using SciSubmit.Models.Review;

namespace SciSubmit.Services
{
    public interface IReviewService
    {
        Task<ReviewerDashboardViewModel> GetReviewerDashboardAsync(int reviewerId);
        Task<ReviewAssignmentItemViewModel?> GetReviewAssignmentAsync(int assignmentId, int reviewerId);
        Task<ReviewDetailsViewModel?> GetReviewDetailsAsync(int assignmentId, int reviewerId);
        Task<bool> SubmitReviewAsync(int assignmentId, int reviewerId, SubmitReviewViewModel model);
        Task<bool> AcceptInvitationAsync(int assignmentId, int reviewerId);
        Task<bool> RejectInvitationAsync(int assignmentId, int reviewerId, string? reason = null);
    }
}


