using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace SciSubmit.Services
{
    public class SmsService : ISmsService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<SmsService> _logger;
        private readonly string? _accountSid;
        private readonly string? _authToken;
        private readonly string? _fromPhoneNumber;

        public SmsService(IConfiguration configuration, ILogger<SmsService> logger)
        {
            _configuration = configuration;
            _logger = logger;
            
            _accountSid = _configuration["SMS:Twilio:AccountSid"];
            _authToken = _configuration["SMS:Twilio:AuthToken"];
            _fromPhoneNumber = _configuration["SMS:Twilio:FromPhoneNumber"];

            // Initialize Twilio client if credentials are provided
            // Uncomment when Twilio package is installed
            // if (!string.IsNullOrEmpty(_accountSid) && !string.IsNullOrEmpty(_authToken))
            // {
            //     TwilioClient.Init(_accountSid, _authToken);
            // }
        }

        public async Task<bool> SendOtpAsync(string phoneNumber, string otp)
        {
            try
            {
                if (string.IsNullOrEmpty(_accountSid) || string.IsNullOrEmpty(_authToken) || string.IsNullOrEmpty(_fromPhoneNumber))
                {
                    _logger.LogWarning("SMS credentials not configured. SMS will not be sent.");
                    return false;
                }

                var message = $"Mã OTP của bạn là: {otp}. Mã có hiệu lực trong 5 phút.";
                return await SendSmsAsync(phoneNumber, message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending OTP SMS to {PhoneNumber}", phoneNumber);
                return false;
            }
        }

        public async Task<bool> SendSmsAsync(string phoneNumber, string message)
        {
            try
            {
                if (string.IsNullOrEmpty(_accountSid) || string.IsNullOrEmpty(_authToken) || string.IsNullOrEmpty(_fromPhoneNumber))
                {
                    _logger.LogWarning("SMS credentials not configured. SMS will not be sent.");
                    return false;
                }

                // Twilio implementation - Uncomment when Twilio package is installed
                // var messageResource = await MessageResource.CreateAsync(
                //     body: message,
                //     from: new PhoneNumber(_fromPhoneNumber),
                //     to: new PhoneNumber(phoneNumber)
                // );
                //
                // if (messageResource.Status == MessageResource.StatusEnum.Failed || 
                //     messageResource.Status == MessageResource.StatusEnum.Undelivered)
                // {
                //     _logger.LogWarning("SMS failed to send to {PhoneNumber}. Status: {Status}", phoneNumber, messageResource.Status);
                //     return false;
                // }
                //
                // _logger.LogInformation("SMS sent successfully to {PhoneNumber}. SID: {Sid}", phoneNumber, messageResource.Sid);
                // return true;

                // Placeholder implementation
                _logger.LogInformation("SMS service placeholder - would send to {PhoneNumber}: {Message}", phoneNumber, message);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending SMS to {PhoneNumber}", phoneNumber);
                return false;
            }
        }
    }
}

