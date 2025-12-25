using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SciSubmit.Models.Conference
{
    public class ProgramSchedule
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ConferenceId { get; set; }

        [MaxLength(500)]
        public string? Time { get; set; }

        [MaxLength(2000)]
        public string? Venue { get; set; }

        [MaxLength(1000)]
        public string? PresentationsLink { get; set; }

        [MaxLength(1000)]
        public string? PapersLink { get; set; }

        [MaxLength(1000)]
        public string? ProgramLink { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Navigation Properties
        [ForeignKey(nameof(ConferenceId))]
        public virtual Conference Conference { get; set; } = null!;

        public virtual ICollection<ProgramItem> Items { get; set; } = new List<ProgramItem>();
    }
}

