using GymManagement.Web.Data.Models;

namespace GymManagement.Web.Data.Repositories
{
    public interface IDiemDanhRepository : IRepository<DiemDanh>
    {
        Task<IEnumerable<DiemDanh>> GetByThanhVienIdAsync(int thanhVienId);
        Task<IEnumerable<DiemDanh>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<IEnumerable<DiemDanh>> GetTodayAttendanceAsync();
        Task<DiemDanh?> GetLatestAttendanceAsync(int thanhVienId);
        Task<bool> HasAttendanceToday(int thanhVienId);
        Task<int> CountAttendanceByDateAsync(DateTime date);
        Task<int> CountAttendanceByMemberAsync(int thanhVienId, DateTime startDate, DateTime endDate);
        Task<IEnumerable<DiemDanh>> GetSuccessfulAttendanceAsync(DateTime startDate, DateTime endDate);
        Task<int> GetAttendanceCountByDateRangeAsync(int thanhVienId, DateTime fromDate, DateTime toDate);
        Task<IEnumerable<DiemDanh>> GetByNguoiDungIdAsync(int nguoiDungId);
        // Note: Methods using LichLop have been removed
    }
}
