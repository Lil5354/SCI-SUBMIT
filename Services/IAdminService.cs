using SciSubmit.Models.Admin;
using SciSubmit.Models.Enums;

namespace SciSubmit.Services
{
    public interface IAdminService
    {
        Task<DashboardStatsViewModel> GetDashboardStatsAsync();
        Task<List<DeadlineViewModel>> GetUpcomingDeadlinesAsync();
        Task<PagedList<SubmissionViewModel>> GetSubmissionsAsync(SubmissionFilterViewModel filter);
        Task<SubmissionDetailsViewModel?> GetSubmissionDetailsAsync(int id);
        Task<bool> ApproveAbstractAsync(int submissionId, int adminId);
        Task<bool> RejectAbstractAsync(int submissionId, int adminId, string reason);
        Task<PagedList<ReviewAssignmentViewModel>> GetReviewAssignmentsAsync(AssignmentFilterViewModel filter);
        Task<List<ReviewerViewModel>> GetAvailableReviewersAsync(int submissionId);
        Task<bool> AssignReviewerAsync(int submissionId, int reviewerId, DateTime deadline, int adminId);
        Task<ConferenceConfigViewModel?> GetConferenceConfigAsync();
        Task<bool> UpdateConferenceAsync(ConferenceConfigViewModel model);
        Task<bool> UpdateConferencePlanAsync(ConferencePlanViewModel model);
        Task<ReportStatisticsViewModel> GetReportStatisticsAsync();
        Task<Dictionary<string, int>> GetSubmissionsByTopicAsync();
        Task<Dictionary<string, int>> GetSubmissionsByStatusAsync();
        Task<ReviewStatisticsViewModel> GetReviewStatisticsAsync();
        Task<PagedList<UserViewModel>> GetUsersAsync(UserFilterViewModel filter);
        Task<bool> CreateUserAsync(CreateUserViewModel model);
        Task<bool> UpdateUserRoleAsync(int userId, string newRole);
        Task<bool> DeactivateUserAsync(int userId);
        
        // Final Decision
        Task<FinalDecisionViewModel?> GetSubmissionForDecisionAsync(int submissionId);
        Task<bool> MakeFinalDecisionAsync(int submissionId, FinalDecisionType decision, string? reason, int adminId);
        
        // Topics Management
        Task<List<TopicViewModel>> GetTopicsAsync();
        Task<bool> CreateTopicAsync(TopicViewModel model);
        Task<bool> UpdateTopicAsync(int id, TopicViewModel model);
        Task<bool> DeleteTopicAsync(int id);
        
        // Keywords Management
        Task<PagedList<KeywordViewModel>> GetKeywordsAsync(KeywordFilterViewModel filter);
        Task<bool> CreateKeywordAsync(string name, int createdBy);
        Task<bool> ApproveKeywordAsync(int keywordId, int adminId);
        Task<bool> RejectKeywordAsync(int keywordId, int adminId);
        Task<bool> DeleteKeywordAsync(int keywordId);
        
        // Settings
        Task<SettingsViewModel> GetSettingsAsync();
        Task<bool> UpdateSettingsAsync(SettingsViewModel model);
    }
}

