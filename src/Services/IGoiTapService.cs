using GymManagement.Web.Models.DTOs;

namespace GymManagement.Web.Services
{
    public interface IGoiTapService
    {
        // Basic CRUD
        Task<GoiTapDto?> GetByIdAsync(int id);
        Task<IEnumerable<GoiTapDto>> GetAllAsync();
        Task<(IEnumerable<GoiTapDto> Items, int TotalCount)> GetPagedAsync(
            int pageNumber, int pageSize, string? searchTerm = null, decimal? minPrice = null, decimal? maxPrice = null);
        Task<GoiTapDto> CreateAsync(CreateGoiTapDto createDto);
        Task<GoiTapDto> UpdateAsync(UpdateGoiTapDto updateDto);
        Task<bool> DeleteAsync(int id);

        // Business methods
        Task<IEnumerable<GoiTapDto>> GetActivePackagesAsync();
        Task<IEnumerable<GoiTapDto>> GetByPriceRangeAsync(decimal minPrice, decimal maxPrice);
        Task<IEnumerable<GoiTapDto>> GetPopularPackagesAsync(int top = 10);
        Task<bool> CanDeletePackageAsync(int id);
    }
}
