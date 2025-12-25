namespace SciSubmit.Models.Admin
{
    public class SubmissionViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string AuthorName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime? SubmittedAt { get; set; }
        public List<string> Topics { get; set; } = new();
        public List<string> Keywords { get; set; } = new();
    }
}












