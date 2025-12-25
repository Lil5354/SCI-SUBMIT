using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SciSubmit.Models.Conference
{
    public class ConferenceLocation
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ConferenceId { get; set; }

        [Required]
        [MaxLength(500)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Address { get; set; }

        [MaxLength(500)]
        public string? City { get; set; }

        [MaxLength(500)]
        public string? Country { get; set; }

        [MaxLength(50)]
        public string? Latitude { get; set; }

        [MaxLength(50)]
        public string? Longitude { get; set; }

        [MaxLength(2000)]
        public string? GoogleMapsEmbedUrl { get; set; }

        [MaxLength(2000)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Navigation Properties
        [ForeignKey(nameof(ConferenceId))]
        public virtual Conference Conference { get; set; } = null!;
    }
}


