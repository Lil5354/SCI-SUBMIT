using System.ComponentModel.DataAnnotations;
using SciSubmit.Models.Enums;

namespace SciSubmit.Models.Admin
{
    public class UpdateUserViewModel
    {
        [Required]
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập email.")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
        [MaxLength(255)]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập họ tên.")]
        [MaxLength(255)]
        [Display(Name = "Họ tên")]
        public string FullName { get; set; } = string.Empty;

        [MaxLength(20)]
        [Display(Name = "Số điện thoại")]
        public string? PhoneNumber { get; set; }

        [MaxLength(500)]
        [Display(Name = "Đơn vị")]
        public string? Affiliation { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn vai trò.")]
        [Display(Name = "Vai trò")]
        public UserRole Role { get; set; }

        [Display(Name = "Xác nhận email")]
        public bool EmailConfirmed { get; set; }

        [Display(Name = "Kích hoạt")]
        public bool IsActive { get; set; }
    }
}




