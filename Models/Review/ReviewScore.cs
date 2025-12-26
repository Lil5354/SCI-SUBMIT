using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SciSubmit.Models.Review
{
    public class ReviewScore
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ReviewId { get; set; }

        [Required]
        [MaxLength(255)]
        public string CriteriaName { get; set; } = string.Empty;

        [Required]
        [Range(1, 5)]
        public int Score { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        [ForeignKey(nameof(ReviewId))]
        public virtual Review Review { get; set; } = null!;
    }
}
















