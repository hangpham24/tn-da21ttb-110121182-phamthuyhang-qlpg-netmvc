using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using GymManagement.Web.Models;
using GymManagement.Web.Services;
using Microsoft.AspNetCore.Authorization;
using GymManagement.Web.Data.Models;
using GymManagement.Web.Data;
using GymManagement.Web.Models.DTOs;
using GymManagement.Web.Models.ViewModels;
using GymManagement.Web.Scripts;

namespace GymManagement.Web.Controllers;

public class HomeController : BaseController
{
    private readonly IBaoCaoService _baoCaoService;
    private readonly IGoiTapService _goiTapService;
    private readonly ILopHocService _lopHocService;
    private readonly IAuthService _authService;
    private readonly GymDbContext _context;
    private readonly IDangKyService _dangKyService;
    private readonly IDiemDanhService _diemDanhService;
    private readonly IBookingService _bookingService;
    private readonly IThanhToanService _thanhToanService;
    private readonly IThongBaoService _thongBaoService;
    private readonly ITinTucService _tinTucService;

    public HomeController(
        IUserSessionService userSessionService,
        ILogger<HomeController> logger,
        IBaoCaoService baoCaoService,
        IGoiTapService goiTapService,
        ILopHocService lopHocService,
        IAuthService authService,
        GymDbContext context,
        IDangKyService dangKyService,
        IDiemDanhService diemDanhService,
        IBookingService bookingService,
        IThanhToanService thanhToanService,
        IThongBaoService thongBaoService,
        ITinTucService tinTucService) : base(userSessionService, logger)
    {
        _baoCaoService = baoCaoService;
        _goiTapService = goiTapService;
        _lopHocService = lopHocService;
        _authService = authService;
        _context = context;
        _dangKyService = dangKyService;
        _diemDanhService = diemDanhService;
        _bookingService = bookingService;
        _thanhToanService = thanhToanService;
        _thongBaoService = thongBaoService;
        _tinTucService = tinTucService;
    }

    public async Task<IActionResult> Index()
    {
        try
        {
            if (_userSessionService.IsUserAuthenticated())
            {
                // Check user role to determine which view to show
                if (IsInRoleSafe("Admin"))
                {
                    // For admin users, redirect to dashboard
                    return RedirectToAction("Dashboard");
                }
                // Members will see the home page with member-specific content
            }

        // Load popular packages separately
        try
        {
            var popularPackages = await _goiTapService.GetPopularPackagesAsync();

            // If user is authenticated and is a member, filter out registered packages
            if (_userSessionService.IsUserAuthenticated() && IsInRoleSafe("Member"))
            {
                var nguoiDungId = GetCurrentNguoiDungIdSafe();
                if (nguoiDungId.HasValue && nguoiDungId.Value > 0)
                {
                    // Get user's active registrations
                    var userRegistrations = await _dangKyService.GetActiveRegistrationsByMemberIdAsync(nguoiDungId.Value);
                    var registeredPackageIds = userRegistrations
                        .Where(r => r.GoiTapId.HasValue)
                        .Select(r => r.GoiTapId!.Value)
                        .ToHashSet();

                    // Filter out already registered packages
                    popularPackages = popularPackages.Where(p => !registeredPackageIds.Contains(p.GoiTapId)).ToList();
                }
            }

            ViewBag.Packages = popularPackages.Take(6).ToList(); // Show top 6 available packages
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while loading popular packages");
            ViewBag.Packages = new List<GoiTapDto>();
        }

        // Load classes separately
        try
        {
            var classes = await _lopHocService.GetActiveClassesAsync();
            ViewBag.Classes = classes.Take(4);   // Show top 4 classes
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while loading classes");
            ViewBag.Classes = new List<LopHocDto>();
        }

        // Load latest news separately
        try
        {
            var latestNews = await _tinTucService.GetPublishedAsync();
            ViewBag.News = latestNews.Take(6).ToList(); // Show top 6 latest published news
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while loading latest news");
            ViewBag.News = new List<TinTucPublicDto>();
        }

            return View(); // This will return Index.cshtml (the public home page)
        }
        catch (Exception ex)
        {
            return HandleError(ex, "Có lỗi xảy ra khi tải trang chủ.");
        }
    }

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Dashboard()
    {
        try
        {
            // Verify user has admin role using safe method
            if (!IsInRoleSafe("Admin"))
            {
                return HandleUnauthorized();
            }

            var currentUser = await GetCurrentUserSafeAsync();
            if (currentUser == null)
            {
                return HandleUserNotFound("Dashboard");
            }

            LogUserAction("AccessDashboard");

            var dashboardData = await _baoCaoService.GetDashboardDataAsync();
            return View(dashboardData);
        }
        catch (Exception ex)
        {
            return HandleError(ex, "Có lỗi xảy ra khi tải dashboard.");
        }
    }

    public IActionResult About()
    {
        return View();
    }

    public IActionResult Contact()
    {
        return View();
    }

    public async Task<IActionResult> Packages()
    {
        try
        {
            var packages = await _goiTapService.GetAllAsync();
            return View(packages);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while loading packages");
            return View(new List<GoiTapDto>());
        }
    }

    public async Task<IActionResult> Classes()
    {
        try
        {
            var classes = await _lopHocService.GetActiveClassesAsync();
            var classDtos = new List<LopHocDto>();

            foreach (var c in classes)
            {
                // Get active booking count instead of DangKy count
                var bookingCount = await _bookingService.GetActiveBookingCountAsync(c.LopHocId);

                classDtos.Add(new LopHocDto
                {
                    LopHocId = c.LopHocId,
                    TenLop = c.TenLop,
                    MoTa = c.MoTa,
                    SucChuaToiDa = c.SucChua,
                    ThoiLuongPhut = c.ThoiLuong,
                    MucDo = GetMucDoFromPrice(c.GiaTuyChinh), // Determine level based on price
                    TrangThai = c.TrangThai == "OPEN" ? "ACTIVE" : c.TrangThai,
                    NgayTao = DateTime.Now,
                    TrainerName = c.Hlv != null ? $"{c.Hlv.Ho} {c.Hlv.Ten}".Trim() : "Chưa phân công",
                    RegisteredCount = bookingCount // Now shows active booking count
                });
            }

            return View(classDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while loading classes");
            return View(new List<LopHocDto>());
        }
    }

    private string GetMucDoFromPrice(decimal? price)
    {
        if (!price.HasValue) return "Cơ bản";

        return price.Value switch
        {
            <= 200000 => "Cơ bản",
            <= 250000 => "Trung bình",
            _ => "Nâng cao"
        };
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error(string? message = null)
    {
        ViewBag.ErrorMessage = message ?? TempData["ErrorMessage"];

        // Show detailed error info only in development
        var environment = HttpContext.RequestServices.GetService<IWebHostEnvironment>();
        if (environment?.IsDevelopment() == true)
        {
            ViewBag.ShowDetails = true;
            // You can add more debug info here if needed
        }

        return View();
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetRealtimeStats()
    {
        try
        {
            var stats = await _baoCaoService.GetRealtimeStatsAsync();
            return Json(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting realtime stats");
            return Json(new { });
        }
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SeedData()
    {
        try
        {
            await DbInitializer.InitializeAsync(_context, _authService);
            return Json(new { success = true, message = "Dữ liệu mẫu đã được tạo thành công!" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while seeding data");
            return Json(new { success = false, message = ex.Message });
        }
    }

    // Member Dashboard - accessible for all authenticated users
    [Authorize]
    public async Task<IActionResult> MemberDashboard()
    {
        try
        {
            // Get current user info
            var userIdClaim = User.FindFirst("NguoiDungId")?.Value;
            var taiKhoanId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim))
            {
                // Try to refresh user claims if NguoiDungId is missing
                if (!string.IsNullOrEmpty(taiKhoanId))
                {
                    _logger.LogWarning("NguoiDungId claim missing for user {TaiKhoanId}, attempting to refresh claims", taiKhoanId);

                    // Redirect to a refresh endpoint or show a message
                    TempData["InfoMessage"] = "Đang cập nhật thông tin người dùng, vui lòng thử lại.";
                    return RedirectToAction("RefreshUserClaims");
                }

                TempData["ErrorMessage"] = "Không tìm thấy thông tin người dùng. Vui lòng đăng nhập lại.";
                return RedirectToAction("Logout", "Auth");
            }

            var memberId = int.Parse(userIdClaim);

            // Get member info and stats
            ViewBag.MemberId = memberId;
            ViewBag.UserName = User.Identity?.Name;
            ViewBag.TaiKhoanId = taiKhoanId;

            // Load comprehensive dashboard data
            var dashboardModel = new MemberDashboardViewModel();

            // 1. Active Registrations
            var allRegistrations = await _dangKyService.GetByMemberIdAsync(memberId);
            dashboardModel.ActiveRegistrations = allRegistrations
                .Where(d => d.TrangThai == "ACTIVE" && d.NgayKetThuc >= DateOnly.FromDateTime(DateTime.Today))
                .ToList();

            // 2. Upcoming Bookings (next 7 days)
            var upcomingDate = DateOnly.FromDateTime(DateTime.Today.AddDays(7));
            var allBookings = await _bookingService.GetByMemberIdAsync(memberId);
            dashboardModel.UpcomingBookings = allBookings
                .Where(b => b.Ngay >= DateOnly.FromDateTime(DateTime.Today) && 
                           b.Ngay <= upcomingDate &&
                           b.TrangThai == "BOOKED")
                .OrderBy(b => b.Ngay)
                .ThenBy(b => b.LopHoc?.GioBatDau)
                .Take(5)
                .ToList();

            // 3. Skip attendance/check-in data - handled at reception station

            // 4. Unread Notifications
            dashboardModel.UnreadNotifications = (await _thongBaoService.GetUnreadByUserIdAsync(memberId))
                .Take(5)
                .ToList();

            // 5. Skip attendance statistics - handled at reception station
            var currentMonth = DateTime.Now.Month;
            var currentYear = DateTime.Now.Year;

            // 6. Payment Statistics
            var memberPayments = await _thanhToanService.GetByMemberIdAsync(memberId);
            dashboardModel.PaymentStats.ThisMonthSpent = memberPayments
                .Where(p => p.NgayThanhToan.Month == currentMonth && 
                           p.NgayThanhToan.Year == currentYear &&
                           p.TrangThai == "SUCCESS")
                .Sum(p => p.SoTien);
            
            dashboardModel.PaymentStats.TotalSpent = memberPayments
                .Where(p => p.TrangThai == "SUCCESS")
                .Sum(p => p.SoTien);

            dashboardModel.PaymentStats.PendingPayments = allRegistrations
                .Count(r => r.TrangThai == "PENDING_PAYMENT");

            dashboardModel.PaymentStats.LastPayment = memberPayments
                .Where(p => p.TrangThai == "SUCCESS")
                .OrderByDescending(p => p.NgayThanhToan)
                .FirstOrDefault();

            // 7. Basic member info
            dashboardModel.MemberName = User.Identity?.Name ?? "";
            dashboardModel.LastLoginTime = DateTime.Now; // Could be tracked in database
            dashboardModel.TotalWorkoutDays = 0; // Remove attendance dependency
            dashboardModel.TotalSpent = dashboardModel.PaymentStats.TotalSpent;
            dashboardModel.CurrentMembershipStatus = dashboardModel.ActiveRegistrations.Any() ? "ACTIVE" : "INACTIVE";

            // 8. Recommended classes (based on member's interests or popular classes)
            var activeClasses = await _lopHocService.GetActiveClassesAsync();
            dashboardModel.RecommendedClasses = activeClasses
                .Where(c => c.TrangThai == "OPEN")
                .Take(3)
                .ToList();

            // 9. Quick Stats
            dashboardModel.QuickStats.ActiveRegistrations = dashboardModel.ActiveRegistrations.Count;
            dashboardModel.QuickStats.UpcomingBookings = dashboardModel.UpcomingBookings.Count;
            dashboardModel.QuickStats.UnreadNotifications = dashboardModel.UnreadNotifications.Count;
            // Removed CheckInsThisWeek and AttendanceStats - not needed
            dashboardModel.QuickStats.HasPendingPayments = dashboardModel.PaymentStats.PendingPayments > 0;

            // Next class time
            var nextBooking = dashboardModel.UpcomingBookings.FirstOrDefault();
            if (nextBooking != null && nextBooking.LopHoc != null)
            {
                dashboardModel.QuickStats.NextClassTime = $"{nextBooking.Ngay:dd/MM} {nextBooking.LopHoc.GioBatDau:HH:mm}";
            }

            return View(dashboardModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while loading member dashboard");
            TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải dashboard.";
            return RedirectToAction("Index");
        }
    }

    // Refresh user claims endpoint
    [Authorize]
    public async Task<IActionResult> RefreshUserClaims()
    {
        try
        {
            var taiKhoanId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(taiKhoanId))
            {
                TempData["ErrorMessage"] = "Không thể xác định người dùng.";
                return RedirectToAction("Logout", "Auth");
            }

            // Get user from database and recreate claims
            var user = await _authService.GetUserByIdAsync(taiKhoanId);
            if (user != null)
            {
                var principal = await _authService.CreateClaimsPrincipalAsync(user);
                await HttpContext.SignInAsync("Cookies", principal);

                TempData["SuccessMessage"] = "Thông tin người dùng đã được cập nhật.";
                return RedirectToAction("MemberDashboard");
            }
            else
            {
                TempData["ErrorMessage"] = "Không tìm thấy thông tin tài khoản.";
                return RedirectToAction("Logout", "Auth");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing user claims");
            TempData["ErrorMessage"] = "Có lỗi xảy ra khi cập nhật thông tin.";
            return RedirectToAction("Index");
        }
    }

    // Debug action to check user claims - ADMIN ONLY for security
    [Authorize(Roles = "Admin")]
    public IActionResult DebugClaims()
    {
        var claims = User.Claims.Select(c => new { c.Type, c.Value }).ToList();
        var roles = User.Claims.Where(c => c.Type == System.Security.Claims.ClaimTypes.Role).Select(c => c.Value).ToList();
        var isInMemberRole = User.IsInRole("Member");

        ViewBag.Claims = claims;
        ViewBag.Roles = roles;
        ViewBag.IsInMemberRole = isInMemberRole;
        ViewBag.IsAuthenticated = User.Identity?.IsAuthenticated;
        ViewBag.UserName = User.Identity?.Name;

        return Json(new {
            claims = claims,
            roles = roles,
            isInMemberRole = isInMemberRole,
            isAuthenticated = User.Identity?.IsAuthenticated,
            userName = User.Identity?.Name
        });
    }

    // Tin tức - Public Actions
    public async Task<IActionResult> TinTuc(string searchTerm = "", int page = 1, int pageSize = 9)
    {
        try
        {
            var allTinTuc = string.IsNullOrWhiteSpace(searchTerm) 
                ? await _tinTucService.GetPublishedAsync()
                : await _tinTucService.SearchAsync(searchTerm);

            // Pagination
            var totalCount = allTinTuc.Count();
            var tinTucs = allTinTuc
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.SearchTerm = searchTerm;
            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalCount = totalCount;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalCount / pageSize);
            ViewBag.NoiBatTinTucs = await _tinTucService.GetNoiBatAsync(5);

            return View(tinTucs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading tin tuc list");
            return View(new List<TinTucPublicDto>());
        }
    }

    public async Task<IActionResult> ChiTietTinTuc(string slug)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(slug))
            {
                return NotFound();
            }

            var tinTuc = await _tinTucService.GetPublishedBySlugAsync(slug);
            if (tinTuc == null)
            {
                return NotFound();
            }

            // Increment view count
            await _tinTucService.IncrementViewAsync(tinTuc.TinTucId);

            // Get related news
            ViewBag.RelatedTinTucs = await _tinTucService.GetRelatedAsync(tinTuc.TinTucId, 4);

            return View(tinTuc);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading tin tuc detail for slug: {Slug}", slug);
            return NotFound();
        }
    }


}
