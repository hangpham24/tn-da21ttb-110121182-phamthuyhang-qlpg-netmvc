using GymManagement.Web.Data.Models;
using GymManagement.Web.Data.Repositories;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System.Globalization;

namespace GymManagement.Web.Services
{
    public class PdfExportService : IPdfExportService
    {
        private readonly INguoiDungRepository _nguoiDungRepository;
        private readonly ILogger<PdfExportService> _logger;

        // PDF Styling Constants
        private static readonly BaseColor GYM_PRIMARY = new BaseColor(44, 62, 80);
        private static readonly BaseColor GYM_SECONDARY = new BaseColor(52, 152, 219);
        private static readonly BaseColor GYM_SUCCESS = new BaseColor(39, 174, 96);
        private static readonly BaseColor GYM_LIGHT_GRAY = new BaseColor(248, 249, 250);

        public PdfExportService(
            INguoiDungRepository nguoiDungRepository,
            ILogger<PdfExportService> logger)
        {
            _nguoiDungRepository = nguoiDungRepository;
            _logger = logger;
        }

        public async Task<byte[]> GenerateSalaryReportAsync(BangLuong bangLuong, BangLuongService.CommissionBreakdown breakdown)
        {
            try
            {
                var trainer = await _nguoiDungRepository.GetByIdAsync(bangLuong.HlvId ?? 0);

                using var stream = new MemoryStream();
                var document = new Document(PageSize.A4, 50, 50, 50, 50);
                var writer = PdfWriter.GetInstance(document, stream);

                document.Open();

                // Add header
                await AddReportHeaderAsync(document, "BÁO CÁO LƯƠNG TRAINER", $"Tháng {bangLuong.Thang}");

                // Add trainer info
                await AddTrainerInfoAsync(document, bangLuong, trainer);

                // Add salary breakdown
                AddDetailedSalaryTable(document, bangLuong, breakdown);

                // Add footer
                AddReportFooter(document);

                document.Close();

                return stream.ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating basic salary report for BangLuongId: {BangLuongId}", bangLuong.BangLuongId);
                throw;
            }
        }

        public async Task<byte[]> GenerateMonthlySalaryReportAsync(IEnumerable<BangLuong> salaries, string month)
        {
            try
            {
                using var stream = new MemoryStream();
                var document = new Document(PageSize.A4.Rotate(), 50, 50, 50, 50); // Landscape
                var writer = PdfWriter.GetInstance(document, stream);

                document.Open();

                // Add header
                await AddReportHeaderAsync(document, "BÁO CÁO LƯƠNG THÁNG", $"Tháng {month}");

                // Add summary stats
                AddMonthlySummary(document, salaries);

                // Add salary table
                AddMonthlySalaryTable(document, salaries);

                // Add footer
                AddReportFooter(document);

                document.Close();

                return stream.ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating monthly salary report for month: {Month}", month);
                throw;
            }
        }

        #region Private Helper Methods

        private async Task AddReportHeaderAsync(Document document, string title, string subtitle)
        {
            var titleParagraph = new Paragraph(title, FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 18, GYM_PRIMARY));
            titleParagraph.Alignment = Element.ALIGN_CENTER;
            document.Add(titleParagraph);

            var subtitleParagraph = new Paragraph(subtitle, FontFactory.GetFont(FontFactory.HELVETICA, 14, GYM_SECONDARY));
            subtitleParagraph.Alignment = Element.ALIGN_CENTER;
            document.Add(subtitleParagraph);

            document.Add(new Paragraph(" ")); // Space
            // Add line separator using paragraph with underline
            var line = new Paragraph("_".PadRight(60, '_'), FontFactory.GetFont(FontFactory.HELVETICA, 1, GYM_PRIMARY));
            line.Alignment = Element.ALIGN_CENTER;
            document.Add(line);
            document.Add(new Paragraph(" ")); // Space
        }

        private async Task AddTrainerInfoAsync(Document document, BangLuong bangLuong, NguoiDung? trainer)
        {
            if (trainer == null) return;

            var table = new PdfPTable(2);
            table.WidthPercentage = 100;
            table.SetWidths(new float[] { 30, 70 });

            // Add trainer information
            AddTableRow(table, "Họ tên:", $"{trainer.Ho} {trainer.Ten}", "Email:", trainer.Email ?? "N/A");
            AddTableRow(table, "Điện thoại:", trainer.SoDienThoai ?? "N/A", "Ngày tham gia:", trainer.NgayTao.ToString("dd/MM/yyyy"));

            document.Add(table);
            document.Add(new Paragraph(" "));
        }

        private void AddDetailedSalaryTable(Document document, BangLuong bangLuong, BangLuongService.CommissionBreakdown breakdown)
        {
            var table = new PdfPTable(2);
            table.WidthPercentage = 100;
            table.SetWidths(new float[] { 70, 30 });

            // Header
            var headerCell1 = CreateHeaderCell("Khoản mục", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12));
            var headerCell2 = CreateHeaderCell("Số tiền (VNĐ)", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12));
            table.AddCell(headerCell1);
            table.AddCell(headerCell2);

            // Salary rows
            AddSalaryRow(table, "Lương cơ bản", bangLuong.LuongCoBan);
            AddSalaryRow(table, "Hoa hồng gói tập", breakdown.PackageCommission);
            AddSalaryRow(table, "Hoa hồng lớp học", breakdown.ClassCommission);
            AddSalaryRow(table, "Hoa hồng cá nhân", breakdown.PersonalCommission);
            AddSalaryRow(table, "Thưởng hiệu suất", breakdown.PerformanceBonus);
            AddSalaryRow(table, "Thưởng điểm danh", breakdown.AttendanceBonus);

            // Total row with highlight
            var totalLabelCell = new PdfPCell(new Phrase("TỔNG LƯƠNG", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12, BaseColor.WHITE)));
            totalLabelCell.BackgroundColor = GYM_SUCCESS;
            totalLabelCell.Padding = 8;
            table.AddCell(totalLabelCell);

            var totalValueCell = new PdfPCell(new Phrase($"{bangLuong.TongThanhToan:N0}", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12, BaseColor.WHITE)));
            totalValueCell.BackgroundColor = GYM_SUCCESS;
            totalValueCell.Padding = 8;
            totalValueCell.HorizontalAlignment = Element.ALIGN_RIGHT;
            table.AddCell(totalValueCell);

            document.Add(table);
            document.Add(new Paragraph(" "));

            // Payment status
            var statusText = bangLuong.NgayThanhToan.HasValue ? 
                $"Đã thanh toán ngày: {bangLuong.NgayThanhToan:dd/MM/yyyy}" : "Chưa thanh toán";
            var statusColor = bangLuong.NgayThanhToan.HasValue ? GYM_SUCCESS : BaseColor.ORANGE;
            
            var statusParagraph = new Paragraph(statusText, FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 11, statusColor));
            statusParagraph.Alignment = Element.ALIGN_CENTER;
            document.Add(statusParagraph);
        }

        private void AddMonthlySummary(Document document, IEnumerable<BangLuong> salaries)
        {
            var totalSalaries = salaries.Sum(s => s.TongThanhToan);
            var totalTrainers = salaries.Count();
            var paidSalaries = salaries.Count(s => s.NgayThanhToan.HasValue);

            var table = new PdfPTable(4);
            table.WidthPercentage = 100;

            AddStatsCard(table, "Tổng số trainer", totalTrainers.ToString(), GYM_PRIMARY);
            AddStatsCard(table, "Tổng chi lương", $"{totalSalaries:N0} VNĐ", GYM_SUCCESS);
            AddStatsCard(table, "Đã thanh toán", paidSalaries.ToString(), GYM_SECONDARY);
            AddStatsCard(table, "Chưa thanh toán", (totalTrainers - paidSalaries).ToString(), BaseColor.ORANGE);

            document.Add(table);
            document.Add(new Paragraph(" "));
        }

        private void AddMonthlySalaryTable(Document document, IEnumerable<BangLuong> salaries)
        {
            var table = new PdfPTable(5);
            table.WidthPercentage = 100;
            table.SetWidths(new float[] { 25, 20, 20, 20, 15 });

            // Headers
            var headers = new[] { "Trainer", "Lương cơ bản", "Hoa hồng", "Tổng lương", "Trạng thái" };
            foreach (var header in headers)
            {
                table.AddCell(CreateHeaderCell(header, FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10)));
            }

            // Data rows
            foreach (var salary in salaries.OrderBy(s => s.Hlv?.Ho))
            {
                table.AddCell(new PdfPCell(new Phrase($"{salary.Hlv?.Ho} {salary.Hlv?.Ten}", FontFactory.GetFont(FontFactory.HELVETICA, 9))) { Padding = 5 });
                table.AddCell(new PdfPCell(new Phrase($"{salary.LuongCoBan:N0}", FontFactory.GetFont(FontFactory.HELVETICA, 9))) { Padding = 5, HorizontalAlignment = Element.ALIGN_RIGHT });
                table.AddCell(new PdfPCell(new Phrase($"{salary.TienHoaHong:N0}", FontFactory.GetFont(FontFactory.HELVETICA, 9))) { Padding = 5, HorizontalAlignment = Element.ALIGN_RIGHT });
                table.AddCell(new PdfPCell(new Phrase($"{salary.TongThanhToan:N0}", FontFactory.GetFont(FontFactory.HELVETICA, 9))) { Padding = 5, HorizontalAlignment = Element.ALIGN_RIGHT });
                
                var status = salary.NgayThanhToan.HasValue ? "Đã TT" : "Chưa TT";
                var statusColor = salary.NgayThanhToan.HasValue ? GYM_SUCCESS : BaseColor.ORANGE;
                table.AddCell(new PdfPCell(new Phrase(status, FontFactory.GetFont(FontFactory.HELVETICA, 9, statusColor))) { Padding = 5, HorizontalAlignment = Element.ALIGN_CENTER });
            }

            document.Add(table);
        }

        private void AddReportFooter(Document document)
        {
            document.Add(new Paragraph(" "));
            // Add line separator using paragraph with underline
            var line = new Paragraph("_".PadRight(60, '_'), FontFactory.GetFont(FontFactory.HELVETICA, 1, GYM_PRIMARY));
            line.Alignment = Element.ALIGN_CENTER;
            document.Add(line);
            
            var footer = new Paragraph("Báo cáo được tạo tự động bởi Hệ thống quản lý phòng Gym", 
                FontFactory.GetFont(FontFactory.HELVETICA_OBLIQUE, 9, BaseColor.GRAY));
            footer.Alignment = Element.ALIGN_CENTER;
            document.Add(footer);
        }

        // Helper methods
        private void AddTableRow(PdfPTable table, string label1, string value1, string label2, string value2)
        {
            table.AddCell(new PdfPCell(new Phrase(label1, FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10))) { Border = Rectangle.NO_BORDER, Padding = 3 });
            table.AddCell(new PdfPCell(new Phrase(value1, FontFactory.GetFont(FontFactory.HELVETICA, 10))) { Border = Rectangle.NO_BORDER, Padding = 3 });
        }

        private void AddSalaryRow(PdfPTable table, string label, decimal amount)
        {
            table.AddCell(new PdfPCell(new Phrase(label, FontFactory.GetFont(FontFactory.HELVETICA, 11))) { Padding = 8, BackgroundColor = GYM_LIGHT_GRAY });
            table.AddCell(new PdfPCell(new Phrase($"{amount:N0}", FontFactory.GetFont(FontFactory.HELVETICA, 11))) { Padding = 8, HorizontalAlignment = Element.ALIGN_RIGHT, BackgroundColor = GYM_LIGHT_GRAY });
        }

        private void AddStatsCard(PdfPTable table, string title, string value, BaseColor color)
        {
            var cell = new PdfPCell();
            cell.AddElement(new Paragraph(title, FontFactory.GetFont(FontFactory.HELVETICA, 9, BaseColor.GRAY)));
            cell.AddElement(new Paragraph(value, FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 14, color)));
            cell.Padding = 10;
            cell.HorizontalAlignment = Element.ALIGN_CENTER;
            table.AddCell(cell);
        }

        private PdfPCell CreateHeaderCell(string text, Font font)
        {
            var cell = new PdfPCell(new Phrase(text, font));
            cell.BackgroundColor = GYM_PRIMARY;
            cell.Padding = 8;
            cell.HorizontalAlignment = Element.ALIGN_CENTER;
            return cell;
        }

        #endregion
    }
}