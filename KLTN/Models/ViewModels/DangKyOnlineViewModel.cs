using System;
using System.ComponentModel.DataAnnotations;
using KLTN.Models.Database;

namespace KLTN.Models.ViewModels
{
    public class DangKyOnlineViewModel
    {
        public int DichVuId { get; set; }
        [Required(ErrorMessage = "Tên dịch vụ không được để trống")]
        public string TenDichVu { get; set; }
        [Required(ErrorMessage = "Loại dịch vụ không được để trống")]
        public string LoaiDichVu { get; set; }
        [Required(ErrorMessage = "Giá dịch vụ không được để trống")]
        public decimal GiaDichVu { get; set; }
        [Required(ErrorMessage = "Tên thành viên không được để trống")]
        public string TenThanhVien { get; set; }
        [Required(ErrorMessage = "Email không được để trống")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; }
        [Required(ErrorMessage = "Số điện thoại không được để trống")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        public string SoDienThoai { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn ngày bắt đầu")]
        [Display(Name = "Ngày bắt đầu")]
        [DataType(DataType.Date)]
        public DateTime NgayBatDau { get; set; }

        [Display(Name = "Ghi chú")]
        public string? GhiChu { get; set; }

        // Thông tin thêm cho lớp học
        public string? TenLopHoc { get; set; }
        public string? LichHoc { get; set; }
        public int? SoLuongToiDa { get; set; }

        // Thông tin gói tập
        public int? MaGoiTap { get; set; }
        public string? TenGoiTap { get; set; }
        public int? ThoiHanThang { get; set; }

        // Thông tin lớp học
        public int? MaLopHoc { get; set; }
        public int? SoBuoi { get; set; }

        [Display(Name = "Phương thức thanh toán")]
        public string? PhuongThucThanhToan { get; set; }
    }
} 