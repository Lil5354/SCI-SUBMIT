namespace SciSubmit.Models.Admin
{
    public class ReviewAssignmentViewModel
    {
        public int Id { get; set; }
        public int SubmissionId { get; set; }
        public string SubmissionTitle { get; set; } = string.Empty;
        public string ReviewerName { get; set; } = string.Empty;
        public string ReviewerEmail { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime InvitedAt { get; set; }
        public DateTime Deadline { get; set; }
        public DateTime? AcceptedAt { get; set; }
        public DateTime? RejectedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? RejectionReason { get; set; }
    }
}

