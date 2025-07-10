using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Http; // Cho IFormFile

namespace KLTN.Models.Database
{
    public class ThanhVien
    {
        [Key]
        public int MaTV { get; set; }

        [StringLength(100)]
        [Display(Name = "Họ tên")]
        public string? HoTen { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Ngày sinh")]
        public DateTime? NgaySinh { get; set; }

        [StringLength(10)]
        [Display(Name = "Giới tính")]
        public string? GioiTinh { get; set; }

        [StringLength(15)]
        [Display(Name = "Số điện thoại")]
        public string? SoDienThoai { get; set; }

        [StringLength(100)]
        [EmailAddress]
        [Display(Name = "Email")]
        public string? Email { get; set; }

        [StringLength(200)]
        [Display(Name = "Địa chỉ")]
        public string? DiaChi { get; set; }

        [StringLength(255)]
        [Display(Name = "Ảnh đại diện")]
        public string? AnhDaiDien { get; set; }

        [NotMapped]
        [Display(Name = "File ảnh đại diện")]
        public IFormFile? AvatarFile { get; set; }

        [ForeignKey("TaiKhoan")]
        [Display(Name = "Tài khoản")]
        public int? MaTK { get; set; }

        // Navigation properties
        public virtual TaiKhoan? TaiKhoan { get; set; }
        public virtual ICollection<DangKy>? DangKys { get; set; }
        public virtual ICollection<LichSuCheckIn>? LichSuCheckIns { get; set; }
        public virtual ICollection<ThongBao>? ThongBaos { get; set; }
        public virtual ICollection<LichSuDangKy>? LichSuDangKys { get; set; }
        public virtual ICollection<PhienTap>? PhienTaps { get; set; }
    }
}
