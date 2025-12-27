using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SciSubmit.Models.Enums;

namespace SciSubmit.Models.Review
{
    public class ReviewAssignment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int SubmissionId { get; set; }

        [Required]
        public int ReviewerId { get; set; }

        [Required]
        public ReviewAssignmentStatus Status { get; set; } = ReviewAssignmentStatus.Pending;

        public DateTime InvitedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public int InvitedBy { get; set; }

        public DateTime? AcceptedAt { get; set; }

        public DateTime? RejectedAt { get; set; }

        public string? RejectionReason { get; set; }

        [Required]
        public DateTime Deadline { get; set; }

        public DateTime? CompletedAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Navigation Properties
        [ForeignKey(nameof(SubmissionId))]
        public virtual Models.Submission.Submission Submission { get; set; } = null!;

        [ForeignKey(nameof(ReviewerId))]
        public virtual Models.Identity.User Reviewer { get; set; } = null!;

        [ForeignKey(nameof(InvitedBy))]
        public virtual Models.Identity.User Inviter { get; set; } = null!;

        public virtual Review? Review { get; set; }
    }
}


















