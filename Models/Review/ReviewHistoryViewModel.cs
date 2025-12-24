namespace SciSubmit.Models.Review
{
    public class ReviewHistoryViewModel
    {
        public List<ReviewHistoryItemViewModel> Reviews { get; set; } = new List<ReviewHistoryItemViewModel>();
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
        public string? SearchTerm { get; set; }
        public string? Recommendation { get; set; }
        public int? Year { get; set; }
    }

    public class ReviewHistoryItemViewModel
    {
        public int Id { get; set; }
        public int SubmissionId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Topic { get; set; } = string.Empty;
        public string Recommendation { get; set; } = string.Empty;
        public string RecommendationDisplay { get; set; } = string.Empty;
        public string RecommendationClass { get; set; } = string.Empty;
        public decimal AverageScore { get; set; }
        public DateTime SubmittedAt { get; set; }
    }
}

