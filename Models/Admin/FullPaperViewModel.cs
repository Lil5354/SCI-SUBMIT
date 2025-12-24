namespace SciSubmit.Models.Admin
{
    public class FullPaperViewModel
    {
        public int Id { get; set; }
        public int SubmissionId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string AuthorName { get; set; } = string.Empty;
        public int VersionNumber { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FileUrl { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public DateTime UploadedAt { get; set; }
        public bool IsCurrentVersion { get; set; }
        public List<ReviewerAssignmentViewModel> AssignedReviewers { get; set; } = new();
        public bool CanAssignReviewer { get; set; }
    }

    public class ReviewerAssignmentViewModel
    {
        public int ReviewAssignmentId { get; set; }
        public int ReviewerId { get; set; }
        public string ReviewerName { get; set; } = string.Empty;
        public string ReviewerEmail { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime? Deadline { get; set; }
    }
}


