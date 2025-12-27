namespace SciSubmit.Models.Admin
{
    public class DeadlineViewModel
    {
        public string Title { get; set; } = string.Empty;
        public DateTime DeadlineDate { get; set; }
        public int RemainingDays { get; set; }
        public string BadgeClass { get; set; } = string.Empty; // "bg-danger", "bg-warning", "bg-info"
    }
}


















