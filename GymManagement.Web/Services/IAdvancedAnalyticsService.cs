using GymManagement.Web.Models.DTOs;

namespace GymManagement.Web.Services
{
    public interface IAdvancedAnalyticsService
    {
        // Export Functionality
        Task<byte[]> ExportReportToPdfAsync(string reportType, Dictionary<string, object> parameters);
        Task<byte[]> ExportReportToExcelAsync(string reportType, Dictionary<string, object> parameters);
        Task<Stream> GenerateCustomReportPdfAsync(CustomReportDto reportConfig);
        Task<Stream> GenerateCustomReportExcelAsync(CustomReportDto reportConfig);

        // Custom Reports
        Task<CustomReportResultDto> ExecuteCustomReportAsync(CustomReportDto reportConfig);
        Task<List<CustomReportTemplateDto>> GetReportTemplatesAsync(string userRole);
        Task<CustomReportTemplateDto> SaveReportTemplateAsync(CustomReportTemplateDto template);
        Task<bool> DeleteReportTemplateAsync(int templateId);

        // Advanced Charts Data
        Task<AdvancedChartDataDto> GetRevenueAnalyticsAsync(DateTime startDate, DateTime endDate, string groupBy = "day");
        Task<AdvancedChartDataDto> GetMembershipAnalyticsAsync(DateTime startDate, DateTime endDate);
        Task<AdvancedChartDataDto> GetAttendanceAnalyticsAsync(DateTime startDate, DateTime endDate);
        Task<AdvancedChartDataDto> GetClassPopularityAnalyticsAsync(DateTime startDate, DateTime endDate);
        Task<AdvancedChartDataDto> GetTrainerPerformanceAnalyticsAsync(int? trainerId = null);
        Task<AdvancedChartDataDto> GetPackagePerformanceAnalyticsAsync();

        // Dashboard Analytics
        Task<DashboardAnalyticsDto> GetDashboardAnalyticsAsync(string userRole, int? userId = null);
        Task<List<KpiMetricDto>> GetKpiMetricsAsync(string userRole, int? userId = null);

        // Comparison Reports
        Task<ComparisonReportDto> GetPeriodComparisonAsync(DateTime period1Start, DateTime period1End, 
            DateTime period2Start, DateTime period2End, string metricType);
        
        // Forecast Analytics
        Task<ForecastDataDto> GetRevenueForecastAsync(int forecastDays = 30);
        Task<ForecastDataDto> GetMembershipGrowthForecastAsync(int forecastDays = 30);
    }
} 