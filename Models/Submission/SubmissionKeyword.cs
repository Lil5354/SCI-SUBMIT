using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SciSubmit.Models.Submission
{
    public class SubmissionKeyword
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int SubmissionId { get; set; }

        [Required]
        public int KeywordId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        [ForeignKey(nameof(SubmissionId))]
        public virtual Submission Submission { get; set; } = null!;

        [ForeignKey(nameof(KeywordId))]
        public virtual Models.Content.Keyword Keyword { get; set; } = null!;
    }
}
















