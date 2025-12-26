using Microsoft.AspNetCore.Mvc;
using SciSubmit.Services;
using Microsoft.Extensions.Logging;

namespace SciSubmit.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TestEmailController : ControllerBase
    {
        private readonly IEmailService _emailService;
        private readonly ILogger<TestEmailController> _logger;

        public TestEmailController(IEmailService emailService, ILogger<TestEmailController> logger)
        {
            _emailService = emailService;
            _logger = logger;
        }

        [HttpGet("send-test")]
        public async Task<IActionResult> SendTestEmail([FromQuery] string toEmail = "thaodtt22@uef.edu.vn")
        {
            try
            {
                Console.WriteLine($"[TEST EMAIL] Starting test email send to: {toEmail}");
                _logger.LogInformation($"[TEST EMAIL] Starting test email send to: {toEmail}");

                var subject = "Test Email from SciSubmit";
                var body = "<h2>Test Email</h2>" +
                          "<p>This is a test email from SciSubmit System.</p>" +
                          $"<p>Sent at: {DateTime.Now:yyyy-MM-dd HH:mm:ss}</p>" +
                          "<p>If you receive this, the email system is working correctly.</p>";

                var success = await _emailService.SendEmailAsync(toEmail, subject, body, isHtml: true);

                if (success)
                {
                    Console.WriteLine($"[TEST EMAIL] Email sent successfully to {toEmail}");
                    _logger.LogInformation($"[TEST EMAIL] Email sent successfully to {toEmail}");
                    return Ok(new { 
                        success = true, 
                        message = $"Test email sent successfully to {toEmail}. Check console log for details." 
                    });
                }
                else
                {
                    Console.WriteLine($"[TEST EMAIL] Failed to send email to {toEmail}");
                    _logger.LogWarning($"[TEST EMAIL] Failed to send email to {toEmail}");
                    return Ok(new { 
                        success = false, 
                        message = $"Failed to send test email to {toEmail}. Check console log for details." 
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TEST EMAIL ERROR] {ex.Message}");
                Console.WriteLine($"[TEST EMAIL ERROR] Stack Trace: {ex.StackTrace}");
                _logger.LogError(ex, $"[TEST EMAIL ERROR] Failed to send test email");
                
                return StatusCode(500, new { 
                    success = false, 
                    message = $"Error: {ex.Message}. Check console log for details." 
                });
            }
        }
    }
}

