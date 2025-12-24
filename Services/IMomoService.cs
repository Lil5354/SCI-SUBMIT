namespace SciSubmit.Services
{
    public interface IMomoService
    {
        /// <summary>
        /// Tạo payment URL để redirect user đến Momo
        /// </summary>
        Task<string> CreatePaymentUrlAsync(int paymentId, decimal amount, string orderInfo, string returnUrl, string notifyUrl);

        /// <summary>
        /// Verify payment callback từ Momo
        /// </summary>
        bool VerifyPaymentCallback(Dictionary<string, string> callbackData, out string transactionId, out decimal amount);
    }
}


