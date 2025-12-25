using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace SciSubmit.Services
{
    public class MomoService : IMomoService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<MomoService> _logger;
        private readonly HttpClient _httpClient;

        public MomoService(IConfiguration configuration, ILogger<MomoService> logger, HttpClient httpClient)
        {
            _configuration = configuration;
            _logger = logger;
            _httpClient = httpClient;
        }

        public async Task<string> CreatePaymentUrlAsync(int paymentId, decimal amount, string orderInfo, string returnUrl, string notifyUrl)
        {
            try
            {
                var partnerCode = _configuration["Payment:Momo:PartnerCode"] ?? "";
                var accessKey = _configuration["Payment:Momo:AccessKey"] ?? "";
                var secretKey = _configuration["Payment:Momo:SecretKey"] ?? "";
                var apiEndpoint = _configuration["Payment:Momo:ApiEndpoint"] ?? "https://test-payment.momo.vn/v2/gateway/api/create";

                if (string.IsNullOrEmpty(partnerCode) || string.IsNullOrEmpty(accessKey) || string.IsNullOrEmpty(secretKey))
                {
                    _logger.LogWarning("Momo configuration is missing. Using test mode.");
                }

                var requestId = Guid.NewGuid().ToString();
                var orderId = paymentId.ToString();
                var requestType = "captureWallet";
                var extraData = "";

                // Create raw signature
                var rawSignature = $"accessKey={accessKey}&amount={(long)(amount * 100)}&extraData={extraData}&ipnUrl={notifyUrl}&orderId={orderId}&orderInfo={orderInfo}&partnerCode={partnerCode}&redirectUrl={returnUrl}&requestId={requestId}&requestType={requestType}";

                // Create signature
                var signature = ComputeHmacSha256(rawSignature, secretKey);

                // Create request body
                var requestBody = new
                {
                    partnerCode = partnerCode,
                    partnerName = "SciSubmit",
                    storeId = "SciSubmit",
                    requestId = requestId,
                    amount = (long)(amount * 100),
                    orderId = orderId,
                    orderInfo = orderInfo,
                    redirectUrl = returnUrl,
                    ipnUrl = notifyUrl,
                    requestType = requestType,
                    extraData = extraData,
                    signature = signature,
                    lang = "vi"
                };

                var jsonContent = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                // Call Momo API
                var response = await _httpClient.PostAsync(apiEndpoint, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Momo API error: {Response}", responseContent);
                    throw new Exception("Failed to create Momo payment URL");
                }

                var result = JsonSerializer.Deserialize<JsonElement>(responseContent);

                if (result.TryGetProperty("payUrl", out var payUrlElement))
                {
                    return payUrlElement.GetString() ?? "";
                }

                throw new Exception("Momo API did not return payUrl");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating Momo payment URL");
                throw;
            }
        }

        public bool VerifyPaymentCallback(Dictionary<string, string> callbackData, out string transactionId, out decimal amount)
        {
            transactionId = string.Empty;
            amount = 0;

            try
            {
                var secretKey = _configuration["Payment:Momo:SecretKey"] ?? "";

                if (string.IsNullOrEmpty(secretKey))
                {
                    _logger.LogWarning("Momo SecretKey is missing. Cannot verify payment.");
                    return false;
                }

                // Get required fields
                if (!callbackData.TryGetValue("orderId", out var orderId) ||
                    !callbackData.TryGetValue("resultCode", out var resultCode) ||
                    !callbackData.TryGetValue("amount", out var amountStr) ||
                    !callbackData.TryGetValue("transId", out var transId) ||
                    !callbackData.TryGetValue("signature", out var signature))
                {
                    return false;
                }

                // Verify signature
                var rawSignature = $"accessKey={callbackData.GetValueOrDefault("accessKey", "")}&amount={amountStr}&extraData={callbackData.GetValueOrDefault("extraData", "")}&message={callbackData.GetValueOrDefault("message", "")}&orderId={orderId}&orderInfo={callbackData.GetValueOrDefault("orderInfo", "")}&orderType={callbackData.GetValueOrDefault("orderType", "")}&partnerCode={callbackData.GetValueOrDefault("partnerCode", "")}&payType={callbackData.GetValueOrDefault("payType", "")}&requestId={callbackData.GetValueOrDefault("requestId", "")}&responseTime={callbackData.GetValueOrDefault("responseTime", "")}&resultCode={resultCode}&transId={transId}";

                var calculatedSignature = ComputeHmacSha256(rawSignature, secretKey);

                if (calculatedSignature != signature)
                {
                    _logger.LogWarning("Momo payment callback signature verification failed");
                    return false;
                }

                // Check result code (0 means success)
                if (resultCode != "0")
                {
                    return false;
                }

                transactionId = transId;
                if (long.TryParse(amountStr, out var amountLong))
                {
                    amount = amountLong / 100m; // Convert from cents
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying Momo payment callback");
                return false;
            }
        }

        private string ComputeHmacSha256(string message, string secretKey)
        {
            var keyBytes = Encoding.UTF8.GetBytes(secretKey);
            var messageBytes = Encoding.UTF8.GetBytes(message);

            using (var hmac = new HMACSHA256(keyBytes))
            {
                var hashBytes = hmac.ComputeHash(messageBytes);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
            }
        }
    }
}







