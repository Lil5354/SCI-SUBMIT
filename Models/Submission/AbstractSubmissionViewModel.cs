using System.ComponentModel.DataAnnotations;

namespace SciSubmit.Models.Submission
{
    public class AbstractSubmissionViewModel
    {
        public int? Id { get; set; } // For editing existing draft

        [Required(ErrorMessage = "Tiêu đề là bắt buộc")]
        [MaxLength(200, ErrorMessage = "Tiêu đề không được vượt quá 200 ký tự")]
        [Display(Name = "Tiêu đề")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tóm tắt là bắt buộc")]
        [MaxLength(2000, ErrorMessage = "Tóm tắt không được vượt quá 300 từ (khoảng 2000 ký tự)")]
        [Display(Name = "Tóm tắt (Abstract)")]
        public string Abstract { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn ít nhất một chủ đề")]
        [Display(Name = "Chủ đề")]
        public int TopicId { get; set; }

        [Required(ErrorMessage = "Vui lòng thêm ít nhất 5 từ khóa")]
        [MinLength(5, ErrorMessage = "Vui lòng thêm ít nhất 5 từ khóa")]
        [MaxLength(6, ErrorMessage = "Tối đa 6 từ khóa")]
        [Display(Name = "Từ khóa")]
        public List<string> Keywords { get; set; } = new List<string>();

        [Required(ErrorMessage = "Vui lòng thêm ít nhất một tác giả")]
        [MinLength(1, ErrorMessage = "Vui lòng thêm ít nhất một tác giả")]
        [Display(Name = "Tác giả")]
        public List<Models.Submission.AuthorViewModel> Authors { get; set; } = new List<Models.Submission.AuthorViewModel>();

        [Display(Name = "File hỗ trợ (Tùy chọn)")]
        public IFormFile? SupportFile { get; set; }

        public bool IsDraft { get; set; } = true; // true = save draft, false = submit
    }

}

