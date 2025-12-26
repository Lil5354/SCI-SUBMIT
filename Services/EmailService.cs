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
                Console.WriteLine($"[EMAIL DEBUG] Starting SendEmailAsync to: {toEmail}");
                var settings = await GetSmtpSettingsAsync();
                
                Console.WriteLine($"[EMAIL DEBUG] SMTP Settings:");
                Console.WriteLine($"[EMAIL DEBUG]   Server: {settings.Server}");
                Console.WriteLine($"[EMAIL DEBUG]   Port: {settings.Port}");
                Console.WriteLine($"[EMAIL DEBUG]   Username: {settings.Username}");
                Console.WriteLine($"[EMAIL DEBUG]   Password: {(string.IsNullOrEmpty(settings.Password) ? "EMPTY" : "***CONFIGURED***")}");
                Console.WriteLine($"[EMAIL DEBUG]   FromEmail: {settings.FromEmail}");
                Console.WriteLine($"[EMAIL DEBUG]   FromName: {settings.FromName}");

                // Kiểm tra nếu không có cấu hình email
                if (string.IsNullOrEmpty(settings.Username) || string.IsNullOrEmpty(settings.Password))
                {
                    var warningMessage = "SMTP credentials not configured. Email will not be sent.";
                    _logger.LogWarning(warningMessage);
                    Console.WriteLine($"[EMAIL WARNING] {warningMessage}");
                    Console.WriteLine($"[EMAIL WARNING] Username: {(string.IsNullOrEmpty(settings.Username) ? "EMPTY" : settings.Username)}");
                    Console.WriteLine($"[EMAIL WARNING] Password: {(string.IsNullOrEmpty(settings.Password) ? "EMPTY" : "***")}");
                    return false;
                }

                Console.WriteLine($"[EMAIL DEBUG] Creating MimeMessage...");
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(settings.FromName, settings.FromEmail));
                message.To.Add(new MailboxAddress("", toEmail));
                message.Subject = subject;
                Console.WriteLine($"[EMAIL DEBUG] From: {settings.FromEmail} ({settings.FromName})");
                Console.WriteLine($"[EMAIL DEBUG] To: {toEmail}");
                Console.WriteLine($"[EMAIL DEBUG] Subject: {subject}");

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

                Console.WriteLine($"[EMAIL DEBUG] Connecting to SMTP server {settings.Server}:{settings.Port}...");
                using (var client = new SmtpClient())
                {
                    // Bypass SSL certificate validation (for development/testing only)
                    client.ServerCertificateValidationCallback = (s, c, h, e) => true;
                    
                    try
                    {
                        Console.WriteLine($"[EMAIL DEBUG] Attempting to connect to {settings.Server}:{settings.Port} with SSL: {settings.UseSsl}");
                        await client.ConnectAsync(settings.Server, settings.Port, 
                            settings.UseSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None);
                        Console.WriteLine($"[EMAIL DEBUG] Connected to SMTP server successfully");
                        
                        if (!string.IsNullOrEmpty(settings.Username))
                        {
                            Console.WriteLine($"[EMAIL DEBUG] Authenticating with username: {settings.Username}...");
                            try
                            {
                                await client.AuthenticateAsync(settings.Username, settings.Password);
                                Console.WriteLine($"[EMAIL DEBUG] Authentication successful");
                            }
                            catch (Exception authEx)
                            {
                                Console.WriteLine($"[EMAIL ERROR] Authentication failed!");
                                Console.WriteLine($"[EMAIL ERROR] Auth Error: {authEx.Message}");
                                Console.WriteLine($"[EMAIL ERROR] Auth Stack Trace: {authEx.StackTrace}");
                                if (authEx.InnerException != null)
                                {
                                    Console.WriteLine($"[EMAIL ERROR] Auth Inner Exception: {authEx.InnerException.Message}");
                                }
                                throw;
                            }
                        }

                        Console.WriteLine($"[EMAIL DEBUG] Sending email from {settings.FromEmail} to {toEmail}...");
                        try
                        {
                            await client.SendAsync(message);
                            Console.WriteLine($"[EMAIL DEBUG] Email sent successfully to SMTP server");
                        }
                        catch (Exception sendEx)
                        {
                            Console.WriteLine($"[EMAIL ERROR] Failed to send email!");
                            Console.WriteLine($"[EMAIL ERROR] Send Error: {sendEx.Message}");
                            Console.WriteLine($"[EMAIL ERROR] Send Stack Trace: {sendEx.StackTrace}");
                            if (sendEx.InnerException != null)
                            {
                                Console.WriteLine($"[EMAIL ERROR] Send Inner Exception: {sendEx.InnerException.Message}");
                            }
                            throw;
                        }
                        
                        await client.DisconnectAsync(true);
                        Console.WriteLine($"[EMAIL DEBUG] Disconnected from SMTP server");
                    }
                    catch (Exception smtpEx)
                    {
                        Console.WriteLine($"[EMAIL ERROR] SMTP Operation failed!");
                        Console.WriteLine($"[EMAIL ERROR] SMTP Error: {smtpEx.Message}");
                        Console.WriteLine($"[EMAIL ERROR] SMTP Error Type: {smtpEx.GetType().Name}");
                        Console.WriteLine($"[EMAIL ERROR] SMTP Stack Trace: {smtpEx.StackTrace}");
                        if (smtpEx.InnerException != null)
                        {
                            Console.WriteLine($"[EMAIL ERROR] SMTP Inner Exception: {smtpEx.InnerException.Message}");
                            Console.WriteLine($"[EMAIL ERROR] SMTP Inner Exception Type: {smtpEx.InnerException.GetType().Name}");
                        }
                        throw;
                    }
                }

                _logger.LogInformation($"Email sent successfully to {toEmail}");
                Console.WriteLine($"[EMAIL SUCCESS] Email sent successfully to {toEmail}");
                return true;
            }
            catch (Exception ex)
            {
                var errorMessage = $"Failed to send email to {toEmail}: {ex.Message}";
                _logger.LogError(ex, errorMessage);
                Console.WriteLine($"[EMAIL ERROR] {errorMessage}");
                Console.WriteLine($"[EMAIL ERROR] Stack Trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"[EMAIL ERROR] Inner Exception: {ex.InnerException.Message}");
                }
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
                var errorMessage = $"Failed to send email notification {notification.Id}: {ex.Message}";
                _logger.LogError(ex, errorMessage);
                Console.WriteLine($"[EMAIL ERROR] {errorMessage}");
                Console.WriteLine($"[EMAIL ERROR] Stack Trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"[EMAIL ERROR] Inner Exception: {ex.InnerException.Message}");
                }
                
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










