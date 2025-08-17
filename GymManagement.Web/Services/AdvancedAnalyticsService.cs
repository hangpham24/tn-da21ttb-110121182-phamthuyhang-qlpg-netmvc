using GymManagement.Web.Data;
using GymManagement.Web.Models.DTOs;
using Microsoft.EntityFrameworkCore;
using iTextSharp.text;
using iTextSharp.text.pdf;
using OfficeOpenXml;
using OfficeOpenXml.Drawing.Chart;
using System.Text.Json;

namespace GymManagement.Web.Services
{
    public class AdvancedAnalyticsService : IAdvancedAnalyticsService
    {
        private readonly GymDbContext _context;
        private readonly ILogger<AdvancedAnalyticsService> _logger;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public AdvancedAnalyticsService(
            GymDbContext context,
            ILogger<AdvancedAnalyticsService> logger,
            IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _logger = logger;
            _webHostEnvironment = webHostEnvironment;
            
            // Set EPPlus license context
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }

        #region Export Functionality

        public async Task<byte[]> ExportReportToPdfAsync(string reportType, Dictionary<string, object> parameters)
        {
            try
            {
                using var stream = new MemoryStream();
                var document = new Document(PageSize.A4, 25, 25, 30, 30);
                var writer = PdfWriter.GetInstance(document, stream);

                document.Open();

                // Header
                var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 18, BaseColor.DARK_GRAY);
                var headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12, BaseColor.BLACK);
                var normalFont = FontFactory.GetFont(FontFactory.HELVETICA, 10, BaseColor.BLACK);

                // Title
                var title = new Paragraph(GetReportTitle(reportType), titleFont)
                {
                    Alignment = Element.ALIGN_CENTER,
                    SpacingAfter = 20
                };
                document.Add(title);

                // Generated date
                var dateInfo = new Paragraph($"Ngày tạo: {DateTime.Now:dd/MM/yyyy HH:mm}", normalFont)
                {
                    Alignment = Element.ALIGN_RIGHT,
                    SpacingAfter = 20
                };
                document.Add(dateInfo);

                // Report content based on type
                await AddReportContentToPdf(document, reportType, parameters, headerFont, normalFont);

                document.Close();
                return stream.ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating PDF report for type: {ReportType}", reportType);
                throw;
            }
        }

        public async Task<byte[]> ExportReportToExcelAsync(string reportType, Dictionary<string, object> parameters)
        {
            try
            {
                using var package = new ExcelPackage();
                
                // Create main worksheet
                var worksheet = package.Workbook.Worksheets.Add(GetReportTitle(reportType));
                
                // Add header
                worksheet.Cells[1, 1].Value = GetReportTitle(reportType);
                worksheet.Cells[1, 1].Style.Font.Size = 16;
                worksheet.Cells[1, 1].Style.Font.Bold = true;
                
                worksheet.Cells[2, 1].Value = $"Ngày tạo: {DateTime.Now:dd/MM/yyyy HH:mm}";
                
                // Add report content
                var currentRow = await AddReportContentToExcel(worksheet, reportType, parameters, 4);
                
                // Auto-fit columns
                worksheet.Cells.AutoFitColumns();
                
                return package.GetAsByteArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating Excel report for type: {ReportType}", reportType);
                throw;
            }
        }

        public async Task<Stream> GenerateCustomReportPdfAsync(CustomReportDto reportConfig)
        {
            var result = await ExecuteCustomReportAsync(reportConfig);
            
            using var stream = new MemoryStream();
            var document = new Document(PageSize.A4, 25, 25, 30, 30);
            var writer = PdfWriter.GetInstance(document, stream);

            document.Open();

            var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16, BaseColor.DARK_GRAY);
            var headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12, BaseColor.BLACK);
            var normalFont = FontFactory.GetFont(FontFactory.HELVETICA, 10, BaseColor.BLACK);

            // Title
            var title = new Paragraph(result.ReportName, titleFont)
            {
                Alignment = Element.ALIGN_CENTER,
                SpacingAfter = 20
            };
            document.Add(title);

            // Summary if included
            if (reportConfig.IncludeSummary && result.Summary.Any())
            {
                document.Add(new Paragraph("TỔNG KẾT", headerFont) { SpacingAfter = 10 });
                
                foreach (var summaryItem in result.Summary)
                {
                    document.Add(new Paragraph($"{summaryItem.Key}: {summaryItem.Value}", normalFont));
                }
                
                document.Add(new Paragraph(" ", normalFont) { SpacingAfter = 10 });
            }

            // Data table
            if (result.Data.Any())
            {
                document.Add(new Paragraph("DỮ LIỆU CHI TIẾT", headerFont) { SpacingAfter = 10 });
                
                var table = CreatePdfTable(result.Data);
                document.Add(table);
            }

            document.Close();
            return new MemoryStream(stream.ToArray());
        }

        public async Task<Stream> GenerateCustomReportExcelAsync(CustomReportDto reportConfig)
        {
            var result = await ExecuteCustomReportAsync(reportConfig);
            
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add(result.ReportName);
            
            var currentRow = 1;
            
            // Title
            worksheet.Cells[currentRow, 1].Value = result.ReportName;
            worksheet.Cells[currentRow, 1].Style.Font.Size = 16;
            worksheet.Cells[currentRow, 1].Style.Font.Bold = true;
            currentRow += 2;
            
            // Summary
            if (reportConfig.IncludeSummary && result.Summary.Any())
            {
                worksheet.Cells[currentRow, 1].Value = "TỔNG KẾT";
                worksheet.Cells[currentRow, 1].Style.Font.Bold = true;
                currentRow++;
                
                foreach (var summaryItem in result.Summary)
                {
                    worksheet.Cells[currentRow, 1].Value = summaryItem.Key;
                    worksheet.Cells[currentRow, 2].Value = summaryItem.Value?.ToString();
                    currentRow++;
                }
                currentRow++;
            }
            
            // Data
            if (result.Data.Any())
            {
                worksheet.Cells[currentRow, 1].Value = "DỮ LIỆU CHI TIẾT";
                worksheet.Cells[currentRow, 1].Style.Font.Bold = true;
                currentRow++;
                
                currentRow = AddDataToExcelWorksheet(worksheet, result.Data, currentRow);
            }

            // Charts if included
            if (reportConfig.IncludeCharts && result.ChartData != null)
            {
                AddChartToExcel(worksheet, result.ChartData, currentRow + 2);
            }
            
            worksheet.Cells.AutoFitColumns();
            
            return new MemoryStream(package.GetAsByteArray());
        }

        #endregion

        #region Custom Reports

        public async Task<CustomReportResultDto> ExecuteCustomReportAsync(CustomReportDto reportConfig)
        {
            try
            {
                var query = BuildDynamicQuery(reportConfig);
                var data = await query.ToListAsync();
                
                var result = new CustomReportResultDto
                {
                    ReportName = reportConfig.ReportName,
                    GeneratedAt = DateTime.Now,
                    TotalRecords = data.Count,
                    Data = data,
                    Summary = await CalculateSummary(data, reportConfig)
                };

                // Generate chart data if configured
                if (!string.IsNullOrEmpty(reportConfig.ChartType) && !string.IsNullOrEmpty(reportConfig.XAxisField))
                {
                    result.ChartData = GenerateChartDataFromResults(data, reportConfig);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing custom report: {ReportName}", reportConfig.ReportName);
                throw;
            }
        }

        public async Task<List<CustomReportTemplateDto>> GetReportTemplatesAsync(string userRole)
        {
            // For now, return predefined templates
            // In the future, this could be stored in database
            return GetPredefinedTemplates(userRole);
        }

        public async Task<CustomReportTemplateDto> SaveReportTemplateAsync(CustomReportTemplateDto template)
        {
            // TODO: Implement database storage for custom templates
            template.TemplateId = new Random().Next(1000, 9999);
            template.CreatedDate = DateTime.Now;
            return template;
        }

        public async Task<bool> DeleteReportTemplateAsync(int templateId)
        {
            // TODO: Implement database deletion
            return true;
        }

        #endregion

        #region Advanced Charts Data

        public async Task<AdvancedChartDataDto> GetRevenueAnalyticsAsync(DateTime startDate, DateTime endDate, string groupBy = "day")
        {
            var payments = await _context.ThanhToans
                .Where(t => t.NgayThanhToan >= startDate && t.NgayThanhToan <= endDate && t.TrangThai == "SUCCESS")
                .ToListAsync();

            var labels = new List<string>();
            var data = new List<decimal>();

            switch (groupBy.ToLower())
            {
                case "day":
                    var dailyRevenue = payments
                        .GroupBy(p => p.NgayThanhToan.Date)
                        .Select(g => new { Date = g.Key, Total = g.Sum(p => p.SoTien) })
                        .OrderBy(x => x.Date)
                        .ToList();

                    labels.AddRange(dailyRevenue.Select(x => x.Date.ToString("dd/MM")));
                    data.AddRange(dailyRevenue.Select(x => x.Total));
                    break;

                case "month":
                    var monthlyRevenue = payments
                        .GroupBy(p => new { p.NgayThanhToan.Year, p.NgayThanhToan.Month })
                        .Select(g => new { YearMonth = g.Key, Total = g.Sum(p => p.SoTien) })
                        .OrderBy(x => x.YearMonth.Year).ThenBy(x => x.YearMonth.Month)
                        .ToList();

                    labels.AddRange(monthlyRevenue.Select(x => $"{x.YearMonth.Month:D2}/{x.YearMonth.Year}"));
                    data.AddRange(monthlyRevenue.Select(x => x.Total));
                    break;
            }

            return new AdvancedChartDataDto
            {
                ChartType = "line",
                Title = "Phân tích Doanh thu",
                Labels = labels,
                Series = new List<ChartSeriesDto>
                {
                    new ChartSeriesDto
                    {
                        Name = "Doanh thu",
                        Data = data,
                        Color = "#3B82F6",
                        Type = "line"
                    }
                },
                Options = new ChartOptionsDto
                {
                    XAxisLabel = groupBy == "day" ? "Ngày" : "Tháng",
                    YAxisLabel = "Doanh thu (VNĐ)",
                    CurrencyFormat = "VND"
                }
            };
        }

        public async Task<AdvancedChartDataDto> GetMembershipAnalyticsAsync(DateTime startDate, DateTime endDate)
        {
            var registrations = await _context.DangKys
                .Include(d => d.GoiTap)
                .Where(d => d.NgayTao >= startDate && d.NgayTao <= endDate)
                .ToListAsync();

            var packageStats = registrations
                .Where(r => r.GoiTap != null)
                .GroupBy(r => r.GoiTap!.TenGoi)
                .Select(g => new { Package = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .ToList();

            return new AdvancedChartDataDto
            {
                ChartType = "doughnut",
                Title = "Phân tích Gói tập",
                Labels = packageStats.Select(x => x.Package).ToList(),
                Series = new List<ChartSeriesDto>
                {
                    new ChartSeriesDto
                    {
                        Name = "Số lượng đăng ký",
                        Data = packageStats.Select(x => (decimal)x.Count).ToList(),
                        Color = "#10B981"
                    }
                }
            };
        }

        public async Task<AdvancedChartDataDto> GetAttendanceAnalyticsAsync(DateTime startDate, DateTime endDate)
        {
            var attendances = await _context.DiemDanhs
                .Where(d => d.ThoiGianCheckIn >= startDate && d.ThoiGianCheckIn <= endDate)
                .ToListAsync();

            var dailyAttendance = attendances
                .GroupBy(a => a.ThoiGianCheckIn.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .OrderBy(x => x.Date)
                .ToList();

            return new AdvancedChartDataDto
            {
                ChartType = "bar",
                Title = "Phân tích Điểm danh",
                Labels = dailyAttendance.Select(x => x.Date.ToString("dd/MM")).ToList(),
                Series = new List<ChartSeriesDto>
                {
                    new ChartSeriesDto
                    {
                        Name = "Lượt điểm danh",
                        Data = dailyAttendance.Select(x => (decimal)x.Count).ToList(),
                        Color = "#8B5CF6"
                    }
                }
            };
        }

        public async Task<AdvancedChartDataDto> GetClassPopularityAnalyticsAsync(DateTime startDate, DateTime endDate)
        {
            var bookings = await _context.Bookings
                .Include(b => b.LopHoc)
                .Where(b => b.NgayTao >= startDate && b.NgayTao <= endDate && b.LopHoc != null)
                .ToListAsync();

            var classStats = bookings
                .GroupBy(b => b.LopHoc!.TenLop)
                .Select(g => new { Class = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(10)
                .ToList();

            return new AdvancedChartDataDto
            {
                ChartType = "bar",
                Title = "Top 10 Lớp học phổ biến",
                Labels = classStats.Select(x => x.Class).ToList(),
                Series = new List<ChartSeriesDto>
                {
                    new ChartSeriesDto
                    {
                        Name = "Số lượt đặt",
                        Data = classStats.Select(x => (decimal)x.Count).ToList(),
                        Color = "#F59E0B"
                    }
                }
            };
        }

        public async Task<AdvancedChartDataDto> GetTrainerPerformanceAnalyticsAsync(int? trainerId = null)
        {
            var query = _context.LopHocs
                .Include(l => l.Hlv)
                .Include(l => l.Bookings)
                .Where(l => l.Hlv != null);

            if (trainerId.HasValue)
            {
                query = query.Where(l => l.HlvId == trainerId);
            }

            var trainerStats = await query
                .GroupBy(l => new { l.HlvId, TrainerName = l.Hlv!.Ho + " " + l.Hlv.Ten })
                .Select(g => new
                {
                    g.Key.TrainerName,
                    ClassCount = g.Count(),
                    BookingCount = g.SelectMany(l => l.Bookings).Count(),
                    AvgCapacity = g.Average(l => l.SucChua)
                })
                .ToListAsync();

            return new AdvancedChartDataDto
            {
                ChartType = "radar",
                Title = "Hiệu suất Huấn luyện viên",
                Labels = new List<string> { "Số lớp", "Lượt đặt", "Sức chứa TB" },
                Series = trainerStats.Select(t => new ChartSeriesDto
                {
                    Name = t.TrainerName,
                    Data = new List<decimal> { t.ClassCount, t.BookingCount, (decimal)t.AvgCapacity },
                    Color = GetRandomColor()
                }).ToList()
            };
        }

        public async Task<AdvancedChartDataDto> GetPackagePerformanceAnalyticsAsync()
        {
            var packages = await _context.GoiTaps
                .Include(g => g.DangKys)
                .Select(g => new
                {
                    g.TenGoi,
                    g.Gia,
                    RegistrationCount = g.DangKys.Count(d => d.TrangThai == "ACTIVE"),
                    Revenue = g.DangKys.Where(d => d.TrangThai == "ACTIVE").Sum(d => d.PhiDangKy ?? 0)
                })
                .ToListAsync();

            return new AdvancedChartDataDto
            {
                ChartType = "bubble",
                Title = "Hiệu suất Gói tập",
                Labels = packages.Select(p => p.TenGoi).ToList(),
                Series = new List<ChartSeriesDto>
                {
                    new ChartSeriesDto
                    {
                        Name = "Gói tập",
                        Data = packages.Select(p => p.Revenue).ToList(),
                        Color = "#EF4444",
                        Properties = new Dictionary<string, object>
                        {
                            ["bubbleData"] = packages.Select(p => new { x = p.RegistrationCount, y = p.Revenue, r = Math.Sqrt((double)p.Gia) / 10 }).ToList()
                        }
                    }
                }
            };
        }

        #endregion

        #region Dashboard Analytics

        public async Task<DashboardAnalyticsDto> GetDashboardAnalyticsAsync(string userRole, int? userId = null)
        {
            var endDate = DateTime.Now;
            var startDate = endDate.AddDays(-30);

            var dashboard = new DashboardAnalyticsDto
            {
                KpiMetrics = await GetKpiMetricsAsync(userRole, userId),
                LastUpdated = DateTime.Now
            };

            // Add role-specific charts
            if (userRole == "Admin")
            {
                dashboard.Charts.Add(await GetRevenueAnalyticsAsync(startDate, endDate));
                dashboard.Charts.Add(await GetMembershipAnalyticsAsync(startDate, endDate));
                dashboard.Charts.Add(await GetAttendanceAnalyticsAsync(startDate, endDate));
                dashboard.Charts.Add(await GetClassPopularityAnalyticsAsync(startDate, endDate));
            }
            else if (userRole == "Trainer" && userId.HasValue)
            {
                dashboard.Charts.Add(await GetTrainerPerformanceAnalyticsAsync(userId));
                dashboard.Charts.Add(await GetAttendanceAnalyticsAsync(startDate, endDate));
            }

            return dashboard;
        }

        public async Task<List<KpiMetricDto>> GetKpiMetricsAsync(string userRole, int? userId = null)
        {
            var metrics = new List<KpiMetricDto>();

            if (userRole == "Admin")
            {
                // Admin KPIs
                var totalMembers = await _context.NguoiDungs.CountAsync(n => n.LoaiNguoiDung == "THANHVIEN" && n.TrangThai == "ACTIVE");
                var thisMonthRevenue = await _context.ThanhToans
                    .Where(t => t.NgayThanhToan.Month == DateTime.Now.Month && t.TrangThai == "SUCCESS")
                    .SumAsync(t => t.SoTien);
                var todayAttendance = await _context.DiemDanhs
                    .CountAsync(d => d.ThoiGianCheckIn.Date == DateTime.Today);

                metrics.AddRange(new[]
                {
                    new KpiMetricDto
                    {
                        Name = "total_members",
                        DisplayName = "Tổng thành viên",
                        Value = totalMembers,
                        Format = "number",
                        Icon = "users",
                        Color = "blue"
                    },
                    new KpiMetricDto
                    {
                        Name = "monthly_revenue",
                        DisplayName = "Doanh thu tháng",
                        Value = thisMonthRevenue,
                        Format = "currency",
                        Icon = "dollar-sign",
                        Color = "green"
                    },
                    new KpiMetricDto
                    {
                        Name = "today_attendance",
                        DisplayName = "Điểm danh hôm nay",
                        Value = todayAttendance,
                        Format = "number",
                        Icon = "check-circle",
                        Color = "purple"
                    }
                });
            }
            else if (userRole == "Trainer" && userId.HasValue)
            {
                // Trainer KPIs
                var myClasses = await _context.LopHocs.CountAsync(l => l.HlvId == userId);
                var myStudents = await _context.DangKys
                    .Include(d => d.LopHoc)
                    .CountAsync(d => d.LopHoc != null && d.LopHoc.HlvId == userId && d.TrangThai == "ACTIVE");

                metrics.AddRange(new[]
                {
                    new KpiMetricDto
                    {
                        Name = "my_classes",
                        DisplayName = "Lớp học của tôi",
                        Value = myClasses,
                        Format = "number",
                        Icon = "book",
                        Color = "blue"
                    },
                    new KpiMetricDto
                    {
                        Name = "my_students",
                        DisplayName = "Học viên của tôi",
                        Value = myStudents,
                        Format = "number",
                        Icon = "users",
                        Color = "green"
                    }
                });
            }

            return metrics;
        }

        #endregion

        #region Comparison & Forecast

        public async Task<ComparisonReportDto> GetPeriodComparisonAsync(DateTime period1Start, DateTime period1End, DateTime period2Start, DateTime period2End, string metricType)
        {
            switch (metricType.ToLower())
            {
                case "revenue":
                    return await GetRevenueComparison(period1Start, period1End, period2Start, period2End);
                case "members":
                    return await GetMemberComparison(period1Start, period1End, period2Start, period2End);
                default:
                    throw new ArgumentException("Unsupported metric type");
            }
        }

        public async Task<ForecastDataDto> GetRevenueForecastAsync(int forecastDays = 30)
        {
            var historicalData = await _context.ThanhToans
                .Where(t => t.TrangThai == "SUCCESS" && t.NgayThanhToan >= DateTime.Now.AddDays(-90))
                .GroupBy(t => t.NgayThanhToan.Date)
                .Select(g => new ForecastPointDto
                {
                    Date = g.Key,
                    Value = g.Sum(t => t.SoTien),
                    IsActual = true
                })
                .OrderBy(p => p.Date)
                .ToListAsync();

            // Simple linear regression for forecast
            var forecast = GenerateLinearForecast(historicalData, forecastDays);

            return new ForecastDataDto
            {
                MetricName = "Doanh thu",
                Historical = historicalData,
                Forecast = forecast,
                Confidence = 0.75m,
                Model = "Linear Regression"
            };
        }

        public async Task<ForecastDataDto> GetMembershipGrowthForecastAsync(int forecastDays = 30)
        {
            var historicalData = await _context.DangKys
                .Where(d => d.NgayTao >= DateTime.Now.AddDays(-90))
                .GroupBy(d => d.NgayTao.Date)
                .Select(g => new ForecastPointDto
                {
                    Date = g.Key,
                    Value = g.Count(),
                    IsActual = true
                })
                .OrderBy(p => p.Date)
                .ToListAsync();

            var forecast = GenerateLinearForecast(historicalData, forecastDays);

            return new ForecastDataDto
            {
                MetricName = "Tăng trưởng thành viên",
                Historical = historicalData,
                Forecast = forecast,
                Confidence = 0.65m,
                Model = "Linear Regression"
            };
        }

        #endregion

        #region Helper Methods

        private async Task AddReportContentToPdf(Document document, string reportType, Dictionary<string, object> parameters, iTextSharp.text.Font headerFont, iTextSharp.text.Font normalFont)
        {
            switch (reportType.ToLower())
            {
                case "revenue":
                    await AddRevenueReportToPdf(document, parameters, headerFont, normalFont);
                    break;
                case "members":
                    await AddMembersReportToPdf(document, parameters, headerFont, normalFont);
                    break;
                case "attendance":
                    await AddAttendanceReportToPdf(document, parameters, headerFont, normalFont);
                    break;
            }
        }

        private async Task<int> AddReportContentToExcel(ExcelWorksheet worksheet, string reportType, Dictionary<string, object> parameters, int startRow)
        {
            switch (reportType.ToLower())
            {
                case "revenue":
                    return await AddRevenueReportToExcel(worksheet, parameters, startRow);
                case "members":
                    return await AddMembersReportToExcel(worksheet, parameters, startRow);
                case "attendance":
                    return await AddAttendanceReportToExcel(worksheet, parameters, startRow);
                default:
                    return startRow;
            }
        }

        private async Task AddRevenueReportToPdf(Document document, Dictionary<string, object> parameters, iTextSharp.text.Font headerFont, iTextSharp.text.Font normalFont)
        {
            var startDate = parameters.ContainsKey("startDate") ? (DateTime)parameters["startDate"] : DateTime.Now.AddDays(-30);
            var endDate = parameters.ContainsKey("endDate") ? (DateTime)parameters["endDate"] : DateTime.Now;

            var revenues = await _context.ThanhToans
                .Include(t => t.DangKy)
                .ThenInclude(d => d.NguoiDung)
                .Where(t => t.NgayThanhToan >= startDate && t.NgayThanhToan <= endDate && t.TrangThai == "SUCCESS")
                .OrderByDescending(t => t.NgayThanhToan)
                .ToListAsync();

            document.Add(new Paragraph("BÁO CÁO DOANH THU", headerFont) { SpacingAfter = 10 });
            document.Add(new Paragraph($"Từ ngày: {startDate:dd/MM/yyyy} - Đến ngày: {endDate:dd/MM/yyyy}", normalFont) { SpacingAfter = 10 });
            document.Add(new Paragraph($"Tổng doanh thu: {revenues.Sum(r => r.SoTien):N0} VNĐ", normalFont) { SpacingAfter = 15 });

            if (revenues.Any())
            {
                var table = new PdfPTable(5);
                table.WidthPercentage = 100;

                // Headers
                table.AddCell(new PdfPCell(new Phrase("Ngày", headerFont)));
                table.AddCell(new PdfPCell(new Phrase("Thành viên", headerFont)));
                table.AddCell(new PdfPCell(new Phrase("Số tiền", headerFont)));
                table.AddCell(new PdfPCell(new Phrase("Phương thức", headerFont)));
                table.AddCell(new PdfPCell(new Phrase("Trạng thái", headerFont)));

                // Data
                foreach (var revenue in revenues.Take(50)) // Limit for PDF
                {
                    table.AddCell(new PdfPCell(new Phrase(revenue.NgayThanhToan.ToString("dd/MM/yyyy"), normalFont)));
                    table.AddCell(new PdfPCell(new Phrase(revenue.DangKy?.NguoiDung?.Ho + " " + revenue.DangKy?.NguoiDung?.Ten, normalFont)));
                    table.AddCell(new PdfPCell(new Phrase($"{revenue.SoTien:N0} VNĐ", normalFont)));
                    table.AddCell(new PdfPCell(new Phrase(revenue.PhuongThuc ?? "", normalFont)));
                    table.AddCell(new PdfPCell(new Phrase(revenue.TrangThai, normalFont)));
                }

                document.Add(table);
            }
        }

        private async Task<int> AddRevenueReportToExcel(ExcelWorksheet worksheet, Dictionary<string, object> parameters, int startRow)
        {
            var startDate = parameters.ContainsKey("startDate") ? (DateTime)parameters["startDate"] : DateTime.Now.AddDays(-30);
            var endDate = parameters.ContainsKey("endDate") ? (DateTime)parameters["endDate"] : DateTime.Now;

            var revenues = await _context.ThanhToans
                .Include(t => t.DangKy)
                .ThenInclude(d => d.NguoiDung)
                .Where(t => t.NgayThanhToan >= startDate && t.NgayThanhToan <= endDate && t.TrangThai == "SUCCESS")
                .OrderByDescending(t => t.NgayThanhToan)
                .ToListAsync();

            worksheet.Cells[startRow, 1].Value = "BÁO CÁO DOANH THU";
            worksheet.Cells[startRow, 1].Style.Font.Bold = true;
            startRow++;

            worksheet.Cells[startRow, 1].Value = $"Từ ngày: {startDate:dd/MM/yyyy} - Đến ngày: {endDate:dd/MM/yyyy}";
            startRow++;

            worksheet.Cells[startRow, 1].Value = $"Tổng doanh thu: {revenues.Sum(r => r.SoTien):N0} VNĐ";
            startRow += 2;

            if (revenues.Any())
            {
                // Headers
                worksheet.Cells[startRow, 1].Value = "Ngày";
                worksheet.Cells[startRow, 2].Value = "Thành viên";
                worksheet.Cells[startRow, 3].Value = "Số tiền";
                worksheet.Cells[startRow, 4].Value = "Phương thức";
                worksheet.Cells[startRow, 5].Value = "Trạng thái";
                
                worksheet.Cells[startRow, 1, startRow, 5].Style.Font.Bold = true;
                startRow++;

                // Data
                foreach (var revenue in revenues)
                {
                    worksheet.Cells[startRow, 1].Value = revenue.NgayThanhToan.ToString("dd/MM/yyyy");
                    worksheet.Cells[startRow, 2].Value = revenue.DangKy?.NguoiDung?.Ho + " " + revenue.DangKy?.NguoiDung?.Ten;
                    worksheet.Cells[startRow, 3].Value = revenue.SoTien;
                    worksheet.Cells[startRow, 4].Value = revenue.PhuongThuc ?? "";
                    worksheet.Cells[startRow, 5].Value = revenue.TrangThai;
                    startRow++;
                }
            }

            return startRow;
        }

        private async Task AddMembersReportToPdf(Document document, Dictionary<string, object> parameters, iTextSharp.text.Font headerFont, iTextSharp.text.Font normalFont)
        {
            var members = await _context.NguoiDungs
                .Where(n => n.LoaiNguoiDung == "THANHVIEN")
                .OrderByDescending(n => n.NgayThamGia)
                .ToListAsync();

            document.Add(new Paragraph("BÁO CÁO THÀNH VIÊN", headerFont) { SpacingAfter = 10 });
            document.Add(new Paragraph($"Tổng số thành viên: {members.Count}", normalFont) { SpacingAfter = 10 });
            document.Add(new Paragraph($"Thành viên hoạt động: {members.Count(m => m.TrangThai == "ACTIVE")}", normalFont) { SpacingAfter = 15 });

            if (members.Any())
            {
                var table = new PdfPTable(5);
                table.WidthPercentage = 100;

                // Headers
                table.AddCell(new PdfPCell(new Phrase("Họ tên", headerFont)));
                table.AddCell(new PdfPCell(new Phrase("Email", headerFont)));
                table.AddCell(new PdfPCell(new Phrase("Ngày tham gia", headerFont)));
                table.AddCell(new PdfPCell(new Phrase("Trạng thái", headerFont)));
                table.AddCell(new PdfPCell(new Phrase("SĐT", headerFont)));

                // Data
                foreach (var member in members.Take(50))
                {
                    table.AddCell(new PdfPCell(new Phrase($"{member.Ho} {member.Ten}", normalFont)));
                    table.AddCell(new PdfPCell(new Phrase(member.Email ?? "", normalFont)));
                    table.AddCell(new PdfPCell(new Phrase(member.NgayThamGia.ToString("dd/MM/yyyy"), normalFont)));
                    table.AddCell(new PdfPCell(new Phrase(member.TrangThai, normalFont)));
                    table.AddCell(new PdfPCell(new Phrase(member.SoDienThoai ?? "", normalFont)));
                }

                document.Add(table);
            }
        }

        private async Task<int> AddMembersReportToExcel(ExcelWorksheet worksheet, Dictionary<string, object> parameters, int startRow)
        {
            var members = await _context.NguoiDungs
                .Where(n => n.LoaiNguoiDung == "THANHVIEN")
                .OrderByDescending(n => n.NgayThamGia)
                .ToListAsync();

            worksheet.Cells[startRow, 1].Value = "BÁO CÁO THÀNH VIÊN";
            worksheet.Cells[startRow, 1].Style.Font.Bold = true;
            startRow++;

            worksheet.Cells[startRow, 1].Value = $"Tổng số thành viên: {members.Count}";
            startRow++;

            worksheet.Cells[startRow, 1].Value = $"Thành viên hoạt động: {members.Count(m => m.TrangThai == "ACTIVE")}";
            startRow += 2;

            if (members.Any())
            {
                // Headers
                worksheet.Cells[startRow, 1].Value = "Họ tên";
                worksheet.Cells[startRow, 2].Value = "Email";
                worksheet.Cells[startRow, 3].Value = "Ngày tham gia";
                worksheet.Cells[startRow, 4].Value = "Trạng thái";
                worksheet.Cells[startRow, 5].Value = "SĐT";
                
                worksheet.Cells[startRow, 1, startRow, 5].Style.Font.Bold = true;
                startRow++;

                // Data
                foreach (var member in members)
                {
                    worksheet.Cells[startRow, 1].Value = $"{member.Ho} {member.Ten}";
                    worksheet.Cells[startRow, 2].Value = member.Email ?? "";
                    worksheet.Cells[startRow, 3].Value = member.NgayThamGia.ToString("dd/MM/yyyy");
                    worksheet.Cells[startRow, 4].Value = member.TrangThai;
                    worksheet.Cells[startRow, 5].Value = member.SoDienThoai ?? "";
                    startRow++;
                }
            }

            return startRow;
        }

        private async Task AddAttendanceReportToPdf(Document document, Dictionary<string, object> parameters, iTextSharp.text.Font headerFont, iTextSharp.text.Font normalFont)
        {
            var startDate = parameters.ContainsKey("startDate") ? (DateTime)parameters["startDate"] : DateTime.Now.AddDays(-30);
            var endDate = parameters.ContainsKey("endDate") ? (DateTime)parameters["endDate"] : DateTime.Now;

            var attendances = await _context.DiemDanhs
                .Include(d => d.ThanhVien)
                .Where(d => d.ThoiGianCheckIn >= startDate && d.ThoiGianCheckIn <= endDate)
                .OrderByDescending(d => d.ThoiGianCheckIn)
                .ToListAsync();

            document.Add(new Paragraph("BÁO CÁO ĐIỂM DANH", headerFont) { SpacingAfter = 10 });
            document.Add(new Paragraph($"Từ ngày: {startDate:dd/MM/yyyy} - Đến ngày: {endDate:dd/MM/yyyy}", normalFont) { SpacingAfter = 10 });
            document.Add(new Paragraph($"Tổng lượt điểm danh: {attendances.Count}", normalFont) { SpacingAfter = 15 });

            if (attendances.Any())
            {
                var table = new PdfPTable(4);
                table.WidthPercentage = 100;

                // Headers
                table.AddCell(new PdfPCell(new Phrase("Thành viên", headerFont)));
                table.AddCell(new PdfPCell(new Phrase("Thời gian vào", headerFont)));
                table.AddCell(new PdfPCell(new Phrase("Thời gian ra", headerFont)));
                table.AddCell(new PdfPCell(new Phrase("Ghi chú", headerFont)));

                // Data
                foreach (var attendance in attendances.Take(50))
                {
                    table.AddCell(new PdfPCell(new Phrase($"{attendance.ThanhVien?.Ho} {attendance.ThanhVien?.Ten}", normalFont)));
                    table.AddCell(new PdfPCell(new Phrase(attendance.ThoiGianCheckIn.ToString("dd/MM/yyyy HH:mm"), normalFont)));
                    table.AddCell(new PdfPCell(new Phrase("", normalFont))); // No ThoiGianRa field
                    table.AddCell(new PdfPCell(new Phrase(attendance.GhiChu ?? "", normalFont)));
                }

                document.Add(table);
            }
        }

        private async Task<int> AddAttendanceReportToExcel(ExcelWorksheet worksheet, Dictionary<string, object> parameters, int startRow)
        {
            var startDate = parameters.ContainsKey("startDate") ? (DateTime)parameters["startDate"] : DateTime.Now.AddDays(-30);
            var endDate = parameters.ContainsKey("endDate") ? (DateTime)parameters["endDate"] : DateTime.Now;

            var attendances = await _context.DiemDanhs
                .Include(d => d.ThanhVien)
                .Where(d => d.ThoiGianCheckIn >= startDate && d.ThoiGianCheckIn <= endDate)
                .OrderByDescending(d => d.ThoiGianCheckIn)
                .ToListAsync();

            worksheet.Cells[startRow, 1].Value = "BÁO CÁO ĐIỂM DANH";
            worksheet.Cells[startRow, 1].Style.Font.Bold = true;
            startRow++;

            worksheet.Cells[startRow, 1].Value = $"Từ ngày: {startDate:dd/MM/yyyy} - Đến ngày: {endDate:dd/MM/yyyy}";
            startRow++;

            worksheet.Cells[startRow, 1].Value = $"Tổng lượt điểm danh: {attendances.Count}";
            startRow += 2;

            if (attendances.Any())
            {
                // Headers
                worksheet.Cells[startRow, 1].Value = "Thành viên";
                worksheet.Cells[startRow, 2].Value = "Thời gian vào";
                worksheet.Cells[startRow, 3].Value = "Thời gian ra";
                worksheet.Cells[startRow, 4].Value = "Ghi chú";
                
                worksheet.Cells[startRow, 1, startRow, 4].Style.Font.Bold = true;
                startRow++;

                // Data
                foreach (var attendance in attendances)
                {
                    worksheet.Cells[startRow, 1].Value = $"{attendance.ThanhVien?.Ho} {attendance.ThanhVien?.Ten}";
                    worksheet.Cells[startRow, 2].Value = attendance.ThoiGianCheckIn.ToString("dd/MM/yyyy HH:mm");
                    worksheet.Cells[startRow, 3].Value = ""; // No ThoiGianRa field
                    worksheet.Cells[startRow, 4].Value = attendance.GhiChu ?? "";
                    startRow++;
                }
            }

            return startRow;
        }

        private string GetReportTitle(string reportType)
        {
            return reportType.ToLower() switch
            {
                "revenue" => "BÁO CÁO DOANH THU",
                "members" => "BÁO CÁO THÀNH VIÊN",
                "attendance" => "BÁO CÁO ĐIỂM DANH",
                "classes" => "BÁO CÁO LỚP HỌC",
                "trainers" => "BÁO CÁO HUẤN LUYỆN VIÊN",
                _ => "BÁO CÁO TỔNG HỢP"
            };
        }

        private PdfPTable CreatePdfTable(List<Dictionary<string, object>> data)
        {
            if (!data.Any()) return new PdfPTable(1);

            var headers = data.First().Keys.ToList();
            var table = new PdfPTable(headers.Count);
            table.WidthPercentage = 100;

            var headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10, BaseColor.BLACK);
            var cellFont = FontFactory.GetFont(FontFactory.HELVETICA, 9, BaseColor.BLACK);

            // Add headers
            foreach (var header in headers)
            {
                table.AddCell(new PdfPCell(new Phrase(header, headerFont)));
            }

            // Add data
            foreach (var row in data)
            {
                foreach (var header in headers)
                {
                    var cellValue = row.ContainsKey(header) ? row[header]?.ToString() ?? "" : "";
                    table.AddCell(new PdfPCell(new Phrase(cellValue, cellFont)));
                }
            }

            return table;
        }

        private int AddDataToExcelWorksheet(ExcelWorksheet worksheet, List<Dictionary<string, object>> data, int startRow)
        {
            if (!data.Any()) return startRow;

            var headers = data.First().Keys.ToList();
            var currentRow = startRow;

            // Add headers
            for (int i = 0; i < headers.Count; i++)
            {
                worksheet.Cells[currentRow, i + 1].Value = headers[i];
                worksheet.Cells[currentRow, i + 1].Style.Font.Bold = true;
            }
            currentRow++;

            // Add data
            foreach (var row in data)
            {
                for (int i = 0; i < headers.Count; i++)
                {
                    var value = row.ContainsKey(headers[i]) ? row[headers[i]] : null;
                    worksheet.Cells[currentRow, i + 1].Value = value?.ToString() ?? "";
                }
                currentRow++;
            }

            return currentRow;
        }

        private void AddChartToExcel(ExcelWorksheet worksheet, AdvancedChartDataDto chartData, int startRow)
        {
            if (!chartData.Series.Any() || !chartData.Labels.Any()) return;

            var chart = worksheet.Drawings.AddChart("chart", eChartType.Line);
            chart.SetPosition(startRow, 0, 1, 0);
            chart.SetSize(600, 400);

            // Add chart data
            var dataStartRow = startRow + 2;
            
            // Add labels
            for (int i = 0; i < chartData.Labels.Count; i++)
            {
                worksheet.Cells[dataStartRow + i, 1].Value = chartData.Labels[i];
            }

            // Add series data
            for (int seriesIndex = 0; seriesIndex < chartData.Series.Count; seriesIndex++)
            {
                var series = chartData.Series[seriesIndex];
                var dataColumn = seriesIndex + 2;

                // Add series header
                worksheet.Cells[dataStartRow - 1, dataColumn].Value = series.Name;

                // Add series data
                for (int i = 0; i < series.Data.Count && i < chartData.Labels.Count; i++)
                {
                    worksheet.Cells[dataStartRow + i, dataColumn].Value = (double)series.Data[i];
                }

                // Add series to chart
                var chartSeries = chart.Series.Add(
                    worksheet.Cells[dataStartRow, dataColumn, dataStartRow + chartData.Labels.Count - 1, dataColumn],
                    worksheet.Cells[dataStartRow, 1, dataStartRow + chartData.Labels.Count - 1, 1]
                );
                chartSeries.Header = series.Name;
            }

            chart.Title.Text = chartData.Title;
        }

        private IQueryable<Dictionary<string, object>> BuildDynamicQuery(CustomReportDto reportConfig)
        {
            // This is a simplified implementation
            // In a real-world scenario, you would build dynamic SQL or LINQ queries
            // based on the report configuration
            
            // For now, return sample data based on report type
            var sampleData = new List<Dictionary<string, object>>();
            
            for (int i = 1; i <= 10; i++)
            {
                sampleData.Add(new Dictionary<string, object>
                {
                    ["Id"] = i,
                    ["Name"] = $"Sample {i}",
                    ["Value"] = i * 100,
                    ["Date"] = DateTime.Now.AddDays(-i)
                });
            }

            return sampleData.AsQueryable();
        }

        private async Task<Dictionary<string, object>> CalculateSummary(List<Dictionary<string, object>> data, CustomReportDto reportConfig)
        {
            var summary = new Dictionary<string, object>
            {
                ["TotalRecords"] = data.Count,
                ["GeneratedAt"] = DateTime.Now
            };

            // Add more summary calculations based on data
            if (data.Any())
            {
                var numericFields = data.First()
                    .Where(kvp => kvp.Value is decimal or double or int or float)
                    .Select(kvp => kvp.Key)
                    .ToList();

                foreach (var field in numericFields)
                {
                    var values = data.Select(d => Convert.ToDecimal(d.ContainsKey(field) ? d[field] : 0)).ToList();
                    summary[$"{field}_Sum"] = values.Sum();
                    summary[$"{field}_Average"] = values.Average();
                    summary[$"{field}_Max"] = values.Max();
                    summary[$"{field}_Min"] = values.Min();
                }
            }

            return summary;
        }

        private AdvancedChartDataDto GenerateChartDataFromResults(List<Dictionary<string, object>> data, CustomReportDto reportConfig)
        {
            if (!data.Any() || string.IsNullOrEmpty(reportConfig.XAxisField) || string.IsNullOrEmpty(reportConfig.YAxisField))
            {
                return new AdvancedChartDataDto();
            }

            var labels = data.Select(d => d.ContainsKey(reportConfig.XAxisField) ? d[reportConfig.XAxisField]?.ToString() ?? "" : "").ToList();
            var values = data.Select(d => 
            {
                if (d.ContainsKey(reportConfig.YAxisField) && decimal.TryParse(d[reportConfig.YAxisField]?.ToString(), out var value))
                {
                    return value;
                }
                return 0m;
            }).ToList();

            return new AdvancedChartDataDto
            {
                ChartType = reportConfig.ChartType,
                Title = reportConfig.ReportName,
                Labels = labels,
                Series = new List<ChartSeriesDto>
                {
                    new ChartSeriesDto
                    {
                        Name = reportConfig.YAxisField,
                        Data = values,
                        Color = "#3B82F6"
                    }
                }
            };
        }

        private List<CustomReportTemplateDto> GetPredefinedTemplates(string userRole)
        {
            var templates = new List<CustomReportTemplateDto>();

            if (userRole == "Admin")
            {
                templates.AddRange(new[]
                {
                    new CustomReportTemplateDto
                    {
                        TemplateId = 1,
                        TemplateName = "Báo cáo doanh thu tháng",
                        Description = "Báo cáo chi tiết doanh thu theo tháng",
                        UserRole = "Admin",
                        IsPublic = true,
                        ReportConfig = new CustomReportDto
                        {
                            ReportName = "Báo cáo doanh thu tháng",
                            ReportType = "mixed",
                            ChartType = "bar"
                        }
                    },
                    new CustomReportTemplateDto
                    {
                        TemplateId = 2,
                        TemplateName = "Thống kê thành viên",
                        Description = "Thống kê tình trạng thành viên",
                        UserRole = "Admin",
                        IsPublic = true,
                        ReportConfig = new CustomReportDto
                        {
                            ReportName = "Thống kê thành viên",
                            ReportType = "chart",
                            ChartType = "pie"
                        }
                    }
                });
            }
            else if (userRole == "Trainer")
            {
                templates.Add(new CustomReportTemplateDto
                {
                    TemplateId = 3,
                    TemplateName = "Báo cáo lớp học của tôi",
                    Description = "Báo cáo chi tiết về các lớp học được phân công",
                    UserRole = "Trainer",
                    IsPublic = false,
                    ReportConfig = new CustomReportDto
                    {
                        ReportName = "Báo cáo lớp học của tôi",
                        ReportType = "table"
                    }
                });
            }

            return templates;
        }

        private async Task<ComparisonReportDto> GetRevenueComparison(DateTime period1Start, DateTime period1End, DateTime period2Start, DateTime period2End)
        {
            var period1Revenue = await _context.ThanhToans
                .Where(t => t.NgayThanhToan >= period1Start && t.NgayThanhToan <= period1End && t.TrangThai == "SUCCESS")
                .SumAsync(t => t.SoTien);

            var period2Revenue = await _context.ThanhToans
                .Where(t => t.NgayThanhToan >= period2Start && t.NgayThanhToan <= period2End && t.TrangThai == "SUCCESS")
                .SumAsync(t => t.SoTien);

            var change = period2Revenue - period1Revenue;
            var changePercent = period1Revenue > 0 ? (change / period1Revenue) * 100 : 0;

            return new ComparisonReportDto
            {
                MetricName = "Doanh thu",
                Period1 = new PeriodDataDto
                {
                    StartDate = period1Start,
                    EndDate = period1End,
                    Value = period1Revenue,
                    Label = $"{period1Start:dd/MM} - {period1End:dd/MM}"
                },
                Period2 = new PeriodDataDto
                {
                    StartDate = period2Start,
                    EndDate = period2End,
                    Value = period2Revenue,
                    Label = $"{period2Start:dd/MM} - {period2End:dd/MM}"
                },
                ChangeAmount = change,
                ChangePercent = changePercent,
                ChangeDirection = change > 0 ? "up" : change < 0 ? "down" : "neutral"
            };
        }

        private async Task<ComparisonReportDto> GetMemberComparison(DateTime period1Start, DateTime period1End, DateTime period2Start, DateTime period2End)
        {
            var period1Members = await _context.NguoiDungs
                .CountAsync(n => n.NgayThamGia >= DateOnly.FromDateTime(period1Start) && n.NgayThamGia <= DateOnly.FromDateTime(period1End));

            var period2Members = await _context.NguoiDungs
                .CountAsync(n => n.NgayThamGia >= DateOnly.FromDateTime(period2Start) && n.NgayThamGia <= DateOnly.FromDateTime(period2End));

            var change = period2Members - period1Members;
            var changePercent = period1Members > 0 ? (decimal)(change * 100) / period1Members : 0;

            return new ComparisonReportDto
            {
                MetricName = "Thành viên mới",
                Period1 = new PeriodDataDto
                {
                    StartDate = period1Start,
                    EndDate = period1End,
                    Value = period1Members,
                    Label = $"{period1Start:dd/MM} - {period1End:dd/MM}"
                },
                Period2 = new PeriodDataDto
                {
                    StartDate = period2Start,
                    EndDate = period2End,
                    Value = period2Members,
                    Label = $"{period2Start:dd/MM} - {period2End:dd/MM}"
                },
                ChangeAmount = change,
                ChangePercent = changePercent,
                ChangeDirection = change > 0 ? "up" : change < 0 ? "down" : "neutral"
            };
        }

        private List<ForecastPointDto> GenerateLinearForecast(List<ForecastPointDto> historicalData, int forecastDays)
        {
            if (historicalData.Count < 2) return new List<ForecastPointDto>();

            // Simple linear regression
            var n = historicalData.Count;
            var sumX = 0.0;
            var sumY = 0.0;
            var sumXY = 0.0;
            var sumX2 = 0.0;

            for (int i = 0; i < n; i++)
            {
                var x = i;
                var y = (double)historicalData[i].Value;
                sumX += x;
                sumY += y;
                sumXY += x * y;
                sumX2 += x * x;
            }

            var slope = (n * sumXY - sumX * sumY) / (n * sumX2 - sumX * sumX);
            var intercept = (sumY - slope * sumX) / n;

            var forecast = new List<ForecastPointDto>();
            var lastDate = historicalData.Last().Date;

            for (int i = 1; i <= forecastDays; i++)
            {
                var forecastValue = (decimal)(slope * (n + i - 1) + intercept);
                var margin = Math.Abs(forecastValue * 0.2m); // 20% margin

                forecast.Add(new ForecastPointDto
                {
                    Date = lastDate.AddDays(i),
                    Value = Math.Max(0, forecastValue),
                    LowerBound = Math.Max(0, forecastValue - margin),
                    UpperBound = forecastValue + margin,
                    IsActual = false
                });
            }

            return forecast;
        }

        private string GetRandomColor()
        {
            var colors = new[] { "#3B82F6", "#10B981", "#F59E0B", "#EF4444", "#8B5CF6", "#06B6D4", "#84CC16", "#F97316" };
            return colors[new Random().Next(colors.Length)];
        }

        #endregion
    }
} 