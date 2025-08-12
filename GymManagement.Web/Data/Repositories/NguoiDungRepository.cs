using GymManagement.Web.Data.Models;
using Microsoft.EntityFrameworkCore;

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
    }
}
