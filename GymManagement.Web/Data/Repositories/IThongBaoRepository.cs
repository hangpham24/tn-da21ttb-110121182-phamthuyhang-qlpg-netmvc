using GymManagement.Web.Data.Models;

namespace GymManagement.Web.Data.Repositories
{
    public interface IThongBaoRepository : IRepository<ThongBao>
    {
        Task<IEnumerable<ThongBao>> GetByNguoiDungIdAsync(int nguoiDungId);
        Task<IEnumerable<ThongBao>> GetUnreadByNguoiDungIdAsync(int nguoiDungId);
        Task<IEnumerable<ThongBao>> GetByKenhAsync(string kenh);
        Task<IEnumerable<ThongBao>> GetRecentNotificationsAsync(int count);
        Task<int> CountUnreadNotificationsAsync(int nguoiDungId);
        Task MarkAsReadAsync(int thongBaoId);
        Task MarkAllAsReadAsync(int nguoiDungId);
        Task<IEnumerable<ThongBao>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);
    }
}
