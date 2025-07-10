using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KLTN.Models.Database
{
    public class DoanhThu
    {
        [Key]
        public int MaDoanhThu { get; set; }

        [ForeignKey("ThanhToan")]
        [Display(Name = "Thanh toán")]
        public int MaThanhToan { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Số tiền")]
        public decimal SoTien { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Ngày ghi nhận")]
        public DateTime Ngay { get; set; }

        [StringLength(500)]
        [Display(Name = "Ghi chú")]
        public string? GhiChu { get; set; }

        [Display(Name = "Ngày tạo")]
        public DateTime NgayTao { get; set; } = DateTime.Now;

        [ForeignKey("TaiKhoan")]
        [Display(Name = "Người tạo")]
        public int NguoiTao { get; set; }

        // Navigation properties
        public virtual ThanhToan? ThanhToan { get; set; }
        public virtual TaiKhoan? TaiKhoan { get; set; }
    }
} 