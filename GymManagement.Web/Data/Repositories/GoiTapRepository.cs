using GymManagement.Web.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace GymManagement.Web.Data.Repositories
{
    public class GoiTapRepository : Repository<GoiTap>, IGoiTapRepository
    {
        public GoiTapRepository(GymDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<GoiTap>> GetActivePackagesAsync()
        {
            return await _dbSet.ToListAsync(); // Assuming all packages are active by default
        }

        public async Task<IEnumerable<GoiTap>> GetByPriceRangeAsync(decimal minPrice, decimal maxPrice)
        {
            return await _dbSet
                .Where(x => x.Gia >= minPrice && x.Gia <= maxPrice)
                .OrderBy(x => x.Gia)
                .ToListAsync();
        }

        public async Task<IEnumerable<GoiTap>> GetPopularPackagesAsync(int top = 10)
        {
            return await _dbSet
                .Include(x => x.DangKys)
                .OrderByDescending(x => x.DangKys.Count)
                .Take(top)
                .ToListAsync();
        }

        public async Task<GoiTap?> GetWithDangKysAsync(int goiTapId)
        {
            return await _dbSet
                .Include(x => x.DangKys)
                .ThenInclude(x => x.NguoiDung)
                .FirstOrDefaultAsync(x => x.GoiTapId == goiTapId);
        }
    }
}
