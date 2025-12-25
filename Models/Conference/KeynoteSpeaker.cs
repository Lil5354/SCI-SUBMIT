using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SciSubmit.Models.Conference
{
    public class KeynoteSpeaker
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ConferenceId { get; set; }

        [Required]
        [MaxLength(500)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Title { get; set; }

        [MaxLength(1000)]
        public string? Affiliation { get; set; }

        [MaxLength(2000)]
        public string? Biography { get; set; }

        [MaxLength(500)]
        public string? Topic { get; set; }

        [MaxLength(1000)]
        public string? PhotoUrl { get; set; }

        public int OrderIndex { get; set; } = 0;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Navigation Properties
        [ForeignKey(nameof(ConferenceId))]
        public virtual Conference Conference { get; set; } = null!;
    }
}


