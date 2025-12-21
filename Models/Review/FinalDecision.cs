using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SciSubmit.Models.Enums;

namespace SciSubmit.Models.Review
{
    public class FinalDecision
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int SubmissionId { get; set; }

        [Required]
        public FinalDecisionType Decision { get; set; }

        [Required]
        public int DecisionBy { get; set; }

        public string? DecisionReason { get; set; }

        [Column(TypeName = "decimal(3,2)")]
        public decimal? AverageScore { get; set; }

        public DateTime DecidedAt { get; set; } = DateTime.UtcNow;

        public DateTime? RevisionDeadline { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        [ForeignKey(nameof(SubmissionId))]
        public virtual Models.Submission.Submission Submission { get; set; } = null!;

        [ForeignKey(nameof(DecisionBy))]
        public virtual Models.Identity.User DecisionMaker { get; set; } = null!;
    }
}

