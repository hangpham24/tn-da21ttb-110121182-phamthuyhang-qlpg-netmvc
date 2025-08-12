using GymManagement.Web.Data.Models;
using GymManagement.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GymManagement.Web.Controllers
{
    [Authorize]
    public class DiemDanhController : Controller
    {
        private readonly IDiemDanhService _diemDanhService;
        private readonly INguoiDungService _nguoiDungService;
        private readonly IAuthService _authService;
        private readonly ILogger<DiemDanhController> _logger;

        public DiemDanhController(
            IDiemDanhService diemDanhService,
            INguoiDungService nguoiDungService,
            IAuthService authService,
            ILogger<DiemDanhController> logger)
        {
            _diemDanhService = diemDanhService;
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

        [Authorize(Roles = "Admin,Trainer")]
        public async Task<IActionResult> Index()
        {
            try
            {
                var attendance = await _diemDanhService.GetAllAsync();
                return View(attendance);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting attendance records");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải danh sách điểm danh.";
                return View(new List<DiemDanh>());
            }
        }

        public async Task<IActionResult> TodayAttendance()
        {
            try
            {
                var attendance = await _diemDanhService.GetTodayAttendanceAsync();
                var count = await _diemDanhService.GetTodayAttendanceCountAsync();
                
                ViewBag.TodayCount = count;
                return View(attendance);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting today's attendance");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải danh sách điểm danh hôm nay.";
                ViewBag.TodayCount = 0;
                return View(new List<DiemDanh>());
            }
        }

        public async Task<IActionResult> MyAttendance()
        {
            try
            {
                var user = await GetCurrentUserAsync();
                if (user?.NguoiDungId == null)
                {
                    return RedirectToAction("Login", "Auth");
                }

                var attendance = await _diemDanhService.GetByMemberIdAsync(user.NguoiDungId.Value);
                return View(attendance);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting user attendance");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải lịch sử điểm danh của bạn.";
                return View(new List<DiemDanh>());
            }
        }

        public IActionResult CheckIn()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ManualCheckIn(int memberId, string? note = null)
        {
            try
            {
                var result = await _diemDanhService.CheckInAsync(memberId, note);
                if (result)
                {
                    return Json(new { success = true, message = "Check-in thành công!" });
                }
                else
                {
                    return Json(new { success = false, message = "Không thể check-in. Thành viên có thể đã check-in hôm nay rồi." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while manual check-in");
                return Json(new { success = false, message = "Có lỗi xảy ra khi check-in." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SelfCheckIn()
        {
            try
            {
                var user = await GetCurrentUserAsync();
                if (user?.NguoiDungId == null)
                {
                    return Json(new { success = false, message = "Vui lòng đăng nhập để check-in." });
                }

                var result = await _diemDanhService.CheckInAsync(user.NguoiDungId.Value);
                if (result)
                {
                    return Json(new { success = true, message = "Check-in thành công!" });
                }
                else
                {
                    return Json(new { success = false, message = "Bạn đã check-in hôm nay rồi." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while self check-in");
                return Json(new { success = false, message = "Có lỗi xảy ra khi check-in." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> FaceRecognitionCheckIn(IFormFile faceImage)
        {
            try
            {
                var user = await GetCurrentUserAsync();
                if (user?.NguoiDungId == null)
                {
                    return Json(new { success = false, message = "Vui lòng đăng nhập để check-in." });
                }

                if (faceImage == null || faceImage.Length == 0)
                {
                    return Json(new { success = false, message = "Vui lòng chụp ảnh khuôn mặt." });
                }

                // Convert image to byte array
                using var memoryStream = new MemoryStream();
                await faceImage.CopyToAsync(memoryStream);
                var imageBytes = memoryStream.ToArray();

                var result = await _diemDanhService.CheckInWithFaceRecognitionAsync(user.NguoiDungId.Value, imageBytes);
                if (result)
                {
                    return Json(new { success = true, message = "Check-in bằng nhận diện khuôn mặt thành công!" });
                }
                else
                {
                    return Json(new { success = false, message = "Không thể nhận diện khuôn mặt. Vui lòng thử lại." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while face recognition check-in");
                return Json(new { success = false, message = "Có lỗi xảy ra khi check-in bằng nhận diện khuôn mặt." });
            }
        }

        [HttpGet]
        public async Task<IActionResult> CheckStatus(int? memberId = null)
        {
            try
            {
                int targetMemberId;
                if (memberId.HasValue)
                {
                    targetMemberId = memberId.Value;
                }
                else
                {
                    var user = await GetCurrentUserAsync();
                    if (user?.NguoiDungId == null)
                    {
                        return Json(new { hasCheckedIn = false, message = "Vui lòng đăng nhập." });
                    }
                    targetMemberId = user.NguoiDungId.Value;
                }

                var hasCheckedIn = await _diemDanhService.HasCheckedInTodayAsync(targetMemberId);
                var latestAttendance = await _diemDanhService.GetLatestAttendanceAsync(targetMemberId);

                return Json(new { 
                    hasCheckedIn = hasCheckedIn,
                    lastCheckIn = latestAttendance?.ThoiGian.ToString("dd/MM/yyyy HH:mm"),
                    message = hasCheckedIn ? "Đã check-in hôm nay" : "Chưa check-in hôm nay"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while checking attendance status");
                return Json(new { hasCheckedIn = false, message = "Có lỗi xảy ra." });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAttendanceStats(int memberId, DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                startDate ??= DateTime.Today.AddDays(-30);
                endDate ??= DateTime.Today;

                var count = await _diemDanhService.GetMemberAttendanceCountAsync(memberId, startDate.Value, endDate.Value);
                var totalDays = (endDate.Value - startDate.Value).Days + 1;
                var attendanceRate = totalDays > 0 ? (double)count / totalDays * 100 : 0;

                return Json(new {
                    success = true,
                    attendanceCount = count,
                    totalDays = totalDays,
                    attendanceRate = Math.Round(attendanceRate, 2),
                    period = $"{startDate:dd/MM/yyyy} - {endDate:dd/MM/yyyy}"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting attendance stats");
                return Json(new { success = false, message = "Có lỗi xảy ra khi tính thống kê điểm danh." });
            }
        }

        public async Task<IActionResult> AttendanceReport(DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                startDate ??= DateTime.Today.AddDays(-7);
                endDate ??= DateTime.Today;

                var attendance = await _diemDanhService.GetAttendanceReportAsync(startDate.Value, endDate.Value);
                
                ViewBag.StartDate = startDate.Value;
                ViewBag.EndDate = endDate.Value;
                ViewBag.TotalRecords = attendance.Count();
                
                return View(attendance);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while generating attendance report");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tạo báo cáo điểm danh.";
                ViewBag.StartDate = DateTime.Today.AddDays(-7);
                ViewBag.EndDate = DateTime.Today;
                ViewBag.TotalRecords = 0;
                return View(new List<DiemDanh>());
            }
        }

        [HttpGet]
        public async Task<IActionResult> ExportAttendance(DateTime startDate, DateTime endDate, string format = "csv")
        {
            try
            {
                var attendance = await _diemDanhService.GetAttendanceReportAsync(startDate, endDate);
                
                if (format.ToLower() == "csv")
                {
                    var csv = "Thời gian,Thành viên,Kết quả nhận dạng,Ghi chú\n";
                    foreach (var record in attendance)
                    {
                        csv += $"{record.ThoiGian:dd/MM/yyyy HH:mm},{record.ThanhVien?.Ho} {record.ThanhVien?.Ten},{(record.KetQuaNhanDang == true ? "Thành công" : "Thất bại")},{record.AnhMinhChung}\n";
                    }

                    var bytes = System.Text.Encoding.UTF8.GetBytes(csv);
                    return File(bytes, "text/csv", $"DiemDanh_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}.csv");
                }

                return BadRequest("Định dạng không được hỗ trợ.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while exporting attendance");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi xuất báo cáo điểm danh.";
                return RedirectToAction(nameof(AttendanceReport));
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetRealtimeStats()
        {
            try
            {
                var todayCount = await _diemDanhService.GetTodayAttendanceCountAsync();
                return Json(new {
                    todayAttendance = todayCount,
                    lastUpdated = DateTime.Now.ToString("HH:mm:ss")
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting realtime stats");
                return Json(new { todayAttendance = 0, lastUpdated = DateTime.Now.ToString("HH:mm:ss") });
            }
        }

        [HttpDelete]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                _logger.LogInformation("🗑️ Attempting to delete attendance record with ID: {AttendanceId}", id);

                // Check if attendance record exists
                var attendance = await _diemDanhService.GetByIdAsync(id);
                if (attendance == null)
                {
                    _logger.LogWarning("❌ Attendance record not found with ID: {AttendanceId}", id);
                    return Json(new {
                        success = false,
                        message = "Không tìm thấy bản ghi điểm danh."
                    });
                }

                // Get current user for authorization check
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentUserId))
                {
                    _logger.LogWarning("❌ Unauthorized delete attempt - no user ID found");
                    return Json(new {
                        success = false,
                        message = "Không có quyền thực hiện thao tác này."
                    });
                }

                // Check if user has permission to delete (admin or the record owner)
                var isAdmin = User.IsInRole("Admin") || User.IsInRole("Manager");
                var isOwner = attendance.ThanhVienId.ToString() == currentUserId;

                if (!isAdmin && !isOwner)
                {
                    _logger.LogWarning("❌ Unauthorized delete attempt by user {UserId} for attendance {AttendanceId}",
                        currentUserId, id);
                    return Json(new {
                        success = false,
                        message = "Bạn không có quyền xóa bản ghi này."
                    });
                }

                // Perform deletion
                var result = await _diemDanhService.DeleteAsync(id);

                if (result)
                {
                    _logger.LogInformation("✅ Successfully deleted attendance record {AttendanceId} by user {UserId}",
                        id, currentUserId);

                    return Json(new {
                        success = true,
                        message = "Đã xóa bản ghi điểm danh thành công."
                    });
                }
                else
                {
                    _logger.LogError("❌ Failed to delete attendance record {AttendanceId}", id);
                    return Json(new {
                        success = false,
                        message = "Không thể xóa bản ghi điểm danh. Vui lòng thử lại."
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error occurred while deleting attendance record {AttendanceId}", id);
                return Json(new {
                    success = false,
                    message = "Có lỗi xảy ra khi xóa bản ghi. Vui lòng thử lại sau."
                });
            }
        }
    }
}
