using GymManagement.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GymManagement.Web.Data;
using OfficeOpenXml;
using Microsoft.Extensions.Logging;

namespace GymManagement.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class BaoCaoController : Controller
    {
        private readonly IBaoCaoService _baoCaoService;
        private readonly IWalkInService _walkInService;
        private readonly ILogger<BaoCaoController> _logger;
        private readonly GymDbContext _context;

        public BaoCaoController(IBaoCaoService baoCaoService, IWalkInService walkInService, GymDbContext context, ILogger<BaoCaoController> logger)
        {
            _baoCaoService = baoCaoService;
            _walkInService = walkInService;
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var dashboardData = await _baoCaoService.GetDashboardDataAsync();
                return View(dashboardData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting dashboard data");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải dữ liệu dashboard.";
                return View(new { });
            }
        }

        public IActionResult Revenue()
        {
            // ✅ OPTIMIZED: Chỉ trả về View, dữ liệu sẽ được load bằng JavaScript
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetRevenueData(DateTime startDate, DateTime endDate, string groupBy = "day", string source = "all")
        {
            try
            {
                // ✅ INPUT VALIDATION: Validate date parameters
                var validationResult = ValidateDateRange(startDate, endDate);
                if (!validationResult.IsValid)
                {
                    return Json(new { success = false, message = validationResult.ErrorMessage });
                }

                // ✅ VALIDATE: groupBy parameter
                if (!IsValidGroupBy(groupBy))
                {
                    return Json(new { success = false, message = "Tham số nhóm dữ liệu không hợp lệ." });
                }

                // ✅ VALIDATE: source parameter
                if (!IsValidSource(source))
                {
                    return Json(new { success = false, message = "Tham số nguồn dữ liệu không hợp lệ." });
                }

                var data = await _baoCaoService.GetRevenueByDateRangeAsync(startDate, endDate, source);
                return Json(new { success = true, data = data });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting revenue data for range {StartDate} to {EndDate}", startDate, endDate);
                return Json(new { success = false, message = "Có lỗi xảy ra khi tải dữ liệu doanh thu." });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetRevenueGrowthRate(DateTime startDate, DateTime endDate, string source = "all")
        {
            try
            {
                // ✅ INPUT VALIDATION: Validate date parameters
                var validationResult = ValidateDateRange(startDate, endDate);
                if (!validationResult.IsValid)
                {
                    return Json(new { success = false, message = validationResult.ErrorMessage });
                }

                // ✅ VALIDATE: source parameter
                if (!IsValidSource(source))
                {
                    return Json(new { success = false, message = "Tham số nguồn dữ liệu không hợp lệ." });
                }

                var growthRate = await _baoCaoService.GetRevenueGrowthRateAsync(startDate, endDate, source);
                return Json(new { success = true, growthRate = growthRate });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while calculating revenue growth rate for range {StartDate} to {EndDate}", startDate, endDate);
                return Json(new { success = false, message = "Có lỗi xảy ra khi tính tỷ lệ tăng trưởng doanh thu.", growthRate = 0 });
            }
        }

        /// <summary>
        /// ✅ NEW: API để lấy tổng chi phí theo date range
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetTotalExpenses(DateTime startDate, DateTime endDate)
        {
            try
            {
                var validationResult = ValidateDateRange(startDate, endDate);
                if (!validationResult.IsValid)
                {
                    return Json(new { success = false, message = validationResult.ErrorMessage });
                }

                var totalExpenses = await _baoCaoService.GetTotalExpensesByDateRangeAsync(startDate, endDate);
                return Json(new { success = true, totalExpenses = totalExpenses });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting total expenses for range {StartDate} to {EndDate}", startDate, endDate);
                return Json(new { success = false, message = "Có lỗi xảy ra khi tải dữ liệu chi phí.", totalExpenses = 0 });
            }
        }

        /// <summary>
        /// ✅ NEW: API để lấy Net Profit theo date range
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetNetProfit(DateTime startDate, DateTime endDate)
        {
            try
            {
                var validationResult = ValidateDateRange(startDate, endDate);
                if (!validationResult.IsValid)
                {
                    return Json(new { success = false, message = validationResult.ErrorMessage });
                }

                var netProfit = await _baoCaoService.GetNetProfitByDateRangeAsync(startDate, endDate);
                return Json(new { success = true, netProfit = netProfit });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while calculating net profit for range {StartDate} to {EndDate}", startDate, endDate);
                return Json(new { success = false, message = "Có lỗi xảy ra khi tính lợi nhuận ròng.", netProfit = 0 });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetRevenueByPaymentMethod(DateTime startDate, DateTime endDate, string source = "all")
        {
            try
            {
                // ✅ INPUT VALIDATION: Validate date parameters
                var validationResult = ValidateDateRange(startDate, endDate);
                if (!validationResult.IsValid)
                {
                    return Json(new { success = false, message = validationResult.ErrorMessage });
                }

                // ✅ VALIDATE: source parameter
                if (!IsValidSource(source))
                {
                    return Json(new { success = false, message = "Tham số nguồn dữ liệu không hợp lệ." });
                }

                var data = await _baoCaoService.GetRevenueByPaymentMethodAsync(startDate, endDate, source);
                return Json(new { success = true, data = data });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting revenue by payment method for range {StartDate} to {EndDate}", startDate, endDate);
                return Json(new { success = false, message = "Có lỗi xảy ra khi tải dữ liệu doanh thu theo phương thức." });
            }
        }

        public IActionResult Membership()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetMembershipData()
        {
            try
            {
                var totalActive = await _baoCaoService.GetTotalActiveMembersAsync();
                var membersByPackage = await _baoCaoService.GetMembersByPackageAsync();
                var registrationTrend = await _baoCaoService.GetMemberRegistrationTrendAsync(12);

                return Json(new { 
                    success = true, 
                    totalActive = totalActive,
                    membersByPackage = membersByPackage,
                    registrationTrend = registrationTrend
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting membership data");
                return Json(new { success = false, message = "Có lỗi xảy ra khi tải dữ liệu thành viên." });
            }
        }

        public IActionResult Attendance()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetAttendanceData(DateTime startDate, DateTime endDate)
        {
            try
            {
                var attendanceTrend = await _baoCaoService.GetAttendanceTrendAsync(startDate, endDate);
                var averageAttendance = await _baoCaoService.GetAverageAttendanceAsync(startDate, endDate);
                var todayAttendanceByTimeSlot = await _baoCaoService.GetAttendanceByTimeSlotAsync(DateTime.Today);

                return Json(new { 
                    success = true, 
                    attendanceTrend = attendanceTrend,
                    averageAttendance = averageAttendance,
                    todayByTimeSlot = todayAttendanceByTimeSlot
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting attendance data");
                return Json(new { success = false, message = "Có lỗi xảy ra khi tải dữ liệu điểm danh." });
            }
        }

        public IActionResult Classes()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetClassData(DateTime startDate, DateTime endDate)
        {
            try
            {
                var popularClasses = await _baoCaoService.GetPopularClassesAsync(startDate, endDate);
                var occupancyRates = await _baoCaoService.GetClassOccupancyRatesAsync(startDate, endDate);
                var cancellationRates = await _baoCaoService.GetClassCancellationRatesAsync(startDate, endDate);

                return Json(new { 
                    success = true, 
                    popularClasses = popularClasses,
                    occupancyRates = occupancyRates,
                    cancellationRates = cancellationRates
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting class data");
                return Json(new { success = false, message = "Có lỗi xảy ra khi tải dữ liệu lớp học." });
            }
        }

        public IActionResult Trainers()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetTrainerData(DateTime startDate, DateTime endDate)
        {
            try
            {
                var trainerRevenue = await _baoCaoService.GetTrainerRevenueAsync(startDate, endDate);
                var trainerClassCount = await _baoCaoService.GetTrainerClassCountAsync(startDate, endDate);
                var currentMonth = DateTime.Now.ToString("yyyy-MM");
                var trainerCommission = await _baoCaoService.GetTrainerCommissionAsync(currentMonth);

                return Json(new { 
                    success = true, 
                    trainerRevenue = trainerRevenue,
                    trainerClassCount = trainerClassCount,
                    trainerCommission = trainerCommission
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting trainer data");
                return Json(new { success = false, message = "Có lỗi xảy ra khi tải dữ liệu huấn luyện viên." });
            }
        }

        public IActionResult Financial()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetFinancialData(int year, int month)
        {
            try
            {
                var monthlySummary = await _baoCaoService.GetMonthlyFinancialSummaryAsync(year, month);
                var startDate = new DateTime(year, month, 1);
                var endDate = startDate.AddMonths(1).AddDays(-1);
                var netProfit = await _baoCaoService.GetNetProfitAsync(startDate, endDate);

                return Json(new { 
                    success = true, 
                    monthlySummary = monthlySummary,
                    netProfit = netProfit
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting financial data");
                return Json(new { success = false, message = "Có lỗi xảy ra khi tải dữ liệu tài chính." });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetRealtimeStats()
        {
            try
            {
                var stats = await _baoCaoService.GetRealtimeStatsAsync();
                return Json(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting realtime stats");
                return Json(new { currentAttendance = 0, todayRevenue = 0, activeMembers = 0, lastUpdated = DateTime.Now });
            }
        }

        [HttpGet]
        public async Task<IActionResult> ExportReport(string reportType, DateTime startDate, DateTime endDate, string format = "csv")
        {
            try
            {
                _logger.LogInformation("Starting report export: {Type}, {Start} to {End}, Format: {Format}", 
                    reportType, startDate, endDate, format);

                switch (reportType.ToLower())
                {
                    case "revenue":
                        var revenueData = await _baoCaoService.GetRevenueByDateRangeAsync(startDate, endDate);
                        if (format.ToLower() == "excel")
                        {
                            using (var package = new ExcelPackage())
                            {
                                var worksheet = package.Workbook.Worksheets.Add("Doanh Thu");
                                worksheet.Cells[1, 1].Value = "Ngày";
                                worksheet.Cells[1, 2].Value = "Doanh Thu";

                                int row = 2;
                                foreach (var item in revenueData)
                                {
                                    worksheet.Cells[row, 1].Value = item.Key;
                                    worksheet.Cells[row, 2].Value = item.Value;
                                    worksheet.Cells[row, 2].Style.Numberformat.Format = "#,##0";
                                    row++;
                                }

                                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                                var fileBytes = package.GetAsByteArray();
                                string fileName = $"DoanhThu_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}.xlsx";
                                return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                            }
                        }
                        else
                        {
                            var csvBuilder = new System.Text.StringBuilder("Ngày,Doanh thu (VNĐ)\n");
                            foreach (var item in revenueData)
                            {
                                csvBuilder.AppendFormat("{0},{1}\n", item.Key, item.Value.ToString("#,##0"));
                            }
                            var bytes = System.Text.Encoding.UTF8.GetBytes(csvBuilder.ToString());
                            string fileName = $"DoanhThu_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}.csv";
                            return File(bytes, "text/csv", fileName);
                        }

                    case "attendance":
                        var attendanceData = await _baoCaoService.GetAttendanceTrendAsync(startDate, endDate);
                        if (format.ToLower() == "excel")
                        {
                            using (var package = new ExcelPackage())
                            {
                                var worksheet = package.Workbook.Worksheets.Add("Điểm Danh");
                                worksheet.Cells[1, 1].Value = "Ngày";
                                worksheet.Cells[1, 2].Value = "Số lượng điểm danh";

                                int row = 2;
                                foreach (var item in attendanceData)
                                {
                                    worksheet.Cells[row, 1].Value = item.Key;
                                    worksheet.Cells[row, 2].Value = item.Value;
                                    row++;
                                }

                                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                                var fileBytes = package.GetAsByteArray();
                                string fileName = $"DiemDanh_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}.xlsx";
                                return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                            }
                        }
                        else
                        {
                            var csvBuilder = new System.Text.StringBuilder("Ngày,Số lượng điểm danh\n");
                            foreach (var item in attendanceData)
                            {
                                csvBuilder.AppendFormat("{0},{1}\n", item.Key, item.Value);
                            }
                            var bytes = System.Text.Encoding.UTF8.GetBytes(csvBuilder.ToString());
                            string fileName = $"DiemDanh_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}.csv";
                            return File(bytes, "text/csv", fileName);
                        }

                    case "membership":
                        if (format.ToLower() == "excel")
                        {
                            var membershipData = await _baoCaoService.GetMembersByPackageAsync();
                            using (var package = new ExcelPackage())
                            {
                                var worksheet = package.Workbook.Worksheets.Add("Thành Viên");
                                worksheet.Cells[1, 1].Value = "Gói tập";
                                worksheet.Cells[1, 2].Value = "Số thành viên";

                                int row = 2;
                                foreach (var item in membershipData)
                                {
                                    worksheet.Cells[row, 1].Value = item.Key;
                                    worksheet.Cells[row, 2].Value = item.Value;
                                    row++;
                                }

                                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                                var fileBytes = package.GetAsByteArray();
                                string fileName = $"ThanhVien_{DateTime.Now:yyyyMMdd}.xlsx";
                                return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                            }
                        }
                        else
                        {
                            var membershipData = await _baoCaoService.GetMembersByPackageAsync();
                            var csvBuilder = new System.Text.StringBuilder("Gói tập,Số thành viên\n");
                            foreach (var item in membershipData)
                            {
                                csvBuilder.AppendFormat("{0},{1}\n", item.Key, item.Value);
                            }
                            var bytes = System.Text.Encoding.UTF8.GetBytes(csvBuilder.ToString());
                            string fileName = $"ThanhVien_{DateTime.Now:yyyyMMdd}.csv";
                            return File(bytes, "text/csv", fileName);
                        }

                    default:
                        _logger.LogWarning("Unsupported report type requested: {Type}", reportType);
                        return BadRequest("Loại báo cáo không được hỗ trợ.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting report: {Type}, {Start} to {End}", reportType, startDate, endDate);
                TempData["ErrorMessage"] = "Đã xảy ra lỗi khi xuất báo cáo. Vui lòng thử lại sau.";
                return StatusCode(500, "Đã xảy ra lỗi khi xuất báo cáo. Vui lòng thử lại sau.");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetQuickStats()
        {
            try
            {
                var today = DateTime.Today;
                var thisMonth = new DateTime(today.Year, today.Month, 1);

                var todayRevenue = await _baoCaoService.GetDailyRevenueAsync(today);
                var monthlyRevenue = await _baoCaoService.GetMonthlyRevenueAsync(today.Year, today.Month);
                var totalMembers = await _baoCaoService.GetTotalActiveMembersAsync();
                var todayAttendance = await _baoCaoService.GetDailyAttendanceAsync(today);

                return Json(new {
                    success = true,
                    todayRevenue = todayRevenue,
                    monthlyRevenue = monthlyRevenue,
                    totalMembers = totalMembers,
                    todayAttendance = todayAttendance
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting quick stats");
                return Json(new { success = false, message = "Có lỗi xảy ra khi tải thống kê nhanh." });
            }
        }



        public IActionResult CustomReport()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> GenerateCustomReport(string reportName, DateTime startDate, DateTime endDate, string[] metrics)
        {
            try
            {
                var reportData = new Dictionary<string, object>();

                foreach (var metric in metrics)
                {
                    switch (metric.ToLower())
                    {
                        case "revenue":
                            reportData["revenue"] = await _baoCaoService.GetRevenueByDateRangeAsync(startDate, endDate);
                            break;
                        case "attendance":
                            reportData["attendance"] = await _baoCaoService.GetAttendanceTrendAsync(startDate, endDate);
                            break;
                        case "membership":
                            reportData["membership"] = await _baoCaoService.GetMembersByPackageAsync();
                            break;
                        case "classes":
                            reportData["classes"] = await _baoCaoService.GetPopularClassesAsync(startDate, endDate);
                            break;
                    }
                }

                ViewBag.ReportName = reportName;
                ViewBag.StartDate = startDate;
                ViewBag.EndDate = endDate;
                ViewBag.ReportData = reportData;

                return View("CustomReportResult");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while generating custom report");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tạo báo cáo tùy chỉnh.";
                return View();
            }
        }

        #region Walk-In Reports

        /// <summary>
        /// Lấy thống kê khách vãng lai
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetWalkInStats(DateTime startDate, DateTime endDate)
        {
            try
            {
                var stats = await _walkInService.GetWalkInStatsAsync(startDate, endDate);
                return Json(new { success = true, data = stats });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting walk-in stats");
                return Json(new { success = false, message = "Có lỗi xảy ra khi tải thống kê khách vãng lai." });
            }
        }

        /// <summary>
        /// Lấy danh sách khách vãng lai theo ngày
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetWalkInSessions(DateTime? date = null)
        {
            try
            {
                var sessions = await _walkInService.GetTodayWalkInsAsync(date);
                return Json(new { success = true, data = sessions });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting walk-in sessions");
                return Json(new { success = false, message = "Có lỗi xảy ra khi tải danh sách khách vãng lai." });
            }
        }

        #endregion

        #region ✅ INPUT VALIDATION HELPERS

        /// <summary>
        /// Validates date range parameters
        /// </summary>
        private (bool IsValid, string ErrorMessage) ValidateDateRange(DateTime startDate, DateTime endDate)
        {
            // Check if dates are valid
            if (startDate == default || endDate == default)
            {
                return (false, "Ngày bắt đầu và ngày kết thúc không được để trống.");
            }

            // Check if start date is not after end date
            if (startDate > endDate)
            {
                return (false, "Ngày bắt đầu không được lớn hơn ngày kết thúc.");
            }

            // Check if date range is not too far in the future
            if (startDate > DateTime.Today.AddDays(1))
            {
                return (false, "Ngày bắt đầu không được vượt quá ngày mai.");
            }

            // Check if date range is not too far in the past (5 years)
            if (startDate < DateTime.Today.AddYears(-5))
            {
                return (false, "Ngày bắt đầu không được quá 5 năm trước.");
            }

            // Check if date range is not too large (max 2 years)
            if ((endDate - startDate).TotalDays > 730)
            {
                return (false, "Khoảng thời gian không được vượt quá 2 năm.");
            }

            return (true, string.Empty);
        }

        /// <summary>
        /// Validates groupBy parameter
        /// </summary>
        private bool IsValidGroupBy(string groupBy)
        {
            var validGroupBy = new[] { "day", "week", "month", "year" };
            return validGroupBy.Contains(groupBy?.ToLower());
        }

        /// <summary>
        /// Validates source parameter
        /// </summary>
        private bool IsValidSource(string source)
        {
            var validSources = new[] { "all", "member", "walkin" };
            return validSources.Contains(source?.ToLower());
        }

        #endregion
    }
}
