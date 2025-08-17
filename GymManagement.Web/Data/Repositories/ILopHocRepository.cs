using GymManagement.Web.Data.Models;

namespace GymManagement.Web.Data.Repositories
{
    public interface ILopHocRepository : IRepository<LopHoc>
    {
        Task<IEnumerable<LopHoc>> GetActiveClassesAsync();
        Task<IEnumerable<LopHoc>> GetActiveClassesWithDetailsAsync();
        Task<IEnumerable<LopHoc>> GetByHuanLuyenVienAsync(int hlvId);
        Task<IEnumerable<LopHoc>> GetByThuTrongTuanAsync(string thuTrongTuan);
        Task<IEnumerable<LopHoc>> GetAvailableClassesAsync(DateOnly date);

        Task<IEnumerable<LopHoc>> GetClassesWithAvailableSlotsAsync();
        Task<IEnumerable<LopHoc>> GetClassesByTrainerAsync(int trainerId);
    }
}
