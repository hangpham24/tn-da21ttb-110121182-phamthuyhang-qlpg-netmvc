using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GymManagement.Web.Data.Models
{
    public class ThanhToanGateway
    {
        public int GatewayId { get; set; }
        
        [Required]
        public int ThanhToanId { get; set; }
        
        [StringLength(30)]
        public string GatewayTen { get; set; } = "VNPAY";
        
        [StringLength(100)]
        public string? GatewayTransId { get; set; }
        
        [StringLength(100)]
        public string? GatewayOrderId { get; set; }
        
        [Column(TypeName = "decimal(12,2)")]
        public decimal? GatewayAmount { get; set; }
        
        [StringLength(10)]
        public string? GatewayRespCode { get; set; }
        
        [StringLength(255)]
        public string? GatewayMessage { get; set; }
        
        public DateTime? ThoiGianCallback { get; set; }

        // Navigation properties
        public virtual ThanhToan ThanhToan { get; set; } = null!;
    }
}
