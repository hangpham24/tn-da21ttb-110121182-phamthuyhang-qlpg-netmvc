using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GymManagement.Web.Data.Models
{
    public class KhuyenMaiUsage
    {
        [Key]
        public int KhuyenMaiUsageId { get; set; }

        [Required]
        public int KhuyenMaiId { get; set; }

        [Required]
        public int NguoiDungId { get; set; }

        public int? ThanhToanId { get; set; }

        public int? DangKyId { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal SoTienGoc { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal SoTienGiam { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal SoTienCuoi { get; set; }

        [Required]
        public DateTime NgaySuDung { get; set; } = DateTime.Now;

        [StringLength(500)]
        public string? GhiChu { get; set; }

        // Navigation properties
        [ForeignKey("KhuyenMaiId")]
        public virtual KhuyenMai KhuyenMai { get; set; } = null!;

        [ForeignKey("NguoiDungId")]
        public virtual NguoiDung NguoiDung { get; set; } = null!;

        [ForeignKey("ThanhToanId")]
        public virtual ThanhToan? ThanhToan { get; set; }

        [ForeignKey("DangKyId")]
        public virtual DangKy? DangKy { get; set; }
    }
}
