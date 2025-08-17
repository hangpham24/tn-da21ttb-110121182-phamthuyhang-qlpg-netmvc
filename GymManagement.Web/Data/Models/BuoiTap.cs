using System.ComponentModel.DataAnnotations;

namespace GymManagement.Web.Data.Models
{
    public class BuoiTap
    {
        public int BuoiTapId { get; set; }
        
        public int? ThanhVienId { get; set; }
        
        public int? LopHocId { get; set; }
        
        [Required]
        public DateTime ThoiGianVao { get; set; }
        
        public DateTime? ThoiGianRa { get; set; }
        
        [StringLength(200)]
        public string? GhiChu { get; set; }

        // Navigation properties
        public virtual NguoiDung? ThanhVien { get; set; }
        public virtual LopHoc? LopHoc { get; set; }
    }
}
