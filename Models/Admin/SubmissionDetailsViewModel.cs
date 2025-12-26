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
        public List<ReviewAssignmentInfoViewModel> ReviewAssignments { get; set; } = new();
        public List<ReviewResultViewModel> ReviewResults { get; set; } = new();
    }

    public class ReviewAssignmentInfoViewModel
    {
        public int Id { get; set; }
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

    public class ReviewResultViewModel
    {
        public int ReviewId { get; set; }
        public int ReviewAssignmentId { get; set; }
        public string ReviewerName { get; set; } = string.Empty;
        public string ReviewerEmail { get; set; } = string.Empty;
        public decimal? AverageScore { get; set; }
        public string? Recommendation { get; set; }
        public string? CommentsForAuthor { get; set; }
        public string? CommentsForAdmin { get; set; }
        public DateTime SubmittedAt { get; set; }
        public List<ReviewScoreViewModel> Scores { get; set; } = new();
    }

    public class ReviewScoreViewModel
    {
        public string CriterionName { get; set; } = string.Empty;
        public int Score { get; set; }
        public int MaxScore { get; set; }
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















