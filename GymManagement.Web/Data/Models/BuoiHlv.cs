using System.ComponentModel.DataAnnotations;

namespace GymManagement.Web.Data.Models
{
    public class BuoiHlv
    {
        public int BuoiHlvId { get; set; }
        
        public int? HlvId { get; set; }
        
        public int? ThanhVienId { get; set; }
        
        public int? LopHocId { get; set; }
        
        [Required]
        public DateOnly NgayTap { get; set; }
        
        [Required]
        public int ThoiLuongPhut { get; set; }
        
        [StringLength(300)]
        public string? GhiChu { get; set; }

        // Navigation properties
        public virtual NguoiDung? Hlv { get; set; }
        public virtual NguoiDung? ThanhVien { get; set; }
        public virtual LopHoc? LopHoc { get; set; }
    }
}
