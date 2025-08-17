using GymManagement.Web.Data.Models;
using GymManagement.Web.Models.DTOs;

namespace GymManagement.Web.Services
{
    public interface ITinTucService
    {
        Task<IEnumerable<TinTucListDto>> GetAllAsync();
        Task<IEnumerable<TinTucPublicDto>> GetPublishedAsync();
        Task<TinTucDetailDto?> GetByIdAsync(int id);
        Task<TinTucPublicDto?> GetBySlugAsync(string slug);
        Task<TinTucPublicDto?> GetPublishedBySlugAsync(string slug);
        Task<TinTuc> CreateAsync(CreateTinTucDto dto, int tacGiaId, string tacGiaTen);
        Task<TinTuc> UpdateAsync(int id, EditTinTucDto dto, int nguoiCapNhatId);
        Task<bool> DeleteAsync(int id);
        Task<IEnumerable<TinTucPublicDto>> GetNoiBatAsync(int count = 5);
        Task<IEnumerable<TinTucPublicDto>> SearchAsync(string keyword);
        Task<IEnumerable<TinTucPublicDto>> GetByTacGiaAsync(int tacGiaId);
        Task<string> GenerateSlugAsync(string title, int? excludeId = null);
        Task<string?> SaveImageAsync(IFormFile image);
        void DeleteImage(string imagePath);
        Task<int> IncrementViewAsync(int id);
        Task<IEnumerable<TinTucPublicDto>> GetRelatedAsync(int id, int count = 4);
    }
}
