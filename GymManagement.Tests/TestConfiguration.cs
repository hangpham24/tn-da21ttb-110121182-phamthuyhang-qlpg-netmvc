using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GymManagement.Tests
{
    /// <summary>
    /// Configuration helper for tests
    /// </summary>
    public static class TestConfiguration
    {
        /// <summary>
        /// Creates a test configuration
        /// </summary>
        public static IConfiguration CreateTestConfiguration()
        {
            var configBuilder = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    {"WalkIn:Settings:AllowDuplicatePhoneToday", "false"},
                    {"WalkIn:Settings:AutoCheckInAfterPayment", "true"},
                    {"WalkIn:Settings:RequirePhoneNumber", "true"},
                    {"WalkIn:Settings:MaxSessionsPerDay", "2"},
                    {"WalkIn:Settings:DefaultCheckoutTime", "22:00"},
                    {"WalkIn:Settings:EnableQRPayment", "true"},
                    {"WalkIn:Settings:EnableCashPayment", "true"},
                    {"WalkIn:DefaultPackages:DayPass:Name", "Vé ngày"},
                    {"WalkIn:DefaultPackages:DayPass:Price", "50000"},
                    {"WalkIn:DefaultPackages:DayPass:DurationHours", "24"},
                    {"WalkIn:DefaultPackages:ThreeHourPass:Name", "Vé 3 giờ"},
                    {"WalkIn:DefaultPackages:ThreeHourPass:Price", "30000"},
                    {"WalkIn:DefaultPackages:ThreeHourPass:DurationHours", "3"}
                });

            return configBuilder.Build();
        }

        /// <summary>
        /// Creates a test logger
        /// </summary>
        public static ILogger<T> CreateTestLogger<T>()
        {
            var serviceProvider = new ServiceCollection()
                .AddLogging(builder => builder.AddConsole())
                .BuildServiceProvider();

            return serviceProvider.GetRequiredService<ILogger<T>>();
        }
    }
}
