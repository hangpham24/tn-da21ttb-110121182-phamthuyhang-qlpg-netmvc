using GymManagement.Web.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace GymManagement.Web.Data.Repositories
{
    public class DangKyRepository : Repository<DangKy>, IDangKyRepository
    {
        public DangKyRepository(GymDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<DangKy>> GetByNguoiDungIdAsync(int nguoiDungId)
        {
            return await _context.DangKys
                .Include(d => d.NguoiDung)
                .Include(d => d.GoiTap)
                .Include(d => d.LopHoc)
                .Where(d => d.NguoiDungId == nguoiDungId)
                .OrderByDescending(d => d.NgayTao)
                .ToListAsync();
        }

        public async Task<IEnumerable<DangKy>> GetActiveRegistrationsAsync()
        {
            return await _context.DangKys
                .Include(d => d.NguoiDung)
                .Include(d => d.GoiTap)
                .Include(d => d.LopHoc)
                .Where(d => d.TrangThai == "ACTIVE" && d.NgayKetThuc >= DateOnly.FromDateTime(DateTime.Today))
                .ToListAsync();
        }

        public async Task<IEnumerable<DangKy>> GetExpiredRegistrationsAsync()
        {
            return await _context.DangKys
                .Include(d => d.NguoiDung)
                .Include(d => d.GoiTap)
                .Include(d => d.LopHoc)
                .Where(d => d.TrangThai == "ACTIVE" && d.NgayKetThuc < DateOnly.FromDateTime(DateTime.Today))
                .ToListAsync();
        }

        public async Task<DangKy?> GetActiveRegistrationByUserAndPackageAsync(int nguoiDungId, int? goiTapId, int? lopHocId)
        {
            return await _context.DangKys
                .Include(d => d.NguoiDung)
                .Include(d => d.GoiTap)
                .Include(d => d.LopHoc)
                .FirstOrDefaultAsync(d => 
                    d.NguoiDungId == nguoiDungId &&
                    d.GoiTapId == goiTapId &&
                    d.LopHocId == lopHocId &&
                    d.TrangThai == "ACTIVE" &&
                    d.NgayKetThuc >= DateOnly.FromDateTime(DateTime.Today));
        }

        public async Task<bool> HasActiveRegistrationAsync(int nguoiDungId, int? goiTapId, int? lopHocId)
        {
            return await _context.DangKys
                .AnyAsync(d => 
                    d.NguoiDungId == nguoiDungId &&
                    d.GoiTapId == goiTapId &&
                    d.LopHocId == lopHocId &&
                    d.TrangThai == "ACTIVE" &&
                    d.NgayKetThuc >= DateOnly.FromDateTime(DateTime.Today));
        }

        public async Task<IEnumerable<DangKy>> GetRegistrationsByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await _context.DangKys
                .Include(d => d.NguoiDung)
                .Include(d => d.GoiTap)
                .Include(d => d.LopHoc)
                .Where(d => d.NgayTao >= startDate && d.NgayTao <= endDate)
                .OrderByDescending(d => d.NgayTao)
                .ToListAsync();
        }

        public async Task<int> CountActiveRegistrationsAsync()
        {
            return await _context.DangKys
                .CountAsync(d => d.TrangThai == "ACTIVE" && d.NgayKetThuc >= DateOnly.FromDateTime(DateTime.Today));
        }

        public async Task<IEnumerable<DangKy>> GetByMemberIdAsync(int nguoiDungId)
        {
            return await _context.DangKys
                .Include(d => d.GoiTap)
                .Include(d => d.LopHoc)
                .Where(d => d.NguoiDungId == nguoiDungId)
                .OrderByDescending(d => d.NgayBatDau)
                .ToListAsync();
        }

        // Override GetByIdAsync to include navigation properties
        public override async Task<DangKy?> GetByIdAsync(int id)
        {
            return await _context.DangKys
                .Include(d => d.NguoiDung)
                .Include(d => d.GoiTap)
                .Include(d => d.LopHoc)
                .FirstOrDefaultAsync(d => d.DangKyId == id);
        }
    }
}
