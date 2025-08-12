using System.ComponentModel.DataAnnotations;

namespace GymManagement.Web.Models.DTOs
{
    public class CreateThongBaoDto
    {
        [Required(ErrorMessage = "Tiêu đề không được để trống")]
        [StringLength(100, ErrorMessage = "Tiêu đề không được vượt quá 100 ký tự")]
        public string TieuDe { get; set; } = string.Empty;

        [Required(ErrorMessage = "Nội dung không được để trống")]
        [StringLength(1000, ErrorMessage = "Nội dung không được vượt quá 1000 ký tự")]
        public string NoiDung { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn kênh gửi")]
        public string Kenh { get; set; } = "APP";

        // Single user
        public int? NguoiDungId { get; set; }

        // Multiple users
        public IEnumerable<int>? NguoiDungIds { get; set; }

        // Send to all members
        public bool SendToAll { get; set; } = false;

        // Filter for bulk send
        public string? LoaiNguoiDungFilter { get; set; }
    }

    public class EditThongBaoDto
    {
        public int ThongBaoId { get; set; }

        [Required(ErrorMessage = "Tiêu đề không được để trống")]
        [StringLength(100, ErrorMessage = "Tiêu đề không được vượt quá 100 ký tự")]
        public string TieuDe { get; set; } = string.Empty;

        [Required(ErrorMessage = "Nội dung không được để trống")]
        [StringLength(1000, ErrorMessage = "Nội dung không được vượt quá 1000 ký tự")]
        public string NoiDung { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn kênh gửi")]
        public string Kenh { get; set; } = "APP";

        public int? NguoiDungId { get; set; }

        public bool DaDoc { get; set; }
    }

    public class ThongBaoListDto
    {
        public int ThongBaoId { get; set; }
        public string? TieuDe { get; set; }
        public string? NoiDung { get; set; }
        public DateTime NgayTao { get; set; }
        public int? NguoiDungId { get; set; }
        public string? NguoiDungTen { get; set; }
        public string? Kenh { get; set; }
        public bool DaDoc { get; set; }
        public string KenhIcon => GetKenhIcon();
        public string StatusBadge => GetStatusBadge();

        private string GetKenhIcon()
        {
            return Kenh?.ToUpper() switch
            {
                "EMAIL" => "📧",
                "SMS" => "📱",
                "APP" => "📱",
                _ => "🔔"
            };
        }

        private string GetStatusBadge()
        {
            return DaDoc ? "success" : "warning";
        }
    }

    public class NotificationStatsDto
    {
        public int TotalNotifications { get; set; }
        public int UnreadNotifications { get; set; }
        public int EmailNotifications { get; set; }
        public int SmsNotifications { get; set; }
        public int AppNotifications { get; set; }
        public int TodayNotifications { get; set; }
        public int ThisWeekNotifications { get; set; }
        public int ThisMonthNotifications { get; set; }
    }
}
