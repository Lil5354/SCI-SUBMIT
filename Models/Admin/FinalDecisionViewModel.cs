using SciSubmit.Models.Admin;

namespace SciSubmit.Models.Admin
{
    public class FinalDecisionViewModel
    {
        public int SubmissionId { get; set; }
        public string SubmissionTitle { get; set; } = string.Empty;
        public List<string> Topics { get; set; } = new List<string>();
        public List<ReviewSummaryViewModel> Reviews { get; set; } = new List<ReviewSummaryViewModel>();
        public decimal AverageScore { get; set; }
        public string SystemRecommendation { get; set; } = string.Empty; // "Accept", "MinorRevision", "MajorRevision", "Reject"
        public bool CanMakeDecision { get; set; }
        public int TotalReviews { get; set; }
        public int CompletedReviews { get; set; }
    }

    public class ReviewSummaryViewModel
    {
        public int ReviewId { get; set; }
        public string ReviewerName { get; set; } = string.Empty;
        public decimal Score { get; set; }
        public string Recommendation { get; set; } = string.Empty;
        public string? CommentsForAuthor { get; set; }
        public DateTime SubmittedAt { get; set; }
    }
}

