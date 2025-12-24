namespace SciSubmit.Models.Review
{
    public class ReviewIndexViewModel
    {
        public List<ReviewAssignmentItemViewModel> Assignments { get; set; } = new List<ReviewAssignmentItemViewModel>();
    }

    public class ReviewAssignmentItemViewModel
    {
        public int Id { get; set; }
        public int SubmissionId { get; set; }
        public string Title { get; set; } = string.Empty;
        public List<string> Authors { get; set; } = new List<string>();
        public string Topic { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string StatusDisplay { get; set; } = string.Empty;
        public string StatusClass { get; set; } = string.Empty;
        public DateTime Deadline { get; set; }
        public DateTime InvitedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public bool HasReview { get; set; }
        public decimal? AverageScore { get; set; }
    }
}






