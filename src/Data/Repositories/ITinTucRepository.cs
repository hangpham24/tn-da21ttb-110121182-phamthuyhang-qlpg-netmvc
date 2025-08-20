using GymManagement.Web.Data.Models;

namespace GymManagement.Web.Data.Repositories
{
    public interface ITinTucRepository : IRepository<TinTuc>
    {
        Task<IEnumerable<TinTuc>> GetPublishedAsync();
        Task<IEnumerable<TinTuc>> GetByTacGiaIdAsync(int tacGiaId);
        Task<IEnumerable<TinTuc>> GetNoiBatAsync(int count = 5);
        Task<TinTuc?> GetBySlugAsync(string slug);
        Task<TinTuc?> GetPublishedBySlugAsync(string slug);
        Task<IEnumerable<TinTuc>> SearchAsync(string keyword);
        Task<bool> SlugExistsAsync(string slug, int? excludeId = null);
        Task<int> IncrementLuotXemAsync(int tinTucId);
        Task<IEnumerable<TinTuc>> GetRelatedAsync(int tinTucId, int count = 4);
    }
}
