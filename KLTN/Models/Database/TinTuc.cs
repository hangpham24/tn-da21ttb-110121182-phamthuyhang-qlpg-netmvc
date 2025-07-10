using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using KLTN.Models;
using Microsoft.AspNetCore.Http;

namespace KLTN.Models.Database
{
    public class TinTuc
    {
        [Key]
        public int MaTinTuc { get; set; }

        [NotMapped]
        public int Id { get { return MaTinTuc; } }

        [Required]
        [StringLength(200)]
        [Display(Name = "Tiêu đề")]
        public string TieuDe { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập mô tả ngắn")]
        [StringLength(500)]
        [Display(Name = "Mô tả ngắn")]
        public string MoTaNgan { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Nội dung")]
        public string NoiDung { get; set; } = string.Empty;

        [StringLength(255)]
        [Display(Name = "Hình ảnh URL")]
        public string? HinhAnhURL { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Danh mục")]
        public string DanhMuc { get; set; } = "TinTuc"; // Default value

        [NotMapped]
        [Display(Name = "File hình ảnh")]
        public IFormFile? HinhAnhFile { get; set; }

        [StringLength(100)]
        [Display(Name = "Tác giả (hiển thị)")]
        public string? TacGiaDisplay { get; set; }

        [ForeignKey("TaiKhoan")]
        [Display(Name = "Người đăng")]
        public int? NguoiDang { get; set; }

        [DataType(DataType.DateTime)]
        [Display(Name = "Ngày đăng")]
        public DateTime NgayDang { get; set; } = DateTime.Now;

        [Display(Name = "Hiển thị")]
        public bool HienThi { get; set; } = true;

        [Display(Name = "Tin nổi bật")]
        public bool NoiBat { get; set; } = false;
        
        // Kept for backward compatibility but no longer used - visibility is controlled by the HienThi property
        [StringLength(50)]
        [Display(Name = "Trạng thái")]
        public string? TrangThai { get; set; } = "Đã duyệt"; // Default to approved

        // Navigation properties
        public virtual TaiKhoan? TaiKhoan { get; set; }
    }
}
