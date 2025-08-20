using GymManagement.Web.Data.Models;

namespace GymManagement.Web.Models.ViewModels
{
    public class MemberDashboardViewModel
    {
        public List<DangKy> ActiveRegistrations { get; set; } = new List<DangKy>();
        public List<Booking> UpcomingBookings { get; set; } = new List<Booking>();
        // Removed attendance/check-in related properties - handled at reception
        public List<ThongBao> UnreadNotifications { get; set; } = new List<ThongBao>();
        public PaymentStatsDto PaymentStats { get; set; } = new PaymentStatsDto();
        public string MemberName { get; set; } = string.Empty;
        public DateTime LastLoginTime { get; set; }
        public int TotalWorkoutDays { get; set; } // Keep for display but set to 0
        public decimal TotalSpent { get; set; }
        public string CurrentMembershipStatus { get; set; } = string.Empty;
        public List<LopHoc> RecommendedClasses { get; set; } = new List<LopHoc>();
        public QuickStatsDto QuickStats { get; set; } = new QuickStatsDto();
    }

    // Removed AttendanceStatsDto and DailyAttendanceDto - not needed for member dashboard

    public class PaymentStatsDto
    {
        public decimal ThisMonthSpent { get; set; }
        public decimal TotalSpent { get; set; }
        public int PendingPayments { get; set; }
        public ThanhToan? LastPayment { get; set; }
        public List<MonthlySpendingDto> MonthlySpending { get; set; } = new List<MonthlySpendingDto>();
    }

    public class MonthlySpendingDto
    {
        public string Month { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime Date { get; set; }
    }

    public class QuickStatsDto
    {
        public int ActiveRegistrations { get; set; }
        public int UpcomingBookings { get; set; }
        public int UnreadNotifications { get; set; }
        // Removed CheckInsThisWeek - not needed
        public string NextClassTime { get; set; } = string.Empty;
        public bool HasPendingPayments { get; set; }
    }
}
