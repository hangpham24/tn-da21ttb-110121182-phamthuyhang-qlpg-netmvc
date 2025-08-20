using GymManagement.Web.Data.Models;

namespace GymManagement.Web.Models.DTOs
{
    public class NguoiDungWithSubscriptionDto : NguoiDungDto
    {
        // Thông tin gói tập hiện tại
        public DangKy? ActivePackageRegistration { get; set; }
        public GoiTap? ActivePackage { get; set; }
        public DateTime? PackageExpiryDate { get; set; }
        public int? DaysRemaining { get; set; }
        public string? PackageStatus { get; set; } // "ACTIVE", "EXPIRING_SOON", "EXPIRED", "NONE"

        // Thông tin lớp học hiện tại
        public IEnumerable<DangKy>? ActiveClassRegistrations { get; set; }
        public int ActiveClassCount { get; set; }
        
        // Tính toán trạng thái gói tập
        public void CalculatePackageStatus()
        {
            if (ActivePackageRegistration == null || PackageExpiryDate == null)
            {
                PackageStatus = "NONE";
                DaysRemaining = null;
                return;
            }

            var today = DateTime.Today;
            var expiryDate = PackageExpiryDate.Value.Date;
            DaysRemaining = (int)(expiryDate - today).TotalDays;

            if (DaysRemaining < 0)
            {
                PackageStatus = "EXPIRED";
            }
            else if (DaysRemaining <= 7)
            {
                PackageStatus = "EXPIRING_SOON";
            }
            else
            {
                PackageStatus = "ACTIVE";
            }
        }
    }
}
