using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SciSubmit.Models.Conference
{
    public class ConferencePlan
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ConferenceId { get; set; }

        [Required]
        public DateTime AbstractSubmissionOpenDate { get; set; }

        [Required]
        public DateTime AbstractSubmissionDeadline { get; set; }

        public DateTime? FullPaperSubmissionOpenDate { get; set; }

        public DateTime? FullPaperSubmissionDeadline { get; set; }

        public DateTime? ReviewDeadline { get; set; }

        public DateTime? ResultAnnouncementDate { get; set; }

        public DateTime? ConferenceDate { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Navigation Properties
        [ForeignKey(nameof(ConferenceId))]
        public virtual Conference Conference { get; set; } = null!;
    }
}












