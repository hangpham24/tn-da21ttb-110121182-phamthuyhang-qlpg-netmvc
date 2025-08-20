using GymManagement.Web.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace GymManagement.Web.Data.Repositories
{
    public class ThongBaoRepository : Repository<ThongBao>, IThongBaoRepository
    {
        public ThongBaoRepository(GymDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<ThongBao>> GetByNguoiDungIdAsync(int nguoiDungId)
        {
            return await _context.ThongBaos
                .Include(t => t.NguoiDung)
                .Where(t => t.NguoiDungId == nguoiDungId)
                .OrderByDescending(t => t.NgayTao)
                .ToListAsync();
        }

        public async Task<IEnumerable<ThongBao>> GetUnreadByNguoiDungIdAsync(int nguoiDungId)
        {
            return await _context.ThongBaos
                .Include(t => t.NguoiDung)
                .Where(t => t.NguoiDungId == nguoiDungId && !t.DaDoc)
                .OrderByDescending(t => t.NgayTao)
                .ToListAsync();
        }

        public async Task<IEnumerable<ThongBao>> GetByKenhAsync(string kenh)
        {
            return await _context.ThongBaos
                .Include(t => t.NguoiDung)
                .Where(t => t.Kenh == kenh)
                .OrderByDescending(t => t.NgayTao)
                .ToListAsync();
        }

        public async Task<IEnumerable<ThongBao>> GetRecentNotificationsAsync(int count)
        {
            return await _context.ThongBaos
                .Include(t => t.NguoiDung)
                .OrderByDescending(t => t.NgayTao)
                .Take(count)
                .ToListAsync();
        }

        public async Task<int> CountUnreadNotificationsAsync(int nguoiDungId)
        {
            return await _context.ThongBaos
                .CountAsync(t => t.NguoiDungId == nguoiDungId && !t.DaDoc);
        }

        public async Task MarkAsReadAsync(int thongBaoId)
        {
            var thongBao = await _context.ThongBaos.FindAsync(thongBaoId);
            if (thongBao != null)
            {
                thongBao.DaDoc = true;
                await _context.SaveChangesAsync();
            }
        }

        public async Task MarkAllAsReadAsync(int nguoiDungId)
        {
            var thongBaos = await _context.ThongBaos
                .Where(t => t.NguoiDungId == nguoiDungId && !t.DaDoc)
                .ToListAsync();

            foreach (var thongBao in thongBaos)
            {
                thongBao.DaDoc = true;
            }

            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<ThongBao>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await _context.ThongBaos
                .Include(t => t.NguoiDung)
                .Where(t => t.NgayTao >= startDate && t.NgayTao <= endDate)
                .OrderByDescending(t => t.NgayTao)
                .ToListAsync();
        }
    }
}
