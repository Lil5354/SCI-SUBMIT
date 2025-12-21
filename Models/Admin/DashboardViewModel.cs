namespace SciSubmit.Models.Admin
{
    public class DashboardViewModel
    {
        public DashboardStatsViewModel Stats { get; set; } = new();
        public List<DeadlineViewModel> Deadlines { get; set; } = new();
    }
}

