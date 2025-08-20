using GymManagement.Web.Models.DTOs;
using GymManagement.Web.Services;

namespace GymManagement.Web.Services
{
    /// <summary>
    /// Background service tự động gửi thông báo gia hạn vào 00h hàng ngày
    /// </summary>
    public class ExpiryNotificationBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ExpiryNotificationBackgroundService> _logger;
        private readonly TimeSpan _dailyRunTime = new TimeSpan(0, 0, 0); // 00:00:00

        public ExpiryNotificationBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<ExpiryNotificationBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("🚀 ExpiryNotificationBackgroundService started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var now = DateTime.Now;
                    var nextRun = GetNextRunTime(now);
                    var delay = nextRun - now;

                    _logger.LogInformation("⏰ Next expiry notification check scheduled at: {NextRun} (in {Delay})", 
                        nextRun.ToString("dd/MM/yyyy HH:mm:ss"), delay);

                    // Wait until next run time
                    await Task.Delay(delay, stoppingToken);

                    if (!stoppingToken.IsCancellationRequested)
                    {
                        await CheckAndSendExpiryNotificationsAsync();
                    }
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("🛑 ExpiryNotificationBackgroundService cancelled");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Error in ExpiryNotificationBackgroundService");
                    // Wait 1 hour before retrying on error
                    await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
                }
            }
        }

        private DateTime GetNextRunTime(DateTime currentTime)
        {
            var today = currentTime.Date;
            var todayRunTime = today.Add(_dailyRunTime);

            // If current time is before today's run time, run today
            if (currentTime < todayRunTime)
            {
                return todayRunTime;
            }
            
            // Otherwise, run tomorrow
            return today.AddDays(1).Add(_dailyRunTime);
        }

        private async Task CheckAndSendExpiryNotificationsAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            
            try
            {
                _logger.LogInformation("🔍 Starting daily expiry notification check at {Time}", DateTime.Now);

                var nguoiDungService = scope.ServiceProvider.GetRequiredService<INguoiDungService>();
                var dangKyService = scope.ServiceProvider.GetRequiredService<IDangKyService>();
                var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
                var thongBaoService = scope.ServiceProvider.GetRequiredService<IThongBaoService>();

                // Get all members
                var allUsers = await nguoiDungService.GetAllAsync();
                var members = allUsers.Where(u => u.LoaiNguoiDung == "THANHVIEN").ToList();

                var expiringUsers = new List<NguoiDungWithSubscriptionDto>();

                // Check each member for expiring packages
                foreach (var user in members)
                {
                    var activeRegistrations = await dangKyService.GetActiveRegistrationsByMemberIdAsync(user.NguoiDungId);
                    var packageRegistration = activeRegistrations.FirstOrDefault(r => r.GoiTapId != null);
                    
                    if (packageRegistration != null)
                    {
                        var expiryDate = packageRegistration.NgayKetThuc.ToDateTime(TimeOnly.MinValue);
                        var daysUntilExpiry = (expiryDate - DateTime.Now).TotalDays;

                        // Check if expiring within 7 days and has email
                        if (daysUntilExpiry >= 0 && daysUntilExpiry <= 7 && !string.IsNullOrEmpty(user.Email))
                        {
                            var userWithSub = new NguoiDungWithSubscriptionDto
                            {
                                NguoiDungId = user.NguoiDungId,
                                Ho = user.Ho,
                                Ten = user.Ten,
                                Email = user.Email,
                                ActivePackageRegistration = packageRegistration,
                                ActivePackage = packageRegistration.GoiTap,
                                PackageExpiryDate = expiryDate
                            };
                            
                            expiringUsers.Add(userWithSub);
                        }
                    }
                }

                if (!expiringUsers.Any())
                {
                    _logger.LogInformation("✅ No users with expiring packages found");
                    return;
                }

                _logger.LogInformation("📧 Found {Count} users with expiring packages, sending notifications...", expiringUsers.Count);

                // Send notifications
                var successCount = 0;
                var failedEmails = new List<string>();

                foreach (var user in expiringUsers)
                {
                    try
                    {
                        var daysRemaining = (int)(user.PackageExpiryDate!.Value - DateTime.Now).TotalDays;
                        var packageName = user.ActivePackage?.TenGoi ?? "Gói tập";
                        var memberName = $"{user.Ho} {user.Ten}".Trim();

                        // Send email using EmailService
                        await emailService.SendMembershipExpiryReminderAsync(
                            user.Email!,
                            memberName,
                            packageName,
                            user.PackageExpiryDate.Value,
                            daysRemaining
                        );

                        // Create notification in database
                        await thongBaoService.CreateNotificationAsync(
                            user.NguoiDungId,
                            $"⏳ Gói tập sắp hết hạn - còn {daysRemaining} ngày",
                            $"Xin chào {memberName},\n\nGói tập \"{packageName}\" của bạn sẽ hết hạn vào ngày {user.PackageExpiryDate.Value:dd/MM/yyyy} (còn {daysRemaining} ngày).\n\nVui lòng liên hệ để gia hạn gói tập và tiếp tục sử dụng dịch vụ.\n\nTrân trọng,\nĐội ngũ Gym Management",
                            "EMAIL"
                        );

                        successCount++;
                        _logger.LogInformation("✅ Sent auto expiry notification to {Email} ({Name})", user.Email, memberName);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "❌ Failed to send auto expiry notification to {Email}", user.Email);
                        failedEmails.Add(user.Email!);
                    }
                }

                _logger.LogInformation("🎯 Daily expiry notification completed - Success: {SuccessCount}, Failed: {FailedCount}", 
                    successCount, failedEmails.Count);

                if (failedEmails.Any())
                {
                    _logger.LogWarning("⚠️ Failed to send notifications to: {FailedEmails}", string.Join(", ", failedEmails));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Critical error in daily expiry notification check");
            }
        }
    }
}
