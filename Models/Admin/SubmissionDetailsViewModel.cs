namespace SciSubmit.Models.Admin
{
    public class SubmissionDetailsViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Abstract { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string AuthorName { get; set; } = string.Empty;
        public string AuthorEmail { get; set; } = string.Empty;
        public string? AuthorAffiliation { get; set; }
        public DateTime? AbstractSubmittedAt { get; set; }
        public DateTime? AbstractReviewedAt { get; set; }
        public string? AbstractRejectionReason { get; set; }
        public DateTime? FullPaperSubmittedAt { get; set; }
        public string? AbstractFileUrl { get; set; }
        public List<AuthorInfoViewModel> CoAuthors { get; set; } = new();
        public List<string> Topics { get; set; } = new();
        public List<string> Keywords { get; set; } = new();
        public List<FullPaperVersionViewModel> FullPaperVersions { get; set; } = new();
        public bool CanApproveAbstract { get; set; }
        public bool CanRejectAbstract { get; set; }
        public bool CanAssignReviewer { get; set; }
        public bool CanMakeFinalDecision { get; set; }
    }

    public class AuthorInfoViewModel
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Affiliation { get; set; }
        public bool IsCorrespondingAuthor { get; set; }
    }

    public class FullPaperVersionViewModel
    {
        public int Id { get; set; }
        public int VersionNumber { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FileUrl { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public DateTime UploadedAt { get; set; }
        public bool IsCurrentVersion { get; set; }
    }
}












