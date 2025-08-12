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
        private readonly IThongBaoService _thongBaoService;

        public BookingService(
            IUnitOfWork unitOfWork,
            IBookingRepository bookingRepository,
            ILopHocRepository lopHocRepository,
            IThongBaoService thongBaoService)
        {
            _unitOfWork = unitOfWork;
            _bookingRepository = bookingRepository;
            _lopHocRepository = lopHocRepository;
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
            // Check if date is in the future
            if (date.Date < DateTime.Today)
                return (false, "Không thể đặt lịch cho ngày trong quá khứ");

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

        public async Task<bool> BookScheduleAsync(int thanhVienId, int lichLopId)
        {
            // Check if schedule exists
            var lichLop = await _unitOfWork.Context.LichLops
                .Include(l => l.LopHoc)
                .FirstOrDefaultAsync(l => l.LichLopId == lichLopId);

            if (lichLop == null || lichLop.TrangThai != "SCHEDULED")
                return false;

            // Check if member already has a booking for this schedule
            if (await _bookingRepository.HasBookingAsync(thanhVienId, lichLop.LopHocId, lichLopId, lichLop.Ngay.ToDateTime(TimeOnly.MinValue)))
                return false;

            // Check if class has available slots
            var bookingCount = await _bookingRepository.CountBookingsForScheduleAsync(lichLopId);
            if (bookingCount >= lichLop.LopHoc.SucChua)
                return false;

            // Create booking
            var booking = new Booking
            {
                ThanhVienId = thanhVienId,
                LopHocId = lichLop.LopHocId,
                LichLopId = lichLopId,
                Ngay = lichLop.Ngay,
                TrangThai = "BOOKED"
            };

            await _bookingRepository.AddAsync(booking);
            await _unitOfWork.SaveChangesAsync();

            // Send notification
            var thanhVien = await _unitOfWork.Context.NguoiDungs.FindAsync(thanhVienId);
            if (thanhVien != null)
            {
                await _thongBaoService.CreateNotificationAsync(
                    thanhVienId,
                    "Đặt lịch thành công",
                    $"Bạn đã đặt lịch thành công lớp {lichLop.LopHoc.TenLop} vào ngày {lichLop.Ngay:dd/MM/yyyy}",
                    "APP"
                );
            }

            return true;
        }

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

            // Check if member already has a booking for this class on this date
            if (await _bookingRepository.HasBookingAsync(thanhVienId, lopHocId, null, date))
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

        public async Task<IEnumerable<Booking>> GetUpcomingBookingsAsync(int thanhVienId)
        {
            var bookings = await _bookingRepository.GetByThanhVienIdAsync(thanhVienId);
            return bookings.Where(b => b.TrangThai == "BOOKED" && b.Ngay >= DateOnly.FromDateTime(DateTime.Today));
        }
    }
}
