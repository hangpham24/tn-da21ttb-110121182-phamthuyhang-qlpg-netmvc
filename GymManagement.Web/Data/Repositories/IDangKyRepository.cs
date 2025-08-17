using GymManagement.Web.Data.Models;

namespace GymManagement.Web.Data.Repositories
{
    public interface IDangKyRepository : IRepository<DangKy>
    {
        Task<IEnumerable<DangKy>> GetByNguoiDungIdAsync(int nguoiDungId);
        Task<IEnumerable<DangKy>> GetByMemberIdAsync(int nguoiDungId);
        Task<IEnumerable<DangKy>> GetActiveRegistrationsAsync();
        Task<IEnumerable<DangKy>> GetExpiredRegistrationsAsync();
        Task<DangKy?> GetActiveRegistrationByUserAndPackageAsync(int nguoiDungId, int? goiTapId, int? lopHocId);
        Task<bool> HasActiveRegistrationAsync(int nguoiDungId, int? goiTapId, int? lopHocId);
        Task<IEnumerable<DangKy>> GetRegistrationsByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<int> CountActiveRegistrationsAsync();
        Task<int> CountActiveRegistrationsForClassAsync(int lopHocId);
        Task<(IEnumerable<DangKy> registrations, int totalCount)> GetPagedAsync(int page, int pageSize, string searchTerm = "", string status = "", string type = "");
    }
}
