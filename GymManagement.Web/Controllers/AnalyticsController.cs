using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using GymManagement.Web.Services;
using GymManagement.Web.Models.DTOs;
using System.Security.Claims;

namespace GymManagement.Web.Controllers
{
    [Authorize]
    public class AnalyticsController : BaseController
    {
        private readonly IAdvancedAnalyticsService _analyticsService;

        public AnalyticsController(
            IAdvancedAnalyticsService analyticsService,
            IUserSessionService userSessionService,
            ILogger<AnalyticsController> logger) : base(userSessionService, logger)
        {
            _analyticsService = analyticsService;
        }

        #region Admin Analytics

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Dashboard()
        {
            try
            {
                LogUserAction("AccessAnalyticsDashboard");

                var dashboardData = await _analyticsService.GetDashboardAnalyticsAsync("Admin");
                return View(dashboardData);
            }
            catch (Exception ex)
            {
                return HandleError(ex, "Có lỗi xảy ra khi tải dashboard analytics.");
            }
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Reports()
        {
            try
            {
                var templates = await _analyticsService.GetReportTemplatesAsync("Admin");
                return View(templates);
            }
            catch (Exception ex)
            {
                return HandleError(ex, "Có lỗi xảy ra khi tải danh sách báo cáo.");
            }
        }

        [Authorize(Roles = "Admin")]
        public IActionResult CustomReport()
        {
            return View(new CustomReportDto());
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExecuteCustomReport([FromBody] CustomReportDto reportConfig)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return Json(new { success = false, message = "Cấu hình báo cáo không hợp lệ." });
                }

                var result = await _analyticsService.ExecuteCustomReportAsync(reportConfig);
                return Json(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing custom report");
                return Json(new { success = false, message = "Có lỗi xảy ra khi thực thi báo cáo." });
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExportReport([FromBody] ExportRequestDto request)
        {
            try
            {
                byte[] fileContent;
                string fileName;
                string mimeType;

                if (request.CustomReport != null)
                {
                    // Custom report export
                    Stream stream;
                    if (request.Format.ToLower() == "pdf")
                    {
                        stream = await _analyticsService.GenerateCustomReportPdfAsync(request.CustomReport);
                        mimeType = "application/pdf";
                        fileName = $"{request.CustomReport.ReportName}_{DateTime.Now:yyyyMMdd}.pdf";
                    }
                    else
                    {
                        stream = await _analyticsService.GenerateCustomReportExcelAsync(request.CustomReport);
                        mimeType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                        fileName = $"{request.CustomReport.ReportName}_{DateTime.Now:yyyyMMdd}.xlsx";
                    }

                    using (stream)
                    {
                        fileContent = new byte[stream.Length];
                        await stream.ReadAsync(fileContent, 0, fileContent.Length);
                    }
                }
                else
                {
                    // Standard report export
                    if (request.Format.ToLower() == "pdf")
                    {
                        fileContent = await _analyticsService.ExportReportToPdfAsync(request.ReportType, request.Parameters);
                        mimeType = "application/pdf";
                        fileName = $"{request.ReportType}_{DateTime.Now:yyyyMMdd}.pdf";
                    }
                    else
                    {
                        fileContent = await _analyticsService.ExportReportToExcelAsync(request.ReportType, request.Parameters);
                        mimeType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                        fileName = $"{request.ReportType}_{DateTime.Now:yyyyMMdd}.xlsx";
                    }
                }

                LogUserAction($"ExportReport_{request.ReportType}_{request.Format}");

                return File(fileContent, mimeType, fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting report: {ReportType}", request.ReportType);
                return Json(new { success = false, message = "Có lỗi xảy ra khi xuất báo cáo." });
            }
        }

        #endregion

        #region Trainer Analytics

        [Authorize(Roles = "Trainer")]
        public async Task<IActionResult> TrainerDashboard()
        {
            try
            {
                var currentUser = await GetCurrentUserSafeAsync();
                if (currentUser?.NguoiDungId == null)
                {
                    return HandleUserNotFound("TrainerDashboard");
                }

                var dashboardData = await _analyticsService.GetDashboardAnalyticsAsync("Trainer", currentUser.NguoiDungId);
                return View(dashboardData);
            }
            catch (Exception ex)
            {
                return HandleError(ex, "Có lỗi xảy ra khi tải dashboard trainer.");
            }
        }

        [Authorize(Roles = "Trainer")]
        public async Task<IActionResult> TrainerReports()
        {
            try
            {
                var templates = await _analyticsService.GetReportTemplatesAsync("Trainer");
                return View(templates);
            }
            catch (Exception ex)
            {
                return HandleError(ex, "Có lỗi xảy ra khi tải danh sách báo cáo trainer.");
            }
        }

        [HttpPost]
        [Authorize(Roles = "Trainer")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExportTrainerReport([FromBody] ExportRequestDto request)
        {
            try
            {
                var currentUser = await GetCurrentUserSafeAsync();
                if (currentUser?.NguoiDungId == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy thông tin trainer." });
                }

                // Add trainer filter to parameters
                request.Parameters["trainerId"] = currentUser.NguoiDungId.Value;

                byte[] fileContent;
                string fileName;
                string mimeType;

                if (request.Format.ToLower() == "pdf")
                {
                    fileContent = await _analyticsService.ExportReportToPdfAsync(request.ReportType, request.Parameters);
                    mimeType = "application/pdf";
                    fileName = $"Trainer_{request.ReportType}_{DateTime.Now:yyyyMMdd}.pdf";
                }
                else
                {
                    fileContent = await _analyticsService.ExportReportToExcelAsync(request.ReportType, request.Parameters);
                    mimeType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                    fileName = $"Trainer_{request.ReportType}_{DateTime.Now:yyyyMMdd}.xlsx";
                }

                LogUserAction($"ExportTrainerReport_{request.ReportType}_{request.Format}");

                return File(fileContent, mimeType, fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting trainer report: {ReportType}", request.ReportType);
                return Json(new { success = false, message = "Có lỗi xảy ra khi xuất báo cáo." });
            }
        }

        #endregion

        #region API Endpoints

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetRevenueAnalytics(DateTime? startDate, DateTime? endDate, string groupBy = "day")
        {
            try
            {
                var start = startDate ?? DateTime.Now.AddDays(-30);
                var end = endDate ?? DateTime.Now;

                var data = await _analyticsService.GetRevenueAnalyticsAsync(start, end, groupBy);
                return Json(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting revenue analytics");
                return Json(new { error = "Có lỗi xảy ra khi tải dữ liệu doanh thu." });
            }
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetMembershipAnalytics(DateTime? startDate, DateTime? endDate)
        {
            try
            {
                var start = startDate ?? DateTime.Now.AddDays(-30);
                var end = endDate ?? DateTime.Now;

                var data = await _analyticsService.GetMembershipAnalyticsAsync(start, end);
                return Json(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting membership analytics");
                return Json(new { error = "Có lỗi xảy ra khi tải dữ liệu thành viên." });
            }
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Trainer")]
        public async Task<IActionResult> GetAttendanceAnalytics(DateTime? startDate, DateTime? endDate)
        {
            try
            {
                var start = startDate ?? DateTime.Now.AddDays(-7);
                var end = endDate ?? DateTime.Now;

                var data = await _analyticsService.GetAttendanceAnalyticsAsync(start, end);
                return Json(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting attendance analytics");
                return Json(new { error = "Có lỗi xảy ra khi tải dữ liệu điểm danh." });
            }
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetClassPopularityAnalytics(DateTime? startDate, DateTime? endDate)
        {
            try
            {
                var start = startDate ?? DateTime.Now.AddDays(-30);
                var end = endDate ?? DateTime.Now;

                var data = await _analyticsService.GetClassPopularityAnalyticsAsync(start, end);
                return Json(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting class popularity analytics");
                return Json(new { error = "Có lỗi xảy ra khi tải dữ liệu lớp học." });
            }
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Trainer")]
        public async Task<IActionResult> GetTrainerPerformanceAnalytics(int? trainerId = null)
        {
            try
            {
                // If trainer role, use their own ID
                if (User.IsInRole("Trainer") && !User.IsInRole("Admin"))
                {
                    var currentUser = await GetCurrentUserSafeAsync();
                    trainerId = currentUser?.NguoiDungId;
                }

                var data = await _analyticsService.GetTrainerPerformanceAnalyticsAsync(trainerId);
                return Json(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting trainer performance analytics");
                return Json(new { error = "Có lỗi xảy ra khi tải dữ liệu hiệu suất." });
            }
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetPackagePerformanceAnalytics()
        {
            try
            {
                var data = await _analyticsService.GetPackagePerformanceAnalyticsAsync();
                return Json(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting package performance analytics");
                return Json(new { error = "Có lỗi xảy ra khi tải dữ liệu gói tập." });
            }
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetPeriodComparison(DateTime period1Start, DateTime period1End, DateTime period2Start, DateTime period2End, string metricType)
        {
            try
            {
                var data = await _analyticsService.GetPeriodComparisonAsync(period1Start, period1End, period2Start, period2End, metricType);
                return Json(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting period comparison");
                return Json(new { error = "Có lỗi xảy ra khi so sánh dữ liệu." });
            }
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetRevenueForecast(int forecastDays = 30)
        {
            try
            {
                var data = await _analyticsService.GetRevenueForecastAsync(forecastDays);
                return Json(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting revenue forecast");
                return Json(new { error = "Có lỗi xảy ra khi dự báo doanh thu." });
            }
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetMembershipGrowthForecast(int forecastDays = 30)
        {
            try
            {
                var data = await _analyticsService.GetMembershipGrowthForecastAsync(forecastDays);
                return Json(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting membership growth forecast");
                return Json(new { error = "Có lỗi xảy ra khi dự báo tăng trưởng." });
            }
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Trainer")]
        public async Task<IActionResult> GetKpiMetrics()
        {
            try
            {
                var userRole = User.IsInRole("Admin") ? "Admin" : "Trainer";
                int? userId = null;

                if (userRole == "Trainer")
                {
                    var currentUser = await GetCurrentUserSafeAsync();
                    userId = currentUser?.NguoiDungId;
                }

                var metrics = await _analyticsService.GetKpiMetricsAsync(userRole, userId);
                return Json(metrics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting KPI metrics");
                return Json(new { error = "Có lỗi xảy ra khi tải chỉ số KPI." });
            }
        }

        #endregion

        #region Template Management

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveReportTemplate([FromBody] CustomReportTemplateDto template)
        {
            try
            {
                var currentUser = await GetCurrentUserSafeAsync();
                template.CreatedBy = currentUser?.TenDangNhap ?? "Unknown";
                template.UserRole = "Admin";

                var savedTemplate = await _analyticsService.SaveReportTemplateAsync(template);
                return Json(new { success = true, data = savedTemplate });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving report template");
                return Json(new { success = false, message = "Có lỗi xảy ra khi lưu template." });
            }
        }

        [HttpDelete]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteReportTemplate(int templateId)
        {
            try
            {
                var result = await _analyticsService.DeleteReportTemplateAsync(templateId);
                return Json(new { success = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting report template");
                return Json(new { success = false, message = "Có lỗi xảy ra khi xóa template." });
            }
        }

        #endregion
    }
} 