namespace SciSubmit.Models.Admin
{
    public class ConferenceConfigViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Location { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool IsActive { get; set; }
        public ConferencePlanViewModel? Plan { get; set; }
    }
}






