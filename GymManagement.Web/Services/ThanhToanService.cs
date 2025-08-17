using GymManagement.Web.Data;
using GymManagement.Web.Data.Models;
using GymManagement.Web.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text;
using System.Security.Cryptography;
using System.Web;

namespace GymManagement.Web.Services
{
    public class ThanhToanService : IThanhToanService
    {
        private readonly IThanhToanRepository _thanhToanRepository;
        private readonly IDangKyRepository _dangKyRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IThongBaoService _thongBaoService;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ThanhToanService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        public ThanhToanService(
            IThanhToanRepository thanhToanRepository,
            IDangKyRepository dangKyRepository,
            IUnitOfWork unitOfWork,
            IThongBaoService thongBaoService,
            IEmailService emailService,
            IConfiguration configuration,
            ILogger<ThanhToanService> logger,
            IHttpClientFactory httpClientFactory)
        {
            _thanhToanRepository = thanhToanRepository;
            _dangKyRepository = dangKyRepository;
            _unitOfWork = unitOfWork;
            _thongBaoService = thongBaoService;
            _emailService = emailService;
            _configuration = configuration;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<IEnumerable<ThanhToan>> GetAllAsync()
        {
            return await _thanhToanRepository.GetAllAsync();
        }

        public async Task<ThanhToan?> GetByIdAsync(int id)
        {
            return await _thanhToanRepository.GetByIdAsync(id);
        }

        public async Task<ThanhToan> CreateAsync(ThanhToan thanhToan)
        {
            var created = await _thanhToanRepository.AddAsync(thanhToan);
            await _unitOfWork.SaveChangesAsync();
            return created;
        }

        public async Task<ThanhToan> UpdateAsync(ThanhToan thanhToan)
        {
            await _thanhToanRepository.UpdateAsync(thanhToan);
            await _unitOfWork.SaveChangesAsync();
            return thanhToan;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var thanhToan = await _thanhToanRepository.GetByIdAsync(id);
            if (thanhToan == null) return false;

            await _thanhToanRepository.DeleteAsync(thanhToan);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<ThanhToan>> GetByRegistrationIdAsync(int dangKyId)
        {
            return await _thanhToanRepository.GetByDangKyIdAsync(dangKyId);
        }

        public async Task<IEnumerable<ThanhToan>> GetByMemberIdAsync(int memberId)
        {
            // Get all payments for registrations belonging to this member
            var memberRegistrations = await _dangKyRepository.GetByMemberIdAsync(memberId);
            var registrationIds = memberRegistrations.Select(r => r.DangKyId).ToList();
            
            var allPayments = new List<ThanhToan>();
            foreach (var registrationId in registrationIds)
            {
                var payments = await _thanhToanRepository.GetByDangKyIdAsync(registrationId);
                allPayments.AddRange(payments);
            }
            
            return allPayments.OrderByDescending(p => p.NgayThanhToan);
        }

        public async Task<IEnumerable<ThanhToan>> GetPendingPaymentsAsync()
        {
            return await _thanhToanRepository.GetPendingPaymentsAsync();
        }

        public async Task<IEnumerable<ThanhToan>> GetSuccessfulPaymentsAsync()
        {
            return await _thanhToanRepository.GetSuccessfulPaymentsAsync();
        }

        public async Task<ThanhToan> CreatePaymentAsync(int dangKyId, decimal soTien, string phuongThuc, string? ghiChu = null)
        {
            var thanhToan = new ThanhToan
            {
                DangKyId = dangKyId,
                SoTien = soTien,
                PhuongThuc = phuongThuc,
                TrangThai = "PENDING",
                NgayThanhToan = DateTime.Now,
                GhiChu = ghiChu
            };

            var created = await _thanhToanRepository.AddAsync(thanhToan);
            await _unitOfWork.SaveChangesAsync();
            return created;
        }

        public async Task<bool> ProcessCashPaymentAsync(int thanhToanId)
        {
            var thanhToan = await _thanhToanRepository.GetPaymentWithGatewayAsync(thanhToanId);
            if (thanhToan == null || thanhToan.TrangThai != "PENDING") return false;

            thanhToan.TrangThai = "SUCCESS";
            thanhToan.NgayThanhToan = DateTime.Now;

            // ✅ SEND REAL-TIME NOTIFICATION
            await SendPaymentNotificationAsync(thanhToan);

            // Activate pending registration if exists
            if (thanhToan.DangKyId.HasValue)
            {
                var registration = await _unitOfWork.Context.DangKys
                    .FirstOrDefaultAsync(d => d.DangKyId == thanhToan.DangKyId.Value);

                if (registration != null && registration.TrangThai == "PENDING_PAYMENT")
                {
                    registration.TrangThai = "ACTIVE";
                    registration.TrangThaiChiTiet = "Thanh toán tiền mặt thành công";
                }
            }

            await _unitOfWork.SaveChangesAsync();

            // Send notifications
            await SendPaymentSuccessNotifications(thanhToan);

            return true;
        }

        // Simplified method to work with new VNPay Area
        public async Task<string> CreateVnPayUrlAsync(int thanhToanId, string returnUrl)
        {
            // This will be handled by JavaScript calling VNPay Area directly
            // Return the VNPay Area endpoint URL
            return $"/VNPayAPI/Home/CreatePayment";
        }

        // Method to get registration info from payment for controller to process
        public async Task<(string registrationType, Dictionary<string, string> registrationInfo)?> GetRegistrationInfoFromPaymentAsync(int thanhToanId)
        {
            var gateway = await _unitOfWork.Context.ThanhToanGateways
                .FirstOrDefaultAsync(g => g.ThanhToanId == thanhToanId);
                
            if (gateway?.GatewayMessage == null) return null;

            var parts = gateway.GatewayMessage.Split('|');
            if (parts.Length < 3) return null;

            var type = parts[0];
            var info = new Dictionary<string, string>();

            switch (type)
            {
                case "PKG":
                    if (parts.Length >= 4)
                    {
                        info["nguoiDungId"] = parts[1];
                        info["goiTapId"] = parts[2];
                        info["thoiHanThang"] = parts[3];
                        if (parts.Length > 4) info["khuyenMaiId"] = parts[4];
                    }
                    break;
                case "CLS":
                    if (parts.Length >= 5)
                    {
                        info["nguoiDungId"] = parts[1];
                        info["lopHocId"] = parts[2];
                        info["ngayBatDau"] = parts[3];
                        info["ngayKetThuc"] = parts[4];
                    }
                    break;
                case "FIX":
                    if (parts.Length >= 3)
                    {
                        info["nguoiDungId"] = parts[1];
                        info["lopHocId"] = parts[2];
                    }
                    break;
            }

            return (type, info);
        }

        public async Task<ThanhToanGateway?> GetGatewayByOrderIdAsync(string orderId)
        {
            return await _unitOfWork.Context.ThanhToanGateways
                .Include(g => g.ThanhToan)
                .FirstOrDefaultAsync(g => g.GatewayOrderId == orderId);
        }

        public async Task<ThanhToan?> GetPaymentWithRegistrationAsync(int thanhToanId)
        {
            return await _unitOfWork.Context.ThanhToans
                .Include(t => t.DangKy)
                    .ThenInclude(d => d.NguoiDung)
                .Include(t => t.DangKy)
                    .ThenInclude(d => d.GoiTap)
                .Include(t => t.DangKy)
                    .ThenInclude(d => d.LopHoc)
                .FirstOrDefaultAsync(t => t.ThanhToanId == thanhToanId);
        }

        public async Task<bool> RefundPaymentAsync(int thanhToanId, string reason)
        {
            var thanhToan = await _thanhToanRepository.GetByIdAsync(thanhToanId);
            if (thanhToan == null || thanhToan.TrangThai != "SUCCESS") return false;

            thanhToan.TrangThai = "REFUND";
            thanhToan.GhiChu = $"Hoàn tiền: {reason}";
            await _unitOfWork.SaveChangesAsync();

            // Send notification
            if (thanhToan.DangKyId.HasValue)
            {
                var dangKy = await _dangKyRepository.GetByIdAsync(thanhToan.DangKyId.Value);
            if (dangKy != null)
            {
                await _thongBaoService.CreateNotificationAsync(
                    dangKy.NguoiDungId,
                    "Hoàn tiền",
                        $"Đã hoàn tiền {thanhToan.SoTien:N0} VNĐ cho đăng ký. Lý do: {reason}",
                        "payment"
                );
                }
            }

            return true;
        }

        public async Task<decimal> GetTotalRevenueAsync(DateTime startDate, DateTime endDate)
        {
            return await _unitOfWork.Context.ThanhToans
                .Where(t => t.TrangThai == "SUCCESS" &&
                           t.NgayThanhToan >= startDate &&
                           t.NgayThanhToan <= endDate)
                .SumAsync(t => t.SoTien);
        }

        // New methods for payment-first registration flow

        public async Task<ThanhToan> CreatePaymentForPackageRegistrationAsync(int nguoiDungId, int goiTapId, int thoiHanThang, string phuongThuc, int? khuyenMaiId = null)
        {
            // Calculate fee manually without DangKyService to avoid circular dependency
            var goiTap = await _unitOfWork.Context.GoiTaps.FindAsync(goiTapId);
            if (goiTap == null) throw new ArgumentException("Gói tập không tồn tại");

            // ✅ FIXED: goiTap.Gia is already the total price for the entire package duration
            // For custom duration, calculate based on monthly rate
            decimal fee;
            if (thoiHanThang == goiTap.ThoiHanThang)
            {
                // Standard package duration - use package price directly
                fee = goiTap.Gia;
            }
            else
            {
                // Custom duration - calculate based on monthly rate
                var monthlyRate = goiTap.Gia / goiTap.ThoiHanThang;
                fee = monthlyRate * thoiHanThang;
            }

            // Apply discount if available
            if (khuyenMaiId.HasValue)
            {
                var khuyenMai = await _unitOfWork.Context.KhuyenMais.FindAsync(khuyenMaiId.Value);
                if (khuyenMai != null && khuyenMai.KichHoat &&
                    DateOnly.FromDateTime(DateTime.Today) >= khuyenMai.NgayBatDau && 
                    DateOnly.FromDateTime(DateTime.Today) <= khuyenMai.NgayKetThuc)
                {
                    decimal discount = fee * (khuyenMai.PhanTramGiam ?? 0) / 100;
                    fee -= discount;
                }
            }

            // Create a temporary pending registration first  
            var tempRegistration = new DangKy
            {
                NguoiDungId = nguoiDungId,
                GoiTapId = goiTapId,
                NgayTao = DateTime.Today,
                NgayBatDau = DateOnly.FromDateTime(DateTime.Today),
                NgayKetThuc = DateOnly.FromDateTime(DateTime.Today.AddMonths(thoiHanThang)),
                TrangThai = "PENDING_PAYMENT",
                PhiDangKy = fee,
                TrangThaiChiTiet = $"Chờ thanh toán - {thoiHanThang} tháng"
            };

            await _unitOfWork.Context.DangKys.AddAsync(tempRegistration);
            await _unitOfWork.SaveChangesAsync();
            
            var thanhToan = new ThanhToan
            {
                DangKyId = tempRegistration.DangKyId, // Use the temp registration ID
                SoTien = fee,
                PhuongThuc = phuongThuc,
                TrangThai = "PENDING",
                NgayThanhToan = DateTime.Now,
                GhiChu = $"Thanh toán đăng ký gói tập - Người dùng: {nguoiDungId}, Gói: {goiTapId}, Thời hạn: {thoiHanThang} tháng"
            };

            var created = await _thanhToanRepository.AddAsync(thanhToan);
            await _unitOfWork.SaveChangesAsync();

            // Gateway record will be created by VNPay Area when payment URL is generated

            return created;
        }

        public async Task<ThanhToan> CreatePaymentForClassRegistrationAsync(int nguoiDungId, int lopHocId, DateTime ngayBatDau, DateTime ngayKetThuc, string phuongThuc)
        {
            // Calculate fee manually
            var lopHoc = await _unitOfWork.Context.LopHocs.FindAsync(lopHocId);
            if (lopHoc == null) throw new ArgumentException("Lớp học không tồn tại");

            decimal fee = lopHoc.GiaTuyChinh ?? 200000m; // Default class fee 200k VND

            // Create a temporary pending registration first
            var tempRegistration = new DangKy
            {
                NguoiDungId = nguoiDungId,
                LopHocId = lopHocId,
                NgayTao = DateTime.Today,
                NgayBatDau = DateOnly.FromDateTime(ngayBatDau),
                NgayKetThuc = DateOnly.FromDateTime(ngayKetThuc),
                TrangThai = "PENDING_PAYMENT",
                PhiDangKy = fee,
                TrangThaiChiTiet = "Chờ thanh toán lớp học"
            };

            await _unitOfWork.Context.DangKys.AddAsync(tempRegistration);
            await _unitOfWork.SaveChangesAsync();
            
            var thanhToan = new ThanhToan
            {
                DangKyId = tempRegistration.DangKyId, // Use the temp registration ID
                SoTien = fee,
                PhuongThuc = phuongThuc,
                TrangThai = "PENDING",
                NgayThanhToan = DateTime.Now,
                GhiChu = $"Thanh toán đăng ký lớp học - Người dùng: {nguoiDungId}, Lớp: {lopHocId}"
            };

            var created = await _thanhToanRepository.AddAsync(thanhToan);
            await _unitOfWork.SaveChangesAsync();

            // Gateway record will be created by VNPay Area when payment URL is generated

            return created;
        }

        public async Task<ThanhToan> CreatePaymentForFixedClassRegistrationAsync(int nguoiDungId, int lopHocId, string phuongThuc)
        {
            // Calculate fee manually
            var lopHoc = await _unitOfWork.Context.LopHocs.FindAsync(lopHocId);
            if (lopHoc == null) throw new ArgumentException("Lớp học không tồn tại");

            decimal fee = lopHoc.GiaTuyChinh ?? 200000m; // Default class fee 200k VND

            // Create a temporary pending registration first
            var tempRegistration = new DangKy
            {
                NguoiDungId = nguoiDungId,
                LopHocId = lopHocId,
                NgayTao = DateTime.Today,
                NgayBatDau = lopHoc.NgayBatDauKhoa ?? DateOnly.FromDateTime(DateTime.Today),
                NgayKetThuc = lopHoc.NgayKetThucKhoa ?? DateOnly.FromDateTime(DateTime.Today.AddMonths(3)),
                TrangThai = "PENDING_PAYMENT",
                PhiDangKy = fee,
                TrangThaiChiTiet = "Chờ thanh toán lớp cố định"
            };

            await _unitOfWork.Context.DangKys.AddAsync(tempRegistration);
            await _unitOfWork.SaveChangesAsync();
            
            var thanhToan = new ThanhToan
            {
                DangKyId = tempRegistration.DangKyId, // Use the temp registration ID
                SoTien = fee,
                PhuongThuc = phuongThuc,
                TrangThai = "PENDING",
                NgayThanhToan = DateTime.Now,
                GhiChu = $"Thanh toán đăng ký lớp học cố định - Người dùng: {nguoiDungId}, Lớp: {lopHocId}"
            };

            var created = await _thanhToanRepository.AddAsync(thanhToan);
            await _unitOfWork.SaveChangesAsync();

            // Gateway record will be created by VNPay Area when payment URL is generated

            return created;
        }

        private async Task SendPaymentSuccessNotifications(ThanhToan thanhToan)
        {
            if (thanhToan.DangKyId.HasValue)
            {
                var dangKy = await _dangKyRepository.GetByIdAsync(thanhToan.DangKyId.Value);
            if (dangKy?.NguoiDung != null)
            {
                // Send in-app notification
                await _thongBaoService.CreateNotificationAsync(
                    dangKy.NguoiDungId,
                    "Thanh toán thành công",
                        $"Thanh toán {thanhToan.SoTien:N0} VNĐ đã được xử lý thành công.",
                        "payment"
                    );
                }
            }
        }

        // Method for renewal payment
        public async Task<ThanhToan> CreatePaymentForRenewalAsync(int dangKyId, int renewalMonths, string phuongThuc)
        {
            // Get registration and calculate fee
            var dangKy = await _unitOfWork.Context.DangKys
                .Include(d => d.GoiTap)
                .FirstOrDefaultAsync(d => d.DangKyId == dangKyId);

            if (dangKy?.GoiTap == null) throw new ArgumentException("Đăng ký không tồn tại");

            // Calculate renewal fee based on current package price
            var monthlyRate = dangKy.GoiTap.Gia / dangKy.GoiTap.ThoiHanThang;
            var renewalFee = monthlyRate * renewalMonths;

            var thanhToan = new ThanhToan
            {
                DangKyId = dangKyId,
                SoTien = renewalFee,
                PhuongThuc = phuongThuc,
                TrangThai = "PENDING",
                NgayThanhToan = DateTime.Now,
                GhiChu = $"Gia hạn gói tập {dangKy.GoiTap.TenGoi} - {renewalMonths} tháng"
            };

            var created = await _thanhToanRepository.AddAsync(thanhToan);
            await _unitOfWork.SaveChangesAsync();

            return created;
        }

        // HMAC method moved to VNPay Area

        #region ✅ REAL-TIME NOTIFICATIONS

        /// <summary>
        /// Send real-time payment notification
        /// </summary>
        private async Task SendPaymentNotificationAsync(ThanhToan payment)
        {
            try
            {
                var httpClient = _httpClientFactory.CreateClient();

                var notificationData = new
                {
                    Amount = payment.SoTien,
                    Method = payment.PhuongThuc ?? "Unknown",
                    CustomerName = payment.DangKy?.NguoiDung?.Ho + " " + payment.DangKy?.NguoiDung?.Ten ?? "Unknown"
                };

                var json = JsonSerializer.Serialize(notificationData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // Send to notification API (fire and forget)
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await httpClient.PostAsync("/api/notifications/payment", content);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to send payment notification");
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error preparing payment notification");
            }
        }

        /// <summary>
        /// Send real-time revenue notification
        /// </summary>
        private async Task SendRevenueNotificationAsync(decimal amount, string period = "hôm nay")
        {
            try
            {
                var httpClient = _httpClientFactory.CreateClient();

                var notificationData = new
                {
                    Amount = amount,
                    Period = period
                };

                var json = JsonSerializer.Serialize(notificationData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // Send to notification API (fire and forget)
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await httpClient.PostAsync("/api/notifications/revenue", content);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to send revenue notification");
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error preparing revenue notification");
            }
        }

        #endregion
    }
}
