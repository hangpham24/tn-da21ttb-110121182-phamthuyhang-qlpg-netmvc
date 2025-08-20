using GymManagement.Web.Data.Models;

namespace GymManagement.Web.Services
{
    public interface IBaoCaoService
    {
        // Revenue Reports
        Task<decimal> GetDailyRevenueAsync(DateTime date);
        Task<decimal> GetMonthlyRevenueAsync(int year, int month);
        Task<decimal> GetYearlyRevenueAsync(int year);
        Task<Dictionary<string, decimal>> GetRevenueByDateRangeAsync(DateTime startDate, DateTime endDate, string source = "all");
        Task<Dictionary<string, decimal>> GetRevenueByPaymentMethodAsync(DateTime startDate, DateTime endDate, string source = "all");
        Task<decimal> GetRevenueGrowthRateAsync(DateTime currentStartDate, DateTime currentEndDate, string source = "all");

        // Membership Reports
        Task<int> GetTotalActiveMembersAsync();
        Task<int> GetNewMembersCountAsync(DateTime startDate, DateTime endDate);
        Task<int> GetExpiredMembersCountAsync(DateTime startDate, DateTime endDate);
        Task<Dictionary<string, int>> GetMembersByPackageAsync();
        Task<Dictionary<string, int>> GetMemberRegistrationTrendAsync(int months);

        // Attendance Reports
        Task<int> GetDailyAttendanceAsync(DateTime date);
        Task<Dictionary<string, int>> GetAttendanceTrendAsync(DateTime startDate, DateTime endDate);
        Task<Dictionary<string, int>> GetAttendanceByTimeSlotAsync(DateTime date);
        Task<double> GetAverageAttendanceAsync(DateTime startDate, DateTime endDate);

        // Class Reports
        Task<Dictionary<string, int>> GetPopularClassesAsync(DateTime startDate, DateTime endDate);
        Task<Dictionary<string, double>> GetClassOccupancyRatesAsync(DateTime startDate, DateTime endDate);
        Task<Dictionary<string, int>> GetClassCancellationRatesAsync(DateTime startDate, DateTime endDate);

        // Trainer Reports
        Task<Dictionary<string, decimal>> GetTrainerRevenueAsync(DateTime startDate, DateTime endDate);
        Task<Dictionary<string, int>> GetTrainerClassCountAsync(DateTime startDate, DateTime endDate);
        Task<Dictionary<string, decimal>> GetTrainerCommissionAsync(string thang);

        // Financial Reports
        Task<Dictionary<string, decimal>> GetMonthlyFinancialSummaryAsync(int year, int month);
        Task<decimal> GetTotalExpensesAsync(DateTime startDate, DateTime endDate);
        Task<decimal> GetNetProfitAsync(DateTime startDate, DateTime endDate);

        // ✅ NEW: Methods for Revenue Report
        Task<decimal> GetTotalExpensesByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<decimal> GetNetProfitByDateRangeAsync(DateTime startDate, DateTime endDate);

        // Dashboard Data
        Task<object> GetDashboardDataAsync();
        Task<object> GetRealtimeStatsAsync();

        // Debug methods
        Task<IEnumerable<ThanhToan>> GetAllPaymentsForDebugAsync();
        Task<IEnumerable<DangKy>> GetAllRegistrationsForDebugAsync();
    }
}
