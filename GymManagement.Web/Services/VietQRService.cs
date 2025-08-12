using System.Text;
using System.Web;

namespace GymManagement.Web.Services
{
    public class VietQRService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<VietQRService> _logger;

        public VietQRService(IConfiguration configuration, ILogger<VietQRService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public string GenerateVietQRUrl(decimal amount, string orderInfo, string orderId)
        {
            try
            {
                var vietQRConfig = _configuration.GetSection("VietQR");
                var bankId = vietQRConfig["BankId"];
                var accountNo = vietQRConfig["AccountNo"];
                var accountName = vietQRConfig["AccountName"];
                var template = vietQRConfig["Template"];

                // Create VietQR URL using VietQR API
                var baseUrl = "https://img.vietqr.io/image";
                var amountInt = (int)amount; // Convert to integer for VietQR

                var vietQRUrl = $"{baseUrl}/{bankId}-{accountNo}-{template}.png?amount={amountInt}&addInfo={HttpUtility.UrlEncode(orderInfo)}&accountName={HttpUtility.UrlEncode(accountName)}";

                _logger.LogInformation($"Generated VietQR URL: {vietQRUrl}");
                return vietQRUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating VietQR URL");
                return string.Empty;
            }
        }

        public string GenerateVietQRData(decimal amount, string orderInfo, string orderId)
        {
            try
            {
                var vietQRConfig = _configuration.GetSection("VietQR");
                var bankId = vietQRConfig["BankId"];
                var accountNo = vietQRConfig["AccountNo"];
                var accountName = vietQRConfig["AccountName"];

                // Tạo QR code chuẩn VietQR theo EMVCo
                var qrBuilder = new StringBuilder();

                // Payload Format Indicator
                qrBuilder.Append("000201");

                // Point of Initiation Method
                qrBuilder.Append("010212");

                // Merchant Account Information
                var merchantInfo = $"0010A000000727012700{bankId.Length:D2}{bankId}01{accountNo.Length:D2}{accountNo}0208QRIBFTTA";
                qrBuilder.Append($"38{merchantInfo.Length:D2}{merchantInfo}");

                // Merchant Category Code
                qrBuilder.Append("52040000");

                // Transaction Currency (VND = 704)
                qrBuilder.Append("5303704");

                // Transaction Amount
                if (amount > 0)
                {
                    var amountStr = ((int)amount).ToString();
                    qrBuilder.Append($"54{amountStr.Length:D2}{amountStr}");
                }

                // Country Code
                qrBuilder.Append("5802VN");

                // Merchant Name
                if (!string.IsNullOrEmpty(accountName))
                {
                    qrBuilder.Append($"59{accountName.Length:D2}{accountName}");
                }

                // Additional Data Field Template
                if (!string.IsNullOrEmpty(orderInfo))
                {
                    var additionalData = $"08{orderInfo.Length:D2}{orderInfo}";
                    qrBuilder.Append($"62{additionalData.Length:D2}{additionalData}");
                }

                // CRC (sẽ được tính sau)
                qrBuilder.Append("6304");

                var qrData = qrBuilder.ToString();
                _logger.LogInformation($"Generated VietQR data for order: {orderId}");
                return qrData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating VietQR data");
                return string.Empty;
            }
        }

        public VietQRInfo GetVietQRInfo(decimal amount, string orderInfo, string orderId)
        {
            var vietQRConfig = _configuration.GetSection("VietQR");
            
            return new VietQRInfo
            {
                BankId = vietQRConfig["BankId"] ?? "",
                AccountNo = vietQRConfig["AccountNo"] ?? "",
                AccountName = vietQRConfig["AccountName"] ?? "",
                Amount = amount,
                OrderInfo = orderInfo,
                OrderId = orderId,
                QRImageUrl = GenerateVietQRUrl(amount, orderInfo, orderId),
                QRData = GenerateVietQRData(amount, orderInfo, orderId)
            };
        }
    }

    public class VietQRInfo
    {
        public string BankId { get; set; } = "";
        public string AccountNo { get; set; } = "";
        public string AccountName { get; set; } = "";
        public decimal Amount { get; set; }
        public string OrderInfo { get; set; } = "";
        public string OrderId { get; set; } = "";
        public string QRImageUrl { get; set; } = "";
        public string QRData { get; set; } = "";
    }
}
