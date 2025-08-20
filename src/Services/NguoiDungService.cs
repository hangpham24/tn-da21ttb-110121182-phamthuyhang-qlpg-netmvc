using GymManagement.Web.Data;
using GymManagement.Web.Data.Models;
using GymManagement.Web.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace GymManagement.Web.Services
{
    public class NguoiDungService : INguoiDungService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<NguoiDungService> _logger;
        private readonly IPasswordService _passwordService;

        public NguoiDungService(IUnitOfWork unitOfWork, ILogger<NguoiDungService> logger, IPasswordService passwordService)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _passwordService = passwordService;
        }

        public async Task<NguoiDungDto?> GetByIdAsync(int id)
        {
            var nguoiDung = await _unitOfWork.NguoiDungs.GetWithTaiKhoanAsync(id);
            return nguoiDung != null ? MapToDtoWithTaiKhoan(nguoiDung) : null;
        }

        public async Task<IEnumerable<NguoiDungDto>> GetAllAsync()
        {
            var nguoiDungs = await _unitOfWork.NguoiDungs.GetAllAsync();
            return nguoiDungs.Select(MapToDto);
        }

        public async Task<(IEnumerable<NguoiDungDto> Items, int TotalCount)> GetPagedAsync(
            int pageNumber, int pageSize, string? searchTerm = null, string? loaiNguoiDung = null)
        {
            var filter = BuildFilter(searchTerm, loaiNguoiDung);
            var orderBy = BuildOrderBy();

            var (items, totalCount) = await _unitOfWork.NguoiDungs.GetPagedAsync(
                pageNumber, pageSize, filter, orderBy);

            return (items.Select(MapToDto), totalCount);
        }

        public async Task<NguoiDungDto> CreateAsync(CreateNguoiDungDto createDto)
        {
            // Validation
            await ValidateCreateAsync(createDto);

            var nguoiDung = new NguoiDung
            {
                LoaiNguoiDung = createDto.LoaiNguoiDung,
                Ho = createDto.Ho,
                Ten = createDto.Ten,
                GioiTinh = createDto.GioiTinh,
                NgaySinh = createDto.NgaySinh,
                SoDienThoai = createDto.SoDienThoai,
                Email = createDto.Email,
                NgayThamGia = DateOnly.FromDateTime(DateTime.Now),
                TrangThai = "ACTIVE"
            };

            await _unitOfWork.NguoiDungs.AddAsync(nguoiDung);
            await _unitOfWork.SaveChangesAsync();

            return MapToDto(nguoiDung);
        }

        public async Task<NguoiDungDto> UpdateAsync(UpdateNguoiDungDto updateDto)
        {
            var nguoiDung = await _unitOfWork.NguoiDungs.GetByIdAsync(updateDto.NguoiDungId);
            if (nguoiDung == null)
                throw new InvalidOperationException("Người dùng không tồn tại");

            // Validation
            await ValidateUpdateAsync(updateDto);

            // Update properties
            nguoiDung.LoaiNguoiDung = updateDto.LoaiNguoiDung;
            nguoiDung.Ho = updateDto.Ho;
            nguoiDung.Ten = updateDto.Ten;
            nguoiDung.GioiTinh = updateDto.GioiTinh;
            nguoiDung.NgaySinh = updateDto.NgaySinh;
            nguoiDung.SoDienThoai = updateDto.SoDienThoai;
            nguoiDung.Email = updateDto.Email;
            nguoiDung.TrangThai = updateDto.TrangThai;

            await _unitOfWork.NguoiDungs.UpdateAsync(nguoiDung);
            await _unitOfWork.SaveChangesAsync();

            return MapToDto(nguoiDung);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            try
            {
                var nguoiDung = await _unitOfWork.NguoiDungs.GetByIdAsync(id);
                if (nguoiDung == null)
                    return false;

                // Check if user can be deleted
                var (canDelete, message) = await CanDeleteUserAsync(id);
                if (!canDelete)
                {
                    throw new InvalidOperationException(message);
                }

                // Delete related data first to avoid foreign key constraints
                await DeleteRelatedDataAsync(id);

                // Delete the user
                await _unitOfWork.NguoiDungs.DeleteAsync(nguoiDung);
                await _unitOfWork.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting user ID: {UserId}", id);
                throw;
            }
        }

        private async Task DeleteRelatedDataAsync(int nguoiDungId)
        {
            try
            {
                // Delete ThongBaos
                var thongBaos = _unitOfWork.Context.ThongBaos.Where(tb => tb.NguoiDungId == nguoiDungId);
                _unitOfWork.Context.ThongBaos.RemoveRange(thongBaos);

                // Delete Face Recognition data
                var mauMats = _unitOfWork.Context.MauMats.Where(mm => mm.NguoiDungId == nguoiDungId);
                _unitOfWork.Context.MauMats.RemoveRange(mauMats);

                // Delete DiemDanhs (attendance records) - Use raw SQL to avoid model mismatch
                await _unitOfWork.Context.Database.ExecuteSqlRawAsync(
                    "DELETE FROM DiemDanhs WHERE ThanhVienId = {0}", nguoiDungId);

                // Delete BuoiTaps (workout sessions) - Use raw SQL to avoid potential issues
                await _unitOfWork.Context.Database.ExecuteSqlRawAsync(
                    "DELETE FROM BuoiTaps WHERE ThanhVienId = {0}", nguoiDungId);

                // Delete ThanhToans (payments) - through DangKys
                var dangKyIds = await _unitOfWork.Context.DangKys
                    .Where(dk => dk.NguoiDungId == nguoiDungId)
                    .Select(dk => dk.DangKyId)
                    .ToListAsync();
                var thanhToans = _unitOfWork.Context.ThanhToans.Where(tt => tt.DangKyId.HasValue && dangKyIds.Contains(tt.DangKyId.Value));
                _unitOfWork.Context.ThanhToans.RemoveRange(thanhToans);

                // Delete DangKys (registrations)
                var dangKys = _unitOfWork.Context.DangKys.Where(dk => dk.NguoiDungId == nguoiDungId);
                _unitOfWork.Context.DangKys.RemoveRange(dangKys);

                // Delete Bookings
                var bookings = _unitOfWork.Context.Bookings.Where(b => b.ThanhVienId == nguoiDungId);
                _unitOfWork.Context.Bookings.RemoveRange(bookings);

                // Save changes for related data deletion
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Successfully deleted related data for user ID: {UserId}", nguoiDungId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting related data for user ID: {UserId}", nguoiDungId);
                throw;
            }
        }

        public async Task<IEnumerable<NguoiDungDto>> GetByLoaiNguoiDungAsync(string loaiNguoiDung)
        {
            var nguoiDungs = await _unitOfWork.NguoiDungs.GetByLoaiNguoiDungAsync(loaiNguoiDung);
            return nguoiDungs.Select(MapToDto);
        }

        public async Task<NguoiDungDto?> GetByEmailAsync(string email)
        {
            var nguoiDung = await _unitOfWork.NguoiDungs.GetByEmailAsync(email);
            return nguoiDung != null ? MapToDto(nguoiDung) : null;
        }

        public async Task<NguoiDungDto?> GetBySoDienThoaiAsync(string soDienThoai)
        {
            var nguoiDung = await _unitOfWork.NguoiDungs.GetBySoDienThoaiAsync(soDienThoai);
            return nguoiDung != null ? MapToDto(nguoiDung) : null;
        }

        public async Task<IEnumerable<NguoiDungDto>> GetActiveUsersAsync()
        {
            var nguoiDungs = await _unitOfWork.NguoiDungs.GetActiveUsersAsync();
            return nguoiDungs.Select(MapToDto);
        }

        public async Task<IEnumerable<NguoiDungDto>> GetHuanLuyenViensAsync()
        {
            var nguoiDungs = await _unitOfWork.NguoiDungs.GetHuanLuyenViensAsync();
            return nguoiDungs.Select(MapToDto);
        }

        public async Task<IEnumerable<NguoiDungDto>> GetThanhViensAsync()
        {
            var nguoiDungs = await _unitOfWork.NguoiDungs.GetThanhViensAsync();
            return nguoiDungs.Select(MapToDto);
        }

        public async Task<IEnumerable<NguoiDungDto>> GetMembersAsync()
        {
            var nguoiDungs = await _unitOfWork.NguoiDungs.GetMembersAsync();
            return nguoiDungs.Select(MapToDto);
        }

        public async Task<IEnumerable<NguoiDungDto>> GetTrainersAsync()
        {
            var nguoiDungs = await _unitOfWork.NguoiDungs.GetTrainersAsync();
            return nguoiDungs.Select(MapToDto);
        }

        public async Task<bool> IsEmailExistsAsync(string email, int? excludeId = null)
        {
            var nguoiDung = await _unitOfWork.NguoiDungs.GetByEmailAsync(email);
            return nguoiDung != null && (excludeId == null || nguoiDung.NguoiDungId != excludeId);
        }

        public async Task<bool> IsSoDienThoaiExistsAsync(string soDienThoai, int? excludeId = null)
        {
            var nguoiDung = await _unitOfWork.NguoiDungs.GetBySoDienThoaiAsync(soDienThoai);
            return nguoiDung != null && (excludeId == null || nguoiDung.NguoiDungId != excludeId);
        }

        public async Task<bool> DeactivateUserAsync(int id)
        {
            var nguoiDung = await _unitOfWork.NguoiDungs.GetByIdAsync(id);
            if (nguoiDung == null)
                return false;

            nguoiDung.TrangThai = "INACTIVE";
            await _unitOfWork.NguoiDungs.UpdateAsync(nguoiDung);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ActivateUserAsync(int id)
        {
            var nguoiDung = await _unitOfWork.NguoiDungs.GetByIdAsync(id);
            if (nguoiDung == null)
                return false;

            nguoiDung.TrangThai = "ACTIVE";
            await _unitOfWork.NguoiDungs.UpdateAsync(nguoiDung);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<NguoiDungDto>> GetAllWithTaiKhoanAsync()
        {
            var nguoiDungs = await _unitOfWork.NguoiDungs.GetAllWithTaiKhoanAsync();
            return nguoiDungs.Select(MapToDtoWithTaiKhoan);
        }

        public async Task<(IEnumerable<NguoiDungDto> Items, int TotalCount)> GetPagedWithTaiKhoanAsync(
            int pageNumber, int pageSize, string? searchTerm = null, string? loaiNguoiDung = null)
        {
            var filter = BuildFilter(searchTerm, loaiNguoiDung);
            var orderBy = BuildOrderBy();

            var (items, totalCount) = await _unitOfWork.NguoiDungs.GetPagedWithTaiKhoanAsync(
                pageNumber, pageSize, filter, orderBy);

            return (items.Select(MapToDtoWithTaiKhoan), totalCount);
        }

        // Private helper methods
        private static NguoiDungDto MapToDto(NguoiDung nguoiDung)
        {
            return new NguoiDungDto
            {
                NguoiDungId = nguoiDung.NguoiDungId,
                LoaiNguoiDung = nguoiDung.LoaiNguoiDung,
                Ho = nguoiDung.Ho,
                Ten = nguoiDung.Ten,
                GioiTinh = nguoiDung.GioiTinh,
                NgaySinh = nguoiDung.NgaySinh,
                SoDienThoai = nguoiDung.SoDienThoai,
                Email = nguoiDung.Email,
                NgayThamGia = nguoiDung.NgayThamGia,
                TrangThai = nguoiDung.TrangThai,
                AnhDaiDien = nguoiDung.AnhDaiDien,
                NgayTao = nguoiDung.NgayTao
            };
        }

        private static NguoiDungDto MapToDtoWithTaiKhoan(NguoiDung nguoiDung)
        {
            return new NguoiDungDto
            {
                NguoiDungId = nguoiDung.NguoiDungId,
                LoaiNguoiDung = nguoiDung.LoaiNguoiDung,
                Ho = nguoiDung.Ho,
                Ten = nguoiDung.Ten,
                GioiTinh = nguoiDung.GioiTinh,
                NgaySinh = nguoiDung.NgaySinh,
                SoDienThoai = nguoiDung.SoDienThoai,
                Email = nguoiDung.Email,
                NgayThamGia = nguoiDung.NgayThamGia,
                TrangThai = nguoiDung.TrangThai,
                AnhDaiDien = nguoiDung.AnhDaiDien,
                NgayTao = nguoiDung.NgayTao,
                // Add TaiKhoan information
                Username = nguoiDung.TaiKhoan?.TenDangNhap,
                HasAccount = nguoiDung.TaiKhoan != null
            };
        }

        private static System.Linq.Expressions.Expression<Func<NguoiDung, bool>>? BuildFilter(
            string? searchTerm, string? loaiNguoiDung)
        {
            if (string.IsNullOrEmpty(searchTerm) && string.IsNullOrEmpty(loaiNguoiDung))
                return null;

            return x => (string.IsNullOrEmpty(searchTerm) || 
                        x.Ho.Contains(searchTerm) || 
                        (x.Ten != null && x.Ten.Contains(searchTerm)) ||
                        (x.Email != null && x.Email.Contains(searchTerm)) ||
                        (x.SoDienThoai != null && x.SoDienThoai.Contains(searchTerm))) &&
                       (string.IsNullOrEmpty(loaiNguoiDung) || x.LoaiNguoiDung == loaiNguoiDung);
        }

        private static Func<IQueryable<NguoiDung>, IOrderedQueryable<NguoiDung>> BuildOrderBy()
        {
            return query => query.OrderBy(x => x.Ho).ThenBy(x => x.Ten);
        }

        private async Task ValidateCreateAsync(CreateNguoiDungDto createDto)
        {
            if (!string.IsNullOrEmpty(createDto.Email))
            {
                if (await IsEmailExistsAsync(createDto.Email))
                    throw new InvalidOperationException("Email đã tồn tại");
            }

            if (!string.IsNullOrEmpty(createDto.SoDienThoai))
            {
                if (await IsSoDienThoaiExistsAsync(createDto.SoDienThoai))
                    throw new InvalidOperationException("Số điện thoại đã tồn tại");
            }
        }

        private async Task ValidateUpdateAsync(UpdateNguoiDungDto updateDto)
        {
            if (!string.IsNullOrEmpty(updateDto.Email))
            {
                if (await IsEmailExistsAsync(updateDto.Email, updateDto.NguoiDungId))
                    throw new InvalidOperationException("Email đã tồn tại");
            }

            if (!string.IsNullOrEmpty(updateDto.SoDienThoai))
            {
                if (await IsSoDienThoaiExistsAsync(updateDto.SoDienThoai, updateDto.NguoiDungId))
                    throw new InvalidOperationException("Số điện thoại đã tồn tại");
            }
        }

        public async Task<bool> UpdateAsync(NguoiDungDto nguoiDungDto)
        {
            try
            {
                var nguoiDung = await _unitOfWork.NguoiDungs.GetByIdAsync(nguoiDungDto.NguoiDungId);
                if (nguoiDung == null) return false;

                // Update fields
                nguoiDung.Ho = nguoiDungDto.Ho;
                nguoiDung.Ten = nguoiDungDto.Ten;
                nguoiDung.Email = nguoiDungDto.Email;
                nguoiDung.SoDienThoai = nguoiDungDto.SoDienThoai;
                nguoiDung.GioiTinh = nguoiDungDto.GioiTinh;
                nguoiDung.NgaySinh = nguoiDungDto.NgaySinh;
                nguoiDung.AnhDaiDien = nguoiDungDto.AnhDaiDien;

                await _unitOfWork.NguoiDungs.UpdateAsync(nguoiDung);
                await _unitOfWork.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
        {
            try
            {
                // Get NguoiDung with TaiKhoan
                var nguoiDung = await _unitOfWork.NguoiDungs.GetWithTaiKhoanAsync(userId);
                if (nguoiDung?.TaiKhoan == null) return false;

                // Verify current password using PasswordService
                if (!_passwordService.VerifyPassword(currentPassword, nguoiDung.TaiKhoan.Salt, nguoiDung.TaiKhoan.MatKhauHash))
                {
                    return false;
                }

                // Hash new password with existing salt
                nguoiDung.TaiKhoan.MatKhauHash = _passwordService.HashPassword(newPassword, nguoiDung.TaiKhoan.Salt);

                await _unitOfWork.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                // Log the exception for debugging
                // You might want to inject ILogger here for better error tracking
                return false;
            }
        }

        public async Task<(bool CanDelete, string Message)> CanDeleteUserAsync(int userId)
        {
            try
            {
                var nguoiDung = await _unitOfWork.NguoiDungs.GetByIdAsync(userId);
                if (nguoiDung == null)
                {
                    return (false, "Không tìm thấy người dùng.");
                }

                // Check if user has active registrations
                var activeRegistrations = nguoiDung.DangKys?.Count(d => d.TrangThai == "ACTIVE") ?? 0;
                if (activeRegistrations > 0)
                {
                    return (false, $"Không thể xóa người dùng này vì đang có {activeRegistrations} đăng ký hoạt động.");
                }

                // Check if user has future bookings
                var futureBookings = nguoiDung.Bookings?.Count(b => b.Ngay >= DateOnly.FromDateTime(DateTime.Today)) ?? 0;
                if (futureBookings > 0)
                {
                    return (false, $"Không thể xóa người dùng này vì đang có {futureBookings} lịch đặt trong tương lai.");
                }

                // Check if user is a trainer with assigned classes
                if (nguoiDung.LoaiNguoiDung == "HLV")
                {
                    var assignedClasses = nguoiDung.LopHocs?.Count(l => l.TrangThai == "OPEN") ?? 0;
                    if (assignedClasses > 0)
                    {
                        return (false, $"Không thể xóa huấn luyện viên này vì đang phụ trách {assignedClasses} lớp học.");
                    }
                }

                // Check if user has linked account
                if (nguoiDung.TaiKhoan != null)
                {
                    return (false, "Không thể xóa người dùng có tài khoản đăng nhập. Vui lòng vô hiệu hóa tài khoản thay vì xóa.");
                }

                return (true, "Người dùng có thể xóa được.");
            }
            catch (Exception ex)
            {
                return (false, $"Có lỗi xảy ra khi kiểm tra: {ex.Message}");
            }
        }

        public async Task<bool> UpdateAvatarAsync(int userId, string avatarPath)
        {
            try
            {
                var nguoiDung = await _unitOfWork.NguoiDungs.GetByIdAsync(userId);
                if (nguoiDung == null)
                    return false;

                nguoiDung.AnhDaiDien = avatarPath;
                await _unitOfWork.NguoiDungs.UpdateAsync(nguoiDung);
                await _unitOfWork.SaveChangesAsync();
                
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
