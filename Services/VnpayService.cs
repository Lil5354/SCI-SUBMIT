using System.Security.Cryptography;
using System.Text;

namespace SciSubmit.Services
{
    public class VnpayService : IVnpayService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<VnpayService> _logger;

        public VnpayService(IConfiguration configuration, ILogger<VnpayService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public string CreatePaymentUrl(int paymentId, decimal amount, string orderInfo, string returnUrl, string ipAddress)
        {
            try
            {
                var vnpUrl = _configuration["Payment:VNPAY:Url"] ?? "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";
                var vnpTmnCode = _configuration["Payment:VNPAY:TmnCode"] ?? "";
                var vnpHashSecret = _configuration["Payment:VNPAY:HashSecret"] ?? "";

                if (string.IsNullOrEmpty(vnpTmnCode) || string.IsNullOrEmpty(vnpHashSecret))
                {
                    _logger.LogWarning("VNPAY configuration is missing. Using test mode.");
                }

                var vnpParams = new Dictionary<string, string>
                {
                    { "vnp_Version", "2.1.0" },
                    { "vnp_Command", "pay" },
                    { "vnp_TmnCode", vnpTmnCode },
                    { "vnp_Amount", ((long)(amount * 100)).ToString() }, // Convert to cents
                    { "vnp_CurrCode", "VND" },
                    { "vnp_TxnRef", paymentId.ToString() },
                    { "vnp_OrderInfo", orderInfo },
                    { "vnp_OrderType", "other" },
                    { "vnp_Locale", "vn" },
                    { "vnp_ReturnUrl", returnUrl },
                    { "vnp_IpAddr", ipAddress },
                    { "vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss") }
                };

                // Sort params by key
                var sortedParams = vnpParams.OrderBy(x => x.Key).ToList();

                // Create query string
                var queryString = string.Join("&", sortedParams.Select(x => $"{x.Key}={Uri.EscapeDataString(x.Value)}"));

                // Create secure hash
                var signData = queryString;
                var vnpSecureHash = HmacSHA512(vnpHashSecret, signData);

                // Add hash to params
                queryString += $"&vnp_SecureHash={vnpSecureHash}";

                return $"{vnpUrl}?{queryString}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating VNPAY payment URL");
                throw;
            }
        }

        public bool VerifyPaymentCallback(Dictionary<string, string> queryParams, out string transactionId, out decimal amount)
        {
            transactionId = string.Empty;
            amount = 0;

            try
            {
                var vnpHashSecret = _configuration["Payment:VNPAY:HashSecret"] ?? "";

                if (string.IsNullOrEmpty(vnpHashSecret))
                {
                    _logger.LogWarning("VNPAY HashSecret is missing. Cannot verify payment.");
                    return false;
                }

                // Get vnp_SecureHash from params
                if (!queryParams.TryGetValue("vnp_SecureHash", out var vnpSecureHash) || string.IsNullOrEmpty(vnpSecureHash))
                {
                    return false;
                }

                // Remove vnp_SecureHash from params for verification
                var paramsForVerify = queryParams
                    .Where(x => x.Key != "vnp_SecureHash" && x.Key != "vnp_SecureHashType")
                    .OrderBy(x => x.Key)
                    .ToDictionary(x => x.Key, x => x.Value);

                // Create query string
                var signData = string.Join("&", paramsForVerify.Select(x => $"{x.Key}={Uri.EscapeDataString(x.Value)}"));

                // Verify hash
                var calculatedHash = HmacSHA512(vnpHashSecret, signData);

                if (calculatedHash != vnpSecureHash)
                {
                    _logger.LogWarning("VNPAY payment callback hash verification failed");
                    return false;
                }

                // Get transaction info
                if (queryParams.TryGetValue("vnp_TransactionNo", out var vnpTransactionNo))
                {
                    transactionId = vnpTransactionNo;
                }

                if (queryParams.TryGetValue("vnp_Amount", out var vnpAmountStr) && long.TryParse(vnpAmountStr, out var vnpAmount))
                {
                    amount = vnpAmount / 100m; // Convert from cents
                }

                // Check response code
                if (queryParams.TryGetValue("vnp_ResponseCode", out var responseCode))
                {
                    return responseCode == "00"; // 00 means success
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying VNPAY payment callback");
                return false;
            }
        }

        private string HmacSHA512(string key, string inputData)
        {
            var hash = new StringBuilder();
            var keyBytes = Encoding.UTF8.GetBytes(key);
            var inputBytes = Encoding.UTF8.GetBytes(inputData);

            using (var hmac = new HMACSHA512(keyBytes))
            {
                var hashValue = hmac.ComputeHash(inputBytes);
                foreach (var theByte in hashValue)
                {
                    hash.Append(theByte.ToString("x2"));
                }
            }

            return hash.ToString();
        }
    }
}


