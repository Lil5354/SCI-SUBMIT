namespace SciSubmit.Models.Admin
{
    public class ReviewStatisticsViewModel
    {
        public int TotalAssignments { get; set; }
        public int PendingAssignments { get; set; }
        public int AcceptedAssignments { get; set; }
        public int CompletedAssignments { get; set; }
        public int RejectedAssignments { get; set; }
        public Dictionary<string, int> AssignmentsByStatus { get; set; } = new();
        public decimal AverageCompletionTime { get; set; } // in days
    }
}






