using GymManagement.Web.Data.Models;
using static GymManagement.Web.Services.BangLuongService;

namespace GymManagement.Web.Services
{
    public interface IPdfExportService
    {
        Task<byte[]> GenerateSalaryReportAsync(BangLuong bangLuong, CommissionBreakdown breakdown);
        Task<byte[]> GenerateMonthlySalaryReportAsync(IEnumerable<BangLuong> salaries, string month);
    }
}