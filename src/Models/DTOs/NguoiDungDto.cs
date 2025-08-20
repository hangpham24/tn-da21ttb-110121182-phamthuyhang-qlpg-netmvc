using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace GymManagement.Web.Models.DTOs
{
    public class NguoiDungDto
    {
        public int NguoiDungId { get; set; }
        
        [Required(ErrorMessage = "Loại người dùng là bắt buộc")]
        [Display(Name = "Loại người dùng")]
        public string LoaiNguoiDung { get; set; } = null!;
        
        [Required(ErrorMessage = "Họ là bắt buộc")]
        [Display(Name = "Họ")]
        [StringLength(50, ErrorMessage = "Họ không được vượt quá 50 ký tự")]
        public string Ho { get; set; } = null!;
        
        [Display(Name = "Tên")]
        [StringLength(50, ErrorMessage = "Tên không được vượt quá 50 ký tự")]
        public string? Ten { get; set; }
        
        [Display(Name = "Giới tính")]
        public string? GioiTinh { get; set; }
        
        [Display(Name = "Ngày sinh")]
        [DataType(DataType.Date)]
        public DateOnly? NgaySinh { get; set; }
        
        [Display(Name = "Số điện thoại")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        public string? SoDienThoai { get; set; }
        
        [Display(Name = "Email")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string? Email { get; set; }
        
        [Display(Name = "Địa chỉ")]
        [StringLength(200, ErrorMessage = "Địa chỉ không được vượt quá 200 ký tự")]
        public string? DiaChi { get; set; }
        
        [Display(Name = "Ngày tham gia")]
        [DataType(DataType.Date)]
        public DateOnly NgayThamGia { get; set; }
        
        [Display(Name = "Trạng thái")]
        public string TrangThai { get; set; } = "ACTIVE";

        [Display(Name = "Ảnh đại diện")]
        public string? AnhDaiDien { get; set; }

        [Display(Name = "Ngày tạo")]
        public DateTime NgayTao { get; set; } = DateTime.Now;

        // Thông tin tài khoản
        [Display(Name = "Tên đăng nhập")]
        public string? Username { get; set; }

        [Display(Name = "Có tài khoản")]
        public bool HasAccount { get; set; }

        [Display(Name = "Họ và tên")]
        public string HoTen => $"{Ho} {Ten}".Trim();
    }

    public class CreateNguoiDungDto
    {
        [Required(ErrorMessage = "Loại người dùng là bắt buộc")]
        [Display(Name = "Loại người dùng")]
        public string LoaiNguoiDung { get; set; } = null!;
        
        [Required(ErrorMessage = "Họ là bắt buộc")]
        [Display(Name = "Họ")]
        [StringLength(50, ErrorMessage = "Họ không được vượt quá 50 ký tự")]
        public string Ho { get; set; } = null!;
        
        [Display(Name = "Tên")]
        [StringLength(50, ErrorMessage = "Tên không được vượt quá 50 ký tự")]
        public string? Ten { get; set; }
        
        [Display(Name = "Giới tính")]
        public string? GioiTinh { get; set; }
        
        [Display(Name = "Ngày sinh")]
        [DataType(DataType.Date)]
        public DateOnly? NgaySinh { get; set; }
        
        [Display(Name = "Số điện thoại")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        public string? SoDienThoai { get; set; }
        
        [Display(Name = "Email")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string? Email { get; set; }

        [Display(Name = "Địa chỉ")]
        [StringLength(200, ErrorMessage = "Địa chỉ không được vượt quá 200 ký tự")]
        public string? DiaChi { get; set; }

        [Display(Name = "Tên đăng nhập")]
        [StringLength(50, ErrorMessage = "Tên đăng nhập không được vượt quá 50 ký tự")]
        public string? Username { get; set; }

        [Display(Name = "Vai trò")]
        public string? VaiTro { get; set; }

        [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
        [Display(Name = "Mật khẩu")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = null!;

        [Display(Name = "Xác nhận mật khẩu")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Mật khẩu xác nhận không khớp")]
        public string ConfirmPassword { get; set; } = null!;

        [Display(Name = "Ảnh đại diện")]
        public IFormFile? Avatar { get; set; }

        [Display(Name = "Trạng thái")]
        public bool TrangThai { get; set; } = true;
    }

    public class UpdateNguoiDungDto
    {
        public int NguoiDungId { get; set; }
        
        [Required(ErrorMessage = "Loại người dùng là bắt buộc")]
        [Display(Name = "Loại người dùng")]
        public string LoaiNguoiDung { get; set; } = null!;
        
        [Required(ErrorMessage = "Họ là bắt buộc")]
        [Display(Name = "Họ")]
        [StringLength(50, ErrorMessage = "Họ không được vượt quá 50 ký tự")]
        public string Ho { get; set; } = null!;
        
        [Display(Name = "Tên")]
        [StringLength(50, ErrorMessage = "Tên không được vượt quá 50 ký tự")]
        public string? Ten { get; set; }
        
        [Display(Name = "Giới tính")]
        public string? GioiTinh { get; set; }
        
        [Display(Name = "Ngày sinh")]
        [DataType(DataType.Date)]
        public DateOnly? NgaySinh { get; set; }
        
        [Display(Name = "Số điện thoại")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        public string? SoDienThoai { get; set; }
        
        [Display(Name = "Email")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string? Email { get; set; }
        
        [Display(Name = "Địa chỉ")]
        [StringLength(200, ErrorMessage = "Địa chỉ không được vượt quá 200 ký tự")]
        public string? DiaChi { get; set; }
        
        [Display(Name = "Trạng thái")]
        public string TrangThai { get; set; } = "ACTIVE";

        [Display(Name = "Ngày tham gia")]
        [DataType(DataType.Date)]
        public DateOnly NgayThamGia { get; set; }

        [Display(Name = "Ngày tạo")]
        public DateTime NgayTao { get; set; } = DateTime.Now;
    }
}
