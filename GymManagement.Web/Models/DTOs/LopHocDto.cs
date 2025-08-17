using System.ComponentModel.DataAnnotations;

namespace GymManagement.Web.Models.DTOs
{
    public class LopHocDto
    {
        public int LopHocId { get; set; }

        [Required(ErrorMessage = "Tên lớp học là bắt buộc")]
        [Display(Name = "Tên lớp học")]
        [StringLength(100, ErrorMessage = "Tên lớp học không được vượt quá 100 ký tự")]
        public string TenLop { get; set; } = null!;

        [Display(Name = "Mô tả")]
        [StringLength(500, ErrorMessage = "Mô tả không được vượt quá 500 ký tự")]
        public string? MoTa { get; set; }

        [Required(ErrorMessage = "Sức chứa tối đa là bắt buộc")]
        [Display(Name = "Sức chứa tối đa")]
        [Range(1, 100, ErrorMessage = "Sức chứa phải từ 1 đến 100 người")]
        public int SucChuaToiDa { get; set; }

        [Display(Name = "Thời lượng (phút)")]
        [Range(15, 300, ErrorMessage = "Thời lượng phải từ 15 đến 300 phút")]
        public int? ThoiLuongPhut { get; set; }

        [Display(Name = "Mức độ")]
        public string? MucDo { get; set; }

        [Display(Name = "Trạng thái")]
        public string TrangThai { get; set; } = "ACTIVE";

        [Display(Name = "Ngày tạo")]
        public DateTime NgayTao { get; set; }

        [Display(Name = "Huấn luyện viên")]
        public string? TrainerName { get; set; }

        [Display(Name = "Số lượng đã đăng ký")]
        public int RegisteredCount { get; set; }

        [Display(Name = "Còn trống")]
        public int AvailableSlots => SucChuaToiDa - RegisteredCount;

        [Display(Name = "Tỷ lệ lấp đầy")]
        public double FillRate => SucChuaToiDa > 0 ? (double)RegisteredCount / SucChuaToiDa * 100 : 0;
    }

    public class CreateLopHocDto
    {
        [Required(ErrorMessage = "Tên lớp học là bắt buộc")]
        [Display(Name = "Tên lớp học")]
        [StringLength(100, ErrorMessage = "Tên lớp học không được vượt quá 100 ký tự")]
        public string TenLop { get; set; } = null!;

        [Display(Name = "Mô tả")]
        [StringLength(500, ErrorMessage = "Mô tả không được vượt quá 500 ký tự")]
        public string? MoTa { get; set; }

        [Required(ErrorMessage = "Sức chứa tối đa là bắt buộc")]
        [Display(Name = "Sức chứa tối đa")]
        [Range(1, 100, ErrorMessage = "Sức chứa phải từ 1 đến 100 người")]
        public int SucChuaToiDa { get; set; }

        [Display(Name = "Thời lượng (phút)")]
        [Range(15, 300, ErrorMessage = "Thời lượng phải từ 15 đến 300 phút")]
        public int? ThoiLuongPhut { get; set; }

        [Display(Name = "Mức độ")]
        public string? MucDo { get; set; }
    }

    public class UpdateLopHocDto
    {
        public int LopHocId { get; set; }

        [Required(ErrorMessage = "Tên lớp học là bắt buộc")]
        [Display(Name = "Tên lớp học")]
        [StringLength(100, ErrorMessage = "Tên lớp học không được vượt quá 100 ký tự")]
        public string TenLop { get; set; } = null!;

        [Display(Name = "Mô tả")]
        [StringLength(500, ErrorMessage = "Mô tả không được vượt quá 500 ký tự")]
        public string? MoTa { get; set; }

        [Required(ErrorMessage = "Sức chứa tối đa là bắt buộc")]
        [Display(Name = "Sức chứa tối đa")]
        [Range(1, 100, ErrorMessage = "Sức chứa phải từ 1 đến 100 người")]
        public int SucChuaToiDa { get; set; }

        [Display(Name = "Thời lượng (phút)")]
        [Range(15, 300, ErrorMessage = "Thời lượng phải từ 15 đến 300 phút")]
        public int? ThoiLuongPhut { get; set; }

        [Display(Name = "Mức độ")]
        public string? MucDo { get; set; }

        [Display(Name = "Trạng thái")]
        public string TrangThai { get; set; } = "ACTIVE";
    }

    // Note: LichLopDto has been removed as LichLop table no longer exists
}
