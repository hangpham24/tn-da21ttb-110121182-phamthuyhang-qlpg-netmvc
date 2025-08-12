using GymManagement.Web.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace GymManagement.Web.Data.Repositories
{
    public class MauMatRepository : IMauMatRepository
    {
        private readonly GymDbContext _context;
        private readonly ILogger<MauMatRepository> _logger;

        public MauMatRepository(GymDbContext context, ILogger<MauMatRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<MauMat?> GetByIdAsync(int id)
        {
            try
            {
                return await _context.MauMats
                    .Include(m => m.NguoiDung)
                    .FirstOrDefaultAsync(m => m.MauMatId == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting MauMat by ID {Id}", id);
                return null;
            }
        }

        public async Task<IEnumerable<MauMat>> GetAllAsync()
        {
            try
            {
                return await _context.MauMats
                    .Include(m => m.NguoiDung)
                    .OrderByDescending(m => m.NgayTao)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all MauMats");
                return new List<MauMat>();
            }
        }

        public async Task<MauMat> AddAsync(MauMat mauMat)
        {
            try
            {
                mauMat.NgayTao = DateTime.Now;
                _context.MauMats.Add(mauMat);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Added new MauMat for user {UserId}", mauMat.NguoiDungId);
                return mauMat;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding MauMat for user {UserId}", mauMat.NguoiDungId);
                throw;
            }
        }

        public async Task<MauMat> UpdateAsync(MauMat mauMat)
        {
            try
            {
                _context.MauMats.Update(mauMat);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Updated MauMat {Id}", mauMat.MauMatId);
                return mauMat;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating MauMat {Id}", mauMat.MauMatId);
                throw;
            }
        }

        public async Task<bool> DeleteAsync(int id)
        {
            try
            {
                var mauMat = await _context.MauMats.FindAsync(id);
                if (mauMat == null)
                    return false;

                _context.MauMats.Remove(mauMat);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Deleted MauMat {Id}", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting MauMat {Id}", id);
                return false;
            }
        }

        public async Task<IEnumerable<MauMat>> GetByNguoiDungIdAsync(int nguoiDungId)
        {
            try
            {
                return await _context.MauMats
                    .Include(m => m.NguoiDung)
                    .Where(m => m.NguoiDungId == nguoiDungId)
                    .OrderByDescending(m => m.NgayTao)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting MauMats for user {UserId}", nguoiDungId);
                return new List<MauMat>();
            }
        }

        public async Task<IEnumerable<MauMat>> GetAllWithUsersAsync()
        {
            try
            {
                return await _context.MauMats
                    .Include(m => m.NguoiDung)
                    .Where(m => m.NguoiDung != null && m.NguoiDung.TrangThai == "Active")
                    .OrderByDescending(m => m.NgayTao)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all MauMats with users");
                return new List<MauMat>();
            }
        }

        public async Task<MauMat?> GetByNguoiDungIdAndAlgorithmAsync(int nguoiDungId, string algorithm)
        {
            try
            {
                return await _context.MauMats
                    .Include(m => m.NguoiDung)
                    .FirstOrDefaultAsync(m => m.NguoiDungId == nguoiDungId && m.ThuatToan == algorithm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting MauMat for user {UserId} with algorithm {Algorithm}", nguoiDungId, algorithm);
                return null;
            }
        }

        public async Task<int> CountAsync()
        {
            try
            {
                return await _context.MauMats.CountAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error counting MauMats");
                return 0;
            }
        }

        public async Task<int> CountByNguoiDungIdAsync(int nguoiDungId)
        {
            try
            {
                return await _context.MauMats
                    .CountAsync(m => m.NguoiDungId == nguoiDungId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error counting MauMats for user {UserId}", nguoiDungId);
                return 0;
            }
        }

        public async Task<int> CountByAlgorithmAsync(string algorithm)
        {
            try
            {
                return await _context.MauMats
                    .CountAsync(m => m.ThuatToan == algorithm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error counting MauMats for algorithm {Algorithm}", algorithm);
                return 0;
            }
        }

        public async Task<IEnumerable<MauMat>> GetByDateRangeAsync(DateTime fromDate, DateTime toDate)
        {
            try
            {
                return await _context.MauMats
                    .Include(m => m.NguoiDung)
                    .Where(m => m.NgayTao >= fromDate && m.NgayTao <= toDate)
                    .OrderByDescending(m => m.NgayTao)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting MauMats by date range");
                return new List<MauMat>();
            }
        }

        public async Task<IEnumerable<MauMat>> GetRecentAsync(int count = 10)
        {
            try
            {
                return await _context.MauMats
                    .Include(m => m.NguoiDung)
                    .OrderByDescending(m => m.NgayTao)
                    .Take(count)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recent MauMats");
                return new List<MauMat>();
            }
        }

        public async Task<bool> DeleteByNguoiDungIdAsync(int nguoiDungId)
        {
            try
            {
                var mauMats = await _context.MauMats
                    .Where(m => m.NguoiDungId == nguoiDungId)
                    .ToListAsync();

                if (mauMats.Any())
                {
                    _context.MauMats.RemoveRange(mauMats);
                    await _context.SaveChangesAsync();
                    
                    _logger.LogInformation("Deleted {Count} MauMats for user {UserId}", mauMats.Count, nguoiDungId);
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting MauMats for user {UserId}", nguoiDungId);
                return false;
            }
        }

        public async Task<bool> BulkInsertAsync(IEnumerable<MauMat> mauMats)
        {
            try
            {
                foreach (var mauMat in mauMats)
                {
                    mauMat.NgayTao = DateTime.Now;
                }

                _context.MauMats.AddRange(mauMats);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Bulk inserted {Count} MauMats", mauMats.Count());
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error bulk inserting MauMats");
                return false;
            }
        }

        public async Task<bool> ExistsAsync(int id)
        {
            try
            {
                return await _context.MauMats.AnyAsync(m => m.MauMatId == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if MauMat {Id} exists", id);
                return false;
            }
        }

        public async Task<bool> HasFaceDataAsync(int nguoiDungId)
        {
            try
            {
                return await _context.MauMats.AnyAsync(m => m.NguoiDungId == nguoiDungId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if user {UserId} has face data", nguoiDungId);
                return false;
            }
        }
    }
}
