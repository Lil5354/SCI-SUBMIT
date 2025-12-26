using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SciSubmit.Models.Enums;

namespace SciSubmit.Models.Submission
{
    public class Submission
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ConferenceId { get; set; }

        [Required]
        public int AuthorId { get; set; }

        [Required]
        [MaxLength(500)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Abstract { get; set; } = string.Empty;

        [Required]
        public SubmissionStatus Status { get; set; } = SubmissionStatus.Draft;

        [MaxLength(1000)]
        public string? AbstractFileUrl { get; set; }

        public DateTime? AbstractSubmittedAt { get; set; }

        public DateTime? AbstractReviewedAt { get; set; }

        public string? AbstractRejectionReason { get; set; }

        public DateTime? FullPaperSubmittedAt { get; set; }

        public DateTime? FinalVersionSubmittedAt { get; set; }

        public PresentationType? PresentationType { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public DateTime? LastSavedAt { get; set; }

        // Navigation Properties
        [ForeignKey(nameof(ConferenceId))]
        public virtual Models.Conference.Conference Conference { get; set; } = null!;

        [ForeignKey(nameof(AuthorId))]
        public virtual Models.Identity.User Author { get; set; } = null!;

        public virtual ICollection<SubmissionAuthor> SubmissionAuthors { get; set; } = new List<SubmissionAuthor>();
        public virtual ICollection<SubmissionKeyword> SubmissionKeywords { get; set; } = new List<SubmissionKeyword>();
        public virtual ICollection<SubmissionTopic> SubmissionTopics { get; set; } = new List<SubmissionTopic>();
        public virtual ICollection<FullPaperVersion> FullPaperVersions { get; set; } = new List<FullPaperVersion>();
        public virtual ICollection<Models.Review.ReviewAssignment> ReviewAssignments { get; set; } = new List<Models.Review.ReviewAssignment>();
        public virtual Models.Review.FinalDecision? FinalDecision { get; set; }
        public virtual ICollection<Models.Payment.Payment> Payments { get; set; } = new List<Models.Payment.Payment>();
    }
}
















