using GymManagement.Web.Data.Models;
using GymManagement.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace GymManagement.Web.Services
{
    /// <summary>
    /// Service đơn giản để kiểm tra quyền lợi của member
    /// Logic rõ ràng: Có gói tập = Miễn phí mọi thứ, Không có = Phải trả phí
    /// </summary>
    public class MemberBenefitService : IMemberBenefitService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<MemberBenefitService> _logger;

        // Giá cố định cho gym đơn giản
        private const decimal CLASS_FEE_FOR_NON_MEMBER = 50000m; // 50k VNĐ/buổi lớp học
        private const decimal WALKIN_FEE = 15000m; // 15k VNĐ/ngày cho khách vãng lai

        public MemberBenefitService(IUnitOfWork unitOfWork, ILogger<MemberBenefitService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        /// <summary>
        /// Kiểm tra member có gói tập đang hoạt động không
        /// </summary>
        public async Task<bool> HasActivePackageAsync(int memberId)
        {
            try
            {
                // Cache key cho performance
                var cacheKey = $"member_active_package_{memberId}_{DateTime.Today:yyyyMMdd}";

                var hasActivePackage = await _unitOfWork.Context.DangKys
                    .AnyAsync(d => d.NguoiDungId == memberId &&
                                  d.GoiTapId != null &&
                                  d.TrangThai == "ACTIVE" &&
                                  d.NgayKetThuc >= DateOnly.FromDateTime(DateTime.Today));

                _logger.LogInformation("Member {MemberId} has active package: {HasPackage}", memberId, hasActivePackage);
                return hasActivePackage;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking active package for member {MemberId}", memberId);
                return false;
            }
        }

        /// <summary>
        /// Lấy thông tin gói tập hiện tại của member
        /// </summary>
        public async Task<DangKy?> GetActivePackageAsync(int memberId)
        {
            try
            {
                return await _unitOfWork.Context.DangKys
                    .Include(d => d.GoiTap)
                    .FirstOrDefaultAsync(d => d.NguoiDungId == memberId &&
                                             d.GoiTapId != null &&
                                             d.TrangThai == "ACTIVE" &&
                                             d.NgayKetThuc >= DateOnly.FromDateTime(DateTime.Today));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active package for member {MemberId}", memberId);
                return null;
            }
        }

        /// <summary>
        /// Kiểm tra member có thể booking lớp học miễn phí không
        /// LOGIC ĐƠN GIẢN: Có gói tập = Miễn phí, Không có = Phải trả 50k
        /// </summary>
        public async Task<(bool CanBook, bool IsFree, decimal Fee, string Reason)> CanBookClassAsync(int memberId, int lopHocId)
        {
            try
            {
                // Validation đầu vào
                if (memberId <= 0)
                    return (false, false, 0, "Member ID không hợp lệ");

                if (lopHocId <= 0)
                    return (false, false, 0, "Lớp học ID không hợp lệ");

                // Kiểm tra lớp học có tồn tại không
                var lopHoc = await _unitOfWork.Context.LopHocs.FindAsync(lopHocId);
                if (lopHoc == null)
                    return (false, false, 0, "Lớp học không tồn tại");

                if (lopHoc.TrangThai != "OPEN")
                    return (false, false, 0, "Lớp học đã đóng");

                // Kiểm tra member có gói tập không
                var hasActivePackage = await HasActivePackageAsync(memberId);

                if (hasActivePackage)
                {
                    // Có gói tập → MIỄN PHÍ
                    return (true, true, 0, "Miễn phí với gói tập");
                }
                else
                {
                    // Không có gói tập → PHẢI TRẢ PHÍ
                    return (true, false, CLASS_FEE_FOR_NON_MEMBER, $"Phí lớp học: {CLASS_FEE_FOR_NON_MEMBER:N0} VNĐ");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking class booking for member {MemberId}, class {LopHocId}", memberId, lopHocId);
                return (false, false, 0, "Có lỗi xảy ra khi kiểm tra");
            }
        }

        /// <summary>
        /// Kiểm tra member có thể check-in gym không
        /// </summary>
        public async Task<(bool CanCheckIn, bool IsFree, decimal Fee, string Reason)> CanCheckInGymAsync(int memberId)
        {
            try
            {
                var hasActivePackage = await HasActivePackageAsync(memberId);

                if (hasActivePackage)
                {
                    return (true, true, 0, "Miễn phí với gói tập");
                }
                else
                {
                    return (false, false, 0, "Cần mua gói tập hoặc vé vãng lai để vào gym");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking gym access for member {MemberId}", memberId);
                return (false, false, 0, "Có lỗi xảy ra khi kiểm tra");
            }
        }

        /// <summary>
        /// Lấy thông tin tổng quan quyền lợi của member
        /// </summary>
        public async Task<MemberBenefitInfo> GetMemberBenefitsAsync(int memberId)
        {
            try
            {
                var activePackage = await GetActivePackageAsync(memberId);
                var hasActivePackage = activePackage != null;

                return new MemberBenefitInfo
                {
                    MemberId = memberId,
                    HasActivePackage = hasActivePackage,
                    PackageName = activePackage?.GoiTap?.TenGoi,
                    PackageExpiry = activePackage?.NgayKetThuc,
                    CanAccessGym = hasActivePackage,
                    CanBookClassesFree = hasActivePackage,
                    ClassFeeIfNotMember = CLASS_FEE_FOR_NON_MEMBER,
                    WalkInFee = WALKIN_FEE
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting member benefits for {MemberId}", memberId);
                return new MemberBenefitInfo { MemberId = memberId };
            }
        }

        /// <summary>
        /// Tính phí cho booking lớp học
        /// </summary>
        public async Task<decimal> CalculateClassBookingFeeAsync(int memberId, int lopHocId)
        {
            var (canBook, isFree, fee, reason) = await CanBookClassAsync(memberId, lopHocId);
            return canBook ? fee : 0;
        }

        /// <summary>
        /// Kiểm tra số lượng booking còn lại trong tháng (nếu có giới hạn)
        /// Hiện tại: KHÔNG GIỚI HẠN cho đơn giản
        /// </summary>
        public async Task<(int Used, int Limit, bool HasLimit)> GetMonthlyBookingUsageAsync(int memberId)
        {
            // Logic đơn giản: Không giới hạn số buổi booking
            var currentMonth = DateTime.Today.Month;
            var currentYear = DateTime.Today.Year;

            var usedBookings = await _unitOfWork.Context.Bookings
                .CountAsync(b => b.ThanhVienId == memberId &&
                               b.Ngay.Month == currentMonth &&
                               b.Ngay.Year == currentYear &&
                               b.TrangThai == "BOOKED");

            return (usedBookings, -1, false); // -1 = unlimited
        }
    }

    /// <summary>
    /// DTO chứa thông tin quyền lợi của member
    /// </summary>
    public class MemberBenefitInfo
    {
        public int MemberId { get; set; }
        public bool HasActivePackage { get; set; }
        public string? PackageName { get; set; }
        public DateOnly? PackageExpiry { get; set; }
        public bool CanAccessGym { get; set; }
        public bool CanBookClassesFree { get; set; }
        public decimal ClassFeeIfNotMember { get; set; }
        public decimal WalkInFee { get; set; }

        public string StatusText => HasActivePackage ? "Thành viên có gói tập" : "Thành viên chưa có gói tập";
        public string GymAccessText => CanAccessGym ? "✅ Được vào gym miễn phí" : "❌ Cần mua gói tập";
        public string ClassAccessText => CanBookClassesFree ? "✅ Booking lớp học miễn phí" : $"💰 Phí lớp học: {ClassFeeIfNotMember:N0} VNĐ/buổi";
    }
}
