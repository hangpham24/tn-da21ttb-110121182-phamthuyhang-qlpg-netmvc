using GymManagement.Web.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace GymManagement.Web.Data.Repositories
{
    public class BookingRepository : Repository<Booking>, IBookingRepository
    {
        public BookingRepository(GymDbContext context) : base(context)
        {
        }

        // ✅ OVERRIDE: GetByIdAsync with proper includes for navigation properties
        public override async Task<Booking?> GetByIdAsync(int id)
        {
            return await _context.Bookings
                .Include(b => b.ThanhVien)
                    .ThenInclude(tv => tv!.TaiKhoan) // Include TaiKhoan for authorization
                .Include(b => b.LopHoc)
                    .ThenInclude(l => l!.Hlv) // Include trainer info
                .FirstOrDefaultAsync(b => b.BookingId == id);
        }

        // ✅ OVERRIDE: GetAllAsync with proper includes for navigation properties
        public override async Task<IEnumerable<Booking>> GetAllAsync()
        {
            return await _context.Bookings
                .Include(b => b.ThanhVien)
                    .ThenInclude(tv => tv!.TaiKhoan) // Include TaiKhoan for authorization
                .Include(b => b.LopHoc)
                    .ThenInclude(l => l!.Hlv) // Include trainer info
                .OrderByDescending(b => b.NgayTao)
                .ToListAsync();
        }

        public async Task<IEnumerable<Booking>> GetByThanhVienIdAsync(int thanhVienId)
        {
            return await _context.Bookings
                .Include(b => b.ThanhVien)
                .Include(b => b.LopHoc)
                .Where(b => b.ThanhVienId == thanhVienId)
                .OrderByDescending(b => b.Ngay)
                .ToListAsync();
        }

        public async Task<IEnumerable<Booking>> GetByLopHocIdAsync(int lopHocId)
        {
            return await _context.Bookings
                .Include(b => b.ThanhVien)
                .Include(b => b.LopHoc)
                .Where(b => b.LopHocId == lopHocId)
                .OrderByDescending(b => b.Ngay)
                .ToListAsync();
        }

        // Note: GetByLichLopIdAsync method removed as LichLop table no longer exists

        public async Task<IEnumerable<Booking>> GetBookingsByDateAsync(DateTime date)
        {
            var dateOnly = DateOnly.FromDateTime(date);
            return await _context.Bookings
                .Include(b => b.ThanhVien)
                .Include(b => b.LopHoc)
                .Where(b => b.Ngay == dateOnly)
                .OrderBy(b => b.LopHoc.GioBatDau)
                .ToListAsync();
        }

        public async Task<IEnumerable<Booking>> GetActiveBookingsAsync()
        {
            return await _context.Bookings
                .Include(b => b.ThanhVien)
                .Include(b => b.LopHoc)
                .Where(b => b.TrangThai == "BOOKED" && b.Ngay >= DateOnly.FromDateTime(DateTime.Today))
                .OrderBy(b => b.Ngay)
                .ThenBy(b => b.LopHoc.GioBatDau)
                .ToListAsync();
        }

        public async Task<int> CountBookingsForClassAsync(int lopHocId, DateTime date)
        {
            var dateOnly = DateOnly.FromDateTime(date);
            return await _context.Bookings
                .CountAsync(b => b.LopHocId == lopHocId &&
                                b.Ngay == dateOnly &&
                                (b.TrangThai == "BOOKED" || b.TrangThai == "ATTENDED"));
        }

        public async Task<int> CountAllActiveBookingsForClassAsync(int lopHocId)
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            return await _context.Bookings
                .CountAsync(b => b.LopHocId == lopHocId &&
                                b.Ngay >= today &&
                                (b.TrangThai == "BOOKED" || b.TrangThai == "ATTENDED"));
        }

        // Note: Methods using LichLop have been simplified to use LopHoc only

        public async Task<bool> HasBookingAsync(int thanhVienId, int lopHocId, DateTime date)
        {
            var dateOnly = DateOnly.FromDateTime(date);
            return await _context.Bookings
                .AnyAsync(b => b.ThanhVienId == thanhVienId &&
                              b.LopHocId == lopHocId &&
                              b.Ngay == dateOnly &&
                              b.TrangThai == "BOOKED");
        }

        public async Task<Booking?> GetActiveBookingAsync(int thanhVienId, int lopHocId, DateTime date)
        {
            var dateOnly = DateOnly.FromDateTime(date);
            return await _context.Bookings
                .Include(b => b.ThanhVien)
                .Include(b => b.LopHoc)
                .FirstOrDefaultAsync(b => b.ThanhVienId == thanhVienId &&
                                         b.LopHocId == lopHocId &&
                                         b.Ngay == dateOnly &&
                                         b.TrangThai == "BOOKED");
        }
    }
}
