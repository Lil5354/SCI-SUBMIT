namespace SciSubmit.Models.Admin
{
    public class ReviewerViewModel
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Affiliation { get; set; }
        public List<string> Keywords { get; set; } = new();
        public int MatchScore { get; set; } // Percentage match với submission keywords
        public int ActiveAssignments { get; set; } // Số assignments đang active
    }
}







