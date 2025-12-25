using System.ComponentModel.DataAnnotations;

namespace SciSubmit.Models.Admin
{
    public class CommitteeViewModel
    {
        public List<CommitteeSectionViewModel> Sections { get; set; } = new();
    }

    public class CommitteeSectionViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Tiêu đề section là bắt buộc")]
        [MaxLength(200, ErrorMessage = "Tiêu đề không được vượt quá 200 ký tự")]
        public string Title { get; set; } = string.Empty;

        [MaxLength(1000, ErrorMessage = "Mô tả không được vượt quá 1000 ký tự")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Loại section là bắt buộc")]
        [MaxLength(50)]
        public string SectionType { get; set; } = "Standard";

        public int OrderIndex { get; set; } = 0;

        public bool IsActive { get; set; } = true;

        public List<CommitteeMemberViewModel> Members { get; set; } = new();
    }

    public class CommitteeMemberViewModel
    {
        public int Id { get; set; }

        public int CommitteeSectionId { get; set; }

        [Required(ErrorMessage = "Tên thành viên là bắt buộc")]
        [MaxLength(500, ErrorMessage = "Tên không được vượt quá 500 ký tự")]
        public string Name { get; set; } = string.Empty;

        [MaxLength(200, ErrorMessage = "Học hàm không được vượt quá 200 ký tự")]
        public string? Title { get; set; }

        [MaxLength(1000, ErrorMessage = "Đơn vị công tác không được vượt quá 1000 ký tự")]
        public string? Affiliation { get; set; }

        [MaxLength(500, ErrorMessage = "Quốc gia không được vượt quá 500 ký tự")]
        public string? Country { get; set; }

        [MaxLength(2000, ErrorMessage = "Mô tả không được vượt quá 2000 ký tự")]
        public string? Description { get; set; }

        [MaxLength(1000, ErrorMessage = "URL ảnh không được vượt quá 1000 ký tự")]
        public string? PhotoUrl { get; set; }

        [MaxLength(500, ErrorMessage = "Chủ đề không được vượt quá 500 ký tự")]
        public string? Topic { get; set; }

        [MaxLength(200, ErrorMessage = "Tên track không được vượt quá 200 ký tự")]
        public string? TrackName { get; set; }

        [MaxLength(2000, ErrorMessage = "Mô tả track không được vượt quá 2000 ký tự")]
        public string? TrackDescription { get; set; }

        public int OrderIndex { get; set; } = 0;

        public bool IsActive { get; set; } = true;
    }
}

