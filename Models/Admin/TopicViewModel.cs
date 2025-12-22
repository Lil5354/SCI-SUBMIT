namespace SciSubmit.Models.Admin
{
    public class TopicViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int OrderIndex { get; set; }
        public bool IsActive { get; set; }
        public int SubmissionCount { get; set; }
    }
}






