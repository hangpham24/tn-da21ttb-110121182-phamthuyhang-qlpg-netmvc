using GymManagement.Web.Data.Models;
using GymManagement.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GymManagement.Web.Controllers
{
    [Authorize]
    public class BangLuongController : Controller
    {
        private readonly IBangLuongService _bangLuongService;
        private readonly INguoiDungService _nguoiDungService;
        private readonly IAuthService _authService;
        private readonly IPdfExportService _pdfExportService;
        private readonly ILogger<BangLuongController> _logger;
        private readonly IAuditLogService _auditLog;

        public BangLuongController(
            IBangLuongService bangLuongService,
            INguoiDungService nguoiDungService,
            IAuthService authService,
            IPdfExportService pdfExportService,
            ILogger<BangLuongController> logger,
            IAuditLogService auditLog)
        {
            _bangLuongService = bangLuongService;
            _nguoiDungService = nguoiDungService;
            _authService = authService;
            _pdfExportService = pdfExportService;
            _logger = logger;
            _auditLog = auditLog;
        }

        // Helper method to get current user
        private async Task<TaiKhoan?> GetCurrentUserAsync()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return null;

            return await _authService.GetUserByIdAsync(userId);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index()
        {
            try
            {
                var salaries = await _bangLuongService.GetAllAsync();
                return View(salaries);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting salaries");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải danh sách bảng lương.";
                return View(new List<BangLuong>());
            }
        }

        [Authorize(Roles = "Trainer")]
        public async Task<IActionResult> MySalary()
        {
            try
            {
                var user = await GetCurrentUserAsync();
                if (user?.NguoiDungId == null)
                {
                    return RedirectToAction("Login", "Auth");
                }

                var salaries = await _bangLuongService.GetByTrainerIdAsync(user.NguoiDungId.Value);
                return View(salaries);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting trainer salary");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải bảng lương của bạn.";
                return View(new List<BangLuong>());
            }
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var salary = await _bangLuongService.GetByIdAsync(id);
                if (salary == null)
                {
                    return NotFound();
                }
                return View(salary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting salary details for ID: {Id}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải thông tin bảng lương.";
                return RedirectToAction(nameof(Index));
            }
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> MonthlyView(string? month = null)
        {
            try
            {
                month ??= DateTime.Now.ToString("yyyy-MM");
                var salaries = await _bangLuongService.GetByMonthAsync(month);
                var totalExpense = await _bangLuongService.GetTotalSalaryExpenseAsync(month);
                
                ViewBag.Month = month;
                ViewBag.TotalExpense = totalExpense;
                return View(salaries);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting monthly salaries");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải bảng lương tháng.";
                ViewBag.Month = month ?? DateTime.Now.ToString("yyyy-MM");
                ViewBag.TotalExpense = 0;
                return View(new List<BangLuong>());
            }
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UnpaidSalaries()
        {
            try
            {
                var salaries = await _bangLuongService.GetUnpaidSalariesAsync();
                return View(salaries);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting unpaid salaries");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải danh sách lương chưa thanh toán.";
                return View(new List<BangLuong>());
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GenerateMonthlySalaries([FromBody] GenerateSalaryRequest request)
        {
            try
            {
                // Server-side validation
                if (request == null || string.IsNullOrWhiteSpace(request.Month))
                {
                    return Json(new { success = false, message = "Tháng không được để trống." });
                }

                // Validate month format
                if (!System.Text.RegularExpressions.Regex.IsMatch(request.Month, @"^\d{4}-(0[1-9]|1[0-2])$"))
                {
                    return Json(new { success = false, message = "Định dạng tháng không hợp lệ. Sử dụng format YYYY-MM." });
                }

                // Check if month is not too far in the future
                if (DateTime.TryParseExact($"{request.Month}-01", "yyyy-MM-dd", null,
                    System.Globalization.DateTimeStyles.None, out var monthDate))
                {
                    var maxFutureDate = DateTime.Today.AddMonths(2);
                    if (monthDate > maxFutureDate)
                    {
                        return Json(new { success = false, message = "Không thể tạo lương quá xa trong tương lai." });
                    }
                }

                var result = await _bangLuongService.GenerateMonthlySalariesAsync(request.Month);
                if (result)
                {
                    // Audit log
                    var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                    _auditLog.LogMonthlySalaryGenerated(request.Month, 0, userId ?? "Unknown"); // Count would need to be returned from service

                    return Json(new { success = true, message = $"Tạo bảng lương tháng {request.Month} thành công!" });
                }
                else
                {
                    return Json(new { success = false, message = "Không thể tạo bảng lương. Có thể lương tháng này đã tồn tại." });
                }
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid argument for generating monthly salaries");
                return Json(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while generating monthly salaries");
                return Json(new { success = false, message = "Có lỗi xảy ra khi tạo bảng lương." });
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> PaySalary(int salaryId)
        {
            try
            {
                // Server-side validation
                if (salaryId <= 0)
                {
                    return Json(new { success = false, message = "ID bảng lương không hợp lệ." });
                }

                var result = await _bangLuongService.PaySalaryAsync(salaryId);
                if (result)
                {
                    // Audit log
                    var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                    var salary = await _bangLuongService.GetByIdAsync(salaryId);
                    if (salary != null)
                    {
                        _auditLog.LogSalaryPaid(salaryId, salary.Thang, salary.TongThanhToan, userId ?? "Unknown");
                    }

                    return Json(new { success = true, message = "Thanh toán lương thành công!" });
                }
                else
                {
                    return Json(new { success = false, message = "Không thể thanh toán lương. Có thể lương đã được thanh toán hoặc không tồn tại." });
                }
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation for paying salary {SalaryId}", salaryId);
                return Json(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while paying salary");
                return Json(new { success = false, message = "Có lỗi xảy ra khi thanh toán lương." });
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> PayAllSalariesForMonth(string month)
        {
            try
            {
                var result = await _bangLuongService.PayAllSalariesForMonthAsync(month);
                if (result)
                {
                    return Json(new { success = true, message = $"Thanh toán tất cả lương tháng {month} thành công!" });
                }
                else
                {
                    return Json(new { success = false, message = "Không thể thanh toán lương." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while paying all salaries for month");
                return Json(new { success = false, message = "Có lỗi xảy ra khi thanh toán lương." });
            }
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CalculateCommission(int trainerId, string month)
        {
            try
            {
                var commission = await _bangLuongService.CalculateCommissionAsync(trainerId, month);
                return Json(new { 
                    success = true, 
                    commission = commission,
                    formattedCommission = commission.ToString("N0") + " VNĐ"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while calculating commission");
                return Json(new { success = false, message = "Có lỗi xảy ra khi tính hoa hồng." });
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete([FromBody] DeleteSalaryRequest request)
        {
            try
            {
                var result = await _bangLuongService.DeleteAsync(request.Id);
                if (result)
                {
                    return Json(new { success = true, message = "Đã xóa bảng lương thành công!" });
                }
                else
                {
                    return Json(new { success = false, message = "Không thể xóa bảng lương." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting salary");
                return Json(new { success = false, message = $"Có lỗi xảy ra khi xóa bảng lương: {ex.Message}" });
            }
        }

        public async Task<IActionResult> GetSalaryExpense(string month)
        {
            try
            {
                var expense = await _bangLuongService.GetTotalSalaryExpenseAsync(month);
                return Json(new { 
                    success = true, 
                    expense = expense,
                    formattedExpense = expense.ToString("N0") + " VNĐ"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting salary expense");
                return Json(new { success = false, message = "Có lỗi xảy ra khi tính chi phí lương." });
            }
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ExportSalaries(string month, string format = "csv")
        {
            try
            {
                var salaries = await _bangLuongService.GetByMonthAsync(month);
                
                if (format.ToLower() == "csv")
                {
                    var csv = "Huấn luyện viên,Tháng,Lương cơ bản,Hoa hồng,Tổng thanh toán,Ngày thanh toán\n";
                    foreach (var salary in salaries)
                    {
                        csv += $"{salary.Hlv?.Ho} {salary.Hlv?.Ten},{salary.Thang},{salary.LuongCoBan},{salary.TienHoaHong},{salary.TongThanhToan},{salary.NgayThanhToan?.ToString("dd/MM/yyyy") ?? "Chưa thanh toán"}\n";
                    }

                    var bytes = System.Text.Encoding.UTF8.GetBytes(csv);
                    return File(bytes, "text/csv", $"BangLuong_{month}.csv");
                }

                return BadRequest("Định dạng không được hỗ trợ.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while exporting salaries");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi xuất bảng lương.";
                return RedirectToAction(nameof(MonthlyView), new { month });
            }
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetTrainerSalaryHistory(int trainerId)
        {
            try
            {
                var salaries = await _bangLuongService.GetByTrainerIdAsync(trainerId);
                return Json(salaries.Select(s => new {
                    month = s.Thang,
                    baseSalary = s.LuongCoBan,
                    commission = s.TienHoaHong,
                    total = s.TongThanhToan,
                    paymentDate = s.NgayThanhToan?.ToString("dd/MM/yyyy"),
                    isPaid = s.NgayThanhToan != null
                }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting trainer salary history");
                return Json(new List<object>());
            }
        }

        [Authorize(Roles = "Admin")]
        public IActionResult SalarySettings()
        {
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public IActionResult UpdateBaseSalary(decimal newBaseSalary)
        {
            try
            {
                // Input validation
                if (newBaseSalary <= 0)
                {
                    return Json(new { success = false, message = "Lương cơ bản phải lớn hơn 0." });
                }

                if (newBaseSalary > 100000000) // 100M VND max
                {
                    return Json(new { success = false, message = "Lương cơ bản không được vượt quá 100,000,000 VNĐ." });
                }

                // For now, we'll just log the change since there's no configuration table
                // In a real implementation, this would update a configuration table
                _logger.LogInformation("Base salary update requested: {NewBaseSalary} VND by user {UserId}",
                    newBaseSalary, User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);

                // Return success with note about implementation
                return Json(new {
                    success = true,
                    message = $"Yêu cầu cập nhật lương cơ bản thành {newBaseSalary:N0} VNĐ đã được ghi nhận. " +
                             "Lưu ý: Cần implement bảng cấu hình để lưu trữ giá trị này."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating base salary");
                return Json(new { success = false, message = "Có lỗi xảy ra khi cập nhật lương cơ bản." });
            }
        }

        #region PDF Export Endpoints

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ExportSalaryDetailPdf(int salaryId)
        {
            try
            {
                var salary = await _bangLuongService.GetByIdAsync(salaryId);
                if (salary == null)
                {
                    return NotFound("Không tìm thấy bảng lương.");
                }

                var breakdown = await _bangLuongService.CalculateDetailedCommissionAsync(salary.HlvId ?? 0, salary.Thang);
                var pdfBytes = await _pdfExportService.GenerateSalaryReportAsync(salary, breakdown);

                var fileName = $"BangLuong_{salary.Hlv?.Ho}_{salary.Hlv?.Ten}_{salary.Thang}.pdf";
                return File(pdfBytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting salary detail PDF for salary ID: {SalaryId}", salaryId);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi xuất báo cáo PDF.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ExportMonthlyReportPdf(string month)
        {
            try
            {
                if (string.IsNullOrEmpty(month))
                {
                    month = DateTime.Now.ToString("yyyy-MM");
                }

                var salaries = await _bangLuongService.GetByMonthAsync(month);
                var pdfBytes = await _pdfExportService.GenerateMonthlySalaryReportAsync(salaries, month);

                var fileName = $"BaoCaoLuongThang_{month}.pdf";
                return File(pdfBytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting monthly report PDF for month: {Month}", month);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi xuất báo cáo tháng PDF.";
                return RedirectToAction(nameof(MonthlyView), new { month });
            }
        }

        // Removed for simplification: ExportTrainerPerformancePdf

        // Removed for simplification: ExportPayrollSummaryPdf

        // Removed for simplification: ExportComparativePdf

        [HttpGet]
        [Authorize(Roles = "Trainer")]
        public async Task<IActionResult> ExportMySalaryPdf(string? month = null)
        {
            try
            {
                var user = await GetCurrentUserAsync();
                if (user?.NguoiDungId == null)
                {
                    return RedirectToAction("Login", "Auth");
                }

                month ??= DateTime.Now.ToString("yyyy-MM");

                var salary = await _bangLuongService.GetByTrainerAndMonthAsync(user.NguoiDungId.Value, month);
                if (salary == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy bảng lương cho tháng này.";
                    return RedirectToAction(nameof(MySalary));
                }

                var breakdown = await _bangLuongService.CalculateDetailedCommissionAsync(user.NguoiDungId.Value, month);
                var pdfBytes = await _pdfExportService.GenerateSalaryReportAsync(salary, breakdown);

                var fileName = $"BangLuong_{month}.pdf";
                return File(pdfBytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting trainer's own salary PDF");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi xuất bảng lương PDF.";
                return RedirectToAction(nameof(MySalary));
            }
        }

        #endregion

        #region Enhanced Reporting Endpoints

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetDetailedCommissionBreakdown(int trainerId, string month)
        {
            try
            {
                var breakdown = await _bangLuongService.CalculateDetailedCommissionAsync(trainerId, month);
                return Json(new
                {
                    success = true,
                    data = new
                    {
                        packageCommission = breakdown.PackageCommission,
                        classCommission = breakdown.ClassCommission,
                        personalCommission = breakdown.PersonalCommission,
                        performanceBonus = breakdown.PerformanceBonus,
                        attendanceBonus = breakdown.AttendanceBonus,
                        totalCommission = breakdown.TotalCommission,
                        studentCount = breakdown.StudentCount,
                        classesTaught = breakdown.ClassesTaught,
                        personalSessions = breakdown.PersonalSessions,
                        attendanceRate = breakdown.AttendanceRate
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting detailed commission breakdown");
                return Json(new { success = false, message = "Có lỗi xảy ra khi tính hoa hồng chi tiết." });
            }
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetSalaryComparison(int trainerId, string month1, string month2)
        {
            try
            {
                var breakdown1 = await _bangLuongService.CalculateDetailedCommissionAsync(trainerId, month1);
                var breakdown2 = await _bangLuongService.CalculateDetailedCommissionAsync(trainerId, month2);

                var comparison = new
                {
                    month1 = new { month = month1, data = breakdown1 },
                    month2 = new { month = month2, data = breakdown2 },
                    changes = new
                    {
                        totalCommissionChange = breakdown2.TotalCommission - breakdown1.TotalCommission,
                        studentCountChange = breakdown2.StudentCount - breakdown1.StudentCount,
                        classesTaughtChange = breakdown2.ClassesTaught - breakdown1.ClassesTaught,
                        attendanceRateChange = breakdown2.AttendanceRate - breakdown1.AttendanceRate
                    }
                };

                return Json(new { success = true, data = comparison });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting salary comparison");
                return Json(new { success = false, message = "Có lỗi xảy ra khi so sánh lương." });
            }
        }

        #endregion
    }

    public class DeleteSalaryRequest
    {
        public int Id { get; set; }
    }

    public class GenerateSalaryRequest
    {
        public string Month { get; set; }
    }
}
