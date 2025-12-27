namespace SciSubmit.Models.Submission
{
    public class MySubmissionsViewModel
    {
        public List<SubmissionListItemViewModel> Submissions { get; set; } = new List<SubmissionListItemViewModel>();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
        public string? SearchTerm { get; set; }
        public string? Status { get; set; }
        public int? TopicId { get; set; }
    }

    public class SubmissionListItemViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public List<string> Authors { get; set; } = new List<string>();
        public string Topic { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string StatusDisplay { get; set; } = string.Empty;
        public string StatusClass { get; set; } = string.Empty;
        public DateTime SubmittedAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}

















