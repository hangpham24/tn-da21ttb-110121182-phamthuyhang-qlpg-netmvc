using GymManagement.Web.Data;
using GymManagement.Web.Data.Models;
using GymManagement.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GymManagement.Web.Controllers
{
    [Authorize]
    public class ThanhToanController : Controller
    {
        private readonly IThanhToanService _thanhToanService;
        private readonly IDangKyService _dangKyService;
        private readonly ILogger<ThanhToanController> _logger;
        private readonly IAuthService _authService;
        private readonly GymDbContext _context;

        public ThanhToanController(
            IThanhToanService thanhToanService,
            IDangKyService dangKyService,
            ILogger<ThanhToanController> logger,
            IAuthService authService,
            GymDbContext context)
        {
            _thanhToanService = thanhToanService;
            _dangKyService = dangKyService;
            _logger = logger;
            _authService = authService;
            _context = context;
        }

        // Helper method to get current user
        private async Task<TaiKhoan?> GetCurrentUserAsync()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return null;

            return await _authService.GetUserByIdAsync(userId);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index()
        {
            try
            {
                var payments = await _thanhToanService.GetAllAsync();
                return View(payments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting payments");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải danh sách thanh toán.";
                return View(new List<ThanhToan>());
            }
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var payment = await _thanhToanService.GetByIdAsync(id);
                if (payment == null)
                {
                    return NotFound();
                }
                return View(payment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting payment details for ID: {Id}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải thông tin thanh toán.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Member")]
        public async Task<IActionResult> CreatePayment(int registrationId, decimal amount, string method, string? note = null)
        {
            try
            {
                // Verify that the registration belongs to the current user
                var user = await GetCurrentUserAsync();
                if (user?.NguoiDungId == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy thông tin người dùng." });
                }

                // Check if registration exists and belongs to current user
                var registration = await _context.DangKys
                    .FirstOrDefaultAsync(d => d.DangKyId == registrationId && d.NguoiDungId == user.NguoiDungId.Value);

                if (registration == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy đăng ký hoặc bạn không có quyền thanh toán cho đăng ký này." });
                }

                if (registration.TrangThai != "PENDING_PAYMENT")
                {
                    return Json(new { success = false, message = "Đăng ký này không cần thanh toán." });
                }

                var payment = await _thanhToanService.CreatePaymentAsync(registrationId, amount, method, note);
                
                if (method == "VNPAY")
                {
                    return Json(new { success = true, thanhToanId = payment.ThanhToanId });
                }
                else if (method == "CASH")
                {
                    // Process cash payment immediately
                    var success = await _thanhToanService.ProcessCashPaymentAsync(payment.ThanhToanId);
                    if (success)
                    {
                        return Redirect("/Member/MyRegistrations?paymentStatus=success&message=Thanh+toán+tiền+mặt+thành+công");
                    }
                    else
                    {
                        return Redirect("/Member/MyRegistrations?paymentStatus=error&message=Có+lỗi+xảy+ra+khi+xử+lý+thanh+toán");
                    }
                }
                
                return Json(new { success = true, message = "Tạo thanh toán thành công!", paymentId = payment.ThanhToanId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating payment");
                return Json(new { success = false, message = "Có lỗi xảy ra khi tạo thanh toán." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Member")]
        public async Task<IActionResult> CreatePackagePayment(int goiTapId, int thoiHanThang, int? khuyenMaiId = null)
        {
            try
            {
                var user = await GetCurrentUserAsync();
                if (user?.NguoiDungId == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy thông tin người dùng." });
                }

                // Check if user already has active package
                if (await _dangKyService.HasActivePackageRegistrationAsync(user.NguoiDungId.Value))
                {
                    return Json(new { success = false, message = "Bạn đã có gói tập đang hoạt động. Mỗi thành viên chỉ có thể sở hữu một gói tập tại một thời điểm." });
                }

                var payment = await _thanhToanService.CreatePaymentForPackageRegistrationAsync(
                    user.NguoiDungId.Value, goiTapId, thoiHanThang, "VNPAY", khuyenMaiId);

                var returnUrl = Url.Action("PaymentConfirm", "Home", new { area = "VNPayAPI" }, Request.Scheme);
                
                return Json(new { success = true, thanhToanId = payment.ThanhToanId, returnUrl = returnUrl });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating package payment");
                return Json(new { success = false, message = "Có lỗi xảy ra khi tạo thanh toán." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Member")]
        public async Task<IActionResult> CreateClassPayment(int lopHocId, DateTime ngayBatDau, DateTime ngayKetThuc)
        {
            try
            {
                var user = await GetCurrentUserAsync();
                if (user?.NguoiDungId == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy thông tin người dùng." });
                }

                // Check if user already has active registration for this class
                if (await _dangKyService.HasActiveClassRegistrationAsync(user.NguoiDungId.Value, lopHocId))
                {
                    return Json(new { success = false, message = "Bạn đã đăng ký lớp học này rồi." });
                }

                var payment = await _thanhToanService.CreatePaymentForClassRegistrationAsync(
                    user.NguoiDungId.Value, lopHocId, ngayBatDau, ngayKetThuc, "VNPAY");

                var returnUrl = Url.Action("PaymentConfirm", "Home", new { area = "VNPayAPI" }, Request.Scheme);
                
                return Json(new { success = true, thanhToanId = payment.ThanhToanId, returnUrl = returnUrl });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating class payment");
                return Json(new { success = false, message = "Có lỗi xảy ra khi tạo thanh toán." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Member")]
        public async Task<IActionResult> CreateFixedClassPayment(int lopHocId)
        {
            try
            {
                var user = await GetCurrentUserAsync();
                if (user?.NguoiDungId == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy thông tin người dùng." });
                }

                // Check if user already has active registration for this class
                if (await _dangKyService.HasActiveClassRegistrationAsync(user.NguoiDungId.Value, lopHocId))
                {
                    return Json(new { success = false, message = "Bạn đã đăng ký lớp học này rồi." });
                }

                var payment = await _thanhToanService.CreatePaymentForFixedClassRegistrationAsync(
                    user.NguoiDungId.Value, lopHocId, "VNPAY");

                var returnUrl = Url.Action("PaymentConfirm", "Home", new { area = "VNPayAPI" }, Request.Scheme);
                
                return Json(new { success = true, thanhToanId = payment.ThanhToanId, returnUrl = returnUrl });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating fixed class payment");
                return Json(new { success = false, message = "Có lỗi xảy ra khi tạo thanh toán." });
            }
        }

        // VnPayReturn method removed - handled by VNPay Area

        private async Task CreateRegistrationAfterSuccessfulPayment(string orderId)
        {
            try
            {
                // Find the payment by gateway order ID
                var gateway = await _thanhToanService.GetGatewayByOrderIdAsync(orderId);
                if (gateway == null) return;

                var registrationInfo = await _thanhToanService.GetRegistrationInfoFromPaymentAsync(gateway.ThanhToanId);
                if (registrationInfo == null) return;

                var (type, info) = registrationInfo.Value;

                switch (type)
                {
                    case "PKG":
                        if (info.ContainsKey("nguoiDungId") && info.ContainsKey("goiTapId") && info.ContainsKey("thoiHanThang"))
                        {
                            var nguoiDungId = int.Parse(info["nguoiDungId"]);
                            var goiTapId = int.Parse(info["goiTapId"]);
                            var thoiHanThang = int.Parse(info["thoiHanThang"]);

                            await _dangKyService.CreatePackageRegistrationAfterPaymentAsync(
                                nguoiDungId, goiTapId, thoiHanThang, gateway.ThanhToanId);
                }
                        break;

                    case "CLS":
                        if (info.ContainsKey("nguoiDungId") && info.ContainsKey("lopHocId") && 
                            info.ContainsKey("ngayBatDau") && info.ContainsKey("ngayKetThuc"))
                        {
                            var nguoiDungId = int.Parse(info["nguoiDungId"]);
                            var lopHocId = int.Parse(info["lopHocId"]);
                            var ngayBatDau = DateTime.Parse(info["ngayBatDau"]);
                            var ngayKetThuc = DateTime.Parse(info["ngayKetThuc"]);

                            await _dangKyService.CreateClassRegistrationAfterPaymentAsync(
                                nguoiDungId, lopHocId, ngayBatDau, ngayKetThuc, gateway.ThanhToanId);
                        }
                        break;

                    case "FIX":
                        if (info.ContainsKey("nguoiDungId") && info.ContainsKey("lopHocId"))
                        {
                            var nguoiDungId = int.Parse(info["nguoiDungId"]);
                            var lopHocId = int.Parse(info["lopHocId"]);

                            await _dangKyService.CreateFixedClassRegistrationAfterPaymentAsync(
                                nguoiDungId, lopHocId, gateway.ThanhToanId);
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating registration after successful payment for order {OrderId}", orderId);
                // Don't throw - payment was successful, just log the error
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ProcessCashPayment(int paymentId)
        {
            try
            {
                var result = await _thanhToanService.ProcessCashPaymentAsync(paymentId);
                if (result)
                {
                    return Json(new { success = true, message = "Xử lý thanh toán tiền mặt thành công!" });
                }
                else
                {
                    return Json(new { success = false, message = "Không thể xử lý thanh toán." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while processing cash payment");
                return Json(new { success = false, message = "Có lỗi xảy ra khi xử lý thanh toán." });
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RefundPayment(int paymentId, string reason)
        {
            try
            {
                var result = await _thanhToanService.RefundPaymentAsync(paymentId, reason);
                if (result)
                {
                    return Json(new { success = true, message = "Hoàn tiền thành công!" });
                }
                else
                {
                    return Json(new { success = false, message = "Không thể hoàn tiền." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while refunding payment");
                return Json(new { success = false, message = "Có lỗi xảy ra khi hoàn tiền." });
            }
        }

        public async Task<IActionResult> PendingPayments()
        {
            try
            {
                var payments = await _thanhToanService.GetPendingPaymentsAsync();
                return View(payments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting pending payments");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải danh sách thanh toán chờ xử lý.";
                return View(new List<ThanhToan>());
            }
        }

        public async Task<IActionResult> SuccessfulPayments()
        {
            try
            {
                var payments = await _thanhToanService.GetSuccessfulPaymentsAsync();
                return View(payments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting successful payments");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải danh sách thanh toán thành công.";
                return View(new List<ThanhToan>());
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetPaymentsByRegistration(int registrationId)
        {
            try
            {
                var payments = await _thanhToanService.GetByRegistrationIdAsync(registrationId);
                return Json(payments.Select(p => new {
                    id = p.ThanhToanId,
                    amount = p.SoTien,
                    method = p.PhuongThuc,
                    status = p.TrangThai,
                    date = p.NgayThanhToan.ToString("dd/MM/yyyy HH:mm"),
                    note = p.GhiChu
                }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting payments by registration");
                return Json(new List<object>());
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetRevenue(DateTime startDate, DateTime endDate)
        {
            try
            {
                // ✅ INPUT VALIDATION: Validate date parameters
                var validationResult = ValidateDateRange(startDate, endDate);
                if (!validationResult.IsValid)
                {
                    return Json(new { success = false, message = validationResult.ErrorMessage });
                }

                var revenue = await _thanhToanService.GetTotalRevenueAsync(startDate, endDate);
                return Json(new {
                    success = true,
                    revenue = revenue,
                    formattedRevenue = revenue.ToString("N0") + " VNĐ"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting revenue for range {StartDate} to {EndDate}", startDate, endDate);
                return Json(new { success = false, message = "Có lỗi xảy ra khi tính doanh thu." });
            }
        }

        public IActionResult PaymentForm(int registrationId)
        {
            ViewBag.RegistrationId = registrationId;
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetRegistrationInfo(int registrationId)
        {
            try
            {
                var registration = await _dangKyService.GetByIdAsync(registrationId);
                if (registration == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy đăng ký." });
                }

                return Json(new {
                    success = true,
                    memberName = $"{registration.NguoiDung?.Ho} {registration.NguoiDung?.Ten}",
                    packageName = registration.GoiTap?.TenGoi ?? registration.LopHoc?.TenLop,
                    startDate = registration.NgayBatDau.ToString("dd/MM/yyyy"),
                    endDate = registration.NgayKetThuc.ToString("dd/MM/yyyy"),
                    status = registration.TrangThai
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting registration info");
                return Json(new { success = false, message = "Có lỗi xảy ra khi tải thông tin đăng ký." });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetPaymentHistory(int registrationId)
        {
            try
            {
                var payments = await _thanhToanService.GetByRegistrationIdAsync(registrationId);
                
                return Json(new {
                    success = true,
                    payments = payments.Select(p => new {
                        thanhToanId = p.ThanhToanId,
                        soTien = p.SoTien,
                        phuongThuc = p.PhuongThuc,
                        trangThai = p.TrangThai,
                        ngayThanhToan = p.NgayThanhToan.ToString("yyyy-MM-ddTHH:mm:ss"),
                        ghiChu = p.GhiChu
                    }).OrderByDescending(p => p.ngayThanhToan)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting payment history");
                return Json(new { success = false, message = "Có lỗi xảy ra khi tải lịch sử thanh toán." });
            }
        }

        #region ✅ INPUT VALIDATION HELPERS

        /// <summary>
        /// Validates date range parameters
        /// </summary>
        private (bool IsValid, string ErrorMessage) ValidateDateRange(DateTime startDate, DateTime endDate)
        {
            // Check if dates are valid
            if (startDate == default || endDate == default)
            {
                return (false, "Ngày bắt đầu và ngày kết thúc không được để trống.");
            }

            // Check if start date is not after end date
            if (startDate > endDate)
            {
                return (false, "Ngày bắt đầu không được lớn hơn ngày kết thúc.");
            }

            // Check if date range is not too far in the future
            if (startDate > DateTime.Today.AddDays(1))
            {
                return (false, "Ngày bắt đầu không được vượt quá ngày mai.");
            }

            // Check if date range is not too far in the past (5 years)
            if (startDate < DateTime.Today.AddYears(-5))
            {
                return (false, "Ngày bắt đầu không được quá 5 năm trước.");
            }

            // Check if date range is not too large (max 2 years)
            if ((endDate - startDate).TotalDays > 730)
            {
                return (false, "Khoảng thời gian không được vượt quá 2 năm.");
            }

            return (true, string.Empty);
        }

        #endregion
    }
}
