using GymManagement.Web.Data.Models;

namespace GymManagement.Web.Services
{
    public interface IDangKyService
    {
        Task<IEnumerable<DangKy>> GetAllAsync();
        Task<DangKy?> GetByIdAsync(int id);
        Task<DangKy> CreateAsync(DangKy dangKy);
        Task<DangKy> UpdateAsync(DangKy dangKy);
        Task<bool> DeleteAsync(int id);
        Task<IEnumerable<DangKy>> GetByMemberIdAsync(int memberId);
        Task<IEnumerable<DangKy>> GetRegistrationsByMemberIdAsync(int memberId);
        Task<IEnumerable<DangKy>> GetActiveRegistrationsAsync();
        Task<IEnumerable<DangKy>> GetExpiredRegistrationsAsync();
        Task<bool> RegisterPackageAsync(int nguoiDungId, int goiTapId, int thoiHanThang);
        Task<bool> RegisterPackageAsync(int nguoiDungId, int goiTapId, int thoiHanThang, int? khuyenMaiId);
        Task<bool> RegisterClassAsync(int nguoiDungId, int lopHocId, DateTime ngayBatDau, DateTime ngayKetThuc);
        Task<bool> RegisterFixedClassAsync(int nguoiDungId, int lopHocId);
        Task<bool> ExtendRegistrationAsync(int dangKyId, int additionalMonths);
        Task<bool> CancelRegistrationAsync(int dangKyId, string reason);
        Task<bool> CancelRegistrationAsync(int dangKyId, int nguoiDungId, string? lyDoHuy);
        Task<decimal> CalculateRegistrationFeeAsync(int goiTapId, int thoiHanThang, int? khuyenMaiId = null);
        Task<bool> HasActivePackageRegistrationAsync(int nguoiDungId);
        Task<bool> HasActiveClassRegistrationAsync(int nguoiDungId, int lopHocId);
        Task<decimal> CalculatePackageFeeAsync(int goiTapId, int thoiHanThang, int? khuyenMaiId = null);
        Task<decimal> CalculateClassFeeAsync(int lopHocId);
        Task<bool> CreatePackageRegistrationAfterPaymentAsync(int nguoiDungId, int goiTapId, int thoiHanThang, int thanhToanId);
        Task<bool> CreateClassRegistrationAfterPaymentAsync(int nguoiDungId, int lopHocId, DateTime ngayBatDau, DateTime ngayKetThuc, int thanhToanId);
        Task<bool> CreateFixedClassRegistrationAfterPaymentAsync(int nguoiDungId, int lopHocId, int thanhToanId);
        Task<int> GetRegistrationCountByUserIdAsync(int nguoiDungId);

        // Add missing methods to fix compilation errors
        Task<IEnumerable<DangKy>> GetActiveRegistrationsByMemberIdAsync(int nguoiDungId);
        Task<int> GetActiveClassRegistrationCountAsync(int lopHocId);
        Task<bool> CanRegisterClassAsync(int nguoiDungId, int lopHocId);
        Task<bool> HasActivePackageAsync(int nguoiDungId);
        Task<bool> CanRegisterClassWithTimeAsync(int nguoiDungId, int lopHocId, DateOnly newStartDate, DateOnly newEndDate);
        Task<(bool CanRegister, string ErrorMessage)> ValidateClassRegistrationAsync(int nguoiDungId, int lopHocId, DateOnly newStartDate, DateOnly newEndDate);

        // Renewal methods
        Task<decimal> CalculateRenewalFeeAsync(int dangKyId, int renewalMonths);
        Task<bool> CanRenewRegistrationAsync(int dangKyId);
        Task<bool> ProcessRenewalPaymentAsync(int dangKyId, int thanhToanId, int renewalMonths);

        // Pagination method
        Task<(IEnumerable<DangKy> registrations, int totalCount)> GetPagedAsync(int page, int pageSize, string searchTerm = "", string status = "", string type = "");
    }
}
