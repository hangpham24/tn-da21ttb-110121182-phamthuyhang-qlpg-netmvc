using GymManagement.Web.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace GymManagement.Web.Data.Repositories
{
    public class TinTucRepository : Repository<TinTuc>, ITinTucRepository
    {
        private readonly GymDbContext _context;

public TinTucRepository(GymDbContext context) : base(context)
        {
            _context = context;
        }

        public override async Task<TinTuc> AddAsync(TinTuc entity)
        {
            await _dbSet.AddAsync(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public override async Task UpdateAsync(TinTuc entity)
        {
            _dbSet.Update(entity);
            await _context.SaveChangesAsync();
        }

        public override async Task DeleteAsync(TinTuc entity)
        {
            _dbSet.Remove(entity);
            await _context.SaveChangesAsync();
        }

        public override async Task<TinTuc?> GetByIdAsync(int id)
        {
            return await _context.TinTucs
                .Include(t => t.TacGia)
                .FirstOrDefaultAsync(t => t.TinTucId == id);
        }

        public override async Task<IEnumerable<TinTuc>> GetAllAsync()
        {
            return await _context.TinTucs
                .Include(t => t.TacGia)
                .OrderByDescending(t => t.NgayTao)
                .ToListAsync();
        }

        public async Task<IEnumerable<TinTuc>> GetPublishedAsync()
        {
            return await _context.TinTucs
                .Include(t => t.TacGia)
                .Where(t => t.TrangThai == "PUBLISHED" && t.NgayXuatBan <= DateTime.Now)
                .OrderByDescending(t => t.NgayXuatBan)
                .ToListAsync();
        }

        public async Task<IEnumerable<TinTuc>> GetByTacGiaIdAsync(int tacGiaId)
        {
            return await _context.TinTucs
                .Include(t => t.TacGia)
                .Where(t => t.TacGiaId == tacGiaId)
                .OrderByDescending(t => t.NgayTao)
                .ToListAsync();
        }

        public async Task<IEnumerable<TinTuc>> GetNoiBatAsync(int count = 5)
        {
            return await _context.TinTucs
                .Include(t => t.TacGia)
                .Where(t => t.TrangThai == "PUBLISHED" && 
                       t.NgayXuatBan <= DateTime.Now && 
                       t.NoiBat)
                .OrderByDescending(t => t.NgayXuatBan)
                .Take(count)
                .ToListAsync();
        }

        public async Task<TinTuc?> GetBySlugAsync(string slug)
        {
            return await _context.TinTucs
                .Include(t => t.TacGia)
                .FirstOrDefaultAsync(t => t.Slug == slug);
        }

        public async Task<TinTuc?> GetPublishedBySlugAsync(string slug)
        {
            return await _context.TinTucs
                .Include(t => t.TacGia)
                .FirstOrDefaultAsync(t => t.Slug == slug && t.TrangThai == "PUBLISHED" && t.NgayXuatBan <= DateTime.Now);
        }

        public async Task<IEnumerable<TinTuc>> SearchAsync(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
                return await GetPublishedAsync();

            keyword = keyword.ToLower();
            return await _context.TinTucs
                .Include(t => t.TacGia)
                .Where(t => t.TrangThai == "PUBLISHED" && 
                       t.NgayXuatBan <= DateTime.Now &&
                       (t.TieuDe.ToLower().Contains(keyword) || 
                        t.MoTaNgan.ToLower().Contains(keyword) ||
                        t.NoiDung.ToLower().Contains(keyword)))
                .OrderByDescending(t => t.NgayXuatBan)
                .ToListAsync();
        }

        public async Task<bool> SlugExistsAsync(string slug, int? excludeId = null)
        {
            var query = _context.TinTucs.Where(t => t.Slug == slug);
            
            if (excludeId.HasValue)
                query = query.Where(t => t.TinTucId != excludeId.Value);
            
            return await query.AnyAsync();
        }

        public async Task<int> IncrementLuotXemAsync(int tinTucId)
        {
            var tinTuc = await _context.TinTucs.FindAsync(tinTucId);
            if (tinTuc != null)
            {
                tinTuc.LuotXem++;
                await _context.SaveChangesAsync();
                return tinTuc.LuotXem;
            }
            return 0;
        }

        public async Task<IEnumerable<TinTuc>> GetRelatedAsync(int tinTucId, int count = 4)
        {
            var currentTinTuc = await _context.TinTucs.FindAsync(tinTucId);
            if (currentTinTuc == null) return new List<TinTuc>();

            // Get the first keyword to search for similar articles
            var firstKeyword = currentTinTuc.MetaKeywords?.Split(',').FirstOrDefault()?.Trim();

            // Get related news (same author or similar keywords)
            return await _context.TinTucs
                .Include(t => t.TacGia)
                .Where(t => t.TinTucId != tinTucId && 
                       t.TrangThai == "PUBLISHED" && 
                       t.NgayXuatBan <= DateTime.Now &&
                       (t.TacGiaId == currentTinTuc.TacGiaId || 
                        (!string.IsNullOrEmpty(firstKeyword) && t.MetaKeywords != null && t.MetaKeywords.Contains(firstKeyword))))
                .OrderByDescending(t => t.NgayXuatBan)
                .Take(count)
                .ToListAsync();
        }
    }
}
