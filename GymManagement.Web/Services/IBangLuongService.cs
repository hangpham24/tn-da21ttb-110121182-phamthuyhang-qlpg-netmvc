using GymManagement.Web.Data.Models;
using static GymManagement.Web.Services.BangLuongService;

namespace GymManagement.Web.Services
{
    public interface IBangLuongService
    {
        Task<IEnumerable<BangLuong>> GetAllAsync();
        Task<BangLuong?> GetByIdAsync(int id);
        Task<BangLuong> CreateAsync(BangLuong bangLuong);
        Task<BangLuong> UpdateAsync(BangLuong bangLuong);
        Task<bool> DeleteAsync(int id);
        Task<IEnumerable<BangLuong>> GetByTrainerIdAsync(int hlvId);
        Task<IEnumerable<BangLuong>> GetByMonthAsync(string thang);
        Task<BangLuong?> GetByTrainerAndMonthAsync(int hlvId, string thang);
        Task<IEnumerable<BangLuong>> GetUnpaidSalariesAsync();
        Task<bool> GenerateMonthlySalariesAsync(string thang);
        Task<bool> PaySalaryAsync(int bangLuongId);
        Task<bool> PayAllSalariesForMonthAsync(string thang);
        Task<decimal> CalculateCommissionAsync(int hlvId, string thang);
        Task<CommissionBreakdown> CalculateDetailedCommissionAsync(int hlvId, string thang);
        Task<decimal> GetTotalSalaryExpenseAsync(string thang);
    }
}
