using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GymManagement.Web.Data.Models
{
    public class DangKy
    {
        public int DangKyId { get; set; }
        
        [Required]
        public int NguoiDungId { get; set; }
        
        public int? GoiTapId { get; set; }
        
        public int? LopHocId { get; set; }
        
        [Required]
        public DateOnly NgayBatDau { get; set; }
        
        [Required]
        public DateOnly NgayKetThuc { get; set; }
        
        [StringLength(20)]
        public string TrangThai { get; set; } = "ACTIVE";

        public DateTime NgayTao { get; set; }

        public decimal? PhiDangKy { get; set; }

        [StringLength(500)]
        public string? LyDoHuy { get; set; }

        [StringLength(20)]
        public string LoaiDangKy { get; set; } = "PACKAGE"; // PACKAGE, CLASS, DAYPASS, HOURPASS

        [StringLength(50)]
        public string? TrangThaiChiTiet { get; set; }

        // Alias properties for backward compatibility
        [NotMapped]
        public virtual NguoiDung? ThanhVien => NguoiDung;

        [NotMapped]
        public DateTime NgayDangKy => NgayTao;

        // Computed properties
        [NotMapped]
        public bool IsClassRegistration => LopHocId.HasValue;

        [NotMapped]
        public bool IsPackageRegistration => GoiTapId.HasValue;

        [NotMapped]
        public bool IsWalkInRegistration => LoaiDangKy == "DAYPASS" || LoaiDangKy == "HOURPASS";

        [NotMapped]
        public bool CanCancel => TrangThai == "ACTIVE" &&
            (IsPackageRegistration || (IsClassRegistration && NgayBatDau > DateOnly.FromDateTime(DateTime.Today.AddDays(1))));

        [NotMapped]
        public string DisplayType => IsClassRegistration ? "Lớp học" :
                                    IsWalkInRegistration ? "Vé lẻ" : "Gói tập";

        // Computed properties for renewal
        [NotMapped]
        public bool IsExpiringSoon => TrangThai == "ACTIVE" && NgayKetThuc <= DateOnly.FromDateTime(DateTime.Today.AddDays(7));

        [NotMapped]
        public bool IsExpired => TrangThai == "ACTIVE" && NgayKetThuc < DateOnly.FromDateTime(DateTime.Today);

        [NotMapped]
        public bool CanRenew => IsPackageRegistration && (TrangThai == "ACTIVE" || (TrangThai == "EXPIRED" && NgayKetThuc >= DateOnly.FromDateTime(DateTime.Today.AddDays(-30))));

        [NotMapped]
        public int DaysUntilExpiry => (NgayKetThuc.ToDateTime(TimeOnly.MinValue) - DateTime.Today).Days;

        [NotMapped]
        public string ExpiryStatus
        {
            get
            {
                if (IsExpired) return "Đã hết hạn";
                if (IsExpiringSoon) return "Sắp hết hạn";
                return "Còn hiệu lực";
            }
        }

        [NotMapped]
        public string ExpiryBadgeClass
        {
            get
            {
                if (IsExpired) return "bg-red-100 text-red-800";
                if (IsExpiringSoon) return "bg-yellow-100 text-yellow-800";
                return "bg-green-100 text-green-800";
            }
        }

        // Navigation properties
        public virtual NguoiDung NguoiDung { get; set; } = null!;
        public virtual GoiTap? GoiTap { get; set; }
        public virtual LopHoc? LopHoc { get; set; }
        public virtual ICollection<ThanhToan> ThanhToans { get; set; } = new List<ThanhToan>();
    }
}
