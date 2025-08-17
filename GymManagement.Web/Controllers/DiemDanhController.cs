using GymManagement.Web.Data.Models;
using GymManagement.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GymManagement.Web.Controllers
{
    [Authorize]
    [Route("[controller]")]
    public class DiemDanhController : BaseController
    {
        private readonly IDiemDanhService _diemDanhService;
        private readonly INguoiDungService _nguoiDungService;
        private readonly IAuthService _authService;
        private readonly IMemberBenefitService _memberBenefitService;

        public DiemDanhController(
            IDiemDanhService diemDanhService,
            INguoiDungService nguoiDungService,
            IAuthService authService,
            IMemberBenefitService memberBenefitService,
            IUserSessionService userSessionService,
            ILogger<DiemDanhController> logger)
            : base(userSessionService, logger)
        {
            _diemDanhService = diemDanhService;
            _nguoiDungService = nguoiDungService;
            _authService = authService;
            _memberBenefitService = memberBenefitService;
        }

        // Helper method to get current user with enhanced security
        private async Task<TaiKhoan?> GetCurrentUserAsync()
        {
            var userId = GetCurrentUserIdSafe();
            if (string.IsNullOrEmpty(userId))
            {
                LogUserAction("GetCurrentUser_Failed", "No user ID found");
                return null;
            }

            try
            {
                var user = await _authService.GetUserByIdAsync(userId);
                if (user == null)
                {
                    LogUserAction("GetCurrentUser_NotFound", new { UserId = userId });
                }
                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current user for ID: {UserId}", userId);
                return null;
            }
        }

        [Authorize(Roles = "Admin,Trainer")]
        [HttpGet("")]
        [HttpGet("Index")]
        public async Task<IActionResult> Index()
        {
            try
            {
                LogUserAction("DiemDanh_Index_Access");

                // Validate permissions
                if (!IsInRoleSafe("Admin") && !IsInRoleSafe("Trainer"))
                {
                    return HandleUnauthorized("Bạn không có quyền xem danh sách điểm danh.");
                }

                var attendance = await _diemDanhService.GetAllAsync();

                // If user is Trainer, filter to only their classes
                if (IsInRoleSafe("Trainer") && !IsInRoleSafe("Admin"))
                {
                    var currentUser = await GetCurrentUserAsync();
                    if (currentUser?.NguoiDungId != null)
                    {
                        // Filter attendance records for trainer's classes only
                        attendance = attendance.Where(a => a.LopHoc?.HlvId == currentUser.NguoiDungId).ToList();
                        LogUserAction("DiemDanh_Index_FilteredForTrainer", new {
                            TrainerId = currentUser.NguoiDungId,
                            RecordCount = attendance.Count()
                        });
                    }
                }

                LogUserAction("DiemDanh_Index_Success", new { RecordCount = attendance.Count() });
                return View(attendance);
            }
            catch (Exception ex)
            {
                return HandleError(ex, "Có lỗi xảy ra khi tải danh sách điểm danh.");
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

        [HttpPost("ManualCheckIn")]
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

        [HttpPost("ManualCheckInWithClass")]
        public async Task<IActionResult> ManualCheckInWithClass(int memberId, int? classId = null, string? note = null)
        {
            try
            {
                var result = await _diemDanhService.CheckInWithClassAsync(memberId, classId, note);
                if (result)
                {
                    return Json(new { success = true, message = "Check-in thành công!" });
                }
                else
                {
                    return Json(new { success = false, message = "Không thể check-in. Thành viên có thể đã check-in hôm nay rồi hoặc lớp học không hợp lệ." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while manual check-in with class");
                return Json(new { success = false, message = "Có lỗi xảy ra khi check-in." });
            }
        }

        [HttpGet("GetAvailableClasses")]
        public async Task<IActionResult> GetAvailableClasses()
        {
            try
            {
                var classes = await _diemDanhService.GetAvailableClassesAsync();
                return Json(new { success = true, classes = classes });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting available classes");
                return Json(new { success = false, message = "Có lỗi xảy ra khi tải danh sách lớp học." });
            }
        }

        [HttpPost("FaceRecognitionCheckInWithClass")]
        [Authorize(Roles = "Admin,Trainer")]
        public async Task<IActionResult> FaceRecognitionCheckInWithClass([FromBody] FaceCheckInWithClassRequest request)
        {
            try
            {
                if (request?.Descriptor == null || request.Descriptor.Length != 128)
                {
                    return Json(new { success = false, message = "Dữ liệu khuôn mặt không hợp lệ" });
                }

                // Convert to float array
                var faceDescriptor = request.Descriptor.Select(d => (float)d).ToArray();

                // Recognize face
                var recognitionResult = await _diemDanhService.RecognizeFaceAsync(faceDescriptor);

                if (!recognitionResult.Success)
                {
                    return Json(new {
                        success = false,
                        message = "Không nhận diện được khuôn mặt. Vui lòng thử lại hoặc liên hệ nhân viên."
                    });
                }

                var memberId = recognitionResult.MemberId!.Value;
                var member = await _nguoiDungService.GetByIdAsync(memberId);

                if (member == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy thông tin hội viên" });
                }

                // Check current session status
                var currentSession = await _diemDanhService.GetLatestAttendanceAsync(memberId);
                var hasActiveSession = currentSession != null &&
                                     currentSession.ThoiGianCheckIn.Date == DateTime.Today &&
                                     currentSession.ThoiGianCheckOut == null;

                if (!hasActiveSession)
                {
                    // CHECK-IN with class selection
                    var checkInSuccess = await _diemDanhService.CheckInWithClassAsync(memberId, request.ClassId);

                    if (checkInSuccess)
                    {
                        var className = request.ClassId.HasValue ?
                            (await _diemDanhService.GetClassNameAsync(request.ClassId.Value)) ?? "Lớp học" :
                            "tập tự do";

                        // Get member package information
                        var packageInfo = await GetMemberPackageInfoAsync(memberId);

                        return Json(new
                        {
                            success = true,
                            action = "checkin",
                            message = $"Check-in thành công {className}!",
                            memberName = $"{member.Ho} {member.Ten}",
                            time = DateTime.Now.ToString("HH:mm dd/MM/yyyy"),
                            confidence = Math.Round(recognitionResult.Confidence * 100, 1),
                            className = className,
                            packageInfo = packageInfo
                        });
                    }
                    else
                    {
                        return Json(new { success = false, message = "Không thể check-in. Có thể đã check-in hôm nay rồi." });
                    }
                }
                else
                {
                    // CHECK-OUT
                    var checkOutSuccess = await _diemDanhService.CheckOutAsync(currentSession.DiemDanhId);

                    if (checkOutSuccess)
                    {
                        var duration = DateTime.Now - currentSession.ThoiGianCheckIn;
                        var durationText = $"{(int)duration.TotalHours}h {duration.Minutes}m";

                        return Json(new
                        {
                            success = true,
                            action = "checkout",
                            message = "Check-out thành công!",
                            memberName = $"{member.Ho} {member.Ten}",
                            duration = durationText,
                            confidence = Math.Round(recognitionResult.Confidence * 100, 1)
                        });
                    }
                    else
                    {
                        return Json(new { success = false, message = "Không thể check-out." });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during face recognition check-in with class");
                return Json(new { success = false, message = "Có lỗi xảy ra khi xử lý nhận diện khuôn mặt." });
            }
        }

        [HttpPost("SelfCheckIn")]
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

        [HttpPost("FaceRecognitionCheckIn")]
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

        [HttpGet("CheckStatus")]
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

        [HttpGet("GetAttendanceStats")]
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

        [HttpGet("ExportAttendance")]
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

        [HttpGet("GetRealtimeStats")]
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

        /// <summary>
        /// API để checkout thủ công cho attendance record
        /// </summary>
        [HttpPost("CheckOut/{id}")]
        [ActionName("CheckOut")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CheckOut(int id)
        {
            _logger.LogInformation("🚪 Manual checkout request received for attendance ID: {AttendanceId}", id);
            try
            {
                var attendance = await _diemDanhService.GetByIdAsync(id);
                if (attendance == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy bản ghi điểm danh." });
                }

                if (attendance.ThoiGianCheckOut != null)
                {
                    return Json(new { success = false, message = "Thành viên đã check-out rồi." });
                }

                if (attendance.ThoiGianCheckIn.Date != DateTime.Today)
                {
                    return Json(new { success = false, message = "Chỉ có thể check-out cho bản ghi hôm nay." });
                }

                // Perform checkout
                var checkOutSuccess = await _diemDanhService.CheckOutAsync(id);

                if (checkOutSuccess)
                {
                    var duration = DateTime.Now - attendance.ThoiGianCheckIn;
                    var durationText = $"{(int)duration.TotalHours}h {duration.Minutes}m";

                    return Json(new
                    {
                        success = true,
                        message = $"Check-out thành công! Thời gian tập: {durationText}",
                        duration = durationText
                    });
                }
                else
                {
                    return Json(new { success = false, message = "Không thể check-out. Vui lòng thử lại." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during manual checkout for attendance ID: {AttendanceId}", id);
                return Json(new { success = false, message = "Có lỗi xảy ra khi check-out." });
            }
        }

        [HttpDelete("Delete/{id}")]
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

        /// <summary>
        /// Lấy thông tin gói tập của member
        /// </summary>
        private async Task<object> GetMemberPackageInfoAsync(int memberId)
        {
            try
            {
                var activePackage = await _memberBenefitService.GetActivePackageAsync(memberId);

                if (activePackage?.GoiTap != null)
                {
                    var remainingDays = (activePackage.NgayKetThuc.ToDateTime(TimeOnly.MinValue) - DateTime.Today).Days;
                    var status = remainingDays > 7 ? "Còn hiệu lực" :
                                remainingDays > 0 ? "Sắp hết hạn" : "Hết hạn";

                    return new
                    {
                        hasPackage = true,
                        packageName = activePackage.GoiTap.TenGoi,
                        expiryDate = activePackage.NgayKetThuc.ToString("dd/MM/yyyy"),
                        remainingDays = Math.Max(0, remainingDays),
                        status = status,
                        isExpiring = remainingDays <= 7 && remainingDays > 0,
                        isExpired = remainingDays <= 0
                    };
                }
                else
                {
                    return new
                    {
                        hasPackage = false,
                        packageName = "Không có gói tập",
                        expiryDate = "",
                        remainingDays = 0,
                        status = "Chưa đăng ký gói tập",
                        isExpiring = false,
                        isExpired = false
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting package info for member {MemberId}", memberId);
                return new
                {
                    hasPackage = false,
                    packageName = "Lỗi tải thông tin",
                    expiryDate = "",
                    remainingDays = 0,
                    status = "Không thể tải thông tin gói tập",
                    isExpiring = false,
                    isExpired = false
                };
            }
        }
    }

    // DTO classes for face recognition with class selection
    public class FaceCheckInWithClassRequest
    {
        public double[] Descriptor { get; set; } = Array.Empty<double>();
        public int? ClassId { get; set; }
    }
}
