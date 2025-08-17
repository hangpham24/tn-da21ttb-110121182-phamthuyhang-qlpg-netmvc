using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GymManagement.Web.Data.Models
{
    public class TinTuc
    {
        [Key]
        public int TinTucId { get; set; }
        
        [Required(ErrorMessage = "Tiêu đề là bắt buộc")]
        [StringLength(200, ErrorMessage = "Tiêu đề không được vượt quá 200 ký tự")]
        public string TieuDe { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Mô tả ngắn là bắt buộc")]
        [StringLength(500, ErrorMessage = "Mô tả ngắn không được vượt quá 500 ký tự")]
        public string MoTaNgan { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Nội dung là bắt buộc")]
        public string NoiDung { get; set; } = string.Empty;
        
        [StringLength(500)]
        public string? AnhDaiDien { get; set; }
        
        public DateTime NgayTao { get; set; } = DateTime.Now;
        
        public DateTime? NgayCapNhat { get; set; }
        
        public DateTime? NgayXuatBan { get; set; }
        
        public int? TacGiaId { get; set; }
        
        [StringLength(100)]
        public string? TenTacGia { get; set; }
        
        public int LuotXem { get; set; } = 0;
        
        [Required]
        [StringLength(20)]
        public string TrangThai { get; set; } = "DRAFT"; // DRAFT, PUBLISHED, ARCHIVED
        
        public bool NoiBat { get; set; } = false;
        
        [StringLength(200)]
        public string? Slug { get; set; }
        
        [StringLength(160)]
        public string? MetaTitle { get; set; }
        
        [StringLength(160)]
        public string? MetaDescription { get; set; }
        
        [StringLength(500)]
        public string? MetaKeywords { get; set; }

        // Navigation properties
        [ForeignKey("TacGiaId")]
        public virtual NguoiDung? TacGia { get; set; }
    }
}
