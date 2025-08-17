using GymManagement.Web.Data.Models;

namespace GymManagement.Web.Data.Repositories
{
    public interface IBangLuongRepository : IRepository<BangLuong>
    {
        Task<IEnumerable<BangLuong>> GetByHlvIdAsync(int hlvId);
        Task<IEnumerable<BangLuong>> GetByMonthAsync(string thang);
        Task<BangLuong?> GetByHlvAndMonthAsync(int hlvId, string thang);
        Task<IEnumerable<BangLuong>> GetUnpaidSalariesAsync();
        Task<IEnumerable<BangLuong>> GetPaidSalariesAsync();
        Task<decimal> GetTotalSalaryByMonthAsync(string thang);
        Task<decimal> GetTotalCommissionByMonthAsync(string thang);
        Task<IEnumerable<BangLuong>> GetSalariesByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<int> GetSalaryCountByMonthAsync(string thang);
    }
}
