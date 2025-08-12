using GymManagement.Web.Data.Models;
using GymManagement.Web.Services;
using GymManagement.Web.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace GymManagement.Web.Controllers
{
    [Authorize]
    public class BookingController : Controller
    {
        private readonly IBookingService _bookingService;
        private readonly ILopHocService _lopHocService;
        private readonly INguoiDungService _nguoiDungService;
        private readonly IAuthService _authService;
        private readonly IEmailService _emailService;
        private readonly IAuthorizationService _authorizationService;
        private readonly IMemberBenefitService _memberBenefitService;
        private readonly ILogger<BookingController> _logger;

        public BookingController(
            IBookingService bookingService,
            ILopHocService lopHocService,
            INguoiDungService nguoiDungService,
            IAuthService authService,
            IEmailService emailService,
            IAuthorizationService authorizationService,
            IMemberBenefitService memberBenefitService,
            ILogger<BookingController> logger)
        {
            _bookingService = bookingService;
            _lopHocService = lopHocService;
            _nguoiDungService = nguoiDungService;
            _authService = authService;
            _emailService = emailService;
            _authorizationService = authorizationService;
            _memberBenefitService = memberBenefitService;
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

        // Helper method to get booking with authorization check
        private async Task<(Booking? booking, bool authorized)> GetAuthorizedBookingAsync(int bookingId, string operation)
        {
            var booking = await _bookingService.GetByIdAsync(bookingId);
            if (booking == null)
                return (null, false);

            var authorizationResult = operation switch
            {
                "Read" => await _authorizationService.AuthorizeAsync(User, booking, BookingOperations.Read),
                "Update" => await _authorizationService.AuthorizeAsync(User, booking, BookingOperations.Update),
                "Cancel" => await _authorizationService.AuthorizeAsync(User, booking, BookingOperations.Cancel),
                "Delete" => await _authorizationService.AuthorizeAsync(User, booking, BookingOperations.Delete),
                _ => AuthorizationResult.Failed()
            };

            return (booking, authorizationResult.Succeeded);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index()
        {
            try
            {
                // 🔒 IMPROVED: Additional authorization check
                var authorizationResult = await _authorizationService.AuthorizeAsync(
                    User, null, BookingOperations.ViewAll);

                if (!authorizationResult.Succeeded)
                {
                    return Forbid();
                }

                var bookings = await _bookingService.GetAllAsync();
                return View(bookings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting bookings");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải danh sách đặt lịch.";
                return View(new List<Booking>());
            }
        }

        [Authorize(Roles = "Member")]
        public async Task<IActionResult> MyBookings()
        {
            try
            {
                var user = await GetCurrentUserAsync();
                if (user?.NguoiDungId == null)
                {
                    return RedirectToAction("Login", "Auth");
                }

                // 🔒 IMPROVED: Ensure user can only see their own bookings
                var bookings = await _bookingService.GetByMemberIdAsync(user.NguoiDungId.Value);

                // Additional security: Filter bookings to ensure they belong to current user
                var filteredBookings = bookings.Where(b =>
                    b.ThanhVien?.TaiKhoan?.Id == User.FindFirst(ClaimTypes.NameIdentifier)?.Value).ToList();

                return View(filteredBookings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting user bookings");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải danh sách đặt lịch của bạn.";
                return View(new List<Booking>());
            }
        }

        [Authorize(Roles = "Admin,Member")]
        public async Task<IActionResult> Create()
        {
            await LoadSelectLists();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Booking booking)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var user = await GetCurrentUserAsync();

                    // If no member is selected, use current user
                    if (booking.ThanhVienId == null)
                    {
                        if (user?.NguoiDungId != null)
                        {
                            booking.ThanhVienId = user.NguoiDungId.Value;
                        }
                    }

                    // 🔒 IMPROVED: Authorization check - Members can only create bookings for themselves
                    if (User.IsInRole("Member") && user?.NguoiDungId != booking.ThanhVienId)
                    {
                        ModelState.AddModelError("", "Bạn chỉ có thể đặt lịch cho chính mình.");
                        await LoadSelectLists();
                        return View(booking);
                    }

                    // Validate required fields
                    if (booking.ThanhVienId == null || booking.LopHocId == null)
                    {
                        ModelState.AddModelError("", "Thông tin thành viên và lớp học là bắt buộc.");
                        await LoadSelectLists();
                        return View(booking);
                    }

                    // 🚀 IMPROVED: Use transaction-safe booking method
                    var bookingDate = booking.Ngay.ToDateTime(TimeOnly.MinValue);
                    var (success, errorMessage) = await _bookingService.BookClassWithTransactionAsync(
                        booking.ThanhVienId.Value,
                        booking.LopHocId.Value,
                        bookingDate,
                        booking.GhiChu);

                    if (success)
                    {
                        // Send booking confirmation email (async, non-blocking)
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                await SendClassBookingConfirmationEmailAsync(
                                    booking.ThanhVienId.Value,
                                    booking.LopHocId.Value,
                                    bookingDate);
                            }
                            catch (Exception emailEx)
                            {
                                _logger.LogWarning(emailEx, "Failed to send booking confirmation email");
                            }
                        });

                        TempData["SuccessMessage"] = "Đặt lịch thành công!";
                        return RedirectToAction(nameof(MyBookings));
                    }
                    else
                    {
                        ModelState.AddModelError("", errorMessage);
                        await LoadSelectLists();
                        return View(booking);
                    }
                }
                await LoadSelectLists();
                return View(booking);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating booking");
                ModelState.AddModelError("", "Có lỗi xảy ra khi đặt lịch.");
                await LoadSelectLists();
                return View(booking);
            }
        }

        /// <summary>
        /// API kiểm tra phí booking lớp học - Logic đơn giản và rõ ràng
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> CheckBookingFee(int classId)
        {
            try
            {
                var user = await GetCurrentUserAsync();
                if (user?.NguoiDungId == null)
                {
                    return Json(new { success = false, message = "Vui lòng đăng nhập." });
                }

                // Sử dụng service mới để kiểm tra phí
                var (canBook, isFree, fee, reason) = await _memberBenefitService.CanBookClassAsync(
                    user.NguoiDungId.Value, classId);

                return Json(new
                {
                    success = true,
                    canBook = canBook,
                    isFree = isFree,
                    fee = fee,
                    feeText = fee > 0 ? $"{fee:N0} VNĐ" : "Miễn phí",
                    reason = reason,
                    message = canBook ? reason : "Không thể đặt lịch"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking booking fee for class {ClassId}", classId);
                return Json(new { success = false, message = "Có lỗi xảy ra khi kiểm tra phí." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> BookClass(int classId, DateTime date, string? note = null)
        {
            try
            {
                var user = await GetCurrentUserAsync();
                if (user?.NguoiDungId == null)
                {
                    return Json(new { success = false, message = "Vui lòng đăng nhập để đặt lịch." });
                }

                // 🚀 IMPROVED: Use transaction-safe booking method
                var (success, errorMessage) = await _bookingService.BookClassWithTransactionAsync(
                    user.NguoiDungId.Value, classId, date, note);

                if (success)
                {
                    // Send booking confirmation email for class booking (async, non-blocking)
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await SendClassBookingConfirmationEmailAsync(user.NguoiDungId.Value, classId, date);
                        }
                        catch (Exception emailEx)
                        {
                            _logger.LogWarning(emailEx, "Failed to send booking confirmation email");
                        }
                    });

                    return Json(new { success = true, message = "Đặt lịch thành công!" });
                }
                else
                {
                    return Json(new { success = false, message = errorMessage });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while booking class");
                return Json(new { success = false, message = "Có lỗi xảy ra khi đặt lịch." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> BookSchedule(int scheduleId)
        {
            try
            {
                var user = await GetCurrentUserAsync();
                if (user?.NguoiDungId == null)
                {
                    return Json(new { success = false, message = "Vui lòng đăng nhập để đặt lịch." });
                }

                var result = await _bookingService.BookScheduleAsync(user.NguoiDungId.Value, scheduleId);
                if (result)
                {
                    return Json(new { success = true, message = "Đặt lịch thành công!" });
                }
                else
                {
                    return Json(new { success = false, message = "Không thể đặt lịch. Lớp có thể đã đầy hoặc bạn đã đặt lịch rồi." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while booking schedule");
                return Json(new { success = false, message = "Có lỗi xảy ra khi đặt lịch." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Cancel(int id)
        {
            try
            {
                // 🔒 IMPROVED: Resource-based authorization using helper method
                var (booking, authorized) = await GetAuthorizedBookingAsync(id, "Cancel");

                if (booking == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy đặt lịch." });
                }

                if (!authorized)
                {
                    return Json(new { success = false, message = "Bạn không có quyền hủy đặt lịch này." });
                }

                var result = await _bookingService.CancelBookingAsync(id);
                if (result)
                {
                    // Send cancellation email (async, non-blocking)
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await SendBookingCancellationEmailAsync(id);
                        }
                        catch (Exception emailEx)
                        {
                            _logger.LogWarning(emailEx, "Failed to send cancellation email for booking {BookingId}", id);
                        }
                    });

                    return Json(new { success = true, message = "Hủy đặt lịch thành công!" });
                }
                else
                {
                    return Json(new { success = false, message = "Không thể hủy đặt lịch." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while canceling booking");
                return Json(new { success = false, message = "Có lỗi xảy ra khi hủy đặt lịch." });
            }
        }

        [HttpGet]
        public async Task<IActionResult> CheckAvailability(int classId, DateTime date)
        {
            try
            {
                var user = await GetCurrentUserAsync();
                if (user?.NguoiDungId == null)
                {
                    return Json(new { canBook = false, message = "Vui lòng đăng nhập." });
                }

                var canBook = await _bookingService.CanBookAsync(user.NguoiDungId.Value, classId, date);
                var availableSlots = await _bookingService.GetAvailableSlotsAsync(classId, date);

                return Json(new { 
                    canBook = canBook, 
                    availableSlots = availableSlots,
                    message = canBook ? "Có thể đặt lịch" : "Không thể đặt lịch"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while checking booking availability");
                return Json(new { canBook = false, message = "Có lỗi xảy ra." });
            }
        }

        [Authorize(Roles = "Admin,Trainer")]
        public async Task<IActionResult> TodayBookings()
        {
            try
            {
                // 🔒 IMPROVED: Additional authorization check
                var authorizationResult = await _authorizationService.AuthorizeAsync(
                    User, null, BookingOperations.ViewAll);

                if (!authorizationResult.Succeeded)
                {
                    return Forbid();
                }

                var bookings = await _bookingService.GetTodayBookingsAsync();
                return View(bookings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting today's bookings");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải danh sách đặt lịch hôm nay.";
                return View(new List<Booking>());
            }
        }

        [Authorize(Roles = "Admin,Member")]
        public async Task<IActionResult> Calendar()
        {
            try
            {
                var classes = await _lopHocService.GetActiveClassesAsync();
                ViewBag.Classes = classes;
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while loading calendar");
                ViewBag.Classes = new List<LopHoc>();
                return View();
            }
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Member")]
        public async Task<IActionResult> GetCalendarEvents(DateTime start, DateTime end, int? classId = null)
        {
            try
            {
                var user = await GetCurrentUserAsync();
                List<object> events = new List<object>();

                if (User.IsInRole("Member") && user?.NguoiDungId != null)
                {
                    // For members, show their bookings and available classes
                    var bookings = await _bookingService.GetByMemberIdAsync(user.NguoiDungId.Value);
                    var startDateOnly = DateOnly.FromDateTime(start.Date);
                    var endDateOnly = DateOnly.FromDateTime(end.Date);

                    // Add member's bookings
                    var bookingEvents = bookings
                        .Where(b => b.Ngay >= startDateOnly && b.Ngay <= endDateOnly)
                        .Select(b => new {
                            id = b.BookingId,
                            title = $"✅ {GetShortClassName(b.LopHoc?.TenLop)}",
                            start = b.Ngay.ToString("yyyy-MM-dd") + "T" + (b.LichLop?.GioBatDau.ToString("HH:mm") ?? b.LopHoc?.GioBatDau.ToString("HH:mm") ?? "08:00"),
                            end = b.Ngay.ToString("yyyy-MM-dd") + "T" + (b.LichLop?.GioKetThuc.ToString("HH:mm") ?? b.LopHoc?.GioKetThuc.ToString("HH:mm") ?? "09:00"),
                            backgroundColor = GetEventColor(b.TrangThai),
                            borderColor = GetEventColor(b.TrangThai),
                            textColor = "#FFFFFF",
                            status = b.TrangThai,
                            type = "booking",
                            fullTitle = b.LopHoc?.TenLop ?? "Lớp học"
                        });

                    events.AddRange(bookingEvents);

                    // Add available classes (both with and without LichLop)
                    var availableClasses = await _lopHocService.GetActiveClassesAsync();
                    if (classId.HasValue)
                    {
                        availableClasses = availableClasses.Where(c => c.LopHocId == classId.Value);
                    }

                    // For classes with LichLop schedules
                    var classEventsWithSchedule = availableClasses
                        .Where(c => c.LichLops != null && c.LichLops.Any())
                        .SelectMany(c => c.LichLops.Where(l => 
                            l.Ngay >= startDateOnly && 
                            l.Ngay <= endDateOnly &&
                            l.TrangThai == "SCHEDULED"))
                        .Select(l => new {
                            id = $"class_{l.LichLopId}_{l.Ngay:yyyyMMdd}",
                            title = $"🏃 {GetShortClassName(l.LopHoc?.TenLop)}",
                            start = l.Ngay.ToString("yyyy-MM-dd") + "T" + l.GioBatDau.ToString("HH:mm"),
                            end = l.Ngay.ToString("yyyy-MM-dd") + "T" + l.GioKetThuc.ToString("HH:mm"),
                            backgroundColor = "#059669",
                            borderColor = "#059669",
                            textColor = "#FFFFFF",
                            status = "AVAILABLE",
                            type = "class",
                            lopHocId = l.LopHocId,
                            lichLopId = l.LichLopId,
                            fullTitle = l.LopHoc?.TenLop ?? "Lớp học"
                        });

                    events.AddRange(classEventsWithSchedule);

                    // For classes without LichLop schedules (regular weekly classes)
                    var classEventsWithoutSchedule = availableClasses
                        .Where(c => c.LichLops == null || !c.LichLops.Any())
                        .SelectMany(c => GenerateWeeklyClassEvents(c, startDateOnly, endDateOnly))
                        .Select(evt => new {
                            id = $"weekly_class_{evt.LopHocId}_{evt.Date:yyyyMMdd}",
                            title = $"📅 {GetShortClassName(evt.TenLop)}",
                            start = evt.Date.ToString("yyyy-MM-dd") + "T" + evt.GioBatDau.ToString("HH:mm"),
                            end = evt.Date.ToString("yyyy-MM-dd") + "T" + evt.GioKetThuc.ToString("HH:mm"),
                            backgroundColor = "#0891B2",
                            borderColor = "#0891B2",
                            textColor = "#FFFFFF",
                            status = "AVAILABLE",
                            type = "weekly_class",
                            lopHocId = evt.LopHocId,
                            fullTitle = evt.TenLop
                        });

                    events.AddRange(classEventsWithoutSchedule);
                }
                else if (User.IsInRole("Admin"))
                {
                    // For admin, show all bookings
                    var allBookings = await _bookingService.GetAllAsync();
                    var startDateOnly = DateOnly.FromDateTime(start.Date);
                    var endDateOnly = DateOnly.FromDateTime(end.Date);

                    var adminEvents = allBookings
                        .Where(b => b.Ngay >= startDateOnly && b.Ngay <= endDateOnly)
                        .Select(b => new {
                            id = b.BookingId,
                            title = $"{GetShortClassName(b.LopHoc?.TenLop)} - {GetShortMemberName(b.ThanhVien)}",
                            start = b.Ngay.ToString("yyyy-MM-dd") + "T" + (b.LichLop?.GioBatDau.ToString("HH:mm") ?? b.LopHoc?.GioBatDau.ToString("HH:mm") ?? "08:00"),
                            end = b.Ngay.ToString("yyyy-MM-dd") + "T" + (b.LichLop?.GioKetThuc.ToString("HH:mm") ?? b.LopHoc?.GioKetThuc.ToString("HH:mm") ?? "09:00"),
                            backgroundColor = GetEventColor(b.TrangThai),
                            borderColor = GetEventColor(b.TrangThai),
                            textColor = "#FFFFFF",
                            status = b.TrangThai,
                            type = "booking",
                            fullTitle = $"{b.LopHoc?.TenLop} - {b.ThanhVien?.Ho} {b.ThanhVien?.Ten}"
                        });

                    events.AddRange(adminEvents);

                    // Also add available classes for admin
                    var availableClasses = await _lopHocService.GetActiveClassesAsync();
                    var availableClassEvents = availableClasses
                        .SelectMany(c => GenerateWeeklyClassEvents(c, DateOnly.FromDateTime(start.Date), DateOnly.FromDateTime(end.Date)))
                        .Select(evt => new {
                            id = $"admin_class_{evt.LopHocId}_{evt.Date:yyyyMMdd}",
                            title = $"📚 {GetShortClassName(evt.TenLop)}",
                            start = evt.Date.ToString("yyyy-MM-dd") + "T" + evt.GioBatDau.ToString("HH:mm"),
                            end = evt.Date.ToString("yyyy-MM-dd") + "T" + evt.GioKetThuc.ToString("HH:mm"),
                            backgroundColor = "#6B7280",
                            borderColor = "#6B7280",
                            textColor = "#FFFFFF",
                            status = "AVAILABLE",
                            type = "admin_class",
                            lopHocId = evt.LopHocId,
                            fullTitle = evt.TenLop
                        });

                    events.AddRange(availableClassEvents);
                }

                return Json(events);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting calendar events");
                return Json(new List<object>());
            }
        }

        private IEnumerable<dynamic> GenerateWeeklyClassEvents(LopHoc lopHoc, DateOnly startDate, DateOnly endDate)
        {
            var events = new List<dynamic>();
            
            // Parse ThuTrongTuan (e.g., "Mon,Wed,Fri" or "2,4,6")
            var daysOfWeek = ParseDaysOfWeek(lopHoc.ThuTrongTuan);
            
            for (var date = startDate; date <= endDate; date = date.AddDays(1))
            {
                var dayOfWeek = (int)date.DayOfWeek;
                if (dayOfWeek == 0) dayOfWeek = 7; // Convert Sunday from 0 to 7
                
                if (daysOfWeek.Contains(dayOfWeek))
                {
                    events.Add(new {
                        LopHocId = lopHoc.LopHocId,
                        TenLop = lopHoc.TenLop,
                        Date = date,
                        GioBatDau = lopHoc.GioBatDau,
                        GioKetThuc = lopHoc.GioKetThuc
                    });
                }
            }
            
            return events;
        }

        private List<int> ParseDaysOfWeek(string thuTrongTuan)
        {
            var days = new List<int>();
            
            if (string.IsNullOrEmpty(thuTrongTuan))
                return days;
                
            var parts = thuTrongTuan.Split(',', StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var part in parts)
            {
                var trimmed = part.Trim();
                
                // Try to parse as number first (1=Monday, 2=Tuesday, etc.)
                if (int.TryParse(trimmed, out int dayNum) && dayNum >= 1 && dayNum <= 7)
                {
                    days.Add(dayNum);
                }
                else
                {
                    // Try to parse as day name
                    var dayOfWeek = trimmed.ToLower() switch
                    {
                        "mon" or "monday" or "thứ 2" => 1,
                        "tue" or "tuesday" or "thứ 3" => 2,
                        "wed" or "wednesday" or "thứ 4" => 3,
                        "thu" or "thursday" or "thứ 5" => 4,
                        "fri" or "friday" or "thứ 6" => 5,
                        "sat" or "saturday" or "thứ 7" => 6,
                        "sun" or "sunday" or "chủ nhật" => 7,
                        _ => 0
                    };
                    
                    if (dayOfWeek > 0)
                    {
                        days.Add(dayOfWeek);
                    }
                }
            }
            
            return days;
        }

        private string GetEventColor(string status)
        {
            return status switch
            {
                "BOOKED" => "#2563EB",      // Darker Blue
                "CANCELED" => "#6B7280",    // Gray
                "ATTENDED" => "#7C3AED",    // Purple
                "AVAILABLE" => "#059669",   // Green
                _ => "#D97706"              // Orange
            };
        }

        private string GetShortClassName(string? className)
        {
            if (string.IsNullOrEmpty(className))
                return "Lớp học";
                
            // Rút gọn tên lớp nếu quá dài
            if (className.Length > 15)
            {
                return className.Substring(0, 12) + "...";
            }
            
            return className;
        }

        private string GetShortMemberName(NguoiDung? member)
        {
            if (member == null)
                return "TV";
                
            // Chỉ hiển thị tên hoặc viết tắt
            if (!string.IsNullOrEmpty(member.Ten))
            {
                return member.Ten.Length > 8 ? member.Ten.Substring(0, 6) + ".." : member.Ten;
            }
            
            if (!string.IsNullOrEmpty(member.Ho))
            {
                return member.Ho.Substring(0, Math.Min(2, member.Ho.Length)).ToUpper();
            }
            
            return "TV";
        }



        #region Email Helper Methods

        private async Task SendBookingConfirmationEmailAsync(Booking booking)
        {
            try
            {
                var member = await _nguoiDungService.GetByIdAsync(booking.ThanhVienId ?? 0);
                var trainer = booking.LopHoc?.HlvId != null ? await _nguoiDungService.GetByIdAsync(booking.LopHoc.HlvId.Value) : null;
                
                if (member != null && !string.IsNullOrEmpty(member.Email))
                {
                    var sessionType = booking.LopHoc?.TenLop ?? "Buổi tập";
                    var instructorName = trainer != null ? $"{trainer.Ho} {trainer.Ten}" : "Đang cập nhật";
                    
                    await _emailService.SendBookingConfirmationEmailAsync(
                        member.Email,
                        $"{member.Ho} {member.Ten}",
                        sessionType,
                        booking.Ngay.ToDateTime(TimeOnly.MinValue),
                        instructorName
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending booking confirmation email for booking {BookingId}", booking.BookingId);
            }
        }

        private async Task SendClassBookingConfirmationEmailAsync(int memberId, int classId, DateTime date)
        {
            try
            {
                var member = await _nguoiDungService.GetByIdAsync(memberId);
                var lopHoc = await _lopHocService.GetByIdAsync(classId);
                var trainer = lopHoc?.HlvId != null ? await _nguoiDungService.GetByIdAsync(lopHoc.HlvId.Value) : null;
                
                if (member != null && lopHoc != null && !string.IsNullOrEmpty(member.Email))
                {
                    var instructorName = trainer != null ? $"{trainer.Ho} {trainer.Ten}" : "Đang cập nhật";
                    
                    await _emailService.SendBookingConfirmationEmailAsync(
                        member.Email,
                        $"{member.Ho} {member.Ten}",
                        lopHoc.TenLop,
                        date,
                        instructorName
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending class booking confirmation email for member {MemberId}, class {ClassId}", memberId, classId);
            }
        }

        private async Task SendBookingCancellationEmailAsync(int bookingId)
        {
            try
            {
                var booking = await _bookingService.GetByIdAsync(bookingId);
                if (booking == null) return;

                var member = await _nguoiDungService.GetByIdAsync(booking.ThanhVienId ?? 0);
                
                if (member != null && !string.IsNullOrEmpty(member.Email))
                {
                    var sessionType = booking.LopHoc?.TenLop ?? "Buổi tập";
                    var sessionTime = booking.Ngay.ToDateTime(TimeOnly.MinValue);
                    var reason = "Theo yêu cầu của thành viên";
                    
                    await _emailService.SendBookingCancellationEmailAsync(
                        member.Email,
                        $"{member.Ho} {member.Ten}",
                        sessionType,
                        sessionTime,
                        reason
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending booking cancellation email for booking {BookingId}", bookingId);
            }
        }

        #endregion

        private async Task LoadSelectLists()
        {
            try
            {
                var classes = await _lopHocService.GetActiveClassesAsync();
                ViewBag.Classes = new SelectList(classes, "LopHocId", "TenLop");

                // Only load members for admin
                if (User.IsInRole("Admin"))
                {
                    var members = await _nguoiDungService.GetMembersAsync();
                    ViewBag.Members = new SelectList(members, "NguoiDungId", "Ho");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while loading select lists");
                ViewBag.Classes = new SelectList(new List<LopHoc>(), "LopHocId", "TenLop");
                ViewBag.Members = new SelectList(new List<NguoiDung>(), "NguoiDungId", "Ho");
            }
        }
    }
}
