using Microsoft.EntityFrameworkCore;
using SciSubmit.Data;
using SciSubmit.Models.Notification;
using SciSubmit.Models.Enums;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace SciSubmit.Services
{
    public class EmailService : IEmailService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(
            ApplicationDbContext context,
            IConfiguration configuration,
            ILogger<EmailService> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// Lấy SMTP settings từ SystemSettings hoặc appsettings.json
        /// </summary>
        private async Task<SmtpSettings> GetSmtpSettingsAsync()
        {
            var settings = new SmtpSettings();

            // Lấy active conference
            var activeConference = await _context.Conferences
                .Where(c => c.IsActive)
                .FirstOrDefaultAsync();

            if (activeConference != null)
            {
                // Load settings từ SystemSettings
                var systemSettings = await _context.SystemSettings
                    .Where(s => s.ConferenceId == activeConference.Id)
                    .ToDictionaryAsync(s => s.Key, s => s.Value);

                settings.Server = systemSettings.GetValueOrDefault("SmtpServer") 
                    ?? _configuration["Email:SmtpServer"] 
                    ?? "smtp.gmail.com";
                
                if (int.TryParse(systemSettings.GetValueOrDefault("SmtpPort") ?? _configuration["Email:SmtpPort"], out var port))
                {
                    settings.Port = port;
                }
                else
                {
                    settings.Port = 587;
                }

                settings.Username = systemSettings.GetValueOrDefault("SmtpUsername") 
                    ?? _configuration["Email:SmtpUsername"] 
                    ?? string.Empty;
                
                settings.Password = systemSettings.GetValueOrDefault("SmtpPassword") 
                    ?? _configuration["Email:SmtpPassword"] 
                    ?? string.Empty;

                if (bool.TryParse(systemSettings.GetValueOrDefault("SmtpUseSsl") ?? _configuration["Email:SmtpUseSsl"], out var useSsl))
                {
                    settings.UseSsl = useSsl;
                }
                else
                {
                    settings.UseSsl = true;
                }

                settings.FromEmail = systemSettings.GetValueOrDefault("FromEmail") 
                    ?? _configuration["Email:FromEmail"] 
                    ?? "noreply@scisubmit.com";
                
                settings.FromName = systemSettings.GetValueOrDefault("FromName") 
                    ?? _configuration["Email:FromName"] 
                    ?? "SciSubmit System";
            }
            else
            {
                // Fallback to appsettings.json
                settings.Server = _configuration["Email:SmtpServer"] ?? "smtp.gmail.com";
                settings.Port = int.Parse(_configuration["Email:SmtpPort"] ?? "587");
                settings.Username = _configuration["Email:SmtpUsername"] ?? string.Empty;
                settings.Password = _configuration["Email:SmtpPassword"] ?? string.Empty;
                settings.UseSsl = bool.Parse(_configuration["Email:SmtpUseSsl"] ?? "true");
                settings.FromEmail = _configuration["Email:FromEmail"] ?? "noreply@scisubmit.com";
                settings.FromName = _configuration["Email:FromName"] ?? "SciSubmit System";
            }

            return settings;
        }

        public async Task<bool> SendEmailAsync(string toEmail, string subject, string body, bool isHtml = true)
        {
            try
            {
                var settings = await GetSmtpSettingsAsync();

                // Kiểm tra nếu không có cấu hình email
                if (string.IsNullOrEmpty(settings.Username) || string.IsNullOrEmpty(settings.Password))
                {
                    _logger.LogWarning("SMTP credentials not configured. Email will not be sent.");
                    return false;
                }

                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(settings.FromName, settings.FromEmail));
                message.To.Add(new MailboxAddress("", toEmail));
                message.Subject = subject;

                var bodyBuilder = new BodyBuilder();
                if (isHtml)
                {
                    bodyBuilder.HtmlBody = body;
                }
                else
                {
                    bodyBuilder.TextBody = body;
                }
                message.Body = bodyBuilder.ToMessageBody();

                using (var client = new SmtpClient())
                {
                    await client.ConnectAsync(settings.Server, settings.Port, 
                        settings.UseSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None);
                    
                    if (!string.IsNullOrEmpty(settings.Username))
                    {
                        await client.AuthenticateAsync(settings.Username, settings.Password);
                    }

                    await client.SendAsync(message);
                    await client.DisconnectAsync(true);
                }

                _logger.LogInformation($"Email sent successfully to {toEmail}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send email to {toEmail}: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SendEmailNotificationAsync(EmailNotification notification)
        {
            if (notification == null)
            {
                return false;
            }

            try
            {
                // Gửi email
                var success = await SendEmailAsync(
                    notification.ToEmail,
                    notification.Subject,
                    notification.Body,
                    isHtml: true);

                // Update notification status
                if (success)
                {
                    notification.Status = EmailNotificationStatus.Sent;
                    notification.SentAt = DateTime.UtcNow;
                }
                else
                {
                    notification.Status = EmailNotificationStatus.Failed;
                }

                _context.EmailNotifications.Update(notification);
                await _context.SaveChangesAsync();

                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send email notification {notification.Id}: {ex.Message}");
                
                // Update status to failed
                notification.Status = EmailNotificationStatus.Failed;
                _context.EmailNotifications.Update(notification);
                await _context.SaveChangesAsync();

                return false;
            }
        }

        public async Task<bool> SendTemplatedEmailAsync(string toEmail, string templateType, Dictionary<string, string> templateData)
        {
            try
            {
                // Lấy active conference
                var activeConference = await _context.Conferences
                    .Where(c => c.IsActive)
                    .FirstOrDefaultAsync();

                if (activeConference == null)
                {
                    _logger.LogWarning("No active conference found for email template");
                    return false;
                }

                // Load template
                var template = await _context.EmailTemplates
                    .FirstOrDefaultAsync(t => t.ConferenceId == activeConference.Id 
                                           && t.Type == templateType 
                                           && t.IsActive);

                if (template == null)
                {
                    _logger.LogWarning($"Email template '{templateType}' not found");
                    return false;
                }

                // Replace template variables
                var subject = template.Subject;
                var body = template.Body;

                foreach (var kvp in templateData)
                {
                    subject = subject.Replace($"{{{kvp.Key}}}", kvp.Value);
                    body = body.Replace($"{{{kvp.Key}}}", kvp.Value);
                }

                // Gửi email
                return await SendEmailAsync(toEmail, subject, body, isHtml: true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send templated email to {toEmail}: {ex.Message}");
                return false;
            }
        }

        private class SmtpSettings
        {
            public string Server { get; set; } = string.Empty;
            public int Port { get; set; } = 587;
            public string Username { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
            public bool UseSsl { get; set; } = true;
            public string FromEmail { get; set; } = string.Empty;
            public string FromName { get; set; } = string.Empty;
        }
    }
}






