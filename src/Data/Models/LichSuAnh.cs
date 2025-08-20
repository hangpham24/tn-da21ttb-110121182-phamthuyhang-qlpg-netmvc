using System.ComponentModel.DataAnnotations;

namespace GymManagement.Web.Data.Models
{
    public class LichSuAnh
    {
        public int LichSuAnhId { get; set; }
        
        public int? NguoiDungId { get; set; }
        
        [StringLength(255)]
        public string? AnhCu { get; set; }
        
        [StringLength(255)]
        public string? AnhMoi { get; set; }
        
        public DateTime NgayCapNhat { get; set; }
        
        [StringLength(200)]
        public string? LyDo { get; set; }

        // Navigation properties
        public virtual NguoiDung? NguoiDung { get; set; }
    }
}
