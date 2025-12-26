using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SciSubmit.Models.Enums;

namespace SciSubmit.Models.Payment
{
    public class Payment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int SubmissionId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required]
        public PaymentMethod PaymentMethod { get; set; }

        [Required]
        public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

        [MaxLength(255)]
        public string? TransactionId { get; set; }

        public DateTime? PaymentDate { get; set; }

        [MaxLength(1000)]
        public string? InvoiceUrl { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Navigation Properties
        [ForeignKey(nameof(SubmissionId))]
        public virtual Models.Submission.Submission Submission { get; set; } = null!;

        [ForeignKey(nameof(UserId))]
        public virtual Models.Identity.User User { get; set; } = null!;
    }
}
















