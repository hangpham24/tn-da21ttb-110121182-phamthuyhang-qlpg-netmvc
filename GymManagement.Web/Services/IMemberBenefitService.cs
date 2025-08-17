using GymManagement.Web.Data.Models;

namespace GymManagement.Web.Services
{
    /// <summary>
    /// Interface cho service kiểm tra quyền lợi member
    /// Logic đơn giản và rõ ràng cho 1 phòng gym
    /// </summary>
    public interface IMemberBenefitService
    {
        /// <summary>
        /// Kiểm tra member có gói tập đang hoạt động không
        /// </summary>
        Task<bool> HasActivePackageAsync(int memberId);

        /// <summary>
        /// Lấy thông tin gói tập hiện tại của member
        /// </summary>
        Task<DangKy?> GetActivePackageAsync(int memberId);

        /// <summary>
        /// Kiểm tra member có thể booking lớp học không và phí bao nhiêu
        /// Return: (CanBook, IsFree, Fee, Reason)
        /// </summary>
        Task<(bool CanBook, bool IsFree, decimal Fee, string Reason)> CanBookClassAsync(int memberId, int lopHocId);

        /// <summary>
        /// Kiểm tra member có thể check-in gym không
        /// </summary>
        Task<(bool CanCheckIn, bool IsFree, decimal Fee, string Reason)> CanCheckInGymAsync(int memberId);

        /// <summary>
        /// Lấy thông tin tổng quan quyền lợi của member
        /// </summary>
        Task<MemberBenefitInfo> GetMemberBenefitsAsync(int memberId);

        /// <summary>
        /// Tính phí cho booking lớp học
        /// </summary>
        Task<decimal> CalculateClassBookingFeeAsync(int memberId, int lopHocId);

        /// <summary>
        /// Kiểm tra số lượng booking đã sử dụng trong tháng
        /// </summary>
        Task<(int Used, int Limit, bool HasLimit)> GetMonthlyBookingUsageAsync(int memberId);
    }
}
