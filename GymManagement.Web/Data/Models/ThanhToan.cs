using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GymManagement.Web.Data.Models
{
    public class ThanhToan
    {
        public int ThanhToanId { get; set; }
        
        public int? DangKyId { get; set; }
        
        [Required]
        [Column(TypeName = "decimal(12,2)")]
        public decimal SoTien { get; set; }
        
        public DateTime NgayThanhToan { get; set; }
        
        [StringLength(20)]
        public string? PhuongThuc { get; set; } // CASH, CARD, BANK, WALLET, VNPAY
        
        [StringLength(20)]
        public string TrangThai { get; set; } = "PENDING"; // PENDING/SUCCESS/FAILED/REFUND
        
        [StringLength(200)]
        public string? GhiChu { get; set; }

        // Navigation properties
        public virtual DangKy? DangKy { get; set; }
        public virtual ThanhToanGateway? ThanhToanGateway { get; set; }
    }
}
