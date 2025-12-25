namespace SciSubmit.Models.Admin
{
    public class ProgramViewModel
    {
        public int Id { get; set; }
        public string? Time { get; set; }
        public string? Venue { get; set; }
        public string? PresentationsLink { get; set; }
        public string? PapersLink { get; set; }
        public string? ProgramLink { get; set; }
        public List<ProgramItemViewModel> Items { get; set; } = new List<ProgramItemViewModel>();
    }

    public class ProgramItemViewModel
    {
        public int Id { get; set; }
        public int ProgramScheduleId { get; set; }
        public string Time { get; set; } = string.Empty;
        public string Contents { get; set; } = string.Empty;
        public int OrderIndex { get; set; }
        public bool IsActive { get; set; }
    }
}

