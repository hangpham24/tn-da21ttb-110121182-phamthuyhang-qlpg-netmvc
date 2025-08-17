using GymManagement.Web.Models.DTOs;

namespace GymManagement.Web.Services
{
    public interface INguoiDungService
    {
        // Basic CRUD
        Task<NguoiDungDto?> GetByIdAsync(int id);
        Task<IEnumerable<NguoiDungDto>> GetAllAsync();
        Task<(IEnumerable<NguoiDungDto> Items, int TotalCount)> GetPagedAsync(
            int pageNumber, int pageSize, string? searchTerm = null, string? loaiNguoiDung = null);
        Task<NguoiDungDto> CreateAsync(CreateNguoiDungDto createDto);
        Task<NguoiDungDto> UpdateAsync(UpdateNguoiDungDto updateDto);
        Task<bool> UpdateAsync(NguoiDungDto nguoiDungDto);
        Task<bool> DeleteAsync(int id);
        Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword);

        // Methods with TaiKhoan included
        Task<IEnumerable<NguoiDungDto>> GetAllWithTaiKhoanAsync();
        Task<(IEnumerable<NguoiDungDto> Items, int TotalCount)> GetPagedWithTaiKhoanAsync(
            int pageNumber, int pageSize, string? searchTerm = null, string? loaiNguoiDung = null);

        // Business methods
        Task<IEnumerable<NguoiDungDto>> GetByLoaiNguoiDungAsync(string loaiNguoiDung);
        Task<NguoiDungDto?> GetByEmailAsync(string email);
        Task<NguoiDungDto?> GetBySoDienThoaiAsync(string soDienThoai);
        Task<IEnumerable<NguoiDungDto>> GetActiveUsersAsync();
        Task<IEnumerable<NguoiDungDto>> GetHuanLuyenViensAsync();
        Task<IEnumerable<NguoiDungDto>> GetThanhViensAsync();
        Task<IEnumerable<NguoiDungDto>> GetMembersAsync();
        Task<IEnumerable<NguoiDungDto>> GetTrainersAsync();
        Task<bool> IsEmailExistsAsync(string email, int? excludeId = null);
        Task<bool> IsSoDienThoaiExistsAsync(string soDienThoai, int? excludeId = null);
        Task<bool> DeactivateUserAsync(int id);
        Task<bool> ActivateUserAsync(int id);
        
        // Additional methods
        Task<(bool CanDelete, string Message)> CanDeleteUserAsync(int userId);
        Task<bool> UpdateAvatarAsync(int userId, string avatarPath);
    }
}
