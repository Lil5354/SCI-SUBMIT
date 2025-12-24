namespace SciSubmit.Services
{
    public interface IEmailService
    {
        /// <summary>
        /// Gửi email đơn giản
        /// </summary>
        Task<bool> SendEmailAsync(string toEmail, string subject, string body, bool isHtml = true);

        /// <summary>
        /// Gửi email từ EmailNotification record
        /// </summary>
        Task<bool> SendEmailNotificationAsync(Models.Notification.EmailNotification notification);

        /// <summary>
        /// Gửi email sử dụng template
        /// </summary>
        Task<bool> SendTemplatedEmailAsync(string toEmail, string templateType, Dictionary<string, string> templateData);
    }
}

