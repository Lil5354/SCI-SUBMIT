namespace SciSubmit.Models.Admin
{
    public class TimelineItem
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime? Date { get; set; }
        public string Icon { get; set; } = "fas fa-circle";
        public string Color { get; set; } = "#3b82f6";
        public int Order { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
