using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SciSubmit.Models.Enums;

namespace SciSubmit.Models.Content
{
    public class Keyword
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ConferenceId { get; set; }

        [Required]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public KeywordStatus Status { get; set; } = KeywordStatus.Pending;

        public int? ApprovedBy { get; set; }

        public DateTime? ApprovedAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int? CreatedBy { get; set; }

        // Navigation Properties
        [ForeignKey(nameof(ConferenceId))]
        public virtual Models.Conference.Conference Conference { get; set; } = null!;

        [ForeignKey(nameof(CreatedBy))]
        public virtual Models.Identity.User? Creator { get; set; }

        [ForeignKey(nameof(ApprovedBy))]
        public virtual Models.Identity.User? Approver { get; set; }

        public virtual ICollection<Models.Submission.SubmissionKeyword> SubmissionKeywords { get; set; } = new List<Models.Submission.SubmissionKeyword>();
        public virtual ICollection<UserKeyword> UserKeywords { get; set; } = new List<UserKeyword>();
    }
}


















