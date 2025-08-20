using GymManagement.Web.Data.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace GymManagement.Web.Data.Repositories
{
    public class NguoiDungRepository : Repository<NguoiDung>, INguoiDungRepository
    {
        public NguoiDungRepository(GymDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<NguoiDung>> GetByLoaiNguoiDungAsync(string loaiNguoiDung)
        {
            return await _dbSet
                .Where(x => x.LoaiNguoiDung == loaiNguoiDung)
                .ToListAsync();
        }

        public async Task<NguoiDung?> GetByEmailAsync(string email)
        {
            return await _dbSet
                .FirstOrDefaultAsync(x => x.Email == email);
        }

        public async Task<NguoiDung?> GetBySoDienThoaiAsync(string soDienThoai)
        {
            return await _dbSet
                .FirstOrDefaultAsync(x => x.SoDienThoai == soDienThoai);
        }

        public async Task<IEnumerable<NguoiDung>> GetActiveUsersAsync()
        {
            return await _dbSet
                .Where(x => x.TrangThai == "ACTIVE")
                .ToListAsync();
        }

        public async Task<IEnumerable<NguoiDung>> GetHuanLuyenViensAsync()
        {
            return await _dbSet
                .Where(x => x.LoaiNguoiDung == "HLV" && x.TrangThai == "ACTIVE")
                .ToListAsync();
        }

        public async Task<IEnumerable<NguoiDung>> GetThanhViensAsync()
        {
            return await _dbSet
                .Where(x => x.LoaiNguoiDung == "THANHVIEN" && x.TrangThai == "ACTIVE")
                .ToListAsync();
        }

        public async Task<IEnumerable<NguoiDung>> GetMembersAsync()
        {
            return await _dbSet
                .Where(x => x.LoaiNguoiDung == "THANHVIEN" && x.TrangThai == "ACTIVE")
                .ToListAsync();
        }

        public async Task<IEnumerable<NguoiDung>> GetTrainersAsync()
        {
            return await _dbSet
                .Where(x => x.LoaiNguoiDung == "HLV" && x.TrangThai == "ACTIVE")
                .ToListAsync();
        }

        public async Task<NguoiDung?> GetWithTaiKhoanAsync(int nguoiDungId)
        {
            return await _dbSet
                .Include(x => x.TaiKhoan)
                .FirstOrDefaultAsync(x => x.NguoiDungId == nguoiDungId);
        }

        public async Task<IEnumerable<NguoiDung>> GetAllWithTaiKhoanAsync()
        {
            return await _dbSet
                .Include(x => x.TaiKhoan)
                .ToListAsync();
        }

        public async Task<(IEnumerable<NguoiDung> Items, int TotalCount)> GetPagedWithTaiKhoanAsync(
            int pageNumber, int pageSize,
            Expression<Func<NguoiDung, bool>>? filter = null,
            Func<IQueryable<NguoiDung>, IOrderedQueryable<NguoiDung>>? orderBy = null)
        {
            IQueryable<NguoiDung> query = _dbSet.Include(x => x.TaiKhoan);

            if (filter != null)
            {
                query = query.Where(filter);
            }

            var totalCount = await query.CountAsync();

            if (orderBy != null)
            {
                query = orderBy(query);
            }

            var items = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();

            return (items, totalCount);
        }
    }
}
