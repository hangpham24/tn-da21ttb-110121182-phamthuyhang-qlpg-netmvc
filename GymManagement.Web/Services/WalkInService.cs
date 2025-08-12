using GymManagement.Web.Data;
using GymManagement.Web.Data.Models;
using GymManagement.Web.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace GymManagement.Web.Services
{
    /// <summary>
    /// Service xử lý nghiệp vụ khách vãng lai (Walk-in customers)
    /// </summary>
    public class WalkInService : IWalkInService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly INguoiDungRepository _nguoiDungRepository;
        private readonly IDangKyRepository _dangKyRepository;
        private readonly IThanhToanRepository _thanhToanRepository;
        private readonly IDiemDanhRepository _diemDanhRepository;
        private readonly IGoiTapRepository _goiTapRepository;
        private readonly IConfiguration _configuration;
        private readonly ILogger<WalkInService> _logger;

        // Giá cố định cho khách vãng lai
        private readonly decimal _fixedPrice;
        private readonly string _fixedPackageName;
        private readonly string _fixedPackageDescription;

        public WalkInService(
            IUnitOfWork unitOfWork,
            INguoiDungRepository nguoiDungRepository,
            IDangKyRepository dangKyRepository,
            IThanhToanRepository thanhToanRepository,
            IDiemDanhRepository diemDanhRepository,
            IGoiTapRepository goiTapRepository,
            IConfiguration configuration,
            ILogger<WalkInService> logger)
        {
            _unitOfWork = unitOfWork;
            _nguoiDungRepository = nguoiDungRepository;
            _dangKyRepository = dangKyRepository;
            _thanhToanRepository = thanhToanRepository;
            _diemDanhRepository = diemDanhRepository;
            _goiTapRepository = goiTapRepository;
            _configuration = configuration;
            _logger = logger;

            // Đọc cấu hình giá cố định
            _fixedPrice = _configuration.GetValue<decimal>("WalkIn:FixedPrice:Amount", 15000);
            _fixedPackageName = _configuration.GetValue<string>("WalkIn:FixedPrice:PackageName", "Vé tập một buổi") ?? "Vé tập một buổi";
            _fixedPackageDescription = _configuration.GetValue<string>("WalkIn:FixedPrice:Description", "Vé tập một buổi trong ngày với giá cố định") ?? "Vé tập một buổi trong ngày với giá cố định";
        }

        public async Task<NguoiDung> CreateGuestAsync(string hoTen, string? soDienThoai = null, string? email = null)
        {
            try
            {
                _logger.LogInformation("Creating walk-in guest: {HoTen}, Phone: {Phone}", hoTen, soDienThoai);

                // Tách họ và tên
                var nameParts = hoTen.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var ho = nameParts.Length > 1 ? string.Join(" ", nameParts.Take(nameParts.Length - 1)) : "";
                var ten = nameParts.LastOrDefault() ?? hoTen;

                var guest = new NguoiDung
                {
                    LoaiNguoiDung = "VANGLAI",
                    Ho = ho,
                    Ten = ten,
                    SoDienThoai = soDienThoai,
                    Email = email,
                    NgayThamGia = DateOnly.FromDateTime(DateTime.Today),
                    TrangThai = "ACTIVE",
                    NgayTao = DateTime.Now
                };

                await _nguoiDungRepository.AddAsync(guest);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Successfully created walk-in guest with ID: {GuestId}", guest.NguoiDungId);
                return guest;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating walk-in guest: {HoTen}", hoTen);
                throw;
            }
        }

        public async Task<NguoiDung?> GetExistingGuestTodayAsync(string soDienThoai)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(soDienThoai))
                    return null;

                var today = DateOnly.FromDateTime(DateTime.Today);
                
                return await _unitOfWork.Context.NguoiDungs
                    .Where(nd => nd.LoaiNguoiDung == "VANGLAI" && 
                                nd.SoDienThoai == soDienThoai &&
                                nd.NgayThamGia == today)
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking existing guest for phone: {Phone}", soDienThoai);
                return null;
            }
        }

        /// <summary>
        /// Tạo vé tập với giá cố định cho khách vãng lai
        /// </summary>
        public async Task<DangKy> CreateFixedPricePassAsync(int guestId)
        {
            try
            {
                _logger.LogInformation("Creating fixed price pass for guest {GuestId}: {Price} VND", guestId, _fixedPrice);

                var today = DateOnly.FromDateTime(DateTime.Today);

                var dangKy = new DangKy
                {
                    NguoiDungId = guestId,
                    LoaiDangKy = "WALKIN_FIXED", // Loại đăng ký mới cho giá cố định
                    NgayBatDau = today,
                    NgayKetThuc = today, // Vé trong ngày
                    PhiDangKy = _fixedPrice,
                    TrangThai = "PENDING_PAYMENT",
                    NgayTao = DateTime.Now
                };

                await _dangKyRepository.AddAsync(dangKy);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Successfully created fixed price pass with ID: {DangKyId}", dangKy.DangKyId);
                return dangKy;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating fixed price pass for guest {GuestId}", guestId);
                throw;
            }
        }

        /// <summary>
        /// Backward compatibility - sẽ sử dụng giá cố định
        /// </summary>
        [Obsolete("Use CreateFixedPricePassAsync instead")]
        public async Task<DangKy> CreateDayPassAsync(int guestId, string packageType, string packageName, decimal price, int durationHours = 24)
        {
            // Chuyển hướng sang method mới với giá cố định
            return await CreateFixedPricePassAsync(guestId);
        }

        public async Task<ThanhToan> ProcessWalkInPaymentAsync(int dangKyId, string phuongThuc, string? ghiChu = null)
        {
            try
            {
                _logger.LogInformation("Processing walk-in payment for registration {DangKyId}, method: {Method}", dangKyId, phuongThuc);

                var dangKy = await _dangKyRepository.GetByIdAsync(dangKyId);
                if (dangKy == null)
                    throw new ArgumentException($"Không tìm thấy đăng ký với ID: {dangKyId}");

                var payment = new ThanhToan
                {
                    DangKyId = dangKyId,
                    SoTien = dangKy.PhiDangKy ?? 0,
                    PhuongThuc = phuongThuc,
                    TrangThai = phuongThuc == "CASH" ? "SUCCESS" : "PENDING", // CASH thì SUCCESS ngay
                    NgayThanhToan = DateTime.Now,
                    GhiChu = ghiChu ?? $"WALKIN - Day Pass"
                };

                await _thanhToanRepository.AddAsync(payment);

                // Nếu thanh toán CASH thì kích hoạt đăng ký ngay
                if (phuongThuc == "CASH")
                {
                    dangKy.TrangThai = "ACTIVE";
                    await _dangKyRepository.UpdateAsync(dangKy);
                }

                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Successfully processed payment with ID: {PaymentId}", payment.ThanhToanId);
                return payment;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing walk-in payment for registration {DangKyId}", dangKyId);
                throw;
            }
        }

        public async Task<bool> ConfirmPaymentAsync(int paymentId)
        {
            try
            {
                _logger.LogInformation("Confirming payment {PaymentId}", paymentId);

                var payment = await _thanhToanRepository.GetByIdAsync(paymentId);
                if (payment == null)
                {
                    _logger.LogWarning("Payment not found: {PaymentId}", paymentId);
                    return false;
                }

                if (payment.TrangThai == "SUCCESS")
                {
                    _logger.LogInformation("Payment {PaymentId} already confirmed", paymentId);
                    return true;
                }

                // Cập nhật trạng thái thanh toán
                payment.TrangThai = "SUCCESS";
                await _thanhToanRepository.UpdateAsync(payment);

                // Kích hoạt đăng ký
                if (payment.DangKyId.HasValue)
                {
                    var dangKy = await _dangKyRepository.GetByIdAsync(payment.DangKyId.Value);
                    if (dangKy != null)
                    {
                        dangKy.TrangThai = "ACTIVE";
                        await _dangKyRepository.UpdateAsync(dangKy);
                    }
                }

                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Successfully confirmed payment {PaymentId}", paymentId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error confirming payment {PaymentId}", paymentId);
                return false;
            }
        }

        public async Task<DiemDanh> CheckInGuestAsync(int guestId, string? ghiChu = null)
        {
            try
            {
                _logger.LogInformation("Checking in guest {GuestId}", guestId);

                var diemDanh = new DiemDanh
                {
                    ThanhVienId = guestId,
                    ThoiGianCheckIn = DateTime.Now,
                    LoaiCheckIn = "Manual",
                    GhiChu = ghiChu ?? "WALKIN_DAYPASS",
                    TrangThai = "Present"
                };

                await _diemDanhRepository.AddAsync(diemDanh);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Successfully checked in guest {GuestId} with attendance ID: {DiemDanhId}", guestId, diemDanh.DiemDanhId);
                return diemDanh;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking in guest {GuestId}", guestId);
                throw;
            }
        }

        public async Task<bool> CheckOutGuestAsync(int diemDanhId)
        {
            try
            {
                _logger.LogInformation("Checking out guest with attendance ID: {DiemDanhId}", diemDanhId);

                var diemDanh = await _diemDanhRepository.GetByIdAsync(diemDanhId);
                if (diemDanh == null)
                {
                    _logger.LogWarning("Attendance record not found: {DiemDanhId}", diemDanhId);
                    return false;
                }

                if (diemDanh.ThoiGianCheckOut.HasValue)
                {
                    _logger.LogInformation("Guest already checked out: {DiemDanhId}", diemDanhId);
                    return true;
                }

                diemDanh.ThoiGianCheckOut = DateTime.Now;
                diemDanh.TrangThai = "Completed";
                await _diemDanhRepository.UpdateAsync(diemDanh);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Successfully checked out guest with attendance ID: {DiemDanhId}", diemDanhId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking out guest with attendance ID: {DiemDanhId}", diemDanhId);
                return false;
            }
        }

        public async Task<List<WalkInSessionInfo>> GetTodayWalkInsAsync(DateTime? date = null)
        {
            try
            {
                var targetDate = date ?? DateTime.Today;
                _logger.LogInformation("Getting walk-in sessions for date: {Date}", targetDate.ToString("yyyy-MM-dd"));

                var sessions = await _unitOfWork.Context.DiemDanhs
                    .Include(d => d.ThanhVien)
                    .Where(d => d.ThanhVien!.LoaiNguoiDung == "VANGLAI" &&
                               d.ThoiGianCheckIn.Date == targetDate.Date)
                    .Select(d => new WalkInSessionInfo
                    {
                        DiemDanhId = d.DiemDanhId,
                        GuestId = d.ThanhVienId!.Value,
                        GuestName = $"{d.ThanhVien!.Ho} {d.ThanhVien.Ten}".Trim(),
                        PhoneNumber = d.ThanhVien.SoDienThoai,
                        PackageName = d.GhiChu ?? "Vé ngày",
                        CheckInTime = d.ThoiGianCheckIn,
                        CheckOutTime = d.ThoiGianCheckOut,
                        Status = d.ThoiGianCheckOut.HasValue ? "Completed" : "Active"
                    })
                    .OrderByDescending(s => s.CheckInTime)
                    .ToListAsync();

                _logger.LogInformation("Found {Count} walk-in sessions for date: {Date}", sessions.Count, targetDate.ToString("yyyy-MM-dd"));
                return sessions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting walk-in sessions for date: {Date}", date?.ToString("yyyy-MM-dd") ?? "today");
                return new List<WalkInSessionInfo>();
            }
        }

        public async Task<WalkInSessionInfo?> GetActiveSessionAsync(int guestId)
        {
            try
            {
                var session = await _unitOfWork.Context.DiemDanhs
                    .Include(d => d.ThanhVien)
                    .Where(d => d.ThanhVienId == guestId &&
                               d.ThoiGianCheckOut == null &&
                               d.ThoiGianCheckIn.Date == DateTime.Today)
                    .Select(d => new WalkInSessionInfo
                    {
                        DiemDanhId = d.DiemDanhId,
                        GuestId = d.ThanhVienId!.Value,
                        GuestName = $"{d.ThanhVien!.Ho} {d.ThanhVien.Ten}".Trim(),
                        PhoneNumber = d.ThanhVien.SoDienThoai,
                        PackageName = d.GhiChu ?? "Vé ngày",
                        CheckInTime = d.ThoiGianCheckIn,
                        CheckOutTime = d.ThoiGianCheckOut,
                        Status = "Active"
                    })
                    .FirstOrDefaultAsync();

                return session;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active session for guest {GuestId}", guestId);
                return null;
            }
        }

        /// <summary>
        /// Lấy thông tin gói vé cố định cho khách vãng lai
        /// </summary>
        public async Task<List<GoiTap>> GetAvailablePackagesAsync()
        {
            try
            {
                // Trả về gói vé cố định thay vì query database
                var fixedPackage = new GoiTap
                {
                    GoiTapId = 0, // ID đặc biệt cho gói cố định
                    TenGoi = _fixedPackageName,
                    Gia = _fixedPrice,
                    MoTa = _fixedPackageDescription,
                    ThoiHanThang = 0
                    // Không có TrangThai property trong GoiTap model
                };

                return new List<GoiTap> { fixedPackage };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting fixed price package");
                return new List<GoiTap>();
            }
        }

        /// <summary>
        /// Lấy thông tin gói vé cố định
        /// </summary>
        public (decimal Price, string Name, string Description) GetFixedPriceInfo()
        {
            return (_fixedPrice, _fixedPackageName, _fixedPackageDescription);
        }

        /// <summary>
        /// Đăng ký khách vãng lai với thanh toán (All-in-one method)
        /// </summary>
        public async Task<WalkInRegistrationResult> RegisterWalkInWithPaymentAsync(
            string fullName,
            string phoneNumber,
            string? email,
            string? note,
            string paymentMethod,
            decimal amount)
        {
            try
            {
                _logger.LogInformation("Registering walk-in with payment: {Name}, Method: {Method}, Amount: {Amount}",
                    fullName, paymentMethod, amount);

                // 1. Tạo khách vãng lai
                var guest = await CreateGuestAsync(fullName, phoneNumber, email);

                // 2. Tạo vé với giá cố định
                var dayPass = await CreateFixedPricePassAsync(guest.NguoiDungId);

                // 3. Xử lý thanh toán
                var payment = await ProcessWalkInPaymentAsync(
                    dayPass.DangKyId,
                    paymentMethod,
                    note ?? $"WALKIN - {_fixedPackageName}");

                // 4. Tự động check-in nếu thanh toán thành công
                DiemDanh? checkIn = null;
                if (payment.TrangThai == "SUCCESS")
                {
                    checkIn = await CheckInGuestAsync(guest.NguoiDungId, note ?? "Auto check-in after payment");
                }

                _logger.LogInformation("Walk-in registration with payment completed for {Name}", fullName);

                return new WalkInRegistrationResult
                {
                    Success = true,
                    GuestId = guest.NguoiDungId,
                    TransactionId = payment.ThanhToanId.ToString(),
                    Message = "Đăng ký và thanh toán thành công"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in walk-in registration with payment for {Name}", fullName);
                return new WalkInRegistrationResult
                {
                    Success = false,
                    Message = "Có lỗi xảy ra khi đăng ký và thanh toán"
                };
            }
        }

        /// <summary>
        /// Tạo thanh toán VNPay cho khách vãng lai
        /// </summary>
        public async Task<WalkInVNPayResult> CreateVNPayPaymentAsync(
            string fullName,
            string phoneNumber,
            string? email,
            string? note,
            decimal amount)
        {
            try
            {
                _logger.LogInformation("Creating VNPay payment for walk-in: {Name}, Amount: {Amount}", fullName, amount);

                // 1. Tạo khách vãng lai
                var guest = await CreateGuestAsync(fullName, phoneNumber, email);

                // 2. Tạo vé với giá cố định
                var dayPass = await CreateFixedPricePassAsync(guest.NguoiDungId);

                // 3. Tạo thanh toán VNPay (PENDING)
                var payment = await ProcessWalkInPaymentAsync(
                    dayPass.DangKyId,
                    "VNPAY",
                    note ?? $"WALKIN - {_fixedPackageName}");

                // 4. Tạo VNPay URL (giả lập - cần tích hợp thật)
                var orderId = $"WALKIN_{payment.ThanhToanId}_{DateTime.Now:yyyyMMddHHmmss}";
                var paymentUrl = $"/VNPay/Payment?orderId={orderId}&amount={amount}&description=WALKIN_{guest.NguoiDungId}";

                _logger.LogInformation("VNPay payment created for walk-in: {Name}, OrderId: {OrderId}", fullName, orderId);

                return new WalkInVNPayResult
                {
                    Success = true,
                    PaymentUrl = paymentUrl,
                    OrderId = orderId,
                    Message = "Tạo thanh toán VNPay thành công"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating VNPay payment for walk-in: {Name}", fullName);
                return new WalkInVNPayResult
                {
                    Success = false,
                    Message = "Có lỗi xảy ra khi tạo thanh toán VNPay"
                };
            }
        }

        public async Task<WalkInStats> GetWalkInStatsAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                _logger.LogInformation("Getting walk-in stats from {StartDate} to {EndDate}", startDate.ToString("yyyy-MM-dd"), endDate.ToString("yyyy-MM-dd"));

                var walkInPayments = await _unitOfWork.Context.ThanhToans
                    .Include(t => t.DangKy)
                        .ThenInclude(d => d!.NguoiDung)
                    .Where(t => t.TrangThai == "SUCCESS" &&
                               t.NgayThanhToan >= startDate &&
                               t.NgayThanhToan <= endDate &&
                               t.DangKy!.NguoiDung!.LoaiNguoiDung == "VANGLAI")
                    .ToListAsync();

                var stats = new WalkInStats
                {
                    TotalSessions = walkInPayments.Count,
                    UniqueGuests = walkInPayments.Select(p => p.DangKy!.NguoiDungId).Distinct().Count(),
                    TotalRevenue = walkInPayments.Sum(p => p.SoTien),
                    AverageSessionValue = walkInPayments.Any() ? walkInPayments.Average(p => p.SoTien) : 0,
                    PaymentMethodBreakdown = walkInPayments
                        .GroupBy(p => p.PhuongThuc ?? "Unknown")
                        .ToDictionary(g => g.Key, g => g.Count()),
                    DailySessionCount = walkInPayments
                        .GroupBy(p => p.NgayThanhToan.Date)
                        .ToDictionary(g => g.Key, g => g.Count())
                };

                _logger.LogInformation("Walk-in stats: {TotalSessions} sessions, {Revenue} VND revenue", stats.TotalSessions, stats.TotalRevenue);
                return stats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting walk-in stats");
                return new WalkInStats();
            }
        }
    }

    public class WalkInRegistrationResult
    {
        public bool Success { get; set; }
        public int GuestId { get; set; }
        public string? TransactionId { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class WalkInVNPayResult
    {
        public bool Success { get; set; }
        public string? PaymentUrl { get; set; }
        public string? OrderId { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}