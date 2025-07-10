using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KLTN.Models.Database
{
    public class TaiKhoan
    {
        [Key]
        public int MaTK { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập")]
        [StringLength(50)]
        // TODO: Tên đăng nhập cần là duy nhất, cần cấu hình trong DbContext bằng Fluent API
        [Display(Name = "Tên đăng nhập")]
        public string TenDangNhap { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
        [StringLength(255)] // Độ dài đủ cho mật khẩu đã băm
        [Display(Name = "Mật khẩu")]
        public string MatKhauHash { get; set; } = string.Empty;

        [ForeignKey("Quyen")]
        [Display(Name = "Quyền")]
        public int MaQuyen { get; set; } // Mặc định một quyền nào đó, ví dụ: "Thành viên"

        [StringLength(20)]
        [Display(Name = "Trạng thái")]
        public string TrangThai { get; set; } = "HoatDong"; // HoatDong, Khoa, ChuaKichHoat

        [Display(Name = "Ngày tạo")]
        public DateTime NgayTao { get; set; } = DateTime.Now;

        [Display(Name = "Lần đăng nhập cuối")]
        public DateTime? LanDangNhapCuoi { get; set; }

        // Navigation properties
        public virtual Quyen? Quyen { get; set; }
        public virtual ThanhVien? ThanhVien { get; set; } // Một tài khoản có thể liên kết với một thành viên (1-1)
        public virtual HuanLuyenVien? HuanLuyenVien { get; set; } // Một tài khoản có thể liên kết với một HLV (1-1)

        public virtual ICollection<BaoCaoTaiChinh>? BaoCaoTaiChinhsLap { get; set; }
        public virtual ICollection<GiaHanDangKy>? GiaHanDangKysLap { get; set; }
        public virtual ICollection<TinTuc>? TinTucsDang { get; set; }
        public virtual ICollection<ThongBao>? ThongBaosGui { get; set; }
        
        // Thêm các collection mới cho ThanhToan
        public virtual ICollection<ThanhToan>? ThanhToansLap { get; set; }

        // Navigation properties for CapNhatAnhNhanDien
        public virtual ICollection<CapNhatAnhNhanDien>? CapNhatAnhNhanDienThucHien { get; set; }
        public virtual ICollection<CapNhatAnhNhanDien>? AnhNhanDienDuocCapNhat { get; set; }
    }
}
