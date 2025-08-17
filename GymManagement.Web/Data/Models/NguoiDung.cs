using System.ComponentModel.DataAnnotations;

namespace GymManagement.Web.Data.Models
{
    public class NguoiDung
    {
        public int NguoiDungId { get; set; }
        
        [Required]
        [StringLength(20)]
        public string LoaiNguoiDung { get; set; } = null!; // THANHVIEN, HLV, NHANVIEN, VANGLAI
        
        [Required]
        [StringLength(50)]
        public string Ho { get; set; } = null!;
        
        [StringLength(50)]
        public string? Ten { get; set; }
        
        [StringLength(10)]
        public string? GioiTinh { get; set; }
        
        public DateOnly? NgaySinh { get; set; }
        
        [StringLength(15)]
        public string? SoDienThoai { get; set; }
        
        [StringLength(100)]
        public string? Email { get; set; }
        
        public DateOnly NgayThamGia { get; set; }

        [StringLength(20)]
        public string TrangThai { get; set; } = "ACTIVE";

        [StringLength(255)]
        public string? AnhDaiDien { get; set; }

        public DateTime NgayTao { get; set; } = DateTime.Now;

        // Navigation properties
        public virtual TaiKhoan? TaiKhoan { get; set; }
        public virtual ICollection<LopHoc> LopHocs { get; set; } = new List<LopHoc>(); // HLV
        public virtual ICollection<DangKy> DangKys { get; set; } = new List<DangKy>();
        public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();
        public virtual ICollection<BuoiHlv> BuoiHlvs { get; set; } = new List<BuoiHlv>(); // HLV
        public virtual ICollection<BuoiHlv> BuoiHlvThanhViens { get; set; } = new List<BuoiHlv>(); // ThanhVien
        public virtual ICollection<BuoiTap> BuoiTaps { get; set; } = new List<BuoiTap>();
        public virtual MauMat? MauMat { get; set; }
        public virtual ICollection<DiemDanh> DiemDanhs { get; set; } = new List<DiemDanh>();
        public virtual ICollection<BangLuong> BangLuongs { get; set; } = new List<BangLuong>();
        public virtual ICollection<ThongBao> ThongBaos { get; set; } = new List<ThongBao>();
        public virtual ICollection<LichSuAnh> LichSuAnhs { get; set; } = new List<LichSuAnh>();
    }
}
