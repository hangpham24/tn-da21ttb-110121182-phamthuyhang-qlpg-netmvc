using GymManagement.Web.Data.Models;
using GymManagement.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims;

namespace GymManagement.Web.Controllers
{
    [Authorize]
    public class DangKyController : Controller
    {
        private readonly IDangKyService _dangKyService;
        private readonly IGoiTapService _goiTapService;
        private readonly ILopHocService _lopHocService;
        private readonly INguoiDungService _nguoiDungService;
        private readonly IAuthService _authService;
        private readonly ILogger<DangKyController> _logger;

        public DangKyController(
            IDangKyService dangKyService,
            IGoiTapService goiTapService,
            ILopHocService lopHocService,
            INguoiDungService nguoiDungService,
            IAuthService authService,
            ILogger<DangKyController> logger)
        {
            _dangKyService = dangKyService;
            _goiTapService = goiTapService;
            _lopHocService = lopHocService;
            _nguoiDungService = nguoiDungService;
            _authService = authService;
            _logger = logger;
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
                var registrations = await _dangKyService.GetAllAsync();
                return View(registrations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting registrations");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải danh sách đăng ký.";
                return View(new List<DangKy>());
            }
        }

        // MyRegistrations action đã được chuyển sang MemberController để tránh trùng lặp

        public async Task<IActionResult> RegisterPackage(int? packageId = null, int? duration = null)
        {
            await LoadPackagesSelectList();

            // Pre-select package if provided from Member/Packages
            if (packageId.HasValue && duration.HasValue)
            {
                ViewBag.PreSelectedPackageId = packageId.Value;
                ViewBag.PreSelectedDuration = duration.Value;

                // Get package details for display
                var goiTapService = HttpContext.RequestServices.GetRequiredService<IGoiTapService>();
                var selectedPackage = await goiTapService.GetByIdAsync(packageId.Value);
                if (selectedPackage != null)
                {
                    ViewBag.PreSelectedPackageName = selectedPackage.TenGoi;
                    ViewBag.PreSelectedPackagePrice = selectedPackage.Gia;
                }
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterPackage(int packageId, int duration, string? promotionCode = null)
        {
            try
            {
                var user = await GetCurrentUserAsync();
                if (user?.NguoiDungId == null)
                {
                    return RedirectToAction("Login", "Auth");
                }

                // Validate promotion code if provided
                int? khuyenMaiId = null;
                if (!string.IsNullOrWhiteSpace(promotionCode))
                {
                    var khuyenMaiService = HttpContext.RequestServices.GetRequiredService<IKhuyenMaiService>();
                    var khuyenMai = await khuyenMaiService.GetByCodeAsync(promotionCode);

                    if (khuyenMai != null && await khuyenMaiService.ValidateCodeAsync(promotionCode))
                    {
                        khuyenMaiId = khuyenMai.KhuyenMaiId;
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "Mã khuyến mãi không hợp lệ hoặc đã hết hạn.";
                        await LoadPackagesSelectList();
                        return View();
                    }
                }

                // Instead of registering directly, create payment first
                // This ensures payment is created with promotion code applied
                var thanhToanService = HttpContext.RequestServices.GetRequiredService<IThanhToanService>();
                var payment = await thanhToanService.CreatePaymentForPackageRegistrationAsync(
                    user.NguoiDungId.Value,
                    packageId,
                    duration,
                    "VNPAY",
                    khuyenMaiId);

                if (payment != null)
                {
                    TempData["SuccessMessage"] = "Đã tạo thanh toán thành công! Đang chuyển hướng đến cổng thanh toán...";

                    // Redirect to VNPay payment
                    return RedirectToAction("CreatePayment", "Home", new {
                        area = "VNPayAPI",
                        thanhToanId = payment.ThanhToanId,
                        returnUrl = Url.Action("PaymentReturn", "ThanhToan", null, Request.Scheme)
                    });
                }
                else
                {
                    TempData["ErrorMessage"] = "Không thể đăng ký gói tập. Bạn có thể đã có gói tập đang hoạt động.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while registering package");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi đăng ký gói tập.";
            }

            await LoadPackagesSelectList();
            return View();
        }

        public async Task<IActionResult> RegisterClass()
        {
            await LoadClassesSelectList();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterClass(int classId, DateTime startDate, DateTime endDate)
        {
            try
            {
                var user = await GetCurrentUserAsync();
                if (user?.NguoiDungId == null)
                {
                    return RedirectToAction("Login", "Auth");
                }

                var result = await _dangKyService.RegisterClassAsync(user.NguoiDungId.Value, classId, startDate, endDate);
                if (result)
                {
                    TempData["SuccessMessage"] = "Đăng ký lớp học thành công!";
                    return RedirectToAction("MyRegistrations", "Member");
                }
                else
                {
                    TempData["ErrorMessage"] = "Không thể đăng ký lớp học. Bạn có thể đã đăng ký lớp này rồi.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while registering class");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi đăng ký lớp học.";
            }

            await LoadClassesSelectList();
            return View();
        }

        /// <summary>
        /// Đăng ký lớp học theo mô hình cố định (member tham gia từ đầu khóa)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Member")]
        public async Task<IActionResult> RegisterFixedClass(int classId)
        {
            try
            {
                var user = await GetCurrentUserAsync();
                if (user?.NguoiDungId == null)
                {
                    return RedirectToAction("Login", "Auth");
                }

                var result = await _dangKyService.RegisterFixedClassAsync(user.NguoiDungId.Value, classId);
                if (result)
                {
                    TempData["SuccessMessage"] = "Đăng ký lớp học thành công! Bạn sẽ tham gia từ đầu khóa học.";
                    return RedirectToAction("MyRegistrations", "Member");
                }
                else
                {
                    TempData["ErrorMessage"] = "Không thể đăng ký lớp học. Lớp có thể đã đầy, đã bắt đầu, hoặc bạn đã đăng ký rồi.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while registering fixed class");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi đăng ký lớp học.";
            }

            return RedirectToAction("Index", "Member");
        }

        /// <summary>
        /// Hủy đăng ký cho member
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Member")]
        public async Task<IActionResult> CancelRegistration(int dangKyId, string? lyDoHuy)
        {
            try
            {
                var user = await GetCurrentUserAsync();
                if (user?.NguoiDungId == null)
                {
                    return RedirectToAction("Login", "Auth");
                }

                var result = await _dangKyService.CancelRegistrationAsync(dangKyId, user.NguoiDungId.Value, lyDoHuy);
                if (result)
                {
                    TempData["SuccessMessage"] = "Hủy đăng ký thành công!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Không thể hủy đăng ký. Có thể đã quá thời hạn hủy hoặc đăng ký không tồn tại.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while cancelling registration");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi hủy đăng ký.";
            }

            return RedirectToAction("MyRegistrations", "Member");
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create()
        {
            await LoadSelectLists();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(DangKy dangKy)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    await _dangKyService.CreateAsync(dangKy);
                    TempData["SuccessMessage"] = "Tạo đăng ký thành công!";
                    return RedirectToAction(nameof(Index));
                }
                await LoadSelectLists();
                return View(dangKy);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating registration");
                ModelState.AddModelError("", "Có lỗi xảy ra khi tạo đăng ký.");
                await LoadSelectLists();
                return View(dangKy);
            }
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var dangKy = await _dangKyService.GetByIdAsync(id);
                if (dangKy == null)
                {
                    return NotFound();
                }
                return View(dangKy);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting registration details for ID: {Id}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải thông tin đăng ký.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        public async Task<IActionResult> Extend(int id, int additionalMonths)
        {
            try
            {
                var result = await _dangKyService.ExtendRegistrationAsync(id, additionalMonths);
                if (result)
                {
                    return Json(new { success = true, message = "Gia hạn đăng ký thành công!" });
                }
                else
                {
                    return Json(new { success = false, message = "Không thể gia hạn đăng ký." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while extending registration");
                return Json(new { success = false, message = "Có lỗi xảy ra khi gia hạn đăng ký." });
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Cancel(int id, string reason)
        {
            try
            {
                var result = await _dangKyService.CancelRegistrationAsync(id, reason);
                if (result)
                {
                    return Json(new { success = true, message = "Hủy đăng ký thành công!" });
                }
                else
                {
                    return Json(new { success = false, message = "Không thể hủy đăng ký." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while canceling registration");
                return Json(new { success = false, message = "Có lỗi xảy ra khi hủy đăng ký." });
            }
        }

        [HttpGet]
        public async Task<IActionResult> CalculateFee(int packageId, int duration, int? promotionId = null)
        {
            try
            {
                var fee = await _dangKyService.CalculateRegistrationFeeAsync(packageId, duration, promotionId);
                return Json(new { 
                    success = true, 
                    fee = fee,
                    formattedFee = fee.ToString("N0") + " VNĐ"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while calculating registration fee");
                return Json(new { success = false, message = "Có lỗi xảy ra khi tính phí đăng ký." });
            }
        }

        public async Task<IActionResult> ActiveRegistrations()
        {
            try
            {
                var registrations = await _dangKyService.GetActiveRegistrationsAsync();
                return View(registrations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting active registrations");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải danh sách đăng ký đang hoạt động.";
                return View(new List<DangKy>());
            }
        }

        public async Task<IActionResult> ExpiredRegistrations()
        {
            try
            {
                var registrations = await _dangKyService.GetExpiredRegistrationsAsync();
                return View(registrations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting expired registrations");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải danh sách đăng ký đã hết hạn.";
                return View(new List<DangKy>());
            }
        }

        private async Task LoadSelectLists()
        {
            try
            {
                var packages = await _goiTapService.GetAllAsync();
                var classes = await _lopHocService.GetActiveClassesAsync();
                var members = await _nguoiDungService.GetMembersAsync();

                ViewBag.Packages = new SelectList(packages, "GoiTapId", "TenGoi");
                ViewBag.Classes = new SelectList(classes, "LopHocId", "TenLop");
                ViewBag.Members = new SelectList(members, "NguoiDungId", "Ho");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while loading select lists");
                ViewBag.Packages = new SelectList(new List<GoiTap>(), "GoiTapId", "TenGoi");
                ViewBag.Classes = new SelectList(new List<LopHoc>(), "LopHocId", "TenLop");
                ViewBag.Members = new SelectList(new List<NguoiDung>(), "NguoiDungId", "Ho");
            }
        }

        private async Task LoadPackagesSelectList()
        {
            try
            {
                var packages = await _goiTapService.GetAllAsync();
                ViewBag.Packages = new SelectList(packages, "GoiTapId", "TenGoi");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while loading packages select list");
                ViewBag.Packages = new SelectList(new List<GoiTap>(), "GoiTapId", "TenGoi");
            }
        }

        private async Task LoadClassesSelectList()
        {
            try
            {
                var classes = await _lopHocService.GetActiveClassesAsync();
                ViewBag.Classes = new SelectList(classes, "LopHocId", "TenLop");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while loading classes select list");
                ViewBag.Classes = new SelectList(new List<LopHoc>(), "LopHocId", "TenLop");
            }
        }
    }
}
