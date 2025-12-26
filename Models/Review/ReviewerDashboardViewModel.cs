namespace SciSubmit.Models.Review
{
    public class ReviewerDashboardViewModel
    {
        public int TotalAssignments { get; set; }
        public int PendingAssignments { get; set; }
        public int AcceptedAssignments { get; set; }
        public int CompletedAssignments { get; set; }
        public int UrgentAssignments { get; set; } // Sắp hết hạn (còn < 3 ngày)
        public List<ReviewAssignmentItemViewModel> PendingReviews { get; set; } = new();
        public List<ReviewAssignmentItemViewModel> CompletedReviews { get; set; } = new();
    }

    public class ReviewAssignmentItemViewModel
    {
        public int Id { get; set; }
        public int SubmissionId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Abstract { get; set; } = string.Empty;
        public List<string> Topics { get; set; } = new();
        public DateTime Deadline { get; set; }
        public DateTime InvitedAt { get; set; }
        public DateTime? AcceptedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string Status { get; set; } = string.Empty;
        public int DaysRemaining { get; set; }
        public bool IsUrgent { get; set; }
        public string? AbstractFileUrl { get; set; }
        public string? FullPaperFileUrl { get; set; }
    }
}


