using GymManagement.Web.Data.Models;

namespace GymManagement.Web.Data.Repositories
{
    public interface IThanhToanRepository : IRepository<ThanhToan>
    {
        Task<IEnumerable<ThanhToan>> GetByDangKyIdAsync(int dangKyId);
        Task<IEnumerable<ThanhToan>> GetPaymentsByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<IEnumerable<ThanhToan>> GetPendingPaymentsAsync();
        Task<IEnumerable<ThanhToan>> GetSuccessfulPaymentsAsync();
        Task<decimal> GetTotalRevenueByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<decimal> GetTotalRevenueByMonthAsync(int year, int month);
        Task<IEnumerable<ThanhToan>> GetPaymentsByMethodAsync(string phuongThuc);
        Task<ThanhToan?> GetPaymentWithGatewayAsync(int thanhToanId);
    }
}
