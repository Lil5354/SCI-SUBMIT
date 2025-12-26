using System.ComponentModel.DataAnnotations;

namespace SciSubmit.Models.Account
{
    public class UserSettingsViewModel
    {
        // Notification Settings
        [Display(Name = "Bật thông báo email")]
        public bool EmailNotificationsEnabled { get; set; } = true;

        [Display(Name = "Thông báo khi được phân công phản biện")]
        public bool ReviewAssignmentNotifications { get; set; } = true;

        [Display(Name = "Thông báo thay đổi trạng thái bài nộp")]
        public bool SubmissionStatusNotifications { get; set; } = true;

        [Display(Name = "Nhắc nhở deadline")]
        public bool DeadlineReminders { get; set; } = true;

        // Language Settings
        [Display(Name = "Ngôn ngữ")]
        public string Language { get; set; } = "vi-VN";

        // Privacy Settings
        [Display(Name = "Hiển thị email công khai")]
        public bool ShowEmailPublicly { get; set; } = false;

        [Display(Name = "Hiển thị số điện thoại công khai")]
        public bool ShowPhonePublicly { get; set; } = false;
    }
}















