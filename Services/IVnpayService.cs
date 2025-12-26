namespace SciSubmit.Services
{
    public interface IVnpayService
    {
        /// <summary>
        /// Tạo payment URL để redirect user đến VNPAY
        /// </summary>
        string CreatePaymentUrl(int paymentId, decimal amount, string orderInfo, string returnUrl, string ipAddress);

        /// <summary>
        /// Verify payment callback từ VNPAY
        /// </summary>
        bool VerifyPaymentCallback(Dictionary<string, string> queryParams, out string transactionId, out decimal amount);
    }
}











