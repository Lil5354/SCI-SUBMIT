using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SciSubmit.Models.Conference
{
    public class Conference
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(500)]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        [MaxLength(500)]
        public string? Location { get; set; }

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Navigation Properties
        public virtual ICollection<ConferencePlan> Plans { get; set; } = new List<ConferencePlan>();
        public virtual ICollection<Models.Content.Topic> Topics { get; set; } = new List<Models.Content.Topic>();
        public virtual ICollection<Models.Content.Keyword> Keywords { get; set; } = new List<Models.Content.Keyword>();
        public virtual ICollection<Models.Submission.Submission> Submissions { get; set; } = new List<Models.Submission.Submission>();
        public virtual ICollection<Models.Review.ReviewCriteria> ReviewCriterias { get; set; } = new List<Models.Review.ReviewCriteria>();
        public virtual ICollection<Models.Payment.PaymentConfiguration> PaymentConfigurations { get; set; } = new List<Models.Payment.PaymentConfiguration>();
        public virtual ICollection<Models.Notification.EmailTemplate> EmailTemplates { get; set; } = new List<Models.Notification.EmailTemplate>();
        public virtual ICollection<SystemSetting> SystemSettings { get; set; } = new List<SystemSetting>();
        public virtual ICollection<KeynoteSpeaker> KeynoteSpeakers { get; set; } = new List<KeynoteSpeaker>();
        public virtual ICollection<ConferenceLocation> ConferenceLocations { get; set; } = new List<ConferenceLocation>();
    }
}






