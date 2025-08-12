using GymManagement.Web.Data;
using GymManagement.Web.Data.Models;
using GymManagement.Web.Data.Repositories;
using GymManagement.Web.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Text.Encodings.Web;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging;

namespace GymManagement.Web.Services
{
    public class BangLuongService : IBangLuongService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IBangLuongRepository _bangLuongRepository;
        private readonly INguoiDungRepository _nguoiDungRepository;
        private readonly IThongBaoService _thongBaoService;
        private readonly IEmailService _emailService;
        private readonly IMemoryCache _cache;
        private readonly IAuditLogService _auditLog;
        private readonly CommissionConfiguration _commissionConfig;
        private readonly ILogger<BangLuongService> _logger;

        // Concurrency control for salary generation
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> _salaryGenerationLocks = new();

        public BangLuongService(
            IUnitOfWork unitOfWork,
            IBangLuongRepository bangLuongRepository,
            INguoiDungRepository nguoiDungRepository,
            IThongBaoService thongBaoService,
            IEmailService emailService,
            IMemoryCache cache,
            IAuditLogService auditLog,
            IOptions<CommissionConfiguration> commissionConfig,
            ILogger<BangLuongService> logger)
        {
            _unitOfWork = unitOfWork;
            _bangLuongRepository = bangLuongRepository;
            _nguoiDungRepository = nguoiDungRepository;
            _thongBaoService = thongBaoService;
            _emailService = emailService;
            _cache = cache;
            _auditLog = auditLog;
            _commissionConfig = commissionConfig.Value;
            _logger = logger;
        }

        public async Task<IEnumerable<BangLuong>> GetAllAsync()
        {
            return await _bangLuongRepository.GetAllAsync();
        }

        public async Task<BangLuong?> GetByIdAsync(int id)
        {
            return await _bangLuongRepository.GetByIdAsync(id);
        }

        public async Task<BangLuong> CreateAsync(BangLuong bangLuong)
        {
            var created = await _bangLuongRepository.AddAsync(bangLuong);
            await _unitOfWork.SaveChangesAsync();

            // Invalidate cache for this month
            InvalidateMonthCache(bangLuong.Thang);

            return created;
        }

        public async Task<BangLuong> UpdateAsync(BangLuong bangLuong)
        {
            await _bangLuongRepository.UpdateAsync(bangLuong);
            await _unitOfWork.SaveChangesAsync();

            // Invalidate cache for this month
            InvalidateMonthCache(bangLuong.Thang);

            return bangLuong;
        }


        public async Task<IEnumerable<BangLuong>> GetByTrainerIdAsync(int hlvId)
        {
            return await _bangLuongRepository.GetByHlvIdAsync(hlvId);
        }

        public async Task<IEnumerable<BangLuong>> GetByMonthAsync(string thang)
        {
            // Use cache for monthly data (15 minutes)
            var cacheKey = $"salary_month_{thang}";

            if (_cache.TryGetValue(cacheKey, out IEnumerable<BangLuong>? cachedSalaries))
            {
                return cachedSalaries!;
            }

            var salaries = await _bangLuongRepository.GetByMonthAsync(thang);

            // Cache for 15 minutes
            _cache.Set(cacheKey, salaries, TimeSpan.FromMinutes(15));

            return salaries;
        }

        public async Task<BangLuong?> GetByTrainerAndMonthAsync(int hlvId, string thang)
        {
            return await _bangLuongRepository.GetByHlvAndMonthAsync(hlvId, thang);
        }

        public async Task<IEnumerable<BangLuong>> GetUnpaidSalariesAsync()
        {
            return await _bangLuongRepository.GetUnpaidSalariesAsync();
        }

        // 🚀 IMPROVED & SIMPLIFIED METHOD WITH TRANSACTION SCOPE AND CONCURRENCY CONTROL
        public async Task<bool> GenerateMonthlySalariesAsync(string thang)
        {
            _logger.LogInformation("=== GenerateMonthlySalariesAsync START for month: {Month} ===", thang);

            // Get or create a semaphore for this month to prevent concurrent salary generation
            var lockKey = $"salary_generation_{thang}";
            var semaphore = _salaryGenerationLocks.GetOrAdd(lockKey, _ => new SemaphoreSlim(1, 1));

            // Wait for exclusive access to salary generation for this month
            await semaphore.WaitAsync();

            try
            {
                // Double-check if salaries already exist after acquiring lock
                var existingSalariesCount = await _bangLuongRepository.GetSalaryCountByMonthAsync(thang);
                _logger.LogInformation("Existing salaries count for month {Month}: {Count}", thang, existingSalariesCount);

                if (existingSalariesCount > 0)
                {
                    _logger.LogWarning("Salaries already exist for month {Month}, count: {Count}", thang, existingSalariesCount);
                    throw new InvalidOperationException($"Bảng lương cho tháng {thang} đã được tạo trước đó. Không thể tạo lại.");
                }

                // Use database transaction to ensure atomicity
                using var transaction = await _unitOfWork.Context.Database.BeginTransactionAsync();

                try
                {
                    // Validate month format
                    if (!IsValidMonthFormat(thang))
                {
                    throw new ArgumentException("Định dạng tháng không hợp lệ. Vui lòng sử dụng định dạng YYYY-MM (ví dụ: 2024-12) và đảm bảo tháng không quá xa trong tương lai.");
                }

                // Get all active trainers
                var trainers = await _nguoiDungRepository.GetTrainersAsync();
                _logger.LogInformation("Found {TrainerCount} trainers for salary generation", trainers.Count());

                if (!trainers.Any())
                {
                    _logger.LogWarning("No trainers found in system for salary generation");
                    throw new InvalidOperationException("Không tìm thấy huấn luyện viên nào trong hệ thống để tạo bảng lương.");
                }

                var successCount = 0;
                var salariesToCreate = new List<BangLuong>();
                var notificationsToSend = new List<(NguoiDung trainer, BangLuong salary, CommissionBreakdown breakdown)>();

                // Phase 1: Validate and prepare all salary records
                foreach (var trainer in trainers)
                {
                    try
                    {
                        _logger.LogInformation("Processing trainer {TrainerId} - {TrainerName}", trainer.NguoiDungId, $"{trainer.Ho} {trainer.Ten}");

                        // Check if salary already exists for this month
                        var existingSalary = await _bangLuongRepository.GetByHlvAndMonthAsync(trainer.NguoiDungId, thang);
                        if (existingSalary != null)
                        {
                            _logger.LogInformation("Salary already exists for trainer {TrainerId}, skipping", trainer.NguoiDungId);
                            continue;
                        }

                        // Calculate base salary - đơn giản hóa
                        decimal baseSalary = await GetBaseSalaryForTrainer(trainer.NguoiDungId);
                        _logger.LogInformation("Base salary for trainer {TrainerId}: {BaseSalary}", trainer.NguoiDungId, baseSalary);

                        // Calculate simplified commission breakdown
                        var commissionBreakdown = await CalculateSimplifiedCommissionAsync(trainer.NguoiDungId, thang);
                        _logger.LogInformation("Commission for trainer {TrainerId}: {Commission}", trainer.NguoiDungId, commissionBreakdown.TotalCommission);

                        // Create salary record - simplified
                        var bangLuong = new BangLuong
                        {
                            HlvId = trainer.NguoiDungId,
                            Thang = thang,
                            LuongCoBan = baseSalary,
                            TienHoaHong = commissionBreakdown.TotalCommission
                        };

                        salariesToCreate.Add(bangLuong);
                        notificationsToSend.Add((trainer, bangLuong, commissionBreakdown));
                        successCount++;
                        _logger.LogInformation("Successfully prepared salary for trainer {TrainerId}", trainer.NguoiDungId);
                    }
                    catch (Exception ex)
                    {
                        // Log error for individual trainer but continue with others
                        _logger.LogError(ex, "Error processing salary for trainer {TrainerId}: {Error}", trainer.NguoiDungId, ex.Message);
                        // This ensures one trainer's error doesn't stop the whole process
                        continue;
                    }
                }

                _logger.LogInformation("Phase 1 completed - {Count} salaries prepared for creation", salariesToCreate.Count);

                // Phase 2: Bulk insert salary records within transaction
                if (salariesToCreate.Any())
                {
                    _logger.LogInformation("Phase 2 - Inserting {Count} salary records", salariesToCreate.Count);

                    foreach (var salary in salariesToCreate)
                    {
                        await _bangLuongRepository.AddAsync(salary);
                    }

                    // Save all salary records in one transaction
                    await _unitOfWork.SaveChangesAsync();
                    _logger.LogInformation("Phase 2 completed - All salary records saved to database");

                    // Commit transaction for salary creation
                    await transaction.CommitAsync();
                }
                else
                {
                    // No salaries to create, rollback transaction
                    await transaction.RollbackAsync();
                    return false;
                }

                // Phase 3: Send notifications and emails (outside transaction)
                // These are non-critical operations that shouldn't rollback salary creation
                foreach (var (trainer, salary, breakdown) in notificationsToSend)
                {
                    try
                    {
                        // Send notification
                        await _thongBaoService.CreateNotificationAsync(
                            trainer.NguoiDungId,
                            "Bảng lương tháng mới",
                            $"Bảng lương tháng {thang} đã được tạo.\n" +
                            $"Lương cơ bản: {salary.LuongCoBan:N0} VNĐ\n" +
                            $"Hoa hồng: {breakdown.TotalCommission:N0} VNĐ\n" +
                            $"Tổng cộng: {salary.TongThanhToan:N0} VNĐ",
                            "APP"
                        );

                        // Send email if available
                        if (!string.IsNullOrEmpty(trainer.Email))
                        {
                            await SendSimplifiedSalaryEmailAsync(trainer, salary, breakdown, thang);
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log notification/email errors but don't fail the whole operation
                        // Salary creation has already been committed successfully
                        continue;
                    }
                }

                // Invalidate cache for this month
                InvalidateMonthCache(thang);

                _logger.LogInformation("GenerateMonthlySalariesAsync completed - Success count: {SuccessCount}", successCount);
                _logger.LogInformation("=== GenerateMonthlySalariesAsync END for month: {Month} ===", thang);

                return successCount > 0;
                }
                catch (Exception ex)
                {
                    // Rollback transaction on any error
                    await transaction.RollbackAsync();
                    throw new Exception($"Lỗi khi tạo bảng lương tháng {thang}: {ex.Message}", ex);
                }
            }
            finally
            {
                // Always release the semaphore to allow other operations
                semaphore.Release();

                // Clean up old semaphores to prevent memory leaks
                if (_salaryGenerationLocks.Count > 100) // Arbitrary threshold
                {
                    var keysToRemove = _salaryGenerationLocks.Keys.Take(50).ToList();
                    foreach (var key in keysToRemove)
                    {
                        if (_salaryGenerationLocks.TryRemove(key, out var oldSemaphore))
                        {
                            oldSemaphore.Dispose();
                        }
                    }
                }
            }
        }

        // 🔧 HELPER METHODS - SIMPLIFIED

        // Enhanced helper method for validation
        private bool IsValidMonthFormat(string thang)
        {
            // Check basic format
            if (string.IsNullOrWhiteSpace(thang) || thang.Length != 7)
                return false;

            // Check YYYY-MM pattern
            if (!System.Text.RegularExpressions.Regex.IsMatch(thang, @"^\d{4}-\d{2}$"))
                return false;

            // Parse and validate date
            if (!DateTime.TryParseExact($"{thang}-01", "yyyy-MM-dd", null,
                System.Globalization.DateTimeStyles.None, out var parsedDate))
                return false;

            // Check reasonable year range (2020-2030)
            if (parsedDate.Year < 2020 || parsedDate.Year > 2030)
                return false;

            // Check not too far in the future (max 2 months ahead)
            var maxFutureDate = DateTime.Today.AddMonths(2);
            if (parsedDate > maxFutureDate)
                return false;

            return true;
        }

        // Simplified base salary calculation
        private async Task<decimal> GetBaseSalaryForTrainer(int hlvId)
        {
            // Đơn giản hóa: lương cơ bản theo kinh nghiệm
            var trainer = await _nguoiDungRepository.GetByIdAsync(hlvId);
            if (trainer == null) return 8000000; // Default 8M VND

            // Calculate years of experience
            var yearsOfExperience = DateTime.Now.Year - trainer.NgayThamGia.Year;
            
            // Simple salary tiers based on experience
            return yearsOfExperience switch
            {
                >= 5 => 15000000, // 15M for 5+ years
                >= 3 => 12000000, // 12M for 3+ years  
                >= 1 => 10000000, // 10M for 1+ years
                _ => 8000000      // 8M for new trainers
            };
        }

        // Simplified commission calculation
        // 🚀 ENHANCED COMMISSION CALCULATION WITH CONFIGURABLE RATES
        private async Task<CommissionBreakdown> CalculateSimplifiedCommissionAsync(int hlvId, string thang)
        {
            // Parse month
            if (!DateTime.TryParseExact($"{thang}-01", "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out var monthStart))
                return new CommissionBreakdown();

            var monthEnd = monthStart.AddMonths(1).AddDays(-1);
            var breakdown = new CommissionBreakdown();

            // 1. Calculate different types of commission with configurable rates and validation
            breakdown.PackageCommission = ValidateCommissionAmount(
                await CalculatePackageCommissionAsync(hlvId, monthStart, monthEnd), "Package Commission");
            breakdown.ClassCommission = ValidateCommissionAmount(
                await CalculateClassCommissionAsync(hlvId, monthStart, monthEnd), "Class Commission");
            breakdown.PersonalCommission = ValidateCommissionAmount(
                await CalculatePersonalTrainingCommissionAsync(hlvId, monthStart, monthEnd), "Personal Training Commission");

            // 2. Calculate performance and attendance bonuses with validation
            breakdown.PerformanceBonus = ValidateCommissionAmount(
                await CalculatePerformanceBonusAsync(hlvId, monthStart, monthEnd), "Performance Bonus");
            breakdown.AttendanceBonus = ValidateCommissionAmount(
                await CalculateAttendanceBonusAsync(hlvId, monthStart, monthEnd), "Attendance Bonus");

            // 3. Apply tier-based commission calculation
            var totalRevenue = breakdown.PackageCommission + breakdown.ClassCommission + breakdown.PersonalCommission;
            var tierBonus = CalculateTierBasedCommission(totalRevenue);
            breakdown.PerformanceBonus += tierBonus;

            // 4. Apply commission cap
            var totalCommission = breakdown.TotalCommission;
            if (totalCommission > _commissionConfig.MaxCommissionPerMonth)
            {
                var reductionFactor = _commissionConfig.MaxCommissionPerMonth / totalCommission;
                breakdown.PackageCommission *= reductionFactor;
                breakdown.ClassCommission *= reductionFactor;
                breakdown.PersonalCommission *= reductionFactor;
                breakdown.PerformanceBonus *= reductionFactor;
                breakdown.AttendanceBonus *= reductionFactor;
            }

            // 5. Populate metrics
            await PopulateBasicMetricsAsync(breakdown, hlvId, monthStart, monthEnd);

            // 6. Log commission calculation for debugging
            _logger.LogInformation($"Commission calculated for Trainer {hlvId} - Month {thang}: " +
                $"Package: {breakdown.PackageCommission:C}, " +
                $"Class: {breakdown.ClassCommission:C}, " +
                $"Personal: {breakdown.PersonalCommission:C}, " +
                $"Performance: {breakdown.PerformanceBonus:C}, " +
                $"Attendance: {breakdown.AttendanceBonus:C}, " +
                $"Total: {breakdown.TotalCommission:C}");

            return breakdown;
        }

        // 🚀 ENHANCED COMMISSION CALCULATION METHODS WITH CONFIGURABLE RATES

        /// <summary>
        /// 🔧 FIXED: Calculate package commission for trainer
        /// Logic: Commission from package sales where trainer is involved in member's training
        /// Fixed Issue: Removed incorrect requirement for LopHocId in package commission
        /// </summary>
        /// <param name="hlvId">Trainer ID</param>
        /// <param name="monthStart">Start of calculation period</param>
        /// <param name="monthEnd">End of calculation period</param>
        /// <returns>Package commission amount</returns>
        private async Task<decimal> CalculatePackageCommissionAsync(int hlvId, DateTime monthStart, DateTime monthEnd)
        {
            // 🔧 FIXED: Package commission should be based on package sales attributed to trainer
            // Logic: Calculate commission for packages sold where trainer is involved in member's training

            // Method 1: Direct package sales where trainer is assigned as primary trainer
            var directPackageRevenue = await _unitOfWork.Context.DangKys
                .Where(d => d.NgayTao >= monthStart && d.NgayTao <= monthEnd)
                .Where(d => d.GoiTapId.HasValue && d.GoiTap != null)
                .Where(d => d.LoaiDangKy == "PACKAGE") // Only package registrations
                .Where(d => d.ThanhToans.Any(t => t.TrangThai == "SUCCESS"))
                // Check if member has any active class with this trainer in the same period
                .Where(d => _unitOfWork.Context.DangKys
                    .Any(classReg => classReg.NguoiDungId == d.NguoiDungId &&
                                   classReg.LopHocId.HasValue &&
                                   classReg.LopHoc != null &&
                                   classReg.LopHoc.HlvId == hlvId &&
                                   classReg.NgayBatDau <= d.NgayKetThuc &&
                                   classReg.NgayKetThuc >= d.NgayBatDau &&
                                   classReg.TrangThai == "ACTIVE"))
                .SumAsync(d => d.GoiTap!.Gia);

            return directPackageRevenue * _commissionConfig.PackageCommissionRate;
        }

        private async Task<decimal> CalculateClassCommissionAsync(int hlvId, DateTime monthStart, DateTime monthEnd)
        {
            // 🔧 IMPROVED: Class commission for standalone class registrations (not part of package)
            var classRevenue = await _unitOfWork.Context.DangKys
                .Where(d => d.NgayTao >= monthStart && d.NgayTao <= monthEnd)
                .Where(d => d.LopHocId.HasValue && d.LopHoc != null && d.LopHoc.HlvId == hlvId)
                .Where(d => d.GoiTapId == null) // Class-only registrations (no package)
                .Where(d => d.LoaiDangKy == "CLASS") // Only class registrations
                .Where(d => d.ThanhToans.Any(t => t.TrangThai == "SUCCESS"))
                .Where(d => d.TrangThai == "ACTIVE") // Only active registrations
                .SumAsync(d => d.LopHoc!.GiaTuyChinh ?? 0);

            return classRevenue * _commissionConfig.ClassCommissionRate;
        }

        private async Task<decimal> CalculatePersonalTrainingCommissionAsync(int hlvId, DateTime monthStart, DateTime monthEnd)
        {
            // 🔧 IMPROVED: Personal training commission calculation
            // Method 1: From class registrations marked as personal training
            var personalClassRevenue = await _unitOfWork.Context.DangKys
                .Where(d => d.NgayTao >= monthStart && d.NgayTao <= monthEnd)
                .Where(d => d.LopHocId.HasValue && d.LopHoc != null && d.LopHoc.HlvId == hlvId)
                .Where(d => d.LopHoc.SucChua <= 2 ||
                           d.LopHoc.TenLop.ToLower().Contains("personal") ||
                           d.LopHoc.TenLop.ToLower().Contains("pt") ||
                           d.LopHoc.TenLop.ToLower().Contains("riêng"))
                .Where(d => d.ThanhToans.Any(t => t.TrangThai == "SUCCESS"))
                .Where(d => d.TrangThai == "ACTIVE")
                .SumAsync(d => d.LopHoc!.GiaTuyChinh ?? 0);

            // Method 2: From confirmed bookings for personal training sessions
            // Convert DateOnly to DateTime for comparison
            var monthStartDateOnly = DateOnly.FromDateTime(monthStart);
            var monthEndDateOnly = DateOnly.FromDateTime(monthEnd);

            var personalBookingRevenue = await _unitOfWork.Context.Bookings
                .Where(b => b.NgayDat >= monthStartDateOnly && b.NgayDat <= monthEndDateOnly)
                .Where(b => b.LopHoc != null && b.LopHoc.HlvId == hlvId)
                .Where(b => b.LopHoc.SucChua <= 2 ||
                           b.LopHoc.TenLop.ToLower().Contains("personal") ||
                           b.LopHoc.TenLop.ToLower().Contains("pt") ||
                           b.LopHoc.TenLop.ToLower().Contains("riêng"))
                .Where(b => b.TrangThai == "CONFIRMED")
                .SumAsync(b => b.LopHoc!.GiaTuyChinh ?? 0);

            var totalPersonalRevenue = personalClassRevenue + personalBookingRevenue;
            return totalPersonalRevenue * _commissionConfig.PersonalTrainingRate;
        }

        private async Task<decimal> CalculatePerformanceBonusAsync(int hlvId, DateTime monthStart, DateTime monthEnd)
        {
            var studentCount = await _unitOfWork.Context.DangKys
                .Where(d => d.NgayTao >= monthStart && d.NgayTao <= monthEnd)
                .Where(d => d.LopHocId.HasValue && d.LopHoc != null && d.LopHoc.HlvId == hlvId)
                .Where(d => d.ThanhToans.Any(t => t.TrangThai == "SUCCESS"))
                .CountAsync();

            if (studentCount >= _commissionConfig.MinStudentCountForBonus)
            {
                var totalRevenue = await _unitOfWork.Context.DangKys
                    .Where(d => d.NgayTao >= monthStart && d.NgayTao <= monthEnd)
                    .Where(d => d.LopHocId.HasValue && d.LopHoc != null && d.LopHoc.HlvId == hlvId)
                    .Where(d => d.ThanhToans.Any(t => t.TrangThai == "SUCCESS"))
                    .SumAsync(d => (d.GoiTap != null ? d.GoiTap.Gia : 0) + (d.LopHoc!.GiaTuyChinh ?? 0));

                return totalRevenue * _commissionConfig.PerformanceBonusRate;
            }

            return 0;
        }

        /// <summary>
        /// 🔧 ADDED: Helper method to validate commission calculations and prevent negative values
        /// </summary>
        private decimal ValidateCommissionAmount(decimal amount, string commissionType)
        {
            if (amount < 0)
            {
                _logger.LogWarning($"Negative commission detected for {commissionType}: {amount}. Setting to 0.");
                return 0;
            }

            if (amount > _commissionConfig.MaxCommissionPerMonth)
            {
                _logger.LogWarning($"Commission amount {amount} exceeds maximum {_commissionConfig.MaxCommissionPerMonth} for {commissionType}. Capping to maximum.");
                return _commissionConfig.MaxCommissionPerMonth;
            }

            return Math.Round(amount, 2); // Round to 2 decimal places for currency
        }

        private async Task<decimal> CalculateAttendanceBonusAsync(int hlvId, DateTime monthStart, DateTime monthEnd)
        {
            // Calculate attendance rate for trainer's classes using LichLop
            // Convert DateTime to DateOnly for comparison
            var monthStartDateOnly = DateOnly.FromDateTime(monthStart);
            var monthEndDateOnly = DateOnly.FromDateTime(monthEnd);

            var totalSessions = await _unitOfWork.Context.LichLops
                .Where(ll => ll.LopHoc != null && ll.LopHoc.HlvId == hlvId)
                .Where(ll => ll.Ngay >= monthStartDateOnly && ll.Ngay <= monthEndDateOnly)
                .CountAsync();

            if (totalSessions == 0) return 0;

            var attendedSessions = await _unitOfWork.Context.DiemDanhs
                .Where(dd => dd.LichLop != null && dd.LichLop.LopHoc != null && dd.LichLop.LopHoc.HlvId == hlvId)
                .Where(dd => dd.ThoiGian >= monthStart && dd.ThoiGian <= monthEnd)
                .Where(dd => dd.TrangThai == "Present")
                .CountAsync();

            var attendanceRate = (decimal)attendedSessions / totalSessions;

            if (attendanceRate >= _commissionConfig.MinAttendanceRateForBonus)
            {
                var totalRevenue = await _unitOfWork.Context.DangKys
                    .Where(d => d.NgayTao >= monthStart && d.NgayTao <= monthEnd)
                    .Where(d => d.LopHocId.HasValue && d.LopHoc != null && d.LopHoc.HlvId == hlvId)
                    .Where(d => d.ThanhToans.Any(t => t.TrangThai == "SUCCESS"))
                    .SumAsync(d => (d.GoiTap != null ? d.GoiTap.Gia : 0) + (d.LopHoc!.GiaTuyChinh ?? 0));

                return totalRevenue * _commissionConfig.AttendanceBonusRate;
            }

            return 0;
        }

        private decimal CalculateTierBasedCommission(decimal totalRevenue)
        {
            decimal tierBonus = 0;
            decimal processedRevenue = 0;

            foreach (var tier in _commissionConfig.CommissionTiers.OrderBy(t => t.MinRevenue))
            {
                if (totalRevenue <= processedRevenue) break;

                var tierRevenue = Math.Min(totalRevenue - processedRevenue, tier.MaxRevenue - tier.MinRevenue);
                if (tierRevenue > 0)
                {
                    tierBonus += tierRevenue * (tier.Rate - _commissionConfig.PackageCommissionRate); // Additional bonus over base rate
                    processedRevenue += tierRevenue;
                }
            }

            return tierBonus;
        }

        // Simplified performance bonus
        private async Task<decimal> CalculateSimplifiedPerformanceBonusAsync(int hlvId, DateTime monthStart, DateTime monthEnd)
        {
            // Count active students this month
            var studentCount = await _unitOfWork.Context.DangKys
                .Include(d => d.LopHoc)
                .Where(d => d.NgayBatDau >= DateOnly.FromDateTime(monthStart) && 
                           d.NgayBatDau <= DateOnly.FromDateTime(monthEnd))
                .Where(d => d.LopHocId.HasValue && d.LopHoc != null && d.LopHoc.HlvId == hlvId)
                .Where(d => d.TrangThai == "ACTIVE")
                .CountAsync();

            // Simple performance bonus tiers
            return studentCount switch
            {
                >= 50 => 1000000, // 1M for 50+ students
                >= 30 => 500000,  // 500k for 30+ students  
                >= 20 => 200000,  // 200k for 20+ students
                >= 10 => 100000,  // 100k for 10+ students
                _ => 0
            };
        }

        // Basic metrics population
        private async Task PopulateBasicMetricsAsync(CommissionBreakdown breakdown, int hlvId, DateTime monthStart, DateTime monthEnd)
        {
            var monthStartDateOnly = DateOnly.FromDateTime(monthStart);
            var monthEndDateOnly = DateOnly.FromDateTime(monthEnd);

            // Count students
            breakdown.StudentCount = await _unitOfWork.Context.DangKys
                .Include(d => d.LopHoc)
                .Where(d => d.NgayBatDau >= monthStartDateOnly && d.NgayBatDau <= monthEndDateOnly)
                .Where(d => d.LopHocId.HasValue && d.LopHoc != null && d.LopHoc.HlvId == hlvId)
                .Where(d => d.TrangThai == "ACTIVE")
                .CountAsync();

            // Count classes taught
            breakdown.ClassesTaught = await _unitOfWork.Context.LichLops
                .Include(l => l.LopHoc)
                .Where(l => l.Ngay >= monthStartDateOnly && l.Ngay <= monthEndDateOnly)
                .Where(l => l.LopHoc != null && l.LopHoc.HlvId == hlvId)
                .CountAsync();
        }

        // Simplified email notification
        private async Task SendSimplifiedSalaryEmailAsync(NguoiDung trainer, BangLuong bangLuong, CommissionBreakdown breakdown, string thang)
        {
            // Sanitize user input to prevent XSS
            var encodedHo = HtmlEncoder.Default.Encode(trainer.Ho ?? "");
            var encodedTen = HtmlEncoder.Default.Encode(trainer.Ten ?? "");
            var encodedThang = HtmlEncoder.Default.Encode(thang);

            var subject = $"Bảng lương tháng {encodedThang}";
            var body = $@"
                <html>
                <body style='font-family: Arial, sans-serif;'>
                    <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                        <h2 style='color: #2563eb;'>Bảng Lương Tháng {encodedThang}</h2>
                        <p>Xin chào <strong>{encodedHo} {encodedTen}</strong>,</p>
                        
                        <div style='background-color: #f8fafc; padding: 20px; border-radius: 8px; margin: 20px 0;'>
                            <h3 style='margin-top: 0;'>Chi tiết lương:</h3>
                            <table style='width: 100%; border-collapse: collapse;'>
                                <tr style='border-bottom: 1px solid #e5e7eb;'>
                                    <td style='padding: 8px 0;'><strong>Lương cơ bản:</strong></td>
                                    <td style='text-align: right; padding: 8px 0;'>{bangLuong.LuongCoBan:N0} VNĐ</td>
                                </tr>
                                <tr style='border-bottom: 1px solid #e5e7eb;'>
                                    <td style='padding: 8px 0;'><strong>Hoa hồng:</strong></td>
                                    <td style='text-align: right; padding: 8px 0;'>{bangLuong.TienHoaHong:N0} VNĐ</td>
                                </tr>
                                <tr style='border-top: 2px solid #2563eb;'>
                                    <td style='padding: 12px 0;'><strong>Tổng cộng:</strong></td>
                                    <td style='text-align: right; padding: 12px 0; font-size: 1.2em; font-weight: bold; color: #2563eb;'>{bangLuong.TongThanhToan:N0} VNĐ</td>
                                </tr>
                            </table>
                        </div>

                        <div style='background-color: #ecfdf5; padding: 15px; border-radius: 8px; margin: 20px 0;'>
                            <h4 style='margin-top: 0; color: #059669;'>Hiệu suất tháng {encodedThang}:</h4>
                            <ul style='margin: 0; padding-left: 20px;'>
                                <li>Số học viên: {breakdown.StudentCount} người</li>
                                <li>Số buổi dạy: {breakdown.ClassesTaught} buổi</li>
                                <li>Hoa hồng cơ bản: {breakdown.PackageCommission:N0} VNĐ</li>
                                <li>Thưởng hiệu suất: {breakdown.PerformanceBonus:N0} VNĐ</li>
                            </ul>
                        </div>

                        <p style='color: #6b7280; font-size: 0.9em;'>
                            <strong>Lưu ý:</strong> Bảng lương được tính dựa trên số học viên đăng ký và hiệu suất dạy học.<br/>
                            Vui lòng liên hệ bộ phận nhân sự nếu có thắc mắc.
                        </p>

                        <hr style='margin: 30px 0; border: none; border-top: 1px solid #e5e7eb;'/>
                        <p style='color: #6b7280; font-size: 0.9em; text-align: center;'>
                            Trân trọng,<br/>
                            <strong>Hệ thống quản lý phòng Gym</strong>
                        </p>
                    </div>
                </body>
                </html>";

            await _emailService.SendEmailAsync(trainer.Email!, $"{encodedHo} {encodedTen}", subject, body);
        }

        // 🔄 EXISTING METHODS (Keep compatibility)

        public async Task<bool> PaySalaryAsync(int bangLuongId)
        {
            var bangLuong = await _bangLuongRepository.GetByIdAsync(bangLuongId);
            if (bangLuong == null)
            {
                throw new InvalidOperationException("Không tìm thấy bảng lương với ID được cung cấp.");
            }

            if (bangLuong.NgayThanhToan != null)
            {
                throw new InvalidOperationException("Bảng lương này đã được thanh toán trước đó.");
            }

            bangLuong.NgayThanhToan = DateOnly.FromDateTime(DateTime.Today);
            await _unitOfWork.SaveChangesAsync();

            // Invalidate cache for this month
            InvalidateMonthCache(bangLuong.Thang);

            // Send notifications
            var trainer = await _nguoiDungRepository.GetByIdAsync(bangLuong.HlvId ?? 0);
            if (trainer != null)
            {
                await _thongBaoService.CreateNotificationAsync(
                    bangLuong.HlvId ?? 0,
                    "Đã thanh toán lương",
                    $"Lương tháng {bangLuong.Thang} đã được thanh toán: {bangLuong.TongThanhToan:N0} VNĐ",
                    "APP"
                );

                // Send email if available
                if (!string.IsNullOrEmpty(trainer.Email))
                {
                    // Sanitize user input to prevent XSS
                    var encodedHo = HtmlEncoder.Default.Encode(trainer.Ho ?? "");
                    var encodedTen = HtmlEncoder.Default.Encode(trainer.Ten ?? "");
                    var encodedThang = HtmlEncoder.Default.Encode(bangLuong.Thang);

                    var subject = $"Xác nhận thanh toán lương tháng {encodedThang}";
                    var body = $@"
                        <html>
                        <body style='font-family: Arial, sans-serif;'>
                            <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                                <h2 style='color: #10b981;'>Xác nhận thanh toán lương</h2>
                                <p>Xin chào <strong>{encodedHo} {encodedTen}</strong>,</p>
                                <p>Lương tháng <strong>{encodedThang}</strong> của bạn đã được thanh toán:</p>
                                
                                <div style='background-color: #f0f9ff; padding: 20px; border-radius: 8px; margin: 20px 0;'>
                                    <ul style='list-style: none; padding: 0; margin: 0;'>
                                        <li style='padding: 5px 0;'>💰 Lương cơ bản: <strong>{bangLuong.LuongCoBan:N0} VNĐ</strong></li>
                                        <li style='padding: 5px 0;'>🎯 Hoa hồng: <strong>{bangLuong.TienHoaHong:N0} VNĐ</strong></li>
                                        <li style='padding: 10px 0; border-top: 2px solid #10b981; margin-top: 10px;'>
                                            💎 <strong>Tổng cộng: {bangLuong.TongThanhToan:N0} VNĐ</strong>
                                        </li>
                                    </ul>
                                </div>
                                
                                <p style='color: #374151;'>📅 <strong>Ngày thanh toán:</strong> {bangLuong.NgayThanhToan:dd/MM/yyyy}</p>
                                
                                <hr style='margin: 30px 0; border: none; border-top: 1px solid #e5e7eb;'/>
                                <p style='color: #6b7280; text-align: center;'>
                                    Trân trọng,<br/>
                                    <strong>Đội ngũ Gym Management</strong>
                                </p>
                            </div>
                        </body>
                        </html>";

                    await _emailService.SendEmailAsync(trainer.Email, $"{encodedHo} {encodedTen}", subject, body);
                }
            }

            return true;
        }

        public async Task<bool> PayAllSalariesForMonthAsync(string thang)
        {
            var unpaidSalaries = await _bangLuongRepository.GetByMonthAsync(thang);
            var unpaidOnly = unpaidSalaries.Where(s => s.NgayThanhToan == null);

            foreach (var salary in unpaidOnly)
            {
                await PaySalaryAsync(salary.BangLuongId);
            }

            return true;
        }

        public async Task<decimal> CalculateCommissionAsync(int hlvId, string thang)
        {
            var result = await CalculateSimplifiedCommissionAsync(hlvId, thang);
            return result.TotalCommission;
        }

        public async Task<CommissionBreakdown> CalculateDetailedCommissionAsync(int hlvId, string thang)
        {
            // For backward compatibility, use simplified version
            return await CalculateSimplifiedCommissionAsync(hlvId, thang);
        }

        public async Task<decimal> GetTotalSalaryExpenseAsync(string thang)
        {
            // Use cache for expense calculation
            var cacheKey = $"salary_expense_{thang}";

            if (_cache.TryGetValue(cacheKey, out decimal cachedExpense))
            {
                return cachedExpense;
            }

            var salaries = await _bangLuongRepository.GetByMonthAsync(thang);
            var totalExpense = salaries.Sum(s => s.TongThanhToan);

            // Cache for 15 minutes
            _cache.Set(cacheKey, totalExpense, TimeSpan.FromMinutes(15));

            return totalExpense;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            try
            {
                var bangLuong = await _bangLuongRepository.GetByIdAsync(id);
                if (bangLuong == null) return false;

                // Only allow deletion if salary has not been paid yet
                if (bangLuong.NgayThanhToan.HasValue)
                {
                    throw new InvalidOperationException("Không thể xóa bảng lương đã được thanh toán.");
                }

                await _bangLuongRepository.DeleteAsync(bangLuong);
                await _unitOfWork.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi xóa bảng lương: {ex.Message}", ex);
            }
        }

        // Helper method to invalidate cache for a specific month
        private void InvalidateMonthCache(string thang)
        {
            _cache.Remove($"salary_month_{thang}");
            _cache.Remove($"salary_expense_{thang}");
        }

        // Helper class for commission breakdown
        public class CommissionBreakdown
        {
            public decimal PackageCommission { get; set; }
            public decimal ClassCommission { get; set; }
            public decimal PersonalCommission { get; set; }
            public decimal PerformanceBonus { get; set; }
            public decimal AttendanceBonus { get; set; }
            public decimal TotalCommission => PackageCommission + ClassCommission + PersonalCommission + PerformanceBonus + AttendanceBonus;
            
            // Performance metrics
            public int StudentCount { get; set; }
            public int ClassesTaught { get; set; }
            public int PersonalSessions { get; set; }
            public decimal AttendanceRate { get; set; }
        }
    }
}
