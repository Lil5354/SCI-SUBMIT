using System.ComponentModel.DataAnnotations;

namespace SciSubmit.Models.Submission
{
    public class CreateSubmissionViewModel
    {
        [Required(ErrorMessage = "Tiêu đề là bắt buộc")]
        [MaxLength(200, ErrorMessage = "Tiêu đề không được vượt quá 200 ký tự")]
        [Display(Name = "Tiêu đề")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tóm tắt là bắt buộc")]
        [MinLength(150, ErrorMessage = "Tóm tắt phải có ít nhất 150 từ")]
        [Display(Name = "Tóm tắt")]
        public string Abstract { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn ít nhất một từ khóa")]
        [Display(Name = "Từ khóa")]
        public List<string> Keywords { get; set; } = new List<string>();

        [Required(ErrorMessage = "Vui lòng chọn chủ đề")]
        [Display(Name = "Chủ đề")]
        public int TopicId { get; set; }

        [Required(ErrorMessage = "Phải có ít nhất một tác giả")]
        [Display(Name = "Tác giả")]
        public List<AuthorViewModel> Authors { get; set; } = new List<AuthorViewModel>();

        [Display(Name = "File hỗ trợ")]
        public IFormFile? SupportFile { get; set; }

        // For auto-save draft
        public int? SubmissionId { get; set; }
    }

    public class AuthorViewModel
    {
        [Required(ErrorMessage = "Họ tên là bắt buộc")]
        [Display(Name = "Họ và tên")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email là bắt buộc")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "Đơn vị")]
        public string? Affiliation { get; set; }

        [Display(Name = "Tác giả chính")]
        public bool IsCorrespondingAuthor { get; set; }
    }
}

