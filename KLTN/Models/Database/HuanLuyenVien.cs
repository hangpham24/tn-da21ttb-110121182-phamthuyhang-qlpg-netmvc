using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Http; // Cho IFormFile

namespace KLTN.Models.Database
{
    public class HuanLuyenVien
    {
        [Key]
        public int MaPT { get; set; }

        [ForeignKey("TaiKhoan")]
        [Display(Name = "Tài khoản")]
        public int MaTK { get; set; }

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
        [Display(Name = "Chuyên môn")]
        public string? ChuyenMon { get; set; }

        [StringLength(200)]
        [Display(Name = "Kinh nghiệm")]
        public string? KinhNghiem { get; set; }

        [StringLength(255)]
        [Display(Name = "Địa chỉ")]
        public string? DiaChi { get; set; }

        [StringLength(255)]
        [Display(Name = "Ảnh đại diện")]
        public string? AnhDaiDien { get; set; }
        
        [NotMapped]
        [Display(Name = "File ảnh đại diện")]
        public IFormFile? AvatarFile { get; set; }

        [StringLength(20)]
        [Display(Name = "Trạng thái HLV")]
        public string? TrangThaiHLV { get; set; } = "HoatDong";

        // Navigation properties
        public virtual TaiKhoan? TaiKhoan { get; set; }
        public virtual ICollection<LopHoc>? LopHocs { get; set; }
        public virtual ICollection<PT_PhanCongHoaHong>? PT_PhanCongHoaHongs { get; set; }
        public virtual ICollection<PhienDay>? PhienDays { get; set; }
        public virtual ICollection<BangLuongPT>? BangLuongPTs { get; set; }
    }
}
