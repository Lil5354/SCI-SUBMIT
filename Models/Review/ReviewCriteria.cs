using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SciSubmit.Models.Review
{
    public class ReviewCriteria
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ConferenceId { get; set; }

        [Required]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public int MaxScore { get; set; } = 5;

        public int OrderIndex { get; set; } = 0;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        [ForeignKey(nameof(ConferenceId))]
        public virtual Models.Conference.Conference Conference { get; set; } = null!;

        public virtual ICollection<ReviewScore> ReviewScores { get; set; } = new List<ReviewScore>();
    }
}

