namespace SciSubmit.Services
{
    public interface ISmsService
    {
        /// <summary>
        /// Gửi SMS OTP
        /// </summary>
        Task<bool> SendOtpAsync(string phoneNumber, string otp);

        /// <summary>
        /// Gửi SMS thông báo
        /// </summary>
        Task<bool> SendSmsAsync(string phoneNumber, string message);
    }
}











