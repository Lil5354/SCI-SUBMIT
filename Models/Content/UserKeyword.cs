using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SciSubmit.Models.Content
{
    public class UserKeyword
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public int KeywordId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        [ForeignKey(nameof(UserId))]
        public virtual Models.Identity.User User { get; set; } = null!;

        [ForeignKey(nameof(KeywordId))]
        public virtual Keyword Keyword { get; set; } = null!;
    }
}

