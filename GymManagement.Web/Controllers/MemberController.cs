using GymManagement.Web.Data.Models;
using GymManagement.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GymManagement.Web.Controllers
{
    [Authorize(Roles = "Member")]
    public class MemberController : BaseController
    {
        private readonly IGoiTapService _goiTapService;
        private readonly ILopHocService _lopHocService;
        private readonly IDangKyService _dangKyService;
        private readonly INguoiDungService _nguoiDungService;
        private readonly IMemberBenefitService _memberBenefitService;

        public MemberController(
            IUserSessionService userSessionService,
            ILogger<MemberController> logger,
            IGoiTapService goiTapService,
            ILopHocService lopHocService,
            IDangKyService dangKyService,
            INguoiDungService nguoiDungService,
            IMemberBenefitService memberBenefitService) : base(userSessionService, logger)
        {
            _goiTapService = goiTapService;
            _lopHocService = lopHocService;
            _dangKyService = dangKyService;
            _nguoiDungService = nguoiDungService;
            _memberBenefitService = memberBenefitService;
        }

        /// <summary>
        /// API lấy thông tin quyền lợi của member - Logic đơn giản
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetMemberBenefits()
        {
            try
            {
                var nguoiDungIdClaim = User.FindFirst("NguoiDungId")?.Value;
                if (string.IsNullOrEmpty(nguoiDungIdClaim))
                {
                    return Json(new { success = false, message = "Không tìm thấy thông tin người dùng." });
                }

                var memberId = int.Parse(nguoiDungIdClaim);
                var benefits = await _memberBenefitService.GetMemberBenefitsAsync(memberId);

                return Json(new
                {
                    success = true,
                    data = benefits
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting member benefits");
                return Json(new { success = false, message = "Có lỗi xảy ra khi lấy thông tin quyền lợi." });
            }
        }



        // Xem tất cả gói tập (chỉ hiển thị những gói chưa đăng ký)
        public async Task<IActionResult> Packages()
        {
            try
            {
                // Lấy tất cả gói tập active
                var allPackages = await _goiTapService.GetActivePackagesAsync();

                // Lấy thông tin member hiện tại
                var nguoiDungIdClaim = User.FindFirst("NguoiDungId")?.Value;
                if (string.IsNullOrEmpty(nguoiDungIdClaim))
                {
                    // Nếu không có thông tin member, hiển thị tất cả
                    ViewBag.TotalPackages = allPackages.Count();
                    ViewBag.HasActivePackage = false;
                    ViewBag.RegisteredPackages = 0;
                    ViewBag.AvailablePackages = allPackages.Count();
                    return View(allPackages);
                }

                var memberId = int.Parse(nguoiDungIdClaim);

                // Lấy danh sách gói đã đăng ký (bao gồm cả ACTIVE và PENDING)
                var userRegistrations = await _dangKyService.GetRegistrationsByMemberIdAsync(memberId);
                var registeredPackageIds = userRegistrations
                    .Where(r => r.GoiTapId.HasValue && (
                        // ACTIVE registrations that haven't expired
                        (r.TrangThai == "ACTIVE" && r.NgayKetThuc >= DateOnly.FromDateTime(DateTime.Today)) ||
                        // PENDING registrations (awaiting payment)
                        r.TrangThai == "PENDING"
                    ))
                    .Select(r => r.GoiTapId!.Value)
                    .ToHashSet();

                // Lọc ra những gói chưa đăng ký (loại bỏ cả ACTIVE và PENDING)
                var availablePackages = allPackages.Where(p => !registeredPackageIds.Contains(p.GoiTapId)).ToList();

                // Check if user has any active package (only ACTIVE, not PENDING)
                var hasActivePackage = userRegistrations.Any(r =>
                    r.GoiTapId.HasValue &&
                    r.TrangThai == "ACTIVE" &&
                    r.NgayKetThuc >= DateOnly.FromDateTime(DateTime.Today));

                ViewBag.TotalPackages = allPackages.Count();
                ViewBag.HasActivePackage = hasActivePackage;
                ViewBag.RegisteredPackages = registeredPackageIds.Count;
                ViewBag.AvailablePackages = availablePackages.Count();

                // Debug logging
                _logger.LogInformation($"Debug Packages: Total={allPackages.Count()}, Registered={registeredPackageIds.Count}, Available={availablePackages.Count()}, HasActive={hasActivePackage}");
                _logger.LogInformation($"RegisteredPackageIds: [{string.Join(", ", registeredPackageIds)}]");

                return View(availablePackages);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while loading packages");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải danh sách gói tập.";
                return View(new List<GoiTapDto>());
            }
        }

        // Chi tiết gói tập
        public async Task<IActionResult> PackageDetails(int id)
        {
            try
            {
                var package = await _goiTapService.GetByIdAsync(id);
                if (package == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy gói tập.";
                    return RedirectToAction(nameof(Packages));
                }

                // Kiểm tra registration status của user hiện tại
                var nguoiDungIdClaim = User.FindFirst("NguoiDungId")?.Value;
                if (!string.IsNullOrEmpty(nguoiDungIdClaim))
                {
                    var memberId = int.Parse(nguoiDungIdClaim);
                    var userRegistrations = await _dangKyService.GetByMemberIdAsync(memberId);
                    var isRegistered = userRegistrations.Any(r =>
                        r.GoiTapId == id &&
                        r.TrangThai == "ACTIVE" &&
                        r.NgayKetThuc >= DateOnly.FromDateTime(DateTime.Today));

                    ViewBag.IsUserRegistered = isRegistered;

                    if (isRegistered)
                    {
                        var userRegistration = userRegistrations.First(r =>
                            r.GoiTapId == id &&
                            r.TrangThai == "ACTIVE" &&
                            r.NgayKetThuc >= DateOnly.FromDateTime(DateTime.Today));
                        ViewBag.UserRegistration = userRegistration;
                    }
                }
                else
                {
                    ViewBag.IsUserRegistered = false;
                }

                return View(package);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while loading package details");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải thông tin gói tập.";
                return RedirectToAction(nameof(Packages));
            }
        }

        // Xem lớp học cố định
        public async Task<IActionResult> FixedClasses()
        {
            try
            {
                // Lấy tất cả lớp học có lịch trình cố định
                var allClasses = await _lopHocService.GetActiveClassesAsync();
                var fixedClasses = allClasses.Where(c => c.IsFixedSchedule).ToList();

                // Lấy thông tin số lượng đăng ký hiện tại cho mỗi lớp
                var registrationCounts = new Dictionary<int, int>();
                foreach (var lopHoc in fixedClasses)
                {
                    var count = await _dangKyService.GetActiveClassRegistrationCountAsync(lopHoc.LopHocId);
                    registrationCounts[lopHoc.LopHocId] = count;
                }

                ViewBag.CurrentRegistrations = registrationCounts;
                return View(fixedClasses);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while loading fixed classes");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải danh sách lớp học cố định.";
                return View(new List<LopHoc>());
            }
        }

        // Xem tất cả lớp học (chỉ hiển thị những lớp chưa đăng ký)
        public async Task<IActionResult> Classes()
        {
            try
            {
                // Lấy tất cả lớp học active
                var allClasses = await _lopHocService.GetActiveClassesAsync();

                // Lấy thông tin member hiện tại
                var nguoiDungIdClaim = User.FindFirst("NguoiDungId")?.Value;
                if (string.IsNullOrEmpty(nguoiDungIdClaim))
                {
                    // Nếu không có thông tin member, hiển thị tất cả
                    return View(allClasses);
                }

                var memberId = int.Parse(nguoiDungIdClaim);

                // 🧘‍♂️ CHÍNH SÁCH: Thành viên có thể đăng ký nhiều lớp học cùng lúc
                var userRegistrations = await _dangKyService.GetByMemberIdAsync(memberId);
                var activeClassIds = userRegistrations
                    .Where(r => r.LopHocId != null &&
                               r.TrangThai == "ACTIVE" &&
                               r.NgayKetThuc >= DateOnly.FromDateTime(DateTime.Today))
                    .Select(r => r.LopHocId.Value)
                    .ToHashSet();

                // Lọc ra những lớp học mà member chưa đăng ký (để tránh đăng ký trùng)
                var availableClasses = new List<LopHoc>();

                foreach (var lopHoc in allClasses)
                {
                    // Kiểm tra xem có thể đăng ký lớp này không (không trùng lịch, không đầy, chưa đăng ký)
                    if (await _dangKyService.CanRegisterClassAsync(memberId, lopHoc.LopHocId))
                    {
                        availableClasses.Add(lopHoc);
                    }
                }

                // Thêm thông tin để hiển thị
                ViewBag.TotalClasses = allClasses.Count();
                ViewBag.RegisteredClasses = activeClassIds.Count;
                ViewBag.AvailableClasses = availableClasses.Count;

                return View(availableClasses);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while loading classes");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải danh sách lớp học.";
                return View(new List<LopHoc>());
            }
        }

        // Chi tiết lớp học
        public async Task<IActionResult> ClassDetails(int id)
        {
            try
            {
                var lopHoc = await _lopHocService.GetByIdAsync(id);
                if (lopHoc == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy lớp học.";
                    return RedirectToAction(nameof(Classes));
                }

                // Kiểm tra registration status của user hiện tại
                var nguoiDungIdClaim = User.FindFirst("NguoiDungId")?.Value;
                if (!string.IsNullOrEmpty(nguoiDungIdClaim))
                {
                    var memberId = int.Parse(nguoiDungIdClaim);
                    var userRegistrations = await _dangKyService.GetByMemberIdAsync(memberId);
                    var isRegistered = userRegistrations.Any(r =>
                        r.LopHocId == id &&
                        r.TrangThai == "ACTIVE" &&
                        r.NgayKetThuc >= DateOnly.FromDateTime(DateTime.Today));

                    ViewBag.IsUserRegistered = isRegistered;

                    if (isRegistered)
                    {
                        var userRegistration = userRegistrations.First(r =>
                            r.LopHocId == id &&
                            r.TrangThai == "ACTIVE" &&
                            r.NgayKetThuc >= DateOnly.FromDateTime(DateTime.Today));
                        ViewBag.UserRegistration = userRegistration;
                    }
                }
                else
                {
                    ViewBag.IsUserRegistered = false;
                }

                return View(lopHoc);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while loading class details");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải thông tin lớp học.";
                return RedirectToAction(nameof(Classes));
            }
        }

        // Đăng ký của tôi
        public async Task<IActionResult> MyRegistrations()
        {
            try
            {
                var nguoiDungIdClaim = User.FindFirst("NguoiDungId")?.Value;
                if (string.IsNullOrEmpty(nguoiDungIdClaim))
                {
                    TempData["ErrorMessage"] = "Không tìm thấy thông tin người dùng.";
                    return RedirectToAction("Login", "Auth");
                }

                var memberId = int.Parse(nguoiDungIdClaim);
                var registrations = await _dangKyService.GetByMemberIdAsync(memberId);
                
                return View(registrations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while loading member registrations");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải danh sách đăng ký.";
                return View(new List<DangKy>());
            }
        }

        // Đăng ký gói tập
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterPackage(int packageId, int duration)
        {
            try
            {
                var nguoiDungIdClaim = User.FindFirst("NguoiDungId")?.Value;
                if (string.IsNullOrEmpty(nguoiDungIdClaim))
                {
                    return Json(new { success = false, message = "Không tìm thấy thông tin người dùng." });
                }

                var memberId = int.Parse(nguoiDungIdClaim);
                var result = await _dangKyService.RegisterPackageAsync(memberId, packageId, duration);
                
                if (result)
                {
                    return Json(new { success = true, message = "Đăng ký gói tập thành công!" });
                }
                else
                {
                    return Json(new { success = false, message = "Không thể đăng ký gói tập. Bạn đã có gói tập đang hoạt động. Mỗi thành viên chỉ có thể sở hữu một gói tập tại một thời điểm." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while registering package");
                return Json(new { success = false, message = "Có lỗi xảy ra khi đăng ký gói tập." });
            }
        }

        // Đăng ký lớp học
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Member")] // Ensure only members can register
        public async Task<IActionResult> RegisterClass(int classId, DateTime startDate, DateTime endDate)
        {
            try
            {
                // Input validation
                if (classId <= 0)
                {
                    return Json(new { success = false, message = "ID lớp học không hợp lệ." });
                }

                if (startDate < DateTime.Today)
                {
                    return Json(new { success = false, message = "Ngày bắt đầu không thể là ngày trong quá khứ." });
                }

                if (endDate <= startDate)
                {
                    return Json(new { success = false, message = "Ngày kết thúc phải sau ngày bắt đầu." });
                }

                var nguoiDungIdClaim = User.FindFirst("NguoiDungId")?.Value;
                if (string.IsNullOrEmpty(nguoiDungIdClaim) || !int.TryParse(nguoiDungIdClaim, out int memberId))
                {
                    return Json(new { success = false, message = "Không tìm thấy thông tin người dùng hợp lệ." });
                }

                var result = await _dangKyService.RegisterClassAsync(memberId, classId, startDate, endDate);

                if (result)
                {
                    return Json(new { success = true, message = "Đăng ký lớp học thành công!" });
                }
                else
                {
                    return Json(new { success = false, message = "Không thể đăng ký lớp học. Lớp có thể đã đầy hoặc bạn đã đăng ký rồi." });
                }
            }
            catch (FormatException ex)
            {
                _logger.LogError(ex, "Invalid format in RegisterClass for memberId");
                return Json(new { success = false, message = "Dữ liệu không hợp lệ." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while registering class for memberId: {MemberId}, classId: {ClassId}",
                    User.FindFirst("NguoiDungId")?.Value, classId);
                return Json(new { success = false, message = "Có lỗi xảy ra khi đăng ký lớp học." });
            }
        }

        // Hủy đăng ký
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelRegistration(int registrationId, string reason)
        {
            try
            {
                var nguoiDungIdClaim = User.FindFirst("NguoiDungId")?.Value;
                if (string.IsNullOrEmpty(nguoiDungIdClaim))
                {
                    return Json(new { success = false, message = "Không tìm thấy thông tin người dùng." });
                }

                // Kiểm tra quyền sở hữu đăng ký
                var registration = await _dangKyService.GetByIdAsync(registrationId);
                if (registration == null || registration.NguoiDungId != int.Parse(nguoiDungIdClaim))
                {
                    return Json(new { success = false, message = "Không có quyền hủy đăng ký này." });
                }

                var result = await _dangKyService.CancelRegistrationAsync(registrationId, reason);
                
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

        // Renewal functionality
        [HttpPost]
        public async Task<IActionResult> GetRenewalInfo(int dangKyId, int renewalMonths)
        {
            try
            {
                var canRenew = await _dangKyService.CanRenewRegistrationAsync(dangKyId);
                if (!canRenew)
                {
                    return Json(new { success = false, message = "Không thể gia hạn đăng ký này." });
                }

                var renewalFee = await _dangKyService.CalculateRenewalFeeAsync(dangKyId, renewalMonths);

                return Json(new {
                    success = true,
                    renewalFee = renewalFee,
                    renewalFeeFormatted = renewalFee.ToString("N0") + " VNĐ",
                    renewalMonths = renewalMonths
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting renewal info for DangKyId: {DangKyId}", dangKyId);
                return Json(new { success = false, message = "Có lỗi xảy ra khi tính phí gia hạn." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateRenewalPayment(int dangKyId, int renewalMonths)
        {
            try
            {
                var user = await _userSessionService.GetCurrentUserAsync();
                if (user?.NguoiDungId == null)
                {
                    return Json(new { success = false, message = "Vui lòng đăng nhập để tiếp tục." });
                }

                // Verify user owns this registration
                var dangKy = await _dangKyService.GetByIdAsync(dangKyId);
                if (dangKy == null || dangKy.NguoiDungId != user.NguoiDungId.Value)
                {
                    return Json(new { success = false, message = "Không tìm thấy đăng ký hoặc bạn không có quyền truy cập." });
                }

                var canRenew = await _dangKyService.CanRenewRegistrationAsync(dangKyId);
                if (!canRenew)
                {
                    return Json(new { success = false, message = "Không thể gia hạn đăng ký này." });
                }

                // Create payment for renewal
                var thanhToanService = HttpContext.RequestServices.GetRequiredService<IThanhToanService>();
                var payment = await thanhToanService.CreatePaymentForRenewalAsync(dangKyId, renewalMonths, "VNPAY");

                return Json(new {
                    success = true,
                    thanhToanId = payment.ThanhToanId,
                    message = "Đã tạo thanh toán gia hạn thành công!"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating renewal payment for DangKyId: {DangKyId}", dangKyId);
                return Json(new { success = false, message = "Có lỗi xảy ra khi tạo thanh toán gia hạn." });
            }
        }
    }
}
