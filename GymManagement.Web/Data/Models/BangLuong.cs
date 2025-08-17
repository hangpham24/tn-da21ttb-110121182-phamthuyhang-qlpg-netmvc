using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GymManagement.Web.Data.Models
{
    public class BangLuong
    {
        public BangLuong()
        {
            NgayTao = DateTime.Now;
        }

        public int BangLuongId { get; set; }
        
        public int? HlvId { get; set; }
        
        [Required]
        [StringLength(7)] // YYYY-MM
        [RegularExpression(@"^\d{4}-(0[1-9]|1[0-2])$", ErrorMessage = "Tháng phải có định dạng YYYY-MM (ví dụ: 2024-01)")]
        [Display(Name = "Tháng")]
        public string Thang { get; set; } = null!;
        
        [Required]
        [Column(TypeName = "decimal(12,2)")]
        [Range(1, 100000000, ErrorMessage = "Lương cơ bản phải từ 1 đến 100,000,000 VNĐ")]
        [Display(Name = "Lương cơ bản")]
        public decimal LuongCoBan { get; set; }
        
        [NotMapped]
        [Display(Name = "Tổng thanh toán")]
        public decimal TongThanhToan => LuongCoBan; // Simplified: only base salary

        [Display(Name = "Ngày thanh toán")]
        public DateOnly? NgayThanhToan { get; set; }

        [StringLength(500, ErrorMessage = "Ghi chú không được vượt quá 500 ký tự")]
        [Display(Name = "Ghi chú")]
        public string? GhiChu { get; set; }

        public DateTime NgayTao { get; set; }

        // Navigation properties
        public virtual NguoiDung? Hlv { get; set; }
    }
}
