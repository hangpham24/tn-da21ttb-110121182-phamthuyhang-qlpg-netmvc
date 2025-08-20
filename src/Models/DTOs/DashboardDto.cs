using System.ComponentModel.DataAnnotations;

namespace GymManagement.Web.Models.DTOs
{
    public class DashboardDto
    {
        [Display(Name = "Tổng số thành viên")]
        public int TotalMembers { get; set; }

        [Display(Name = "Thành viên hoạt động")]
        public int ActiveMembers { get; set; }

        [Display(Name = "Tổng số huấn luyện viên")]
        public int TotalTrainers { get; set; }

        [Display(Name = "Tổng số lớp học")]
        public int TotalClasses { get; set; }

        [Display(Name = "Lớp học hôm nay")]
        public int TodayClasses { get; set; }

        [Display(Name = "Doanh thu tháng này")]
        [DataType(DataType.Currency)]
        public decimal MonthlyRevenue { get; set; }

        [Display(Name = "Doanh thu hôm nay")]
        [DataType(DataType.Currency)]
        public decimal TodayRevenue { get; set; }

        [Display(Name = "Số lượng đăng ký mới")]
        public int NewRegistrations { get; set; }

        [Display(Name = "Điểm danh hôm nay")]
        public int TodayAttendance { get; set; }

        [Display(Name = "Gói tập phổ biến")]
        public List<PopularPackageDto> PopularPackages { get; set; } = new();

        [Display(Name = "Doanh thu theo tháng")]
        public List<MonthlyRevenueDto> MonthlyRevenueData { get; set; } = new();

        [Display(Name = "Thống kê điểm danh")]
        public List<AttendanceStatsDto> AttendanceStats { get; set; } = new();

        [Display(Name = "Lớp học sắp tới")]
        public List<UpcomingClassDto> UpcomingClasses { get; set; } = new();

        [Display(Name = "Thành viên mới")]
        public List<RecentMemberDto> RecentMembers { get; set; } = new();
    }

    public class PopularPackageDto
    {
        public string PackageName { get; set; } = null!;
        public int RegistrationCount { get; set; }
        public decimal Revenue { get; set; }
    }

    public class MonthlyRevenueDto
    {
        public string Month { get; set; } = null!;
        public decimal Revenue { get; set; }
    }

    public class AttendanceStatsDto
    {
        public string Date { get; set; } = null!;
        public int AttendanceCount { get; set; }
    }

    public class UpcomingClassDto
    {
        public string ClassName { get; set; } = null!;
        public string TrainerName { get; set; } = null!;
        public DateTime StartTime { get; set; }
        public int RegisteredCount { get; set; }
        public int MaxCapacity { get; set; }
    }

    public class RecentMemberDto
    {
        public string MemberName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public DateTime JoinDate { get; set; }
        public string PackageName { get; set; } = null!;
    }
}
