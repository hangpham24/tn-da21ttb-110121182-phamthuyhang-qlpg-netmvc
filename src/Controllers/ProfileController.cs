using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using GymManagement.Web.Services;
using GymManagement.Web.Models.DTOs;
using System.Security.Claims;

namespace GymManagement.Web.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly INguoiDungService _nguoiDungService;
        private readonly IDangKyService _dangKyService;
        private readonly IDiemDanhService _diemDanhService;
        private readonly ILogger<ProfileController> _logger;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ProfileController(
            INguoiDungService nguoiDungService, 
            IDangKyService dangKyService,
            IDiemDanhService diemDanhService,
            ILogger<ProfileController> logger, 
            IWebHostEnvironment webHostEnvironment)
        {
            _nguoiDungService = nguoiDungService;
            _dangKyService = dangKyService;
            _diemDanhService = diemDanhService;
            _logger = logger;
            _webHostEnvironment = webHostEnvironment;
        }

        // GET: Profile
        public async Task<IActionResult> Index()
        {
            try
            {
                var nguoiDungIdClaim = User.FindFirst("NguoiDungId")?.Value;
                if (string.IsNullOrEmpty(nguoiDungIdClaim))
                {
                    // Log for debugging
                    var username = User.FindFirst(ClaimTypes.Name)?.Value;
                    _logger.LogWarning("User {Username} does not have NguoiDungId claim", username);

                    TempData["ErrorMessage"] = "Tài khoản của bạn chưa được liên kết với thông tin cá nhân. Vui lòng liên hệ quản trị viên.";
                    return RedirectToAction("Index", "Home");
                }

                if (!int.TryParse(nguoiDungIdClaim, out int nguoiDungId))
                {
                    _logger.LogError("Invalid NguoiDungId claim value: {NguoiDungIdClaim}", nguoiDungIdClaim);
                    TempData["ErrorMessage"] = "Thông tin tài khoản không hợp lệ. Vui lòng đăng nhập lại.";
                    return RedirectToAction("Login", "Auth");
                }

                var user = await _nguoiDungService.GetByIdAsync(nguoiDungId);
                if (user == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy thông tin người dùng.";
                    return RedirectToAction("Index", "Home");
                }

                return View(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while loading user profile");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải thông tin cá nhân.";
                return RedirectToAction("Index", "Home");
            }
        }

        // GET: Profile/Edit
        public async Task<IActionResult> Edit()
        {
            try
            {
                var nguoiDungIdClaim = User.FindFirst("NguoiDungId")?.Value;
                if (string.IsNullOrEmpty(nguoiDungIdClaim))
                {
                    TempData["ErrorMessage"] = "Không tìm thấy thông tin người dùng trong phiên đăng nhập.";
                    return RedirectToAction("Login", "Auth");
                }

                if (!int.TryParse(nguoiDungIdClaim, out int nguoiDungId))
                {
                    _logger.LogError("Invalid NguoiDungId claim value in Edit: {NguoiDungIdClaim}", nguoiDungIdClaim);
                    TempData["ErrorMessage"] = "Thông tin tài khoản không hợp lệ. Vui lòng đăng nhập lại.";
                    return RedirectToAction("Login", "Auth");
                }

                var user = await _nguoiDungService.GetByIdAsync(nguoiDungId);
                if (user == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy thông tin người dùng.";
                    return RedirectToAction("Index");
                }

                return View(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while loading user profile for edit");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải thông tin cá nhân.";
                return RedirectToAction("Index");
            }
        }

        // POST: Profile/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(NguoiDungDto model, IFormFile? avatarFile)
        {
            try
            {
                // Sử dụng cùng claim như GET method để consistency
                var nguoiDungIdClaim = User.FindFirst("NguoiDungId")?.Value;
                if (string.IsNullOrEmpty(nguoiDungIdClaim))
                {
                    TempData["ErrorMessage"] = "Không tìm thấy thông tin người dùng trong phiên đăng nhập.";
                    return RedirectToAction("Login", "Auth");
                }

                if (!int.TryParse(nguoiDungIdClaim, out int nguoiDungId))
                {
                    _logger.LogError("Invalid NguoiDungId claim value in Edit POST: {NguoiDungIdClaim}", nguoiDungIdClaim);
                    TempData["ErrorMessage"] = "Thông tin tài khoản không hợp lệ. Vui lòng đăng nhập lại.";
                    return RedirectToAction("Login", "Auth");
                }

                // Kiểm tra user có quyền edit profile này không
                if (model.NguoiDungId != nguoiDungId)
                {
                    TempData["ErrorMessage"] = "Bạn không có quyền chỉnh sửa thông tin này.";
                    return RedirectToAction("Index");
                }

                if (ModelState.IsValid)
                {
                    // Lấy thông tin user hiện tại để giữ lại ảnh cũ nếu không upload ảnh mới
                    var currentUser = await _nguoiDungService.GetByIdAsync(nguoiDungId);
                    if (currentUser == null)
                    {
                        TempData["ErrorMessage"] = "Không tìm thấy thông tin người dùng.";
                        return View(model);
                    }

                    // Xử lý upload avatar nếu có
                    if (avatarFile != null && avatarFile.Length > 0)
                    {
                        var avatarPath = await ProcessAvatarUpload(avatarFile, nguoiDungId);
                        if (!string.IsNullOrEmpty(avatarPath))
                        {
                            model.AnhDaiDien = avatarPath;
                        }
                        else
                        {
                            ModelState.AddModelError("", "Có lỗi xảy ra khi tải lên ảnh đại diện.");
                            return View(model);
                        }
                    }
                    else
                    {
                        // Giữ lại ảnh cũ nếu không upload ảnh mới
                        model.AnhDaiDien = currentUser.AnhDaiDien;
                    }

                    var result = await _nguoiDungService.UpdateAsync(model);
                    if (result)
                    {
                        TempData["SuccessMessage"] = "Cập nhật thông tin cá nhân thành công!";
                        return RedirectToAction("Index");
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "Có lỗi xảy ra khi cập nhật thông tin cá nhân.";
                    }
                }

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating user profile");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi cập nhật thông tin cá nhân.";
                return View(model);
            }
        }

        // GET: Profile/ChangePassword
        public IActionResult ChangePassword()
        {
            return View();
        }

        // POST: Profile/ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordDto model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var nguoiDungIdClaim = User.FindFirst("NguoiDungId")?.Value;
                    if (string.IsNullOrEmpty(nguoiDungIdClaim))
                    {
                        TempData["ErrorMessage"] = "Không tìm thấy thông tin người dùng trong phiên đăng nhập.";
                        return RedirectToAction("Login", "Auth");
                    }

                    var result = await _nguoiDungService.ChangePasswordAsync(int.Parse(nguoiDungIdClaim), model.CurrentPassword, model.NewPassword);
                    if (result)
                    {
                        TempData["SuccessMessage"] = "Đổi mật khẩu thành công!";
                        return RedirectToAction("Index");
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "Mật khẩu hiện tại không đúng hoặc có lỗi xảy ra.";
                    }
                }

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while changing password");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi đổi mật khẩu.";
                return View(model);
            }
        }

        // GET: Profile/Security
        public IActionResult Security()
        {
            return View();
        }

        // Xử lý upload avatar
        private async Task<string?> ProcessAvatarUpload(IFormFile avatarFile, int userId)
        {
            try
            {
                // Kiểm tra file type
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var fileExtension = Path.GetExtension(avatarFile.FileName).ToLowerInvariant();

                if (!allowedExtensions.Contains(fileExtension))
                {
                    _logger.LogWarning("Invalid file extension for avatar upload: {Extension}", fileExtension);
                    return null;
                }

                // Kiểm tra file size (max 5MB)
                if (avatarFile.Length > 5 * 1024 * 1024)
                {
                    _logger.LogWarning("Avatar file too large: {Size} bytes", avatarFile.Length);
                    return null;
                }

                // Tạo thư mục uploads nếu chưa có
                var uploadsPath = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "avatars");
                if (!Directory.Exists(uploadsPath))
                {
                    Directory.CreateDirectory(uploadsPath);
                }

                // Tạo tên file unique
                var fileName = $"avatar_{userId}_{DateTime.Now:yyyyMMddHHmmss}{fileExtension}";
                var filePath = Path.Combine(uploadsPath, fileName);

                // Xóa avatar cũ nếu có
                await DeleteOldAvatar(userId);

                // Lưu file mới
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await avatarFile.CopyToAsync(stream);
                }

                // Trả về relative path để lưu vào database
                return $"/uploads/avatars/{fileName}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while processing avatar upload for user {UserId}", userId);
                return null;
            }
        }

        // Xóa avatar cũ
        private async Task DeleteOldAvatar(int userId)
        {
            try
            {
                var user = await _nguoiDungService.GetByIdAsync(userId);
                if (user?.AnhDaiDien != null && user.AnhDaiDien.StartsWith("/uploads/avatars/"))
                {
                    var oldFilePath = Path.Combine(_webHostEnvironment.WebRootPath, user.AnhDaiDien.TrimStart('/'));
                    if (System.IO.File.Exists(oldFilePath))
                    {
                        System.IO.File.Delete(oldFilePath);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting old avatar for user {UserId}", userId);
                // Không throw exception vì đây không phải critical error
            }
        }

        private int? GetCurrentNguoiDungIdSafe()
        {
            try
            {
                var userIdClaim = User.FindFirst("NguoiDungId")?.Value;
                _logger.LogInformation("Getting NguoiDungId claim: {UserIdClaim}, IsAuthenticated: {IsAuthenticated}",
                    userIdClaim, User.Identity?.IsAuthenticated);

                if (string.IsNullOrEmpty(userIdClaim))
                {
                    _logger.LogWarning("NguoiDungId claim is null or empty. Available claims: {Claims}",
                        string.Join(", ", User.Claims.Select(c => $"{c.Type}={c.Value}")));
                    return null;
                }

                if (int.TryParse(userIdClaim, out var userId))
                {
                    _logger.LogInformation("Successfully parsed NguoiDungId: {UserId}", userId);
                    return userId;
                }

                _logger.LogError("Failed to parse NguoiDungId claim: {UserIdClaim}", userIdClaim);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current NguoiDungId safely");
                return null;
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetPersonalStats()
        {
            try
            {
                _logger.LogInformation("GetPersonalStats called");
                var nguoiDungId = GetCurrentNguoiDungIdSafe();
                if (!nguoiDungId.HasValue)
                {
                    _logger.LogWarning("GetPersonalStats: No NguoiDungId found");
                    return Json(new { success = false, message = "Không tìm thấy thông tin người dùng." });
                }

                _logger.LogInformation("GetPersonalStats: Processing for NguoiDungId: {NguoiDungId}", nguoiDungId.Value);

                // DEBUG: Check attendance data for this user
                var attendanceCount = await _diemDanhService.GetAttendanceCountByUserIdAsync(nguoiDungId.Value);
                _logger.LogInformation("DEBUG: User {UserId} has {AttendanceCount} attendance records", nguoiDungId.Value, attendanceCount);

                // Get user registrations and calculate total cost (including completed registrations)
                var registrations = await _dangKyService.GetByMemberIdAsync(nguoiDungId.Value);
                var totalCost = registrations
                    .Where(r => r.TrangThai == "ACTIVE" || r.TrangThai == "COMPLETED" || r.TrangThai == "EXPIRED")
                    .Sum(r => r.PhiDangKy ?? 0);

                // Calculate date ranges more accurately
                var now = DateTime.Now;
                var today = DateTime.Today;

                // This month: from 1st day of current month to now
                var thisMonthStart = new DateTime(now.Year, now.Month, 1);

                // This week: from Monday to Sunday (Vietnamese week starts on Monday)
                var daysFromMonday = ((int)today.DayOfWeek + 6) % 7; // Convert Sunday=0 to Monday=0
                var thisWeekStart = today.AddDays(-daysFromMonday);
                var thisWeekEnd = thisWeekStart.AddDays(7);

                // Get attendance stats with improved date ranges
                var attendanceStats = new
                {
                    TotalSessions = await _diemDanhService.GetMemberAttendanceCountAsync(nguoiDungId.Value, DateTime.MinValue, now),
                    ThisMonth = await _diemDanhService.GetMemberAttendanceCountAsync(nguoiDungId.Value, thisMonthStart, now),
                    ThisWeek = await _diemDanhService.GetMemberAttendanceCountAsync(nguoiDungId.Value, thisWeekStart, thisWeekEnd),
                    LastMonth = await _diemDanhService.GetMemberAttendanceCountAsync(nguoiDungId.Value,
                        thisMonthStart.AddMonths(-1), thisMonthStart.AddDays(-1))
                };

                // Calculate frequency more accurately
                var firstAttendance = await _diemDanhService.GetFirstAttendanceDateAsync(nguoiDungId.Value);
                double frequency = 0;
                if (firstAttendance.HasValue && attendanceStats.TotalSessions > 0)
                {
                    var daysSinceFirst = Math.Max(1, (now - firstAttendance.Value).Days);
                    var weeksSinceFirst = Math.Max(1, daysSinceFirst / 7.0);
                    frequency = attendanceStats.TotalSessions / weeksSinceFirst;
                }

                // Get monthly attendance data sequentially to avoid DbContext concurrency issues
                var monthlyData = new List<object>();

                for (int i = 5; i >= 0; i--)
                {
                    var month = now.AddMonths(-i);
                    var startOfMonth = new DateTime(month.Year, month.Month, 1);
                    var endOfMonth = startOfMonth.AddMonths(1);

                    var count = await _diemDanhService.GetMemberAttendanceCountAsync(nguoiDungId.Value, startOfMonth, endOfMonth);
                    var monthName = month.ToString("MMM yyyy", new System.Globalization.CultureInfo("vi-VN"));

                    monthlyData.Add(new { Month = monthName, Count = count });
                }

                // Calculate additional metrics
                var monthlyDataCounts = monthlyData.Select(m => (int)((dynamic)m).Count).Where(c => c > 0).ToList();
                var averageSessionsPerMonth = monthlyDataCounts.Any() ? monthlyDataCounts.Average() : 0;
                var monthlyGrowth = attendanceStats.LastMonth > 0 ?
                    Math.Round(((double)(attendanceStats.ThisMonth - attendanceStats.LastMonth) / attendanceStats.LastMonth) * 100, 1) : 0;

                return Json(new
                {
                    success = true,
                    stats = new
                    {
                        TotalSessions = attendanceStats.TotalSessions,
                        TotalCost = totalCost,
                        Frequency = Math.Round(frequency, 1),
                        ThisMonth = attendanceStats.ThisMonth,
                        LastMonth = attendanceStats.LastMonth,
                        ThisWeek = attendanceStats.ThisWeek,
                        MonthlyData = monthlyData,
                        AverageSessionsPerMonth = Math.Round(averageSessionsPerMonth, 1),
                        MonthlyGrowth = monthlyGrowth,
                        FirstAttendanceDate = firstAttendance?.ToString("dd/MM/yyyy")
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting personal stats");
                return Json(new { success = false, message = "Có lỗi xảy ra khi tải thống kê." });
            }
        }



        [HttpGet]
        public async Task<IActionResult> GetDetailedStats()
        {
            try
            {
                _logger.LogInformation("GetDetailedStats called");
                var nguoiDungId = GetCurrentNguoiDungIdSafe();
                if (!nguoiDungId.HasValue)
                {
                    _logger.LogWarning("GetDetailedStats: No NguoiDungId found");
                    return Json(new { success = false, message = "Không tìm thấy thông tin người dùng." });
                }

                _logger.LogInformation("GetDetailedStats: Processing for NguoiDungId: {NguoiDungId}", nguoiDungId.Value);

                var now = DateTime.Now;
                var today = DateTime.Today;

                // Calculate workout streak (consecutive days with attendance)
                var workoutStreak = await CalculateWorkoutStreakAsync(nguoiDungId.Value);

                // Calculate average session duration
                var averageSessionDuration = await CalculateAverageSessionDurationAsync(nguoiDungId.Value);

                // Get most active day of week
                var mostActiveDay = await GetMostActiveDayOfWeekAsync(nguoiDungId.Value);

                // Get favorite time slot
                var favoriteTimeSlot = await GetFavoriteTimeSlotAsync(nguoiDungId.Value);

                // Calculate consistency score (percentage of weeks with at least one workout in last 3 months)
                var consistencyScore = await CalculateConsistencyScoreAsync(nguoiDungId.Value);

                return Json(new
                {
                    success = true,
                    detailedStats = new
                    {
                        WorkoutStreak = workoutStreak,
                        AverageSessionDuration = averageSessionDuration,
                        MostActiveDay = mostActiveDay,
                        FavoriteTimeSlot = favoriteTimeSlot,
                        ConsistencyScore = consistencyScore
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting detailed stats");
                return Json(new { success = false, message = "Có lỗi xảy ra khi tải thống kê chi tiết." });
            }
        }

        private async Task<int> CalculateWorkoutStreakAsync(int nguoiDungId)
        {
            var attendances = await _diemDanhService.GetAttendanceReportAsync(DateTime.Today.AddDays(-90), DateTime.Today);
            var attendanceDates = attendances
                .Where(a => a.ThanhVienId == nguoiDungId)
                .Select(a => a.ThoiGian.Date)
                .Distinct()
                .OrderByDescending(d => d)
                .ToList();

            if (!attendanceDates.Any()) return 0;

            int streak = 0;
            var currentDate = DateTime.Today;

            foreach (var date in attendanceDates)
            {
                if (date == currentDate || date == currentDate.AddDays(-1))
                {
                    streak++;
                    currentDate = date.AddDays(-1);
                }
                else
                {
                    break;
                }
            }

            return streak;
        }

        private async Task<string> CalculateAverageSessionDurationAsync(int nguoiDungId)
        {
            // This would require session duration data - for now return placeholder
            // In a real implementation, you'd track check-in and check-out times
            return "1.5 giờ"; // Placeholder
        }

        private async Task<string> GetMostActiveDayOfWeekAsync(int nguoiDungId)
        {
            var attendances = await _diemDanhService.GetAttendanceReportAsync(DateTime.Today.AddDays(-90), DateTime.Today);
            var dayStats = attendances
                .Where(a => a.ThanhVienId == nguoiDungId)
                .GroupBy(a => a.ThoiGian.DayOfWeek)
                .Select(g => new { Day = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .FirstOrDefault();

            if (dayStats == null) return "Chưa có dữ liệu";

            var dayNames = new Dictionary<DayOfWeek, string>
            {
                { DayOfWeek.Monday, "Thứ 2" },
                { DayOfWeek.Tuesday, "Thứ 3" },
                { DayOfWeek.Wednesday, "Thứ 4" },
                { DayOfWeek.Thursday, "Thứ 5" },
                { DayOfWeek.Friday, "Thứ 6" },
                { DayOfWeek.Saturday, "Thứ 7" },
                { DayOfWeek.Sunday, "Chủ nhật" }
            };

            return dayNames[dayStats.Day];
        }

        private async Task<string> GetFavoriteTimeSlotAsync(int nguoiDungId)
        {
            var attendances = await _diemDanhService.GetAttendanceReportAsync(DateTime.Today.AddDays(-90), DateTime.Today);
            var timeSlots = attendances
                .Where(a => a.ThanhVienId == nguoiDungId)
                .GroupBy(a => a.ThoiGian.Hour switch
                {
                    >= 6 and < 12 => "Sáng (6:00-12:00)",
                    >= 12 and < 18 => "Chiều (12:00-18:00)",
                    >= 18 and < 22 => "Tối (18:00-22:00)",
                    _ => "Khác"
                })
                .Select(g => new { TimeSlot = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .FirstOrDefault();

            return timeSlots?.TimeSlot ?? "Chưa có dữ liệu";
        }

        private async Task<double> CalculateConsistencyScoreAsync(int nguoiDungId)
        {
            var threeMonthsAgo = DateTime.Today.AddDays(-90);
            var attendances = await _diemDanhService.GetAttendanceReportAsync(threeMonthsAgo, DateTime.Today);

            var attendanceDates = attendances
                .Where(a => a.ThanhVienId == nguoiDungId)
                .Select(a => a.ThoiGian.Date)
                .Distinct()
                .ToList();

            if (!attendanceDates.Any()) return 0;

            // Calculate weeks with at least one workout
            var weeksWithWorkout = 0;
            var totalWeeks = 0;
            var currentWeekStart = threeMonthsAgo;

            while (currentWeekStart <= DateTime.Today)
            {
                var weekEnd = currentWeekStart.AddDays(7);
                if (attendanceDates.Any(d => d >= currentWeekStart && d < weekEnd))
                {
                    weeksWithWorkout++;
                }
                totalWeeks++;
                currentWeekStart = weekEnd;
            }

            return totalWeeks > 0 ? Math.Round((double)weeksWithWorkout / totalWeeks * 100, 1) : 0;
        }

        [HttpGet]
        public async Task<IActionResult> MyTrainers()
        {
            try
            {
                var nguoiDungId = GetCurrentNguoiDungIdSafe();
                if (!nguoiDungId.HasValue)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy thông tin người dùng.";
                    return RedirectToAction("Index");
                }

                // Get all classes the member has registered for
                var registrations = await _dangKyService.GetByMemberIdAsync(nguoiDungId.Value);
                var classRegistrations = registrations.Where(r => r.LopHocId.HasValue).ToList();

                // Get trainers from those classes
                var trainerIds = classRegistrations
                    .Where(r => r.LopHoc?.HlvId.HasValue == true)
                    .Select(r => r.LopHoc!.HlvId!.Value)
                    .Distinct()
                    .ToList();

                var trainers = new List<object>();
                foreach (var trainerId in trainerIds)
                {
                    var trainer = await _nguoiDungService.GetByIdAsync(trainerId);
                    if (trainer != null)
                    {
                        // Get classes taught by this trainer that the member is registered for
                        var myClassesWithTrainer = classRegistrations
                            .Where(r => r.LopHoc?.HlvId == trainerId)
                            .Select(r => r.LopHoc!.TenLop)
                            .ToList();

                        trainers.Add(new
                        {
                            trainer.NguoiDungId,
                            HoTen = trainer.HoTen,
                            Email = trainer.Email,
                            SoDienThoai = trainer.SoDienThoai,
                            MyClasses = myClassesWithTrainer,
                            TotalClasses = myClassesWithTrainer.Count
                        });
                    }
                }

                ViewBag.Trainers = trainers;
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting trainers");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải danh sách huấn luyện viên.";
                return RedirectToAction("Index");
            }
        }

        [HttpGet]
        public async Task<IActionResult> DebugCurrentUser()
        {
            try
            {
                var nguoiDungId = GetCurrentNguoiDungIdSafe();
                var user = nguoiDungId.HasValue ? await _nguoiDungService.GetByIdAsync(nguoiDungId.Value) : null;
                var attendanceCount = nguoiDungId.HasValue ? await _diemDanhService.GetAttendanceCountByUserIdAsync(nguoiDungId.Value) : 0;
                var registrationCount = nguoiDungId.HasValue ? await _dangKyService.GetRegistrationCountByUserIdAsync(nguoiDungId.Value) : 0;

                return Json(new {
                    success = true,
                    currentUserId = nguoiDungId,
                    userName = user?.Ho + " " + user?.Ten,
                    email = user?.Email,
                    attendanceCount = attendanceCount,
                    registrationCount = registrationCount,
                    claims = User.Claims.Select(c => new { c.Type, c.Value }).ToList()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DebugCurrentUser");
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> DebugDiemDanhData()
        {
            try
            {
                var nguoiDungId = GetCurrentNguoiDungIdSafe();
                if (!nguoiDungId.HasValue)
                {
                    return Json(new { success = false, message = "No user ID found" });
                }

                // Get raw attendance data to check field values
                var allAttendances = await _diemDanhService.GetByMemberIdAsync(nguoiDungId.Value);
                var attendanceData = allAttendances.Take(10).Select(d => new {
                    DiemDanhId = d.DiemDanhId,
                    ThanhVienId = d.ThanhVienId,
                    ThoiGian = d.ThoiGian,
                    ThoiGianCheckIn = d.ThoiGianCheckIn,
                    ThoiGianCheckOut = d.ThoiGianCheckOut,
                    TrangThai = d.TrangThai
                }).ToList();

                // Test different count methods
                var countByThoiGian = await _diemDanhService.GetMemberAttendanceCountAsync(nguoiDungId.Value, DateTime.MinValue, DateTime.Now);
                var countByUserId = await _diemDanhService.GetAttendanceCountByUserIdAsync(nguoiDungId.Value);

                // Test personal stats methods - using existing methods
                var currentMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                var nextMonth = currentMonth.AddMonths(1);
                var thisMonthCount = await _diemDanhService.GetMemberAttendanceCountAsync(nguoiDungId.Value, currentMonth, nextMonth.AddDays(-1));

                return Json(new {
                    success = true,
                    userId = nguoiDungId.Value,
                    totalRecords = allAttendances.Count(),
                    countByThoiGian = countByThoiGian,
                    countByUserId = countByUserId,
                    thisMonthAttendance = thisMonthCount,
                    sampleData = attendanceData,
                    dateRangeTest = new {
                        thisMonth = await _diemDanhService.GetMemberAttendanceCountAsync(nguoiDungId.Value,
                            new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1), DateTime.Now),
                        last30Days = await _diemDanhService.GetMemberAttendanceCountAsync(nguoiDungId.Value,
                            DateTime.Now.AddDays(-30), DateTime.Now)
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while debugging DiemDanh data");
                return Json(new { success = false, message = ex.Message, stackTrace = ex.StackTrace });
            }
        }


    }
}
