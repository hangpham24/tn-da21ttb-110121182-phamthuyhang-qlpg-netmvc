using GymManagement.Web.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace GymManagement.Web.Data.Repositories
{
    public class ThanhToanRepository : Repository<ThanhToan>, IThanhToanRepository
    {
        public ThanhToanRepository(GymDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<ThanhToan>> GetByDangKyIdAsync(int dangKyId)
        {
            return await _context.ThanhToans
                .Include(t => t.DangKy)
                    .ThenInclude(d => d.NguoiDung)
                .Include(t => t.DangKy)
                    .ThenInclude(d => d.GoiTap)
                .Include(t => t.DangKy)
                    .ThenInclude(d => d.LopHoc)
                .Include(t => t.ThanhToanGateway)
                .Where(t => t.DangKyId == dangKyId)
                .OrderByDescending(t => t.NgayThanhToan)
                .ToListAsync();
        }

        public async Task<IEnumerable<ThanhToan>> GetPaymentsByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await _context.ThanhToans
                .Include(t => t.DangKy)
                    .ThenInclude(d => d.NguoiDung)
                .Include(t => t.DangKy)
                    .ThenInclude(d => d.GoiTap)
                .Include(t => t.DangKy)
                    .ThenInclude(d => d.LopHoc)
                .Where(t => t.NgayThanhToan >= startDate && t.NgayThanhToan <= endDate)
                .OrderByDescending(t => t.NgayThanhToan)
                .ToListAsync();
        }

        public async Task<IEnumerable<ThanhToan>> GetPendingPaymentsAsync()
        {
            return await _context.ThanhToans
                .Include(t => t.DangKy)
                    .ThenInclude(d => d.NguoiDung)
                .Where(t => t.TrangThai == "PENDING")
                .OrderByDescending(t => t.NgayThanhToan)
                .ToListAsync();
        }

        public async Task<IEnumerable<ThanhToan>> GetSuccessfulPaymentsAsync()
        {
            return await _context.ThanhToans
                .Include(t => t.DangKy)
                    .ThenInclude(d => d.NguoiDung)
                .Where(t => t.TrangThai == "SUCCESS")
                .OrderByDescending(t => t.NgayThanhToan)
                .ToListAsync();
        }

        public async Task<decimal> GetTotalRevenueByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await _context.ThanhToans
                .Where(t => t.TrangThai == "SUCCESS" && 
                           t.NgayThanhToan >= startDate && 
                           t.NgayThanhToan <= endDate)
                .SumAsync(t => t.SoTien);
        }

        public async Task<decimal> GetTotalRevenueByMonthAsync(int year, int month)
        {
            return await _context.ThanhToans
                .Where(t => t.TrangThai == "SUCCESS" && 
                           t.NgayThanhToan.Year == year && 
                           t.NgayThanhToan.Month == month)
                .SumAsync(t => t.SoTien);
        }

        public async Task<IEnumerable<ThanhToan>> GetPaymentsByMethodAsync(string phuongThuc)
        {
            return await _context.ThanhToans
                .Include(t => t.DangKy)
                    .ThenInclude(d => d.NguoiDung)
                .Where(t => t.PhuongThuc == phuongThuc)
                .OrderByDescending(t => t.NgayThanhToan)
                .ToListAsync();
        }

        public async Task<ThanhToan?> GetPaymentWithGatewayAsync(int thanhToanId)
        {
            return await _context.ThanhToans
                .Include(t => t.DangKy)
                    .ThenInclude(d => d.NguoiDung)
                .Include(t => t.ThanhToanGateway)
                .FirstOrDefaultAsync(t => t.ThanhToanId == thanhToanId);
        }
    }
}
