namespace SciSubmit.Models.Admin
{
    public class SubmissionFilterViewModel
    {
        public string? Status { get; set; }
        public int? TopicId { get; set; }
        public int? KeywordId { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? SearchTerm { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}







