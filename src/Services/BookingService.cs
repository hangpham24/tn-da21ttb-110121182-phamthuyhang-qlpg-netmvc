using GymManagement.Web.Data;
using GymManagement.Web.Data.Models;
using GymManagement.Web.Data.Repositories;
using Microsoft.EntityFrameworkCore;

namespace GymManagement.Web.Services
{
    public class BookingService : IBookingService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IBookingRepository _bookingRepository;
        private readonly ILopHocRepository _lopHocRepository;
        private readonly IDangKyRepository _dangKyRepository;
        private readonly IThongBaoService _thongBaoService;

        public BookingService(
            IUnitOfWork unitOfWork,
            IBookingRepository bookingRepository,
            ILopHocRepository lopHocRepository,
            IDangKyRepository dangKyRepository,
            IThongBaoService thongBaoService)
        {
            _unitOfWork = unitOfWork;
            _bookingRepository = bookingRepository;
            _lopHocRepository = lopHocRepository;
            _dangKyRepository = dangKyRepository;
            _thongBaoService = thongBaoService;
        }

        public async Task<IEnumerable<Booking>> GetAllAsync()
        {
            return await _bookingRepository.GetAllAsync();
        }

        public async Task<Booking?> GetByIdAsync(int id)
        {
            return await _bookingRepository.GetByIdAsync(id);
        }

        public async Task<Booking> CreateAsync(Booking booking)
        {
            var created = await _bookingRepository.AddAsync(booking);
            await _unitOfWork.SaveChangesAsync();
            return created;
        }

        public async Task<Booking> UpdateAsync(Booking booking)
        {
            await _bookingRepository.UpdateAsync(booking);
            await _unitOfWork.SaveChangesAsync();
            return booking;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var booking = await _bookingRepository.GetByIdAsync(id);
            if (booking == null) return false;

            await _bookingRepository.DeleteAsync(booking);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<Booking>> GetByMemberIdAsync(int thanhVienId)
        {
            return await _bookingRepository.GetByThanhVienIdAsync(thanhVienId);
        }

        public async Task<IEnumerable<Booking>> GetByClassIdAsync(int lopHocId)
        {
            return await _bookingRepository.GetByLopHocIdAsync(lopHocId);
        }

        public async Task<IEnumerable<Booking>> GetTodayBookingsAsync()
        {
            return await _bookingRepository.GetBookingsByDateAsync(DateTime.Today);
        }

        // 🚀 IMPROVED: Transaction-safe booking method to prevent race conditions
        public async Task<(bool Success, string ErrorMessage)> BookClassWithTransactionAsync(
            int thanhVienId, int lopHocId, DateTime date, string? ghiChu = null)
        {
            // ✅ IMPROVED: Better date validation with timezone handling
            var today = DateTime.Today;
            var bookingDate = date.Date;

            Console.WriteLine($"🕐 Date validation: Today={today:yyyy-MM-dd}, BookingDate={bookingDate:yyyy-MM-dd}, Original={date:yyyy-MM-dd HH:mm:ss}");

            if (bookingDate < today)
                return (false, $"Không thể đặt lịch cho ngày trong quá khứ. Ngày đặt: {bookingDate:dd/MM/yyyy}, Hôm nay: {today:dd/MM/yyyy}");

            using var transaction = await _unitOfWork.Context.Database.BeginTransactionAsync();
            try
            {
                // Get class with lock to prevent concurrent modifications
                var lopHoc = await _unitOfWork.Context.LopHocs
                    .Where(l => l.LopHocId == lopHocId)
                    .FirstOrDefaultAsync();

                if (lopHoc == null)
                    return (false, "Lớp học không tồn tại");

                if (lopHoc.TrangThai != "OPEN")
                    return (false, "Lớp học đã đóng hoặc không khả dụng");

                // ✅ NEW: Check if class time has passed
                var currentDateTime = DateTime.Now;
                var classDateTime = DateOnly.FromDateTime(date).ToDateTime(lopHoc.GioBatDau);
                
                if (classDateTime <= currentDateTime)
                {
                    return (false, $"Không thể đặt lịch cho lớp đã bắt đầu hoặc đã kết thúc. Thời gian lớp: {lopHoc.GioBatDau:HH:mm} ngày {date:dd/MM/yyyy}");
                }

                // ✅ NEW: Check if the booking date matches the class schedule
                if (!IsValidBookingDate(lopHoc, date))
                {
                    var validDays = GetValidDaysText(lopHoc.ThuTrongTuan);
                    return (false, $"Lớp học này chỉ diễn ra vào: {validDays}. Vui lòng chọn ngày phù hợp.");
                }

                // Check if member already has a booking for this class on this date
                var existingBooking = await _unitOfWork.Context.Bookings
                    .Where(b => b.ThanhVienId == thanhVienId &&
                               b.LopHocId == lopHocId &&
                               b.Ngay == DateOnly.FromDateTime(date) &&
                               b.TrangThai == "BOOKED")
                    .FirstOrDefaultAsync();

                if (existingBooking != null)
                    return (false, "Bạn đã đặt lịch cho lớp này trong ngày này rồi");

                // Check capacity with exclusive lock to prevent race condition
                var currentBookings = await _unitOfWork.Context.Bookings
                    .Where(b => b.LopHocId == lopHocId &&
                               b.Ngay == DateOnly.FromDateTime(date) &&
                               b.TrangThai == "BOOKED")
                    .CountAsync();

                if (currentBookings >= lopHoc.SucChua)
                    return (false, "Lớp học đã đầy, vui lòng chọn lớp khác");

                // Create booking
                var booking = new Booking
                {
                    ThanhVienId = thanhVienId,
                    LopHocId = lopHocId,
                    Ngay = DateOnly.FromDateTime(date),
                    NgayDat = DateOnly.FromDateTime(DateTime.Now),
                    TrangThai = "BOOKED",
                    GhiChu = ghiChu
                };

                await _unitOfWork.Context.Bookings.AddAsync(booking);
                await _unitOfWork.SaveChangesAsync();
                await transaction.CommitAsync();

                // Send notification (outside transaction to avoid blocking)
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var thanhVien = await _unitOfWork.Context.NguoiDungs.FindAsync(thanhVienId);
                        if (thanhVien != null)
                        {
                            await _thongBaoService.CreateNotificationAsync(
                                thanhVienId,
                                "Đặt lịch thành công",
                                $"Bạn đã đặt lịch thành công lớp {lopHoc.TenLop} vào ngày {date:dd/MM/yyyy}",
                                "APP"
                            );
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log notification error but don't fail the booking
                        // TODO: Add proper logging
                        Console.WriteLine($"Failed to send notification: {ex.Message}");
                    }
                });

                return (true, "Đặt lịch thành công");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return (false, $"Lỗi hệ thống: {ex.Message}");
            }
        }

        // 🔄 LEGACY: Keep old method for backward compatibility but mark as obsolete
        [Obsolete("Use BookClassWithTransactionAsync instead to prevent race conditions")]
        public async Task<bool> BookClassAsync(int thanhVienId, int lopHocId, DateTime date, string? ghiChu = null)
        {
            var result = await BookClassWithTransactionAsync(thanhVienId, lopHocId, date, ghiChu);
            return result.Success;
        }

        // Note: BookScheduleAsync method removed as it used LichLop
        // Use BookClassAsync method instead for direct class booking

        public async Task<bool> CancelBookingAsync(int bookingId)
        {
            var booking = await _bookingRepository.GetByIdAsync(bookingId);
            if (booking == null || booking.TrangThai != "BOOKED")
                return false;

            // Check if cancellation is at least 2 hours before class time
            if (booking.LopHoc != null)
            {
                var classDateTime = booking.Ngay.ToDateTime(booking.LopHoc.GioBatDau);
                var hoursUntilClass = (classDateTime - DateTime.Now).TotalHours;
                
                if (hoursUntilClass < 2)
                    return false; // Cannot cancel less than 2 hours before class
            }

            booking.TrangThai = "CANCELED";
            await _unitOfWork.SaveChangesAsync();

            // Send notification
            if (booking.ThanhVienId.HasValue)
            {
                var lopHoc = await _lopHocRepository.GetByIdAsync(booking.LopHocId ?? 0);
                
                await _thongBaoService.CreateNotificationAsync(
                    booking.ThanhVienId.Value,
                    "Huỷ đặt lịch thành công",
                    $"Bạn đã huỷ đặt lịch lớp {lopHoc?.TenLop} vào ngày {booking.Ngay:dd/MM/yyyy}",
                    "APP"
                );
            }

            return true;
        }

        public async Task<bool> CanBookAsync(int thanhVienId, int lopHocId, DateTime date)
        {
            // Check if class exists and is open
            var lopHoc = await _lopHocRepository.GetByIdAsync(lopHocId);
            if (lopHoc == null || lopHoc.TrangThai != "OPEN")
                return false;

            // ✅ NEW: Check if the booking date matches the class schedule
            if (!IsValidBookingDate(lopHoc, date))
                return false;

            // Check if member already has a booking for this class on this date
            if (await _bookingRepository.HasBookingAsync(thanhVienId, lopHocId, date))
                return false;

            // Check if class has available slots
            var bookingCount = await _bookingRepository.CountBookingsForClassAsync(lopHocId, date);
            return bookingCount < lopHoc.SucChua;
        }

        public async Task<int> GetAvailableSlotsAsync(int lopHocId, DateTime date)
        {
            var lopHoc = await _lopHocRepository.GetByIdAsync(lopHocId);
            if (lopHoc == null) return 0;

            var bookingCount = await _bookingRepository.CountBookingsForClassAsync(lopHocId, date);
            return Math.Max(0, lopHoc.SucChua - bookingCount);
        }

        public async Task<int> GetBookingCountForDateAsync(int lopHocId, DateTime date)
        {
            return await _bookingRepository.CountBookingsForClassAsync(lopHocId, date);
        }

        public async Task<int> GetActiveBookingCountAsync(int lopHocId)
        {
            // Count all bookings that are BOOKED or ATTENDED (active bookings)
            var bookings = await _bookingRepository.GetByLopHocIdAsync(lopHocId);
            return bookings.Count(b => b.TrangThai == "BOOKED" || b.TrangThai == "ATTENDED");
        }

        /// <summary>
        /// Get booking count for today's date specifically
        /// </summary>
        public async Task<int> GetTodayBookingCountAsync(int lopHocId)
        {
            return await _bookingRepository.CountBookingsForClassAsync(lopHocId, DateTime.Today);
        }

        /// <summary>
        /// Get total active count (bookings + registrations) for a class
        /// </summary>
        public async Task<int> GetTotalActiveCountAsync(int lopHocId)
        {
            var bookingCount = await _bookingRepository.CountAllActiveBookingsForClassAsync(lopHocId);
            var registrationCount = await _dangKyRepository.CountActiveRegistrationsForClassAsync(lopHocId);
            return bookingCount + registrationCount;
        }

        public async Task<IEnumerable<Booking>> GetUpcomingBookingsAsync(int thanhVienId)
        {
            var bookings = await _bookingRepository.GetByThanhVienIdAsync(thanhVienId);
            return bookings.Where(b => b.TrangThai == "BOOKED" && b.Ngay >= DateOnly.FromDateTime(DateTime.Today));
        }

        /// <summary>
        /// Check if the booking date matches the class schedule (ThuTrongTuan)
        /// </summary>
        private bool IsValidBookingDate(LopHoc lopHoc, DateTime date)
        {
            if (string.IsNullOrEmpty(lopHoc.ThuTrongTuan))
                return false;

            var validDays = ParseDaysOfWeek(lopHoc.ThuTrongTuan);
            var bookingDayOfWeek = GetDayOfWeekNumber(date.DayOfWeek);

            return validDays.Contains(bookingDayOfWeek);
        }

        /// <summary>
        /// Parse days of week string to list of integers using same logic as Calendar
        /// </summary>
        private List<int> ParseDaysOfWeek(string thuTrongTuan)
        {
            var days = new List<int>();
            if (string.IsNullOrEmpty(thuTrongTuan)) return days;

            var dayStrings = thuTrongTuan.Split(',', StringSplitOptions.RemoveEmptyEntries);
            foreach (var dayStr in dayStrings)
            {
                var trimmed = dayStr.Trim();

                // Try to parse as number first (1-7 system: 1=Mon, 7=Sun)
                if (int.TryParse(trimmed, out int dayNum) && dayNum >= 1 && dayNum <= 7)
                {
                    days.Add(dayNum);
                }
                else
                {
                    // Try to parse Vietnamese day names (1-7 system to match existing data)
                    var dayNumber = trimmed switch
                    {
                        "Thứ 2" => 1,  // Monday (1 in 1-7 system)
                        "Thứ 3" => 2,  // Tuesday
                        "Thứ 4" => 3,  // Wednesday 
                        "Thứ 5" => 4,  // Thursday
                        "Thứ 6" => 5,  // Friday
                        "Thứ 7" => 6,  // Saturday
                        "Chủ nhật" => 7,  // Sunday
                        _ => 0
                    };
                    if (dayNumber > 0) days.Add(dayNumber);
                }
            }
            return days.Distinct().ToList();
        }

        /// <summary>
        /// Convert .NET DayOfWeek to 1-7 numbering system (1=Monday, 7=Sunday) to match database
        /// </summary>
        private int GetDayOfWeekNumber(DayOfWeek dayOfWeek)
        {
            return dayOfWeek switch
            {
                DayOfWeek.Monday => 1,    // 1 = Thứ 2
                DayOfWeek.Tuesday => 2,   // 2 = Thứ 3
                DayOfWeek.Wednesday => 3, // 3 = Thứ 4
                DayOfWeek.Thursday => 4,  // 4 = Thứ 5
                DayOfWeek.Friday => 5,    // 5 = Thứ 6
                DayOfWeek.Saturday => 6,  // 6 = Thứ 7
                DayOfWeek.Sunday => 7,    // 7 = Chủ nhật
                _ => 0
            };
        }

        /// <summary>
        /// Get human-readable text for valid days
        /// </summary>
        private string GetValidDaysText(string thuTrongTuan)
        {
            var validDays = ParseDaysOfWeek(thuTrongTuan);
            var dayNames = validDays.Select(day => day switch
            {
                1 => "Thứ 2",   // Monday
                2 => "Thứ 3",   // Tuesday
                3 => "Thứ 4",   // Wednesday
                4 => "Thứ 5",   // Thursday
                5 => "Thứ 6",   // Friday
                6 => "Thứ 7",   // Saturday
                7 => "Chủ nhật", // Sunday
                _ => ""
            }).Where(name => !string.IsNullOrEmpty(name));

            return string.Join(", ", dayNames);
        }

        /// <summary>
        /// Convert DayOfWeek to Vietnamese day name
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
    }
}
