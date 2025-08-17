using System.ComponentModel.DataAnnotations;

namespace GymManagement.Web.Data.Models
{
    public class ThongBao
    {
        public int ThongBaoId { get; set; }
        
        [StringLength(100)]
        public string? TieuDe { get; set; }
        
        [StringLength(1000)]
        public string? NoiDung { get; set; }
        
        public DateTime NgayTao { get; set; }
        
        public int? NguoiDungId { get; set; }
        
        [StringLength(10)]
        public string? Kenh { get; set; } // EMAIL, SMS, APP
        
        public bool DaDoc { get; set; } = false;

        // Navigation properties
        public virtual NguoiDung? NguoiDung { get; set; }
    }
}
