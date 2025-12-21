using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SciSubmit.Models.Submission
{
    public class FullPaperVersion
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int SubmissionId { get; set; }

        [Required]
        public int VersionNumber { get; set; }

        [Required]
        [MaxLength(1000)]
        public string FileUrl { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string FileName { get; set; } = string.Empty;

        [Required]
        public long FileSize { get; set; }

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public int UploadedBy { get; set; }

        public bool IsCurrentVersion { get; set; } = true;

        // Navigation Properties
        [ForeignKey(nameof(SubmissionId))]
        public virtual Submission Submission { get; set; } = null!;

        [ForeignKey(nameof(UploadedBy))]
        public virtual Models.Identity.User Uploader { get; set; } = null!;
    }
}
