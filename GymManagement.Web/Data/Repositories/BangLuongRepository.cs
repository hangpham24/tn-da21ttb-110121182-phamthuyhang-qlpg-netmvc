using GymManagement.Web.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace GymManagement.Web.Data.Repositories
{
    public class BangLuongRepository : Repository<BangLuong>, IBangLuongRepository
    {
        public BangLuongRepository(GymDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<BangLuong>> GetByHlvIdAsync(int hlvId)
        {
            return await _context.BangLuongs
                .Include(b => b.Hlv)
                .Where(b => b.HlvId == hlvId)
                .OrderByDescending(b => b.Thang)
                .ToListAsync();
        }

        public async Task<IEnumerable<BangLuong>> GetByMonthAsync(string thang)
        {
            return await _context.BangLuongs
                .Include(b => b.Hlv)
                .Where(b => b.Thang == thang)
                .OrderBy(b => b.Hlv.Ho)
                .ThenBy(b => b.Hlv.Ten)
                .ToListAsync();
        }

        public async Task<BangLuong?> GetByHlvAndMonthAsync(int hlvId, string thang)
        {
            return await _context.BangLuongs
                .Include(b => b.Hlv)
                .FirstOrDefaultAsync(b => b.HlvId == hlvId && b.Thang == thang);
        }

        public async Task<IEnumerable<BangLuong>> GetUnpaidSalariesAsync()
        {
            return await _context.BangLuongs
                .Include(b => b.Hlv)
                .Where(b => b.NgayThanhToan == null)
                .OrderBy(b => b.Thang)
                .ThenBy(b => b.Hlv.Ho)
                .ToListAsync();
        }

        public async Task<IEnumerable<BangLuong>> GetPaidSalariesAsync()
        {
            return await _context.BangLuongs
                .Include(b => b.Hlv)
                .Where(b => b.NgayThanhToan != null)
                .OrderByDescending(b => b.NgayThanhToan)
                .ToListAsync();
        }

        public async Task<decimal> GetTotalSalaryByMonthAsync(string thang)
        {
            // ✅ FIX: Tính cả lương chưa thanh toán để có Net Profit chính xác
            return await _context.BangLuongs
                .Where(b => b.Thang == thang)
                .SumAsync(b => b.LuongCoBan);
        }

        public async Task<decimal> GetTotalCommissionByMonthAsync(string thang)
        {
            // ✅ FIX: Tính cả hoa hồng chưa thanh toán để có Net Profit chính xác
            return await _context.BangLuongs
                .Where(b => b.Thang == thang)
                .SumAsync(b => b.TienHoaHong);
        }

        // ✅ NEW: Methods để tính riêng lương đã thanh toán (nếu cần)
        public async Task<decimal> GetPaidSalaryByMonthAsync(string thang)
        {
            return await _context.BangLuongs
                .Where(b => b.Thang == thang && b.NgayThanhToan != null)
                .SumAsync(b => b.LuongCoBan);
        }

        public async Task<decimal> GetPaidCommissionByMonthAsync(string thang)
        {
            return await _context.BangLuongs
                .Where(b => b.Thang == thang && b.NgayThanhToan != null)
                .SumAsync(b => b.TienHoaHong);
        }

        public async Task<IEnumerable<BangLuong>> GetSalariesByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            var startDateOnly = DateOnly.FromDateTime(startDate);
            var endDateOnly = DateOnly.FromDateTime(endDate);

            return await _context.BangLuongs
                .Include(b => b.Hlv)
                .Where(b => b.NgayThanhToan >= startDateOnly && b.NgayThanhToan <= endDateOnly)
                .OrderByDescending(b => b.NgayThanhToan)
                .ToListAsync();
        }

        public async Task<int> GetSalaryCountByMonthAsync(string thang)
        {
            return await _context.BangLuongs
                .Where(bl => bl.Thang == thang)
                .CountAsync();
        }
    }
}
