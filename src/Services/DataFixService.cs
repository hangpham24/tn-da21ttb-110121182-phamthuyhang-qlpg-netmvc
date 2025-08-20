using GymManagement.Web.Data;
using GymManagement.Web.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace GymManagement.Web.Services
{
    /// <summary>
    /// Service để fix dữ liệu và tạo các records thiếu
    /// </summary>
    public class DataFixService
    {
        private readonly GymDbContext _context;
        private readonly ILogger<DataFixService> _logger;

        public DataFixService(GymDbContext context, ILogger<DataFixService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Tạo ThanhToan records cho các DangKy hiện có mà chưa có thanh toán
        /// </summary>
        public async Task<(int created, decimal totalAmount)> CreateMissingPaymentRecordsAsync()
        {
            _logger.LogInformation("🔧 Starting to create missing payment records...");

            // Lấy các đăng ký chưa có thanh toán
            var registrationsWithoutPayments = await _context.DangKys
                .Include(d => d.GoiTap)
                .Include(d => d.LopHoc)
                .Include(d => d.NguoiDung)
                .Where(d => !d.ThanhToans.Any() && d.PhiDangKy > 0)
                .ToListAsync();

            _logger.LogInformation("📊 Found {Count} registrations without payments", registrationsWithoutPayments.Count);

            var createdPayments = new List<ThanhToan>();
            decimal totalAmount = 0;

            foreach (var dangKy in registrationsWithoutPayments)
            {
                // Tính số tiền thanh toán
                decimal soTien = dangKy.PhiDangKy ?? 0;
                
                if (soTien <= 0)
                {
                    // Fallback pricing
                    if (dangKy.GoiTapId.HasValue && dangKy.GoiTap != null)
                    {
                        soTien = dangKy.GoiTap.Gia;
                    }
                    else if (dangKy.LopHocId.HasValue && dangKy.LopHoc != null)
                    {
                        soTien = dangKy.LopHoc.GiaTuyChinh ?? 200000m;
                    }
                    else
                    {
                        soTien = 100000m; // Default walk-in price
                    }
                }

                // Xác định trạng thái thanh toán
                string trangThai = dangKy.TrangThai switch
                {
                    "ACTIVE" => "SUCCESS",
                    "CANCELED" => "REFUND", 
                    _ => "PENDING"
                };

                // Tạo ghi chú
                string ghiChu = "BACKFILL - ";
                if (dangKy.GoiTapId.HasValue && dangKy.GoiTap != null)
                {
                    ghiChu += $"Gói tập: {dangKy.GoiTap.TenGoi}";
                }
                else if (dangKy.LopHocId.HasValue && dangKy.LopHoc != null)
                {
                    ghiChu += $"Lớp học: {dangKy.LopHoc.TenLop}";
                }
                else
                {
                    ghiChu += "Đăng ký tại quầy";
                }
                ghiChu += " - Tạo tự động từ đăng ký hiện có";

                var thanhToan = new ThanhToan
                {
                    DangKyId = dangKy.DangKyId,
                    SoTien = soTien,
                    NgayThanhToan = dangKy.NgayTao,
                    PhuongThuc = "CASH", // Giả định thanh toán tiền mặt cho đăng ký cũ
                    TrangThai = trangThai,
                    GhiChu = ghiChu
                };

                _context.ThanhToans.Add(thanhToan);
                createdPayments.Add(thanhToan);
                totalAmount += soTien;

                _logger.LogInformation("💰 Created payment for DangKy {DangKyId}: {Amount:N0} VND - {Status}", 
                    dangKy.DangKyId, soTien, trangThai);
            }

            if (createdPayments.Any())
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("✅ Successfully created {Count} payment records, total amount: {Total:N0} VND", 
                    createdPayments.Count, totalAmount);
            }
            else
            {
                _logger.LogInformation("ℹ️ No missing payment records found to create");
            }

            return (createdPayments.Count, totalAmount);
        }

        /// <summary>
        /// Kiểm tra và báo cáo trạng thái dữ liệu thanh toán
        /// </summary>
        public async Task<object> GetPaymentDataStatusAsync()
        {
            var dangKyCount = await _context.DangKys.CountAsync();
            var dangKyWithFee = await _context.DangKys.Where(d => d.PhiDangKy > 0).CountAsync();
            var dangKyTotalFee = await _context.DangKys.Where(d => d.PhiDangKy > 0).SumAsync(d => d.PhiDangKy ?? 0);
            
            var thanhToanCount = await _context.ThanhToans.CountAsync();
            var thanhToanSuccess = await _context.ThanhToans.Where(t => t.TrangThai == "SUCCESS").CountAsync();
            var thanhToanTotalAmount = await _context.ThanhToans.SumAsync(t => t.SoTien);
            var thanhToanSuccessAmount = await _context.ThanhToans.Where(t => t.TrangThai == "SUCCESS").SumAsync(t => t.SoTien);

            var dangKyWithoutPayment = await _context.DangKys
                .Where(d => !d.ThanhToans.Any() && d.PhiDangKy > 0)
                .CountAsync();

            return new
            {
                DangKy = new
                {
                    Total = dangKyCount,
                    WithFee = dangKyWithFee,
                    TotalFeeAmount = dangKyTotalFee,
                    WithoutPayment = dangKyWithoutPayment
                },
                ThanhToan = new
                {
                    Total = thanhToanCount,
                    Success = thanhToanSuccess,
                    TotalAmount = thanhToanTotalAmount,
                    SuccessAmount = thanhToanSuccessAmount
                }
            };
        }
    }
}
