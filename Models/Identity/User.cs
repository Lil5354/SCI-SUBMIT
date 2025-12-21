using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SciSubmit.Models.Enums;

namespace SciSubmit.Models.Identity
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(255)]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? PhoneNumber { get; set; }

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string FullName { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Affiliation { get; set; }

        [Required]
        public UserRole Role { get; set; } = UserRole.Guest;

        public bool EmailConfirmed { get; set; } = false;

        public bool PhoneNumberConfirmed { get; set; } = false;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public DateTime? LastLoginAt { get; set; }

        [MaxLength(255)]
        public string? GoogleId { get; set; }

        // Navigation Properties
        public virtual ICollection<Models.Submission.Submission> Submissions { get; set; } = new List<Models.Submission.Submission>();
        public virtual ICollection<Models.Review.ReviewAssignment> ReviewAssignments { get; set; } = new List<Models.Review.ReviewAssignment>();
        public virtual ICollection<Models.Content.UserKeyword> UserKeywords { get; set; } = new List<Models.Content.UserKeyword>();
        public virtual ICollection<Models.Content.Keyword> CreatedKeywords { get; set; } = new List<Models.Content.Keyword>();
        public virtual ICollection<Models.Content.Keyword> ApprovedKeywords { get; set; } = new List<Models.Content.Keyword>();
        public virtual ICollection<Models.Review.FinalDecision> FinalDecisions { get; set; } = new List<Models.Review.FinalDecision>();
        public virtual ICollection<Models.Payment.Payment> Payments { get; set; } = new List<Models.Payment.Payment>();
    }
}
