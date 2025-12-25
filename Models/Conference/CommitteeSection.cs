using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SciSubmit.Models.Conference
{
    public class CommitteeSection
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ConferenceId { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty; // "General Chair", "Program Committee", etc.

        [MaxLength(1000)]
        public string? Description { get; set; }

        [MaxLength(50)]
        public string SectionType { get; set; } = "Standard"; // "GeneralChair", "ProgramCommittee", "TrackChairs", "KeynoteSpeakers", "PublicationChairs", "LocalOrganizing"

        public int OrderIndex { get; set; } = 0;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Navigation Properties
        [ForeignKey(nameof(ConferenceId))]
        public virtual Conference Conference { get; set; } = null!;

        public virtual ICollection<CommitteeMember> Members { get; set; } = new List<CommitteeMember>();
    }
}

