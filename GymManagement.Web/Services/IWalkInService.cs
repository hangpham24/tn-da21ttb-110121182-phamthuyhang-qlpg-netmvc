using GymManagement.Web.Data.Models;

namespace GymManagement.Web.Services
{
    /// <summary>
    /// Interface cho dịch vụ xử lý khách vãng lai (Walk-in customers)
    /// </summary>
    public interface IWalkInService
    {
        /// <summary>
        /// Tạo nhanh khách vãng lai với thông tin tối thiểu
        /// </summary>
        /// <param name="hoTen">Họ và tên đầy đủ</param>
        /// <param name="soDienThoai">Số điện thoại (optional)</param>
        /// <param name="email">Email (optional)</param>
        /// <returns>Thông tin khách vãng lai đã tạo</returns>
        Task<NguoiDung> CreateGuestAsync(string hoTen, string? soDienThoai = null, string? email = null);

        /// <summary>
        /// Kiểm tra khách vãng lai đã tồn tại trong ngày theo số điện thoại
        /// </summary>
        /// <param name="soDienThoai">Số điện thoại cần kiểm tra</param>
        /// <returns>Thông tin khách nếu đã tồn tại, null nếu chưa có</returns>
        Task<NguoiDung?> GetExistingGuestTodayAsync(string soDienThoai);

        /// <summary>
        /// Tạo vé tập với giá cố định cho khách vãng lai
        /// </summary>
        /// <param name="guestId">ID khách vãng lai</param>
        /// <returns>Thông tin đăng ký vé</returns>
        Task<DangKy> CreateFixedPricePassAsync(int guestId);

        /// <summary>
        /// Tạo vé ngày/giờ cho khách vãng lai (Backward compatibility)
        /// </summary>
        /// <param name="guestId">ID khách vãng lai</param>
        /// <param name="packageType">Loại vé (DAYPASS, HOURPASS)</param>
        /// <param name="packageName">Tên gói vé</param>
        /// <param name="price">Giá vé</param>
        /// <param name="durationHours">Thời lượng (giờ)</param>
        /// <returns>Thông tin đăng ký vé</returns>
        [Obsolete("Use CreateFixedPricePassAsync instead")]
        Task<DangKy> CreateDayPassAsync(int guestId, string packageType, string packageName, decimal price, int durationHours = 24);

        /// <summary>
        /// Xử lý thanh toán cho khách vãng lai
        /// </summary>
        /// <param name="dangKyId">ID đăng ký vé</param>
        /// <param name="phuongThuc">Phương thức thanh toán (CASH, BANK)</param>
        /// <param name="ghiChu">Ghi chú thanh toán</param>
        /// <returns>Thông tin thanh toán</returns>
        Task<ThanhToan> ProcessWalkInPaymentAsync(int dangKyId, string phuongThuc, string? ghiChu = null);

        /// <summary>
        /// Xác nhận thanh toán và kích hoạt vé
        /// </summary>
        /// <param name="paymentId">ID thanh toán</param>
        /// <returns>True nếu thành công</returns>
        Task<bool> ConfirmPaymentAsync(int paymentId);

        /// <summary>
        /// Check-in tự động cho khách vãng lai sau thanh toán thành công
        /// </summary>
        /// <param name="guestId">ID khách vãng lai</param>
        /// <param name="ghiChu">Ghi chú check-in</param>
        /// <returns>Thông tin điểm danh</returns>
        Task<DiemDanh> CheckInGuestAsync(int guestId, string? ghiChu = null);

        /// <summary>
        /// Check-out thủ công cho khách vãng lai
        /// </summary>
        /// <param name="diemDanhId">ID điểm danh</param>
        /// <returns>True nếu thành công</returns>
        Task<bool> CheckOutGuestAsync(int diemDanhId);

        /// <summary>
        /// Lấy danh sách khách vãng lai đang tập trong ngày
        /// </summary>
        /// <param name="date">Ngày cần lấy (mặc định hôm nay)</param>
        /// <returns>Danh sách khách đang tập</returns>
        Task<List<WalkInSessionInfo>> GetTodayWalkInsAsync(DateTime? date = null);

        /// <summary>
        /// Lấy thông tin phiên tập của khách vãng lai
        /// </summary>
        /// <param name="guestId">ID khách vãng lai</param>
        /// <returns>Thông tin phiên tập hiện tại</returns>
        Task<WalkInSessionInfo?> GetActiveSessionAsync(int guestId);

        /// <summary>
        /// Lấy thông tin gói vé cố định cho khách vãng lai
        /// </summary>
        /// <returns>Danh sách gói vé (chỉ có 1 gói cố định)</returns>
        Task<List<GoiTap>> GetAvailablePackagesAsync();

        /// <summary>
        /// Lấy thông tin gói vé cố định
        /// </summary>
        /// <returns>Tuple chứa giá, tên và mô tả</returns>
        (decimal Price, string Name, string Description) GetFixedPriceInfo();

        /// <summary>
        /// Thống kê khách vãng lai theo ngày/tuần/tháng
        /// </summary>
        /// <param name="startDate">Ngày bắt đầu</param>
        /// <param name="endDate">Ngày kết thúc</param>
        /// <returns>Thống kê chi tiết</returns>
        Task<WalkInStats> GetWalkInStatsAsync(DateTime startDate, DateTime endDate);

        /// <summary>
        /// Đăng ký khách vãng lai với thanh toán (All-in-one method)
        /// </summary>
        /// <param name="fullName">Họ tên đầy đủ</param>
        /// <param name="phoneNumber">Số điện thoại</param>
        /// <param name="email">Email (tùy chọn)</param>
        /// <param name="note">Ghi chú (tùy chọn)</param>
        /// <param name="paymentMethod">Phương thức thanh toán (CASH, VNPAY)</param>
        /// <param name="amount">Số tiền thanh toán</param>
        /// <returns>Kết quả đăng ký và thanh toán</returns>
        Task<WalkInRegistrationResult> RegisterWalkInWithPaymentAsync(
            string fullName,
            string phoneNumber,
            string? email,
            string? note,
            string paymentMethod,
            decimal amount);

        /// <summary>
        /// Tạo thanh toán VNPay cho khách vãng lai
        /// </summary>
        /// <param name="fullName">Họ tên đầy đủ</param>
        /// <param name="phoneNumber">Số điện thoại</param>
        /// <param name="email">Email (tùy chọn)</param>
        /// <param name="note">Ghi chú (tùy chọn)</param>
        /// <param name="amount">Số tiền thanh toán</param>
        /// <returns>Kết quả tạo thanh toán VNPay</returns>
        Task<WalkInVNPayResult> CreateVNPayPaymentAsync(
            string fullName,
            string phoneNumber,
            string? email,
            string? note,
            decimal amount);
    }

    /// <summary>
    /// Thông tin phiên tập của khách vãng lai
    /// </summary>
    public class WalkInSessionInfo
    {
        public int DiemDanhId { get; set; }
        public int GuestId { get; set; }
        public string GuestName { get; set; } = null!;
        public string? PhoneNumber { get; set; }
        public string PackageName { get; set; } = null!;
        public DateTime CheckInTime { get; set; }
        public DateTime? CheckOutTime { get; set; }
        public string Status { get; set; } = null!; // "Active", "Completed"
        public TimeSpan? Duration => CheckOutTime?.Subtract(CheckInTime);
        public bool IsActive => CheckOutTime == null;
    }

    /// <summary>
    /// Thống kê khách vãng lai
    /// </summary>
    public class WalkInStats
    {
        public int TotalSessions { get; set; }
        public int UniqueGuests { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal AverageSessionValue { get; set; }
        public Dictionary<string, int> PaymentMethodBreakdown { get; set; } = new();
        public Dictionary<string, int> PackageTypeBreakdown { get; set; } = new();
        public Dictionary<DateTime, int> DailySessionCount { get; set; } = new();
    }
}
