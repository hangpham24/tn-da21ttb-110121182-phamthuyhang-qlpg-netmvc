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

        public async Task<int> CountActiveRegistrationsForClassAsync(int lopHocId)
        {
            return await _context.DangKys
                .CountAsync(d => d.LopHocId == lopHocId &&
                                d.TrangThai == "ACTIVE" &&
                                d.NgayKetThuc >= DateOnly.FromDateTime(DateTime.Today));
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

        // Override GetAllAsync to include navigation properties
        public override async Task<IEnumerable<DangKy>> GetAllAsync()
        {
            return await _context.DangKys
                .Include(d => d.NguoiDung)
                .Include(d => d.GoiTap)
                .Include(d => d.LopHoc)
                .OrderByDescending(d => d.NgayTao)
                .ToListAsync();
        }

        public async Task<(IEnumerable<DangKy> registrations, int totalCount)> GetPagedAsync(int page, int pageSize, string searchTerm = "", string status = "", string type = "")
        {
            var query = _context.DangKys
                .Include(d => d.NguoiDung)
                .Include(d => d.GoiTap)
                .Include(d => d.LopHoc)
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(d =>
                    (d.NguoiDung.Ho + " " + d.NguoiDung.Ten).Contains(searchTerm) ||
                    d.NguoiDung.Email.Contains(searchTerm) ||
                    d.NguoiDung.SoDienThoai.Contains(searchTerm));
            }

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(d => d.TrangThai == status);
            }

            if (!string.IsNullOrEmpty(type))
            {
                if (type == "PACKAGE")
                {
                    query = query.Where(d => d.GoiTapId.HasValue);
                }
                else if (type == "CLASS")
                {
                    query = query.Where(d => d.LopHocId.HasValue);
                }
            }

            var totalCount = await query.CountAsync();

            var registrations = await query
                .OrderByDescending(d => d.NgayTao)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (registrations, totalCount);
        }
    }
}
