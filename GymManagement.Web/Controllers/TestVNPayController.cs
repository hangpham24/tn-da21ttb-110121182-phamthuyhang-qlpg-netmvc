using Microsoft.AspNetCore.Mvc;
using GymManagement.Web.Areas.VNPayAPI;

namespace GymManagement.Web.Controllers
{
    public class TestVNPayController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<TestVNPayController> _logger;

        public TestVNPayController(IConfiguration configuration, ILogger<TestVNPayController> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public IActionResult TestSignature()
        {
            try
            {
                var vnpayConfig = _configuration.GetSection("VnPay");
                var tmnCode = vnpayConfig["TmnCode"];
                var hashSecret = vnpayConfig["HashSecret"];
                var baseUrl = vnpayConfig["BaseUrl"];

                var vnpay = new VnPayLibrary();

                // Test data following official sample exactly
                var createDate = DateTime.Now;
                var orderId = DateTime.Now.Ticks; // Use ticks like official sample

                vnpay.AddRequestData("vnp_Version", VnPayLibrary.VERSION);
                vnpay.AddRequestData("vnp_Command", "pay");
                vnpay.AddRequestData("vnp_TmnCode", tmnCode);
                vnpay.AddRequestData("vnp_Amount", "360000000"); // 3,600,000 VND in cents
                vnpay.AddRequestData("vnp_CurrCode", "VND");
                vnpay.AddRequestData("vnp_TxnRef", orderId.ToString());
                vnpay.AddRequestData("vnp_OrderInfo", "Test thanh toan gym");
                vnpay.AddRequestData("vnp_OrderType", "other");
                vnpay.AddRequestData("vnp_Locale", "vn");
                vnpay.AddRequestData("vnp_ReturnUrl", "http://localhost:5003/VNPayAPI/Home/PaymentConfirm");
                vnpay.AddRequestData("vnp_IpAddr", "127.0.0.1");
                vnpay.AddRequestData("vnp_CreateDate", createDate.ToString("yyyyMMddHHmmss"));

                var paymentUrl = vnpay.CreateRequestUrl(baseUrl, hashSecret);

                _logger.LogInformation($"Test VNPay URL: {paymentUrl}");

                return Json(new
                {
                    success = true,
                    paymentUrl = paymentUrl,
                    tmnCode = tmnCode,
                    hashSecret = hashSecret
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing VNPay signature");
                return Json(new { success = false, error = ex.Message });
            }
        }
    }
}
