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
            var userIdClaim = User.FindFirst("NguoiDungId")?.Value;
            return int.TryParse(userIdClaim, out var userId) ? userId : null;
        }

        [HttpGet]
        public async Task<IActionResult> GetPersonalStats()
        {
            try
            {
                var nguoiDungId = GetCurrentNguoiDungIdSafe();
                if (!nguoiDungId.HasValue)
                {
                    return Json(new { success = false, message = "Không tìm thấy thông tin người dùng." });
                }

                // Get user registrations and attendance
                var registrations = await _dangKyService.GetByMemberIdAsync(nguoiDungId.Value);
                var totalCost = registrations.Where(r => r.TrangThai == "ACTIVE").Sum(r => r.PhiDangKy ?? 0);
                
                // Get attendance stats
                var attendanceStats = new
                {
                    TotalSessions = await _diemDanhService.GetMemberAttendanceCountAsync(nguoiDungId.Value, DateTime.MinValue, DateTime.MaxValue),
                    ThisMonth = await _diemDanhService.GetMemberAttendanceCountAsync(nguoiDungId.Value, 
                        new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1), DateTime.Now),
                    ThisWeek = await _diemDanhService.GetMemberAttendanceCountAsync(nguoiDungId.Value, 
                        DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek), DateTime.Today.AddDays(1))
                };

                // Calculate frequency (sessions per week average)
                var firstAttendance = await _diemDanhService.GetFirstAttendanceDateAsync(nguoiDungId.Value);
                var weeksSinceFirst = firstAttendance.HasValue ? 
                    Math.Max(1, (DateTime.Now - firstAttendance.Value).Days / 7) : 1;
                var frequency = (double)attendanceStats.TotalSessions / weeksSinceFirst;

                // Monthly attendance data for chart (last 6 months)
                var monthlyData = new List<object>();
                for (int i = 5; i >= 0; i--)
                {
                    var month = DateTime.Now.AddMonths(-i);
                    var startOfMonth = new DateTime(month.Year, month.Month, 1);
                    var endOfMonth = startOfMonth.AddMonths(1);
                    var count = await _diemDanhService.GetMemberAttendanceCountAsync(nguoiDungId.Value, startOfMonth, endOfMonth);
                    
                    monthlyData.Add(new
                    {
                        Month = month.ToString("MMM yyyy", new System.Globalization.CultureInfo("vi-VN")),
                        Count = count
                    });
                }

                return Json(new
                {
                    success = true,
                    stats = new
                    {
                        TotalSessions = attendanceStats.TotalSessions,
                        TotalCost = totalCost,
                        Frequency = Math.Round(frequency, 1),
                        ThisMonth = attendanceStats.ThisMonth,
                        ThisWeek = attendanceStats.ThisWeek,
                        MonthlyData = monthlyData
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
    }
}
