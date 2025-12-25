using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SciSubmit.Models.Conference
{
    public class CommitteeMember
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int CommitteeSectionId { get; set; }

        [Required]
        [MaxLength(500)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? Title { get; set; } // "Prof.", "Assoc. Prof. Dr.", etc.

        [MaxLength(1000)]
        public string? Affiliation { get; set; }

        [MaxLength(500)]
        public string? Country { get; set; }

        [MaxLength(2000)]
        public string? Description { get; set; } // Additional info like "Rector of VKU"

        [MaxLength(1000)]
        public string? PhotoUrl { get; set; }

        [MaxLength(500)]
        public string? Topic { get; set; } // For keynote speakers

        [MaxLength(200)]
        public string? TrackName { get; set; } // For track chairs: "Track 1", "Track 2", etc.

        [MaxLength(2000)]
        public string? TrackDescription { get; set; } // Track description

        public int OrderIndex { get; set; } = 0;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Navigation Properties
        [ForeignKey(nameof(CommitteeSectionId))]
        public virtual CommitteeSection CommitteeSection { get; set; } = null!;
    }
}

