using System.ComponentModel.DataAnnotations;

namespace GymManagement.Web.Models.DTOs
{
    public class CreateTinTucDto
    {
        [Required(ErrorMessage = "Tiêu đề là bắt buộc")]
        [StringLength(200, ErrorMessage = "Tiêu đề không được vượt quá 200 ký tự")]
        public string TieuDe { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Mô tả ngắn là bắt buộc")]
        [StringLength(500, ErrorMessage = "Mô tả ngắn không được vượt quá 500 ký tự")]
        public string MoTaNgan { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Nội dung là bắt buộc")]
        public string NoiDung { get; set; } = string.Empty;
        
        public IFormFile? AnhDaiDien { get; set; }
        
        public DateTime? NgayXuatBan { get; set; }
        
        public string TrangThai { get; set; } = "DRAFT";
        
        public bool NoiBat { get; set; } = false;
        
        [StringLength(160)]
        public string? MetaTitle { get; set; }
        
        [StringLength(160)]
        public string? MetaDescription { get; set; }
        
        [StringLength(500)]
        public string? MetaKeywords { get; set; }
    }

    public class EditTinTucDto : CreateTinTucDto
    {
        public int TinTucId { get; set; }
        public string? CurrentAnhDaiDien { get; set; }
        public bool RemoveImage { get; set; } = false;
    }

    public class TinTucListDto
    {
        public int TinTucId { get; set; }
        public string TieuDe { get; set; } = string.Empty;
        public string MoTaNgan { get; set; } = string.Empty;
        public string? AnhDaiDien { get; set; }
        public DateTime NgayTao { get; set; }
        public DateTime? NgayXuatBan { get; set; }
        public string? TenTacGia { get; set; }
        public int LuotXem { get; set; }
        public string TrangThai { get; set; } = string.Empty;
        public bool NoiBat { get; set; }
        public string? Slug { get; set; }
    }

    public class TinTucDetailDto : TinTucListDto
    {
        public string NoiDung { get; set; } = string.Empty;
        public DateTime? NgayCapNhat { get; set; }
        public int? TacGiaId { get; set; }
        public string? MetaTitle { get; set; }
        public string? MetaDescription { get; set; }
        public string? MetaKeywords { get; set; }
    }

    public class TinTucPublicDto
    {
        public int TinTucId { get; set; }
        public string TieuDe { get; set; } = string.Empty;
        public string MoTaNgan { get; set; } = string.Empty;
        public string NoiDung { get; set; } = string.Empty;
        public string? AnhDaiDien { get; set; }
        public DateTime? NgayXuatBan { get; set; }
        public string? TenTacGia { get; set; }
        public int LuotXem { get; set; }
        public string? Slug { get; set; }
        public bool NoiBat { get; set; }
    }
}
