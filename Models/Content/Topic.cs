using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SciSubmit.Models.Content
{
    public class Topic
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ConferenceId { get; set; }

        [Required]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int OrderIndex { get; set; } = 0;

        // Navigation Properties
        [ForeignKey(nameof(ConferenceId))]
        public virtual Models.Conference.Conference Conference { get; set; } = null!;
        public virtual ICollection<Models.Submission.SubmissionTopic> SubmissionTopics { get; set; } = new List<Models.Submission.SubmissionTopic>();
    }
}
