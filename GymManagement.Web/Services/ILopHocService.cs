using GymManagement.Web.Data.Models;
using GymManagement.Web.Models.DTOs;

namespace GymManagement.Web.Services
{
    public interface ILopHocService
    {
        Task<IEnumerable<LopHoc>> GetAllAsync();
        Task<LopHoc?> GetByIdAsync(int id);
        Task<LopHoc> CreateAsync(LopHoc lopHoc);
        Task<LopHoc> UpdateAsync(LopHoc lopHoc);
        Task<bool> DeleteAsync(int id);
        Task<IEnumerable<LopHoc>> GetActiveClassesAsync();
        Task<IEnumerable<LopHoc>> GetClassesByTrainerAsync(int hlvId);
        Task<bool> IsClassAvailableAsync(int lopHocId, DateTime date);
        Task<int> GetAvailableSlotsAsync(int lopHocId, DateTime date);

        
        // New methods
        Task<(bool CanDelete, string Message)> CanDeleteClassAsync(int lopHocId);
        void ClearCache();
    }
}
