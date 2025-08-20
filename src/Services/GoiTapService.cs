using GymManagement.Web.Data;
using GymManagement.Web.Data.Models;
using GymManagement.Web.Models.DTOs;

namespace GymManagement.Web.Services
{
    public class GoiTapService : IGoiTapService
    {
        private readonly IUnitOfWork _unitOfWork;

        public GoiTapService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<GoiTapDto?> GetByIdAsync(int id)
        {
            var goiTap = await _unitOfWork.GoiTaps.GetByIdAsync(id);
            return goiTap != null ? MapToDto(goiTap) : null;
        }

        public async Task<IEnumerable<GoiTapDto>> GetAllAsync()
        {
            var goiTaps = await _unitOfWork.GoiTaps.GetAllAsync();
            return goiTaps.Select(MapToDto);
        }

        public async Task<(IEnumerable<GoiTapDto> Items, int TotalCount)> GetPagedAsync(
            int pageNumber, int pageSize, string? searchTerm = null, decimal? minPrice = null, decimal? maxPrice = null)
        {
            var filter = BuildFilter(searchTerm, minPrice, maxPrice);
            var orderBy = BuildOrderBy();

            var (items, totalCount) = await _unitOfWork.GoiTaps.GetPagedAsync(
                pageNumber, pageSize, filter, orderBy);

            return (items.Select(MapToDto), totalCount);
        }

        public async Task<GoiTapDto> CreateAsync(CreateGoiTapDto createDto)
        {
            var goiTap = new GoiTap
            {
                TenGoi = createDto.TenGoi,
                ThoiHanThang = createDto.ThoiHanThang,
                SoBuoiToiDa = createDto.SoBuoiToiDa,
                Gia = createDto.Gia,
                MoTa = createDto.MoTa
                // Note: LoaiGoi, TrangThai, UuDaiDacBiet are not in GoiTap model
                // They will be handled by MapToDto with default values
            };

            await _unitOfWork.GoiTaps.AddAsync(goiTap);
            await _unitOfWork.SaveChangesAsync();

            return MapToDto(goiTap);
        }

        public async Task<GoiTapDto> UpdateAsync(UpdateGoiTapDto updateDto)
        {
            var goiTap = await _unitOfWork.GoiTaps.GetByIdAsync(updateDto.GoiTapId);
            if (goiTap == null)
                throw new ArgumentException("Gói tập không tồn tại");

            // Update properties
            goiTap.TenGoi = updateDto.TenGoi;
            goiTap.ThoiHanThang = updateDto.ThoiHanThang;
            goiTap.SoBuoiToiDa = updateDto.SoBuoiToiDa;
            goiTap.Gia = updateDto.Gia;
            goiTap.MoTa = updateDto.MoTa;

            await _unitOfWork.GoiTaps.UpdateAsync(goiTap);
            await _unitOfWork.SaveChangesAsync();

            return MapToDto(goiTap);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            if (!await CanDeletePackageAsync(id))
                return false;

            var goiTap = await _unitOfWork.GoiTaps.GetByIdAsync(id);
            if (goiTap == null)
                return false;

            await _unitOfWork.GoiTaps.DeleteAsync(goiTap);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<GoiTapDto>> GetActivePackagesAsync()
        {
            var goiTaps = await _unitOfWork.GoiTaps.GetActivePackagesAsync();
            return goiTaps.Select(MapToDto);
        }

        public async Task<IEnumerable<GoiTapDto>> GetByPriceRangeAsync(decimal minPrice, decimal maxPrice)
        {
            var goiTaps = await _unitOfWork.GoiTaps.GetByPriceRangeAsync(minPrice, maxPrice);
            return goiTaps.Select(MapToDto);
        }

        public async Task<IEnumerable<GoiTapDto>> GetPopularPackagesAsync(int top = 10)
        {
            var goiTaps = await _unitOfWork.GoiTaps.GetPopularPackagesAsync(top);
            return goiTaps.Select(MapToDto);
        }

        public async Task<bool> CanDeletePackageAsync(int id)
        {
            // Check if package has active registrations
            var hasActiveRegistrations = await _unitOfWork.DangKys.ExistsAsync(
                x => x.GoiTapId == id && x.TrangThai == "ACTIVE");
            
            return !hasActiveRegistrations;
        }

        // Private helper methods
        private static GoiTapDto MapToDto(GoiTap goiTap)
        {
            return new GoiTapDto
            {
                GoiTapId = goiTap.GoiTapId,
                TenGoi = goiTap.TenGoi,
                ThoiHanThang = goiTap.ThoiHanThang,
                SoBuoiToiDa = goiTap.SoBuoiToiDa,
                Gia = goiTap.Gia,
                MoTa = goiTap.MoTa,
                LoaiGoi = "BASIC", // Default value since not in model
                TrangThai = "ACTIVE", // Default value since not in model
                UuDaiDacBiet = null, // Default value since not in model
                NgayTao = DateTime.Now // Default value since not in model
            };
        }

        private static System.Linq.Expressions.Expression<Func<GoiTap, bool>>? BuildFilter(
            string? searchTerm, decimal? minPrice, decimal? maxPrice)
        {
            if (string.IsNullOrEmpty(searchTerm) && minPrice == null && maxPrice == null)
                return null;

            return x => (string.IsNullOrEmpty(searchTerm) || 
                        x.TenGoi.Contains(searchTerm) || 
                        (x.MoTa != null && x.MoTa.Contains(searchTerm))) &&
                       (minPrice == null || x.Gia >= minPrice) &&
                       (maxPrice == null || x.Gia <= maxPrice);
        }

        private static Func<IQueryable<GoiTap>, IOrderedQueryable<GoiTap>> BuildOrderBy()
        {
            return query => query.OrderBy(x => x.Gia);
        }
    }
}
