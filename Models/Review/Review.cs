using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SciSubmit.Models.Enums;

namespace SciSubmit.Models.Review
{
    public class Review
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ReviewAssignmentId { get; set; }

        [Required]
        public int SubmissionId { get; set; }

        [Required]
        public int ReviewerId { get; set; }

        [Column(TypeName = "decimal(3,2)")]
        public decimal? AverageScore { get; set; }

        [MaxLength(50)]
        public string? Recommendation { get; set; } // Accept, MinorRevision, MajorRevision, Reject

        public string? CommentsForAuthor { get; set; }

        public string? CommentsForAdmin { get; set; }

        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Navigation Properties
        [ForeignKey(nameof(ReviewAssignmentId))]
        public virtual ReviewAssignment ReviewAssignment { get; set; } = null!;

        [ForeignKey(nameof(SubmissionId))]
        public virtual Models.Submission.Submission Submission { get; set; } = null!;

        [ForeignKey(nameof(ReviewerId))]
        public virtual Models.Identity.User Reviewer { get; set; } = null!;

        public virtual ICollection<ReviewScore> ReviewScores { get; set; } = new List<ReviewScore>();
    }
}
