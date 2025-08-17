using GymManagement.Web.Data.Models;

namespace GymManagement.Web.Services
{
    public interface IThanhToanService
    {
        Task<IEnumerable<ThanhToan>> GetAllAsync();
        Task<ThanhToan?> GetByIdAsync(int id);
        Task<ThanhToan> CreateAsync(ThanhToan thanhToan);
        Task<ThanhToan> UpdateAsync(ThanhToan thanhToan);
        Task<bool> DeleteAsync(int id);
        Task<IEnumerable<ThanhToan>> GetByRegistrationIdAsync(int dangKyId);
        Task<IEnumerable<ThanhToan>> GetByMemberIdAsync(int memberId);
        Task<IEnumerable<ThanhToan>> GetPendingPaymentsAsync();
        Task<IEnumerable<ThanhToan>> GetSuccessfulPaymentsAsync();
        Task<ThanhToan> CreatePaymentAsync(int dangKyId, decimal soTien, string phuongThuc, string? ghiChu = null);
        Task<bool> ProcessCashPaymentAsync(int thanhToanId);
        Task<string> CreateVnPayUrlAsync(int thanhToanId, string returnUrl);
        Task<bool> RefundPaymentAsync(int thanhToanId, string reason);
        Task<decimal> GetTotalRevenueAsync(DateTime startDate, DateTime endDate);

        // New methods for payment-first registration
        Task<ThanhToan> CreatePaymentForPackageRegistrationAsync(int nguoiDungId, int goiTapId, int thoiHanThang, string phuongThuc, int? khuyenMaiId = null);
        Task<ThanhToan> CreatePaymentForClassRegistrationAsync(int nguoiDungId, int lopHocId, DateTime ngayBatDau, DateTime ngayKetThuc, string phuongThuc);
        Task<ThanhToan> CreatePaymentForFixedClassRegistrationAsync(int nguoiDungId, int lopHocId, string phuongThuc);
        Task<(string registrationType, Dictionary<string, string> registrationInfo)?> GetRegistrationInfoFromPaymentAsync(int thanhToanId);
        Task<ThanhToanGateway?> GetGatewayByOrderIdAsync(string orderId);
        Task<ThanhToan?> GetPaymentWithRegistrationAsync(int thanhToanId);

        // Renewal payment method
        Task<ThanhToan> CreatePaymentForRenewalAsync(int dangKyId, int renewalMonths, string phuongThuc);
    }
}
