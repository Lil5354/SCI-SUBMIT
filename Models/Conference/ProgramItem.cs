using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SciSubmit.Models.Conference
{
    public class ProgramItem
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ProgramScheduleId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Time { get; set; } = string.Empty;

        [Required]
        public string Contents { get; set; } = string.Empty;

        public int OrderIndex { get; set; } = 0;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Navigation Properties
        [ForeignKey(nameof(ProgramScheduleId))]
        public virtual ProgramSchedule ProgramSchedule { get; set; } = null!;
    }
}

