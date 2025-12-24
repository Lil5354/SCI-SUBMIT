using System.ComponentModel.DataAnnotations;
using SciSubmit.Models.Enums;

namespace SciSubmit.Models.Admin
{
    public class CreateUserViewModel
    {
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
        public UserRole Role { get; set; } = UserRole.Author;

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
        [MinLength(6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự.")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu.")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Mật khẩu xác nhận không khớp.")]
        [Display(Name = "Xác nhận mật khẩu")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Display(Name = "Xác nhận email")]
        public bool EmailConfirmed { get; set; } = true;

        [Display(Name = "Kích hoạt")]
        public bool IsActive { get; set; } = true;
    }
}






