using System.ComponentModel.DataAnnotations;

namespace GymManagement.Web.Data.Models
{
    public class TaiKhoan
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [StringLength(100)]
        public string TenDangNhap { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(256)]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string MatKhauHash { get; set; } = string.Empty;

        [Required]
        public string Salt { get; set; } = string.Empty;

        public int? NguoiDungId { get; set; }
        public virtual NguoiDung? NguoiDung { get; set; }

        public bool KichHoat { get; set; } = true;
        public bool EmailXacNhan { get; set; } = false;

        public DateTime NgayTao { get; set; } = DateTime.UtcNow;
        public DateTime? LanDangNhapCuoi { get; set; }

        // Navigation properties
        public virtual ICollection<TaiKhoanVaiTro> TaiKhoanVaiTros { get; set; } = new List<TaiKhoanVaiTro>();
        public virtual ICollection<ExternalLogin> ExternalLogins { get; set; } = new List<ExternalLogin>();
    }
}
