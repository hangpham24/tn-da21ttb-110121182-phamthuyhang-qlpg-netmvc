using GymManagement.Web.Data.Models;

namespace GymManagement.Web.Services
{
    public interface IThongBaoService
    {
        Task<IEnumerable<ThongBao>> GetAllAsync();
        Task<ThongBao?> GetByIdAsync(int id);
        Task<ThongBao> CreateAsync(ThongBao thongBao);
        Task<ThongBao> UpdateAsync(ThongBao thongBao);
        Task<bool> DeleteAsync(int id);
        Task<IEnumerable<ThongBao>> GetByUserIdAsync(int nguoiDungId);
        Task<IEnumerable<ThongBao>> GetUnreadByUserIdAsync(int nguoiDungId);
        Task<int> CountUnreadNotificationsAsync(int nguoiDungId);
        Task<bool> MarkAsReadAsync(int thongBaoId);
        Task<bool> MarkAllAsReadAsync(int nguoiDungId);
        Task<ThongBao> CreateNotificationAsync(int nguoiDungId, string tieuDe, string noiDung, string kenh);
        Task SendBulkNotificationAsync(IEnumerable<int> nguoiDungIds, string tieuDe, string noiDung, string kenh);
        Task SendNotificationToAllMembersAsync(string tieuDe, string noiDung, string kenh);
        Task<bool> DeleteOldNotificationsAsync(int daysOld = 30);
        Task<IEnumerable<ThongBao>> GetRecentNotificationsAsync(int nguoiDungId, int count = 5);
    }
}
