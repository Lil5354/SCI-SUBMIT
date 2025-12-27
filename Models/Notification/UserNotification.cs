using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SciSubmit.Models.Enums;

namespace SciSubmit.Models.Notification
{
    public class UserNotification
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public NotificationType Type { get; set; }

        [Required]
        [MaxLength(255)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Message { get; set; } = string.Empty;

        [Required]
        public NotificationStatus Status { get; set; } = NotificationStatus.Unread;

        public int? RelatedSubmissionId { get; set; }

        public int? RelatedUserId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ReadAt { get; set; }

        // Navigation Properties
        [ForeignKey(nameof(UserId))]
        public virtual Models.Identity.User User { get; set; } = null!;

        [ForeignKey(nameof(RelatedSubmissionId))]
        public virtual Models.Submission.Submission? RelatedSubmission { get; set; }

        [ForeignKey(nameof(RelatedUserId))]
        public virtual Models.Identity.User? RelatedUser { get; set; }

        // Computed property
        [NotMapped]
        public bool IsRead => Status == NotificationStatus.Read;
    }
}

