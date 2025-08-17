using GymManagement.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymManagement.Web.Controllers
{
    /// <summary>
    /// Controller để fix dữ liệu và tạo các records thiếu
    /// </summary>
    [Authorize(Roles = "Admin")]
    public class DataFixController : Controller
    {
        private readonly DataFixService _dataFixService;
        private readonly ILogger<DataFixController> _logger;

        public DataFixController(DataFixService dataFixService, ILogger<DataFixController> logger)
        {
            _dataFixService = dataFixService;
            _logger = logger;
        }

        /// <summary>
        /// Trang chính để quản lý data fix
        /// </summary>
        public async Task<IActionResult> Index()
        {
            var status = await _dataFixService.GetPaymentDataStatusAsync();
            return View(status);
        }

        /// <summary>
        /// API để tạo các ThanhToan records thiếu
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateMissingPayments()
        {
            try
            {
                _logger.LogInformation("🔧 Admin requested to create missing payment records");
                
                var (created, totalAmount) = await _dataFixService.CreateMissingPaymentRecordsAsync();
                
                return Json(new
                {
                    success = true,
                    message = $"✅ Đã tạo thành công {created} bản ghi thanh toán với tổng giá trị {totalAmount:N0} VND",
                    created = created,
                    totalAmount = totalAmount
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error creating missing payment records");
                return Json(new
                {
                    success = false,
                    message = $"❌ Lỗi khi tạo bản ghi thanh toán: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// API để lấy trạng thái dữ liệu thanh toán
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetPaymentStatus()
        {
            try
            {
                var status = await _dataFixService.GetPaymentDataStatusAsync();
                return Json(new { success = true, data = status });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error getting payment status");
                return Json(new
                {
                    success = false,
                    message = $"❌ Lỗi khi lấy trạng thái: {ex.Message}"
                });
            }
        }
    }
}
