using GymManagement.Web.Data.Models;

namespace GymManagement.Web.Data.Repositories
{
    public interface IGoiTapRepository : IRepository<GoiTap>
    {
        Task<IEnumerable<GoiTap>> GetActivePackagesAsync();
        Task<IEnumerable<GoiTap>> GetByPriceRangeAsync(decimal minPrice, decimal maxPrice);
        Task<IEnumerable<GoiTap>> GetPopularPackagesAsync(int top = 10);
        Task<GoiTap?> GetWithDangKysAsync(int goiTapId);
    }
}
