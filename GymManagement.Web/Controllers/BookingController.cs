using GymManagement.Web.Data.Models;
using GymManagement.Web.Services;
using GymManagement.Web.Authorization;
using GymManagement.Web.Data.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

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
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<BookingController> _logger;

        public BookingController(
            IBookingService bookingService,
            ILopHocService lopHocService,
            INguoiDungService nguoiDungService,
            IAuthService authService,
            IEmailService emailService,
            IAuthorizationService authorizationService,
            IMemberBenefitService memberBenefitService,
            IUnitOfWork unitOfWork,
            ILogger<BookingController> logger)
        {
            _bookingService = bookingService;
            _lopHocService = lopHocService;
            _nguoiDungService = nguoiDungService;
            _authService = authService;
            _emailService = emailService;
            _authorizationService = authorizationService;
            _memberBenefitService = memberBenefitService;
            _unitOfWork = unitOfWork;
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
                        // Send booking confirmation email (fire and forget)
                        _ = SendClassBookingConfirmationEmailAsync(
                            booking.ThanhVienId.Value,
                            booking.LopHocId.Value,
                            bookingDate);

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

        // ✅ DTO for JSON binding
        public class BookClassRequest
        {
            public int ClassId { get; set; }
            public DateTime Date { get; set; }
            public string? Note { get; set; }
        }

        [HttpPost]
        public async Task<IActionResult> BookClass([FromBody] BookClassRequest request)
        {
            try
            {
                var user = await GetCurrentUserAsync();
                if (user?.NguoiDungId == null)
                {
                    return Json(new { success = false, message = "Vui lòng đăng nhập để đặt lịch." });
                }

                // ✅ IMPROVED: Add logging for debugging date issues
                _logger.LogInformation("📅 BookClass request: ClassId={ClassId}, Date={Date:yyyy-MM-dd HH:mm:ss}, User={UserId}",
                    request.ClassId, request.Date, user.NguoiDungId.Value);

                // 🚀 IMPROVED: Use transaction-safe booking method
                var (success, errorMessage) = await _bookingService.BookClassWithTransactionAsync(
                    user.NguoiDungId.Value, request.ClassId, request.Date, request.Note);

                if (success)
                {
                    // Send booking confirmation email for class booking (fire and forget)
                    _ = SendClassBookingConfirmationEmailAsync(user.NguoiDungId.Value, request.ClassId, request.Date);

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

                // Note: BookScheduleAsync method removed - use BookClassAsync instead
                var result = await _bookingService.BookClassAsync(user.NguoiDungId.Value, scheduleId, DateTime.Now);
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
                            start = b.Ngay.ToString("yyyy-MM-dd") + "T" + (b.LopHoc?.GioBatDau.ToString("HH:mm") ?? "08:00"),
                            end = b.Ngay.ToString("yyyy-MM-dd") + "T" + (b.LopHoc?.GioKetThuc.ToString("HH:mm") ?? "09:00"),
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

                    // Generate dynamic schedules for classes with capacity check
                    var classEventsWithSchedule = new List<object>();
                    foreach (var lopHoc in availableClasses)
                    {
                        var thuTrongTuan = lopHoc.ThuTrongTuan.Split(',').Select(t => t.Trim()).ToList();
                        var currentDate = start;
                        while (currentDate <= end)
                        {
                            var dayOfWeek = GetVietnameseDayOfWeek(currentDate.DayOfWeek);
                            if (thuTrongTuan.Contains(dayOfWeek))
                            {
                                // Check capacity for this specific date (both Bookings and DangKys)
                                var dateOnly = DateOnly.FromDateTime(currentDate);

                                // Count individual bookings for this specific date
                                var currentBookings = await _unitOfWork.Context.Bookings
                                    .Where(b => b.LopHocId == lopHoc.LopHocId &&
                                               b.Ngay == dateOnly &&
                                               b.TrangThai == "BOOKED")
                                    .CountAsync();

                                // Count active registrations (DangKys) that cover this date
                                var activeRegistrations = await _unitOfWork.Context.DangKys
                                    .Where(d => d.LopHocId == lopHoc.LopHocId &&
                                               d.TrangThai == "ACTIVE" &&
                                               d.NgayBatDau <= dateOnly &&
                                               d.NgayKetThuc >= dateOnly)
                                    .CountAsync();

                                // Total occupied slots = bookings + registrations
                                var totalOccupied = currentBookings + activeRegistrations;
                                var availableSlots = lopHoc.SucChua - totalOccupied;
                                var fillRate = lopHoc.SucChua > 0 ? (double)totalOccupied / lopHoc.SucChua * 100 : 0;

                                // Determine color and status based on capacity
                                string backgroundColor, borderColor, status, icon;
                                bool isFull = totalOccupied >= lopHoc.SucChua;

                                if (isFull)
                                {
                                    backgroundColor = "#FCA5A5"; // Light red for full classes
                                    borderColor = "#EF4444";     // Red border
                                    status = "FULL";
                                    icon = "🚫";
                                }
                                else if (fillRate >= 80)
                                {
                                    backgroundColor = "#FDE68A"; // Light yellow for nearly full
                                    borderColor = "#F59E0B";     // Yellow border
                                    status = "NEARLY_FULL";
                                    icon = "⚠️";
                                }
                                else
                                {
                                    backgroundColor = "#86EFAC"; // Light green for available
                                    borderColor = "#10B981";     // Green border
                                    status = "AVAILABLE";
                                    icon = "📚";
                                }

                                classEventsWithSchedule.Add(new {
                                    id = $"class_{lopHoc.LopHocId}_{currentDate:yyyyMMdd}",
                                    title = $"{icon} {GetShortClassName(lopHoc.TenLop)} ({availableSlots}/{lopHoc.SucChua})",
                                    start = currentDate.ToString("yyyy-MM-dd") + "T" + lopHoc.GioBatDau.ToString("HH:mm"),
                                    end = currentDate.ToString("yyyy-MM-dd") + "T" + lopHoc.GioKetThuc.ToString("HH:mm"),
                                    backgroundColor = backgroundColor,
                                    borderColor = borderColor,
                                    textColor = isFull ? "#7F1D1D" : "#FFFFFF", // Dark red text for full classes
                                    type = "class",
                                    lopHocId = lopHoc.LopHocId,
                                    fullTitle = lopHoc.TenLop,
                                    status = status,
                                    isFull = isFull,
                                    availableSlots = availableSlots,
                                    totalCapacity = lopHoc.SucChua,
                                    currentBookings = currentBookings,
                                    activeRegistrations = activeRegistrations,
                                    totalOccupied = totalOccupied,
                                    fillRate = Math.Round(fillRate, 1)
                                });
                            }
                            currentDate = currentDate.AddDays(1);
                        }
                    }
                    events.AddRange(classEventsWithSchedule);

                    // Note: All classes now use dynamic schedule generation
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
                            start = b.Ngay.ToString("yyyy-MM-dd") + "T" + (b.LopHoc?.GioBatDau.ToString("HH:mm") ?? "08:00"),
                            end = b.Ngay.ToString("yyyy-MM-dd") + "T" + (b.LopHoc?.GioKetThuc.ToString("HH:mm") ?? "09:00"),
                            backgroundColor = GetEventColor(b.TrangThai),
                            borderColor = GetEventColor(b.TrangThai),
                            textColor = "#FFFFFF",
                            status = b.TrangThai,
                            type = "booking",
                            fullTitle = $"{b.LopHoc?.TenLop} - {b.ThanhVien?.Ho} {b.ThanhVien?.Ten}"
                        });

                    events.AddRange(adminEvents);

                    // Also add available classes for admin with capacity info
                    var availableClasses = await _lopHocService.GetActiveClassesAsync();
                    var availableClassEvents = new List<object>();

                    foreach (var lopHoc in availableClasses)
                    {
                        var weeklyEvents = GenerateWeeklyClassEvents(lopHoc, DateOnly.FromDateTime(start.Date), DateOnly.FromDateTime(end.Date));
                        foreach (var evt in weeklyEvents)
                        {
                            // Check capacity for this specific date (both Bookings and DangKys)
                            var currentBookings = await _unitOfWork.Context.Bookings
                                .Where(b => b.LopHocId == evt.LopHocId &&
                                           b.Ngay == evt.Date &&
                                           b.TrangThai == "BOOKED")
                                .CountAsync();

                            // Count active registrations (DangKys) that cover this date
                            var activeRegistrations = await _unitOfWork.Context.DangKys
                                .Where(d => d.LopHocId == evt.LopHocId &&
                                           d.TrangThai == "ACTIVE" &&
                                           d.NgayBatDau <= evt.Date &&
                                           d.NgayKetThuc >= evt.Date)
                                .CountAsync();

                            // Total occupied slots = bookings + registrations
                            var totalOccupied = currentBookings + activeRegistrations;
                            var availableSlots = lopHoc.SucChua - totalOccupied;
                            var fillRate = lopHoc.SucChua > 0 ? (double)totalOccupied / lopHoc.SucChua * 100 : 0;

                            // Determine color and status based on capacity
                            string backgroundColor, borderColor, status, icon;
                            bool isFull = totalOccupied >= lopHoc.SucChua;

                            if (isFull)
                            {
                                backgroundColor = "#FCA5A5"; // Light red for full classes
                                borderColor = "#EF4444";     // Red border
                                status = "FULL";
                                icon = "🚫";
                            }
                            else if (fillRate >= 80)
                            {
                                backgroundColor = "#FDE68A"; // Light yellow for nearly full
                                borderColor = "#F59E0B";     // Yellow border
                                status = "NEARLY_FULL";
                                icon = "⚠️";
                            }
                            else
                            {
                                backgroundColor = "#D1D5DB"; // Light gray for admin view
                                borderColor = "#6B7280";     // Gray border
                                status = "AVAILABLE";
                                icon = "📚";
                            }

                            availableClassEvents.Add(new {
                                id = $"admin_class_{evt.LopHocId}_{evt.Date:yyyyMMdd}",
                                title = $"{icon} {GetShortClassName(evt.TenLop)} ({totalOccupied}/{lopHoc.SucChua})",
                                start = evt.Date.ToString("yyyy-MM-dd") + "T" + evt.GioBatDau.ToString("HH:mm"),
                                end = evt.Date.ToString("yyyy-MM-dd") + "T" + evt.GioKetThuc.ToString("HH:mm"),
                                backgroundColor = backgroundColor,
                                borderColor = borderColor,
                                textColor = isFull ? "#7F1D1D" : "#374151", // Dark text for admin view
                                status = status,
                                type = "admin_class",
                                lopHocId = evt.LopHocId,
                                fullTitle = evt.TenLop,
                                isFull = isFull,
                                availableSlots = availableSlots,
                                totalCapacity = lopHoc.SucChua,
                                currentBookings = currentBookings,
                                activeRegistrations = activeRegistrations,
                                totalOccupied = totalOccupied,
                                fillRate = Math.Round(fillRate, 1)
                            });
                        }
                    }

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

        private IEnumerable<WeeklyClassEvent> GenerateWeeklyClassEvents(LopHoc lopHoc, DateOnly startDate, DateOnly endDate)
        {
            var events = new List<WeeklyClassEvent>();

            // Parse ThuTrongTuan (e.g., "Mon,Wed,Fri" or "2,4,6")
            var daysOfWeek = ParseDaysOfWeek(lopHoc.ThuTrongTuan);

            for (var date = startDate; date <= endDate; date = date.AddDays(1))
            {
                var dayOfWeek = (int)date.DayOfWeek;
                if (dayOfWeek == 0) dayOfWeek = 7; // Convert Sunday from 0 to 7

                if (daysOfWeek.Contains(dayOfWeek))
                {
                    events.Add(new WeeklyClassEvent
                    {
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
                    var allUsers = await _nguoiDungService.GetAllAsync();
                    var userList = allUsers
                        .Where(u => u.TrangThai == "ACTIVE" &&
                                   (u.LoaiNguoiDung == "THANHVIEN" || u.LoaiNguoiDung == "VANGLAI")) // Chỉ thành viên và vãng lai
                        .OrderBy(u => u.Ho)
                        .ThenBy(u => u.Ten)
                        .Select(u => new {
                            NguoiDungId = u.NguoiDungId,
                            FullName = $"{u.Ho} {u.Ten}".Trim(),
                            UserType = u.LoaiNguoiDung switch
                            {
                                "THANHVIEN" => "👤",
                                "VANGLAI" => "🚶",
                                _ => "👤"
                            },
                            DisplayName = $"{u.Ho} {u.Ten}".Trim() + $" ({u.LoaiNguoiDung switch
                            {
                                "THANHVIEN" => "👤 Thành viên",
                                "VANGLAI" => "🚶 Vãng lai",
                                _ => "👤 Thành viên"
                            }})"
                        })
                        .ToList();

                    ViewBag.Members = new SelectList(userList, "NguoiDungId", "DisplayName");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while loading select lists");
                ViewBag.Classes = new SelectList(new List<LopHoc>(), "LopHocId", "TenLop");
                ViewBag.Members = new SelectList(new List<NguoiDung>(), "NguoiDungId", "Ho");
            }
        }

        /// <summary>
        /// Helper method to convert DayOfWeek to Vietnamese
        /// </summary>
        private string GetVietnameseDayOfWeek(DayOfWeek dayOfWeek)
        {
            return dayOfWeek switch
            {
                DayOfWeek.Monday => "Thứ 2",
                DayOfWeek.Tuesday => "Thứ 3",
                DayOfWeek.Wednesday => "Thứ 4",
                DayOfWeek.Thursday => "Thứ 5",
                DayOfWeek.Friday => "Thứ 6",
                DayOfWeek.Saturday => "Thứ 7",
                DayOfWeek.Sunday => "Chủ nhật",
                _ => ""
            };
        }

        // ✅ NEW: API endpoints for booking management

        /// <summary>
        /// API để xem chi tiết booking
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetBookingDetails(int id)
        {
            try
            {
                var booking = await _bookingService.GetByIdAsync(id);
                if (booking == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy đặt lịch." });
                }

                var details = new
                {
                    BookingId = booking.BookingId,
                    ThanhVien = booking.ThanhVien != null ? new
                    {
                        HoTen = $"{booking.ThanhVien.Ho} {booking.ThanhVien.Ten}".Trim(),
                        Email = booking.ThanhVien.Email,
                        SoDienThoai = booking.ThanhVien.SoDienThoai,
                        LoaiNguoiDung = booking.ThanhVien.LoaiNguoiDung
                    } : null,
                    LopHoc = booking.LopHoc != null ? new
                    {
                        TenLop = booking.LopHoc.TenLop,
                        GioBatDau = booking.LopHoc.GioBatDau.ToString("HH:mm"),
                        GioKetThuc = booking.LopHoc.GioKetThuc.ToString("HH:mm"),
                        HuanLuyenVien = booking.LopHoc.Hlv != null ? $"{booking.LopHoc.Hlv.Ho} {booking.LopHoc.Hlv.Ten}".Trim() : "Chưa phân công",
                        SucChua = booking.LopHoc.SucChua
                    } : null,
                    Ngay = booking.Ngay.ToString("dd/MM/yyyy"),
                    TrangThai = booking.TrangThai,
                    NgayTao = booking.NgayTao.ToString("dd/MM/yyyy HH:mm"),
                    GhiChu = booking.GhiChu
                };

                return Json(new { success = true, data = details });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting booking details for ID: {BookingId}", id);
                return Json(new { success = false, message = "Có lỗi xảy ra khi tải chi tiết đặt lịch." });
            }
        }

        /// <summary>
        /// API để hủy booking
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CancelBooking(int id)
        {
            try
            {
                var result = await _bookingService.CancelBookingAsync(id);
                if (result)
                {
                    return Json(new { success = true, message = "Đã hủy đặt lịch thành công." });
                }
                else
                {
                    return Json(new { success = false, message = "Không thể hủy đặt lịch này." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error canceling booking ID: {BookingId}", id);
                return Json(new { success = false, message = "Có lỗi xảy ra khi hủy đặt lịch." });
            }
        }

        /// <summary>
        /// API để đánh dấu đã tham gia
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> MarkAttended(int id)
        {
            try
            {
                var booking = await _bookingService.GetByIdAsync(id);
                if (booking == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy đặt lịch." });
                }

                if (booking.TrangThai != "BOOKED")
                {
                    return Json(new { success = false, message = "Chỉ có thể điểm danh cho đặt lịch đã xác nhận." });
                }

                if (booking.Ngay != DateOnly.FromDateTime(DateTime.Today))
                {
                    return Json(new { success = false, message = "Chỉ có thể điểm danh cho lớp học hôm nay." });
                }

                // Update booking status to ATTENDED
                booking.TrangThai = "ATTENDED";
                await _bookingService.UpdateAsync(booking);

                return Json(new { success = true, message = "Đã đánh dấu tham gia thành công." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking booking as attended ID: {BookingId}", id);
                return Json(new { success = false, message = "Có lỗi xảy ra khi đánh dấu tham gia." });
            }
        }

        /// <summary>
        /// API để kiểm tra trạng thái thanh toán của booking
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CheckBookingPaymentStatus(int id)
        {
            try
            {
                var booking = await _bookingService.GetByIdAsync(id);
                if (booking == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy đặt lịch." });
                }

                if (booking.ThanhVienId == null)
                {
                    return Json(new { success = false, message = "Booking không có thông tin thành viên." });
                }

                // Kiểm tra member có gói tập không
                var (canBook, isFree, fee, reason) = await _memberBenefitService.CanBookClassAsync(
                    booking.ThanhVienId.Value, booking.LopHocId ?? 0);

                return Json(new
                {
                    success = true,
                    isFree = isFree,
                    fee = fee,
                    feeText = fee > 0 ? $"{fee:N0} VNĐ" : "Miễn phí",
                    reason = reason,
                    memberName = booking.ThanhVien != null ? $"{booking.ThanhVien.Ho} {booking.ThanhVien.Ten}".Trim() : "N/A",
                    className = booking.LopHoc?.TenLop ?? "N/A"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking payment status for booking ID: {BookingId}", id);
                return Json(new { success = false, message = "Có lỗi xảy ra khi kiểm tra trạng thái thanh toán." });
            }
        }

        /// <summary>
        /// API để thanh toán và check-in tại quầy
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> PayAndCheckIn([FromBody] PayAndCheckInRequest request)
        {
            try
            {
                if (request == null || request.Id <= 0)
                {
                    return Json(new { success = false, message = "Dữ liệu không hợp lệ." });
                }

                var booking = await _bookingService.GetByIdAsync(request.Id);
                if (booking == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy đặt lịch." });
                }

                if (booking.ThanhVienId == null || booking.LopHocId == null)
                {
                    return Json(new { success = false, message = "Booking thiếu thông tin cần thiết." });
                }

                if (booking.TrangThai != "BOOKED")
                {
                    return Json(new { success = false, message = "Chỉ có thể xử lý booking đã xác nhận." });
                }

                if (booking.Ngay != DateOnly.FromDateTime(DateTime.Today))
                {
                    return Json(new { success = false, message = "Chỉ có thể check-in cho lớp học hôm nay." });
                }

                // Kiểm tra phí
                var (canBook, isFree, fee, reason) = await _memberBenefitService.CanBookClassAsync(
                    booking.ThanhVienId.Value, booking.LopHocId.Value);

                if (!canBook)
                {
                    return Json(new { success = false, message = reason });
                }

                // Nếu có phí, tạo payment record
                if (!isFree && fee > 0)
                {
                    var thanhToanService = HttpContext.RequestServices.GetRequiredService<IThanhToanService>();

                    // Tạo payment record cho booking
                    var payment = new ThanhToan
                    {
                        SoTien = fee,
                        PhuongThuc = "CASH", // Thanh toán tại quầy
                        TrangThai = "SUCCESS", // Đã thanh toán
                        NgayThanhToan = DateTime.Now,
                        GhiChu = $"Thanh toán tại quầy cho booking ID: {booking.BookingId} - Lớp: {booking.LopHoc?.TenLop}"
                    };

                    await thanhToanService.CreateAsync(payment);
                }

                // Check-in member
                var diemDanhService = HttpContext.RequestServices.GetRequiredService<IDiemDanhService>();
                var checkInSuccess = await diemDanhService.CheckInWithClassAsync(
                    booking.ThanhVienId.Value, booking.LopHocId.Value, "Check-in tại quầy");

                if (!checkInSuccess)
                {
                    return Json(new { success = false, message = "Không thể check-in. Member có thể đã check-in rồi." });
                }

                // Update booking status
                booking.TrangThai = "ATTENDED";
                await _bookingService.UpdateAsync(booking);

                var memberName = booking.ThanhVien != null ? $"{booking.ThanhVien.Ho} {booking.ThanhVien.Ten}".Trim() : "N/A";
                var message = isFree ?
                    $"Check-in thành công cho {memberName} (Miễn phí với gói tập)" :
                    $"Thanh toán {fee:N0} VNĐ và check-in thành công cho {memberName}";

                return Json(new { success = true, message = message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing pay and check-in for booking ID: {BookingId}", request?.Id ?? 0);
                return Json(new { success = false, message = "Có lỗi xảy ra khi xử lý thanh toán và check-in." });
            }
        }

        /// <summary>
        /// API để xuất Excel
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ExportExcel()
        {
            try
            {
                var bookings = await _bookingService.GetAllAsync();

                // Create simple CSV content (can be enhanced to proper Excel later)
                var csv = new System.Text.StringBuilder();
                csv.AppendLine("Thành viên,Lớp học,Ngày,Thời gian,Trạng thái,Ngày đặt,Ghi chú");

                foreach (var booking in bookings.OrderByDescending(b => b.NgayTao))
                {
                    var memberName = booking.ThanhVien != null ? $"{booking.ThanhVien.Ho} {booking.ThanhVien.Ten}".Trim() : "N/A";
                    var className = booking.LopHoc?.TenLop ?? "N/A";
                    var timeRange = booking.LopHoc != null ?
                        $"{booking.LopHoc.GioBatDau:HH:mm} - {booking.LopHoc.GioKetThuc:HH:mm}" : "N/A";
                    var statusText = booking.TrangThai switch
                    {
                        "BOOKED" => "Đã đặt",
                        "CANCELED" => "Đã hủy",
                        "ATTENDED" => "Đã tham gia",
                        _ => booking.TrangThai
                    };

                    csv.AppendLine($"\"{memberName}\",\"{className}\",\"{booking.Ngay:dd/MM/yyyy}\",\"{timeRange}\",\"{statusText}\",\"{booking.NgayTao:dd/MM/yyyy HH:mm}\",\"{booking.GhiChu ?? ""}\"");
                }

                var bytes = System.Text.Encoding.UTF8.GetBytes(csv.ToString());
                var fileName = $"DanhSachDatLich_{DateTime.Now:yyyyMMdd_HHmmss}.csv";

                return File(bytes, "text/csv", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting bookings to Excel");
                return Json(new { success = false, message = "Có lỗi xảy ra khi xuất file Excel." });
            }
        }
        // ✅ API endpoint to check class availability
        [HttpGet]
        public async Task<IActionResult> CheckAvailability(int classId, string date)
        {
            try
            {
                if (!DateOnly.TryParse(date, out var dateOnly))
                {
                    return Json(new { canBook = false, message = "Ngày không hợp lệ" });
                }

                var lopHoc = await _lopHocService.GetByIdAsync(classId);
                if (lopHoc == null)
                {
                    return Json(new { canBook = false, message = "Lớp học không tồn tại" });
                }

                // Check capacity (both Bookings and DangKys)
                var currentBookings = await _unitOfWork.Context.Bookings
                    .Where(b => b.LopHocId == classId &&
                               b.Ngay == dateOnly &&
                               b.TrangThai == "BOOKED")
                    .CountAsync();

                var activeRegistrations = await _unitOfWork.Context.DangKys
                    .Where(d => d.LopHocId == classId &&
                               d.TrangThai == "ACTIVE" &&
                               d.NgayBatDau <= dateOnly &&
                               d.NgayKetThuc >= dateOnly)
                    .CountAsync();

                var totalOccupied = currentBookings + activeRegistrations;
                var availableSlots = lopHoc.SucChua - totalOccupied;
                var canBook = availableSlots > 0;

                return Json(new {
                    canBook = canBook,
                    availableSlots = availableSlots,
                    totalCapacity = lopHoc.SucChua,
                    currentBookings = currentBookings,
                    activeRegistrations = activeRegistrations,
                    totalOccupied = totalOccupied,
                    message = canBook ? "Có thể đặt lịch" : "Lớp học đã đầy"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking class availability for class {ClassId} on {Date}", classId, date);
                return Json(new { canBook = false, message = "Lỗi hệ thống" });
            }
        }
    }

    // ✅ DTO classes for API requests
    public class BookClassRequest
    {
        public int ClassId { get; set; }
        public DateTime Date { get; set; }
        public string? Note { get; set; }
    }

    public class WeeklyClassEvent
    {
        public int LopHocId { get; set; }
        public string TenLop { get; set; } = string.Empty;
        public DateOnly Date { get; set; }
        public TimeOnly GioBatDau { get; set; }
        public TimeOnly GioKetThuc { get; set; }
    }

    public class PayAndCheckInRequest
    {
        public int Id { get; set; }
    }
}
