using SciSubmit.Models.Enums;

namespace SciSubmit.Models.Submission
{
    public class AuthorDashboardViewModel
    {
        public int TotalSubmissions { get; set; }
        public int UnderReview { get; set; }
        public int Accepted { get; set; }
        public int Rejected { get; set; }
        public int CurrentProgressStep { get; set; } // 1: Abstract, 2: Full Paper, 3: Review, 4: Accepted
        public List<RecentSubmissionViewModel> RecentSubmissions { get; set; } = new();
        public RecentSubmissionViewModel? TrackedSubmission { get; set; } // Currently tracked submission
    }

    public class RecentSubmissionViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public SubmissionStatus Status { get; set; }
        public DateTime? SubmittedAt { get; set; }
        public DateTime? AbstractReviewedAt { get; set; }
        public DateTime? FullPaperSubmittedAt { get; set; }
    }
}

