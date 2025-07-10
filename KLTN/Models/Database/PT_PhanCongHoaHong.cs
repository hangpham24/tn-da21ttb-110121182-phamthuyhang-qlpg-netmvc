using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KLTN.Models.Database
{
    public class PT_PhanCongHoaHong
    {
        [Key]
        public int MaPhanCong { get; set; }

        [Required]
        [ForeignKey("HuanLuyenVien")]
        public int MaPT { get; set; }

        [ForeignKey("GoiTap")]
        public int? MaGoiTap { get; set; }

        [ForeignKey("LopHoc")]
        public int? MaLopHoc { get; set; }

        [Required]
        [Column(TypeName = "decimal(5, 2)")]
        public decimal PhanTramHoaHong { get; set; }

        // Navigation properties
        public virtual HuanLuyenVien? HuanLuyenVien { get; set; }
        public virtual GoiTap? GoiTap { get; set; }
        public virtual LopHoc? LopHoc { get; set; }
    }
} 