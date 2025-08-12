using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GymManagement.Web.Data.Models
{
    public class CauHinhHoaHong
    {
        [Key]
        public int CauHinhHoaHongId { get; set; }

        public int? GoiTapId { get; set; }

        [Required]
        [Range(0, 100)]
        public decimal PhanTramHoaHong { get; set; }

        public DateTime NgayTao { get; set; } = DateTime.Now;

        // Navigation properties
        [ForeignKey("GoiTapId")]
        public virtual GoiTap? GoiTap { get; set; }
    }
}
