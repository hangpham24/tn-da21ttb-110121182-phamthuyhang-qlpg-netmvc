using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KLTN.Models.Database
{
    public class LopHoc
    {
        [Key]
        public int MaLop { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Tên lớp")]
        public string TenLop { get; set; } = string.Empty;

        [ForeignKey("HuanLuyenVien")]
        [Display(Name = "Huấn luyện viên")]
        public int? MaPT { get; set; }

        [Required]
        [DataType(DataType.Time)]
        [Display(Name = "Thời gian bắt đầu")]
        public TimeSpan ThoiGianBatDau { get; set; }

        [Required]
        [DataType(DataType.Time)]
        [Display(Name = "Thời gian kết thúc")]
        public TimeSpan ThoiGianKetThuc { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Ngày trong tuần")]
        public string NgayTrongTuan { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Số lượng tối đa")]
        public int SoLuongToiDa { get; set; }

        [Display(Name = "Số lượng hiện tại")]
        public int SoLuongHienTai { get; set; } = 0;

        [StringLength(20)]
        [Display(Name = "Trạng thái")]
        public string TrangThai { get; set; } = "DangMo";

        [StringLength(500)]
        [Display(Name = "Ghi chú")]
        public string? GhiChu { get; set; }

        // Navigation properties
        public virtual HuanLuyenVien? HuanLuyenVien { get; set; }
        public virtual ICollection<DangKy>? DangKys { get; set; }
        public virtual ICollection<PT_PhanCongHoaHong>? PT_PhanCongHoaHongs { get; set; }
        public virtual ICollection<PhienDay>? PhienDays { get; set; }
        public virtual DichVu? DichVu { get; set; }
    }
} 