namespace SciSubmit.Models.Admin
{
    public class SettingsViewModel
    {
        // Email Settings
        public string SmtpServer { get; set; } = "smtp.gmail.com";
        public int SmtpPort { get; set; } = 587;
        public string SmtpUsername { get; set; } = string.Empty;
        public string SmtpPassword { get; set; } = string.Empty;
        public bool SmtpUseSsl { get; set; } = true;
        public string FromEmail { get; set; } = string.Empty;
        public string FromName { get; set; } = "SciSubmit";
        
        // System Settings
        public int MaxFileSizeMB { get; set; } = 10;
        public int MaxKeywordsPerSubmission { get; set; } = 6;
        public int MaxAuthorsPerSubmission { get; set; } = 10;
        public int ReviewDeadlineDays { get; set; } = 14;
        
        // Notification Settings
        public bool EmailNotificationsEnabled { get; set; } = true;
        public bool AutoAssignReviewers { get; set; } = false;
    }
}












