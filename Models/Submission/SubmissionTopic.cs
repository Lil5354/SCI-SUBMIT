using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SciSubmit.Models.Submission
{
    public class SubmissionTopic
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int SubmissionId { get; set; }

        [Required]
        public int TopicId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        [ForeignKey(nameof(SubmissionId))]
        public virtual Submission Submission { get; set; } = null!;

        [ForeignKey(nameof(TopicId))]
        public virtual Models.Content.Topic Topic { get; set; } = null!;
    }
}
