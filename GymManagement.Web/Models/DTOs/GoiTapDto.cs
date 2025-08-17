using System.ComponentModel.DataAnnotations;

namespace GymManagement.Web.Models.DTOs
{
    public class GoiTapDto
    {
        public int GoiTapId { get; set; }
        
        [Required(ErrorMessage = "Tên gói là bắt buộc")]
        [Display(Name = "Tên gói")]
        [StringLength(100, ErrorMessage = "Tên gói không được vượt quá 100 ký tự")]
        public string TenGoi { get; set; } = null!;
        
        [Required(ErrorMessage = "Thời hạn tháng là bắt buộc")]
        [Display(Name = "Thời hạn (tháng)")]
        [Range(1, 60, ErrorMessage = "Thời hạn phải từ 1 đến 60 tháng")]
        public int ThoiHanThang { get; set; }
        
        [Display(Name = "Số buổi tối đa")]
        [Range(0, int.MaxValue, ErrorMessage = "Số buổi tối đa phải lớn hơn hoặc bằng 0")]
        public int? SoBuoiToiDa { get; set; }
        
        [Required(ErrorMessage = "Giá là bắt buộc")]
        [Display(Name = "Giá (VNĐ)")]
        [Range(0, double.MaxValue, ErrorMessage = "Giá phải lớn hơn 0")]
        [DataType(DataType.Currency)]
        public decimal Gia { get; set; }
        
        [Display(Name = "Mô tả")]
        [StringLength(500, ErrorMessage = "Mô tả không được vượt quá 500 ký tự")]
        public string? MoTa { get; set; }

        [Display(Name = "Loại gói")]
        public string? LoaiGoi { get; set; }

        [Display(Name = "Trạng thái")]
        public string TrangThai { get; set; } = "ACTIVE";

        [Display(Name = "Ưu đãi đặc biệt")]
        public string? UuDaiDacBiet { get; set; }

        [Display(Name = "Ngày tạo")]
        public DateTime NgayTao { get; set; } = DateTime.Now;

        [Display(Name = "Giá định dạng")]
        public string GiaFormatted => Gia.ToString("N0") + " VNĐ";

        [Display(Name = "Số lượng đăng ký")]
        public int? SoLuongDangKy { get; set; }
    }

    public class CreateGoiTapDto
    {
        [Required(ErrorMessage = "Tên gói là bắt buộc")]
        [Display(Name = "Tên gói")]
        [StringLength(100, ErrorMessage = "Tên gói không được vượt quá 100 ký tự")]
        public string TenGoi { get; set; } = null!;
        
        [Required(ErrorMessage = "Thời hạn tháng là bắt buộc")]
        [Display(Name = "Thời hạn (tháng)")]
        [Range(1, 60, ErrorMessage = "Thời hạn phải từ 1 đến 60 tháng")]
        public int ThoiHanThang { get; set; }
        
        [Display(Name = "Số buổi tối đa")]
        [Range(0, int.MaxValue, ErrorMessage = "Số buổi tối đa phải lớn hơn hoặc bằng 0")]
        public int? SoBuoiToiDa { get; set; }
        
        [Required(ErrorMessage = "Giá là bắt buộc")]
        [Display(Name = "Giá (VNĐ)")]
        [Range(0, double.MaxValue, ErrorMessage = "Giá phải lớn hơn 0")]
        [DataType(DataType.Currency)]
        public decimal Gia { get; set; }
        
        [Display(Name = "Mô tả")]
        [StringLength(500, ErrorMessage = "Mô tả không được vượt quá 500 ký tự")]
        public string? MoTa { get; set; }


    }

    public class UpdateGoiTapDto
    {
        public int GoiTapId { get; set; }
        
        [Required(ErrorMessage = "Tên gói là bắt buộc")]
        [Display(Name = "Tên gói")]
        [StringLength(100, ErrorMessage = "Tên gói không được vượt quá 100 ký tự")]
        public string TenGoi { get; set; } = null!;
        
        [Required(ErrorMessage = "Thời hạn tháng là bắt buộc")]
        [Display(Name = "Thời hạn (tháng)")]
        [Range(1, 60, ErrorMessage = "Thời hạn phải từ 1 đến 60 tháng")]
        public int ThoiHanThang { get; set; }
        
        [Display(Name = "Số buổi tối đa")]
        [Range(0, int.MaxValue, ErrorMessage = "Số buổi tối đa phải lớn hơn hoặc bằng 0")]
        public int? SoBuoiToiDa { get; set; }
        
        [Required(ErrorMessage = "Giá là bắt buộc")]
        [Display(Name = "Giá (VNĐ)")]
        [Range(0, double.MaxValue, ErrorMessage = "Giá phải lớn hơn 0")]
        [DataType(DataType.Currency)]
        public decimal Gia { get; set; }
        
        [Display(Name = "Mô tả")]
        [StringLength(500, ErrorMessage = "Mô tả không được vượt quá 500 ký tự")]
        public string? MoTa { get; set; }

        [Display(Name = "Loại gói")]
        public string? LoaiGoi { get; set; }

        [Display(Name = "Trạng thái")]
        public string TrangThai { get; set; } = "ACTIVE";

        [Display(Name = "Ưu đãi đặc biệt")]
        public string? UuDaiDacBiet { get; set; }
    }
}
