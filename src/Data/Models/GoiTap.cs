using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GymManagement.Web.Data.Models
{
    public class GoiTap
    {
        public int GoiTapId { get; set; }
        
        [Required]
        [StringLength(100)]
        public string TenGoi { get; set; } = null!;
        
        [Required]
        public int ThoiHanThang { get; set; }
        
        public int? SoBuoiToiDa { get; set; }
        
        [Required]
        [Column(TypeName = "decimal(12,2)")]
        public decimal Gia { get; set; }
        
        [StringLength(500)]
        public string? MoTa { get; set; }

        // Navigation properties
        public virtual ICollection<DangKy> DangKys { get; set; } = new List<DangKy>();
    }
}
