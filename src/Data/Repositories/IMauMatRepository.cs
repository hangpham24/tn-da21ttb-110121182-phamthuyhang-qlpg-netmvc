using GymManagement.Web.Data.Models;

namespace GymManagement.Web.Data.Repositories
{
    public interface IMauMatRepository
    {
        // Basic CRUD operations
        Task<MauMat?> GetByIdAsync(int id);
        Task<IEnumerable<MauMat>> GetAllAsync();
        Task<MauMat> AddAsync(MauMat mauMat);
        Task<MauMat> UpdateAsync(MauMat mauMat);
        Task<bool> DeleteAsync(int id);

        // Specialized queries
        Task<IEnumerable<MauMat>> GetByNguoiDungIdAsync(int nguoiDungId);
        Task<IEnumerable<MauMat>> GetAllWithUsersAsync();
        Task<MauMat?> GetByNguoiDungIdAndAlgorithmAsync(int nguoiDungId, string algorithm);
        
        // Statistics and counts
        Task<int> CountAsync();
        Task<int> CountByNguoiDungIdAsync(int nguoiDungId);
        Task<int> CountByAlgorithmAsync(string algorithm);
        
        // Date-based queries
        Task<IEnumerable<MauMat>> GetByDateRangeAsync(DateTime fromDate, DateTime toDate);
        Task<IEnumerable<MauMat>> GetRecentAsync(int count = 10);
        
        // Bulk operations
        Task<bool> DeleteByNguoiDungIdAsync(int nguoiDungId);
        Task<bool> BulkInsertAsync(IEnumerable<MauMat> mauMats);
        
        // Validation helpers
        Task<bool> ExistsAsync(int id);
        Task<bool> HasFaceDataAsync(int nguoiDungId);
    }
}
