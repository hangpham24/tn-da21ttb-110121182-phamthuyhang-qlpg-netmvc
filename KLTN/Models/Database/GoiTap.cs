using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KLTN.Models.Database
{
    public class GoiTap
    {
        [Key]
        public int MaGoi { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Tên gói tập")]
        public string TenGoi { get; set; } = string.Empty;

        [StringLength(500)]
        [Display(Name = "Mô tả")]
        public string? MoTa { get; set; }

        [Display(Name = "Thời hạn (tháng)")]
        public int ThoiHanThang { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Giá tiền")]
        public decimal GiaTien { get; set; }

        [Display(Name = "Số lần tập tối đa")]
        public int? SoLanTapToiDa { get; set; }

        [ForeignKey("KhuyenMai")]
        [Display(Name = "Khuyến mãi")]
        public int? MaKM { get; set; }

        // Navigation properties
        public virtual KhuyenMai? KhuyenMai { get; set; }
        public virtual ICollection<DangKy>? DangKys { get; set; }
        public virtual ICollection<LichSuDangKy>? LichSuDangKys { get; set; }
        public virtual ICollection<PT_PhanCongHoaHong>? PT_PhanCongHoaHongs { get; set; }
        public virtual ICollection<PhienDay>? PhienDays { get; set; }
        public virtual DichVu? DichVu { get; set; }
    }
} 