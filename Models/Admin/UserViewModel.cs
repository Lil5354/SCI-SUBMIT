namespace SciSubmit.Models.Admin
{
    public class UserViewModel
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Affiliation { get; set; }
        public string Role { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public int SubmissionCount { get; set; }
        public int ReviewAssignmentCount { get; set; }
    }
}







