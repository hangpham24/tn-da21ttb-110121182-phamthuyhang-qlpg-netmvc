using System.ComponentModel.DataAnnotations;

namespace GymManagement.Web.Data.Models
{
    public class KhuyenMai
    {
        public int KhuyenMaiId { get; set; }
        
        [Required]
        [StringLength(50)]
        public string MaCode { get; set; } = null!;
        
        [StringLength(300)]
        public string? MoTa { get; set; }
        
        [Range(0, 100)]
        public int? PhanTramGiam { get; set; }
        
        [Required]
        public DateOnly NgayBatDau { get; set; }
        
        [Required]
        public DateOnly NgayKetThuc { get; set; }
        
        public bool KichHoat { get; set; } = true;

        public DateTime NgayTao { get; set; } = DateTime.Now;
    }
}
