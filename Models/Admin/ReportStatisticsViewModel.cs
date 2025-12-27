namespace SciSubmit.Models.Admin
{
    public class ReportStatisticsViewModel
    {
        public Dictionary<string, int> SubmissionsByStatus { get; set; } = new();
        public Dictionary<string, int> SubmissionsByTopic { get; set; } = new();
        public Dictionary<string, int> SubmissionsByMonth { get; set; } = new();
        public ReviewStatisticsViewModel ReviewStats { get; set; } = new();
        public int TotalSubmissions { get; set; }
        public int TotalAccepted { get; set; }
        public int TotalRejected { get; set; }
        public int TotalUnderReview { get; set; }
        public decimal AverageReviewScore { get; set; }
    }
}


















