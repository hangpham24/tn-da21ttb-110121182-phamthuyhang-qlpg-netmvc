namespace GymManagement.Web.Models
{
    /// <summary>
    /// Configuration model for commission calculation rates
    /// </summary>
    public class CommissionConfiguration
    {
        /// <summary>
        /// Base commission rate for package registrations (default: 5%)
        /// </summary>
        public decimal PackageCommissionRate { get; set; } = 0.05m;

        /// <summary>
        /// Commission rate for class registrations (default: 3%)
        /// </summary>
        public decimal ClassCommissionRate { get; set; } = 0.03m;

        /// <summary>
        /// Commission rate for personal training sessions (default: 10%)
        /// </summary>
        public decimal PersonalTrainingRate { get; set; } = 0.10m;

        /// <summary>
        /// Performance bonus rate based on student count (default: 2%)
        /// </summary>
        public decimal PerformanceBonusRate { get; set; } = 0.02m;

        /// <summary>
        /// Attendance bonus rate for high attendance classes (default: 1%)
        /// </summary>
        public decimal AttendanceBonusRate { get; set; } = 0.01m;

        /// <summary>
        /// Minimum student count to qualify for performance bonus (default: 10)
        /// </summary>
        public int MinStudentCountForBonus { get; set; } = 10;

        /// <summary>
        /// Minimum attendance rate to qualify for attendance bonus (default: 80%)
        /// </summary>
        public decimal MinAttendanceRateForBonus { get; set; } = 0.80m;

        /// <summary>
        /// Maximum commission cap per month (default: 5,000,000 VND)
        /// </summary>
        public decimal MaxCommissionPerMonth { get; set; } = 5000000m;

        /// <summary>
        /// Tier-based commission rates for high performers
        /// </summary>
        public List<CommissionTier> CommissionTiers { get; set; } = new()
        {
            new CommissionTier { MinRevenue = 0, MaxRevenue = 10000000, Rate = 0.05m },
            new CommissionTier { MinRevenue = 10000000, MaxRevenue = 20000000, Rate = 0.07m },
            new CommissionTier { MinRevenue = 20000000, MaxRevenue = decimal.MaxValue, Rate = 0.10m }
        };
    }

    /// <summary>
    /// Commission tier for progressive commission calculation
    /// </summary>
    public class CommissionTier
    {
        /// <summary>
        /// Minimum revenue for this tier (inclusive)
        /// </summary>
        public decimal MinRevenue { get; set; }

        /// <summary>
        /// Maximum revenue for this tier (exclusive)
        /// </summary>
        public decimal MaxRevenue { get; set; }

        /// <summary>
        /// Commission rate for this tier
        /// </summary>
        public decimal Rate { get; set; }
    }
}
