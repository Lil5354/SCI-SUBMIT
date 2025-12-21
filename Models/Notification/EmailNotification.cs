using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SciSubmit.Models.Enums;

namespace SciSubmit.Models.Notification
{
    public class EmailNotification
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(255)]
        [EmailAddress]
        public string ToEmail { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string Subject { get; set; } = string.Empty;

        [Required]
        public string Body { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Type { get; set; } = string.Empty;

        [Required]
        public EmailNotificationStatus Status { get; set; } = EmailNotificationStatus.Pending;

        public DateTime? SentAt { get; set; }

        public int? RelatedSubmissionId { get; set; }

        public int? RelatedUserId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        [ForeignKey(nameof(RelatedSubmissionId))]
        public virtual Models.Submission.Submission? RelatedSubmission { get; set; }

        [ForeignKey(nameof(RelatedUserId))]
        public virtual Models.Identity.User? RelatedUser { get; set; }
    }
}
