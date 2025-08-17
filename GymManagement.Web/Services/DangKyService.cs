using GymManagement.Web.Data;
using GymManagement.Web.Data.Models;
using GymManagement.Web.Data.Repositories;
using Microsoft.EntityFrameworkCore;

namespace GymManagement.Web.Services
{
    public class DangKyService : IDangKyService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IDangKyRepository _dangKyRepository;
        private readonly IGoiTapRepository _goiTapRepository;
        private readonly ILopHocRepository _lopHocRepository;
        private readonly IThongBaoService _thongBaoService;
        private readonly ILogger<DangKyService> _logger;

        public DangKyService(
            IUnitOfWork unitOfWork,
            IDangKyRepository dangKyRepository,
            IGoiTapRepository goiTapRepository,
            ILopHocRepository lopHocRepository,
            IThongBaoService thongBaoService,
            ILogger<DangKyService> logger)
        {
            _unitOfWork = unitOfWork;
            _dangKyRepository = dangKyRepository;
            _goiTapRepository = goiTapRepository;
            _lopHocRepository = lopHocRepository;
            _thongBaoService = thongBaoService;
            _logger = logger;
        }

        public async Task<IEnumerable<DangKy>> GetAllAsync()
        {
            return await _dangKyRepository.GetAllAsync();
        }

        public async Task<(IEnumerable<DangKy> registrations, int totalCount)> GetPagedAsync(int page, int pageSize, string searchTerm = "", string status = "", string type = "")
        {
            return await _dangKyRepository.GetPagedAsync(page, pageSize, searchTerm, status, type);
        }

        public async Task<DangKy?> GetByIdAsync(int id)
        {
            return await _dangKyRepository.GetByIdAsync(id);
        }

        public async Task<DangKy> CreateAsync(DangKy dangKy)
        {
            // ⚠️ BUSINESS RULE VALIDATION: Prevent duplicate active package registrations
            if (dangKy.GoiTapId.HasValue && dangKy.GoiTapId > 0)
            {
                var hasActivePackage = await HasActivePackageAsync(dangKy.NguoiDungId);
                if (hasActivePackage)
                {
                    throw new InvalidOperationException($"Thành viên ID {dangKy.NguoiDungId} đã có gói tập đang hoạt động. Không thể tạo thêm gói tập mới.");
                }
            }

            // ⚠️ BUSINESS RULE VALIDATION: Prevent overlapping class registrations
            if (dangKy.LopHocId.HasValue && dangKy.LopHocId > 0)
            {
                var (canRegister, errorMessage) = await ValidateClassRegistrationAsync(
                    dangKy.NguoiDungId,
                    dangKy.LopHocId.Value,
                    dangKy.NgayBatDau,
                    dangKy.NgayKetThuc);

                if (!canRegister)
                {
                    throw new InvalidOperationException(errorMessage);
                }
            }

            var created = await _dangKyRepository.AddAsync(dangKy);
            await _unitOfWork.SaveChangesAsync();
            return created;
        }

        public async Task<DangKy> UpdateAsync(DangKy dangKy)
        {
            await _dangKyRepository.UpdateAsync(dangKy);
            await _unitOfWork.SaveChangesAsync();
            return dangKy;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var dangKy = await _dangKyRepository.GetByIdAsync(id);
            if (dangKy == null) return false;

            await _dangKyRepository.DeleteAsync(dangKy);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<DangKy>> GetByMemberIdAsync(int nguoiDungId)
        {
            return await _dangKyRepository.GetByNguoiDungIdAsync(nguoiDungId);
        }

        public async Task<int> GetRegistrationCountByUserIdAsync(int nguoiDungId)
        {
            var registrations = await _dangKyRepository.GetByNguoiDungIdAsync(nguoiDungId);
            return registrations.Count();
        }

        public async Task<IEnumerable<DangKy>> GetActiveRegistrationsAsync()
        {
            return await _dangKyRepository.GetActiveRegistrationsAsync();
        }

        public async Task<IEnumerable<DangKy>> GetExpiredRegistrationsAsync()
        {
            return await _dangKyRepository.GetExpiredRegistrationsAsync();
        }

        public async Task<bool> RegisterPackageAsync(int nguoiDungId, int goiTapId, int thoiHanThang)
        {
            return await RegisterPackageAsync(nguoiDungId, goiTapId, thoiHanThang, null);
        }

        public async Task<bool> RegisterPackageAsync(int nguoiDungId, int goiTapId, int thoiHanThang, int? khuyenMaiId = null)
        {
            // Check if package exists
            var goiTap = await _goiTapRepository.GetByIdAsync(goiTapId);
            if (goiTap == null) return false;

            // 🔒 CHÍNH SÁCH: Mỗi thành viên chỉ có thể sở hữu một gói tập tại một thời điểm
            // Check if user already has ANY active package registration
            var existingActivePackages = await _dangKyRepository.GetByMemberIdAsync(nguoiDungId);
            var hasActivePackage = existingActivePackages.Any(d =>
                d.GoiTapId != null &&
                d.TrangThai == "ACTIVE" &&
                d.NgayKetThuc >= DateOnly.FromDateTime(DateTime.Today));

            if (hasActivePackage)
            {
                // User already has an active package, cannot register another one
                return false;
            }

            // Calculate dates
            var ngayBatDau = DateOnly.FromDateTime(DateTime.Today);
            var ngayKetThuc = DateOnly.FromDateTime(DateTime.Today.AddMonths(thoiHanThang));

            // Create registration
            var dangKy = new DangKy
            {
                NguoiDungId = nguoiDungId,
                GoiTapId = goiTapId,
                NgayBatDau = ngayBatDau,
                NgayKetThuc = ngayKetThuc,
                TrangThai = "ACTIVE",
                NgayTao = DateTime.Now
            };

            await _dangKyRepository.AddAsync(dangKy);
            await _unitOfWork.SaveChangesAsync();

            // Send notification
            await _thongBaoService.CreateNotificationAsync(
                nguoiDungId,
                "Đăng ký gói tập thành công",
                $"Bạn đã đăng ký thành công gói {goiTap.TenGoi}. Thời hạn: {ngayBatDau:dd/MM/yyyy} - {ngayKetThuc:dd/MM/yyyy}",
                "APP"
            );

            return true;
        }

        public async Task<bool> RegisterClassAsync(int nguoiDungId, int lopHocId, DateTime ngayBatDau, DateTime ngayKetThuc)
        {
            // Check if class exists
            var lopHoc = await _lopHocRepository.GetByIdAsync(lopHocId);
            if (lopHoc == null || lopHoc.TrangThai != "OPEN") return false;

            // Check if user already has active registration for this specific class
            if (await _dangKyRepository.HasActiveRegistrationAsync(nguoiDungId, null, lopHocId))
                return false;

            // 🧘‍♂️ CHÍNH SÁCH: Thành viên có thể đăng ký nhiều lớp học cùng lúc
            // Kiểm tra trùng lịch thời gian với các lớp đã đăng ký
            var existingActiveClasses = await _dangKyRepository.GetByMemberIdAsync(nguoiDungId);
            var activeClassRegistrations = existingActiveClasses.Where(d =>
                d.LopHocId != null &&
                d.TrangThai == "ACTIVE" &&
                d.NgayKetThuc >= DateOnly.FromDateTime(DateTime.Today)).ToList();

            // Check for time conflicts
            foreach (var existingReg in activeClassRegistrations)
            {
                if (existingReg.LopHoc != null && HasTimeConflict(lopHoc, existingReg.LopHoc))
                {
                    // Time conflict detected
                    return false;
                }
            }

            // Check class capacity with real-time validation
            var classRegistrationCount = await GetActiveRegistrationCountAsync(lopHocId);
            if (classRegistrationCount >= lopHoc.SucChua)
            {
                // Class is full
                return false;
            }

            // Double-check capacity in a transaction to prevent race conditions
            using var transaction = await _unitOfWork.Context.Database.BeginTransactionAsync();
            try
            {
                // Re-check capacity within transaction
                var currentCount = await GetActiveRegistrationCountAsync(lopHocId);
                if (currentCount >= lopHoc.SucChua)
                {
                    await transaction.RollbackAsync();
                    return false;
                }

                // Create registration within transaction
                var dangKy = new DangKy
                {
                    NguoiDungId = nguoiDungId,
                    LopHocId = lopHocId,
                    NgayBatDau = DateOnly.FromDateTime(ngayBatDau),
                    NgayKetThuc = DateOnly.FromDateTime(ngayKetThuc),
                    TrangThai = "ACTIVE",
                    NgayTao = DateTime.Now
                };

                await _dangKyRepository.AddAsync(dangKy);
                await _unitOfWork.SaveChangesAsync();
                await transaction.CommitAsync();

                // Send notification after successful registration
                await _thongBaoService.CreateNotificationAsync(
                    nguoiDungId,
                    "Đăng ký lớp học thành công",
                    $"Bạn đã đăng ký thành công lớp {lopHoc.TenLop}. Thời gian: {lopHoc.GioBatDau:HH:mm}-{lopHoc.GioKetThuc:HH:mm}",
                    "APP"
                );

                return true;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw; // Re-throw to be handled by calling method
            }
        }

        /// <summary>
        /// Get active registration count for a specific class (real-time)
        /// </summary>
        private async Task<int> GetActiveRegistrationCountAsync(int lopHocId)
        {
            return await _unitOfWork.Context.DangKys
                .Where(d => d.LopHocId == lopHocId &&
                           d.TrangThai == "ACTIVE" &&
                           d.NgayKetThuc >= DateOnly.FromDateTime(DateTime.Today))
                .CountAsync();
        }

        public async Task<bool> ExtendRegistrationAsync(int dangKyId, int additionalMonths)
        {
            var dangKy = await _dangKyRepository.GetByIdAsync(dangKyId);
            if (dangKy == null || dangKy.TrangThai != "ACTIVE") return false;

            dangKy.NgayKetThuc = dangKy.NgayKetThuc.AddMonths(additionalMonths);
            await _unitOfWork.SaveChangesAsync();

            // Send notification
            await _thongBaoService.CreateNotificationAsync(
                dangKy.NguoiDungId,
                "Gia hạn đăng ký thành công",
                $"Đăng ký của bạn đã được gia hạn thêm {additionalMonths} tháng. Ngày hết hạn mới: {dangKy.NgayKetThuc:dd/MM/yyyy}",
                "APP"
            );

            return true;
        }

        // New methods for renewal functionality
        public async Task<decimal> CalculateRenewalFeeAsync(int dangKyId, int renewalMonths)
        {
            var dangKy = await _dangKyRepository.GetByIdAsync(dangKyId);
            if (dangKy?.GoiTap == null) return 0;

            // Use current package price (not the original price when registered)
            var currentPackage = await _goiTapRepository.GetByIdAsync(dangKy.GoiTapId.Value);
            if (currentPackage == null) return 0;

            // Calculate based on monthly rate
            var monthlyRate = currentPackage.Gia / currentPackage.ThoiHanThang;
            return monthlyRate * renewalMonths;
        }

        public async Task<bool> CanRenewRegistrationAsync(int dangKyId)
        {
            var dangKy = await _dangKyRepository.GetByIdAsync(dangKyId);
            if (dangKy == null) return false;

            // Can renew if:
            // 1. It's a package registration (not class)
            // 2. Status is ACTIVE or EXPIRED within 30 days
            return dangKy.IsPackageRegistration &&
                   (dangKy.TrangThai == "ACTIVE" ||
                    (dangKy.TrangThai == "EXPIRED" && dangKy.NgayKetThuc >= DateOnly.FromDateTime(DateTime.Today.AddDays(-30))));
        }

        public async Task<bool> ProcessRenewalPaymentAsync(int dangKyId, int thanhToanId, int renewalMonths)
        {
            // Verify payment exists and is successful
            var thanhToan = await _unitOfWork.Context.ThanhToans.FindAsync(thanhToanId);
            if (thanhToan == null || thanhToan.TrangThai != "SUCCESS") return false;

            // Get registration
            var dangKy = await _dangKyRepository.GetByIdAsync(dangKyId);
            if (dangKy == null || !await CanRenewRegistrationAsync(dangKyId)) return false;

            // If expired, reactivate and extend from today
            if (dangKy.TrangThai == "EXPIRED")
            {
                dangKy.TrangThai = "ACTIVE";
                dangKy.NgayKetThuc = DateOnly.FromDateTime(DateTime.Today.AddMonths(renewalMonths));
            }
            else
            {
                // If still active, extend from current expiry date
                dangKy.NgayKetThuc = dangKy.NgayKetThuc.AddMonths(renewalMonths);
            }

            await _unitOfWork.SaveChangesAsync();

            // Send notification
            await _thongBaoService.CreateNotificationAsync(
                dangKy.NguoiDungId,
                "Gia hạn gói tập thành công",
                $"Gói tập {dangKy.GoiTap?.TenGoi} đã được gia hạn thêm {renewalMonths} tháng. Ngày hết hạn mới: {dangKy.NgayKetThuc:dd/MM/yyyy}",
                "APP"
            );

            return true;
        }

        public async Task<bool> CancelRegistrationAsync(int dangKyId, string reason)
        {
            var dangKy = await _dangKyRepository.GetByIdAsync(dangKyId);
            if (dangKy == null) return false;

            dangKy.TrangThai = "CANCELED";
            await _unitOfWork.SaveChangesAsync();

            // Send notification
            await _thongBaoService.CreateNotificationAsync(
                dangKy.NguoiDungId,
                "Huỷ đăng ký",
                $"Đăng ký của bạn đã bị huỷ. Lý do: {reason}",
                "APP"
            );

            return true;
        }

        public async Task<decimal> CalculateRegistrationFeeAsync(int goiTapId, int thoiHanThang, int? khuyenMaiId = null)
        {
            var goiTap = await _goiTapRepository.GetByIdAsync(goiTapId);
            if (goiTap == null) return 0;

            // ✅ FIXED: goiTap.Gia is already the total price for the entire package duration
            // For custom duration, calculate based on monthly rate
            decimal totalFee;
            if (thoiHanThang == goiTap.ThoiHanThang)
            {
                // Standard package duration - use package price directly
                totalFee = goiTap.Gia;
            }
            else
            {
                // Custom duration - calculate based on monthly rate
                var monthlyRate = goiTap.Gia / goiTap.ThoiHanThang;
                totalFee = monthlyRate * thoiHanThang;
            }

            // Apply discount if available
            if (khuyenMaiId.HasValue)
            {
                var khuyenMai = await _unitOfWork.Context.KhuyenMais.FindAsync(khuyenMaiId.Value);
                if (khuyenMai != null && khuyenMai.KichHoat &&
                    DateOnly.FromDateTime(DateTime.Today) >= khuyenMai.NgayBatDau && DateOnly.FromDateTime(DateTime.Today) <= khuyenMai.NgayKetThuc)
                {
                    decimal discount = totalFee * (khuyenMai.PhanTramGiam ?? 0) / 100;
                    totalFee -= discount;
                }
            }

            return totalFee;
        }

        public async Task<bool> HasActivePackageRegistrationAsync(int nguoiDungId)
        {
            return await _unitOfWork.Context.DangKys
                .AnyAsync(d => d.NguoiDungId == nguoiDungId &&
                              d.GoiTapId != null &&
                              d.TrangThai == "ACTIVE" &&
                              d.NgayKetThuc >= DateOnly.FromDateTime(DateTime.Today));
        }

        public async Task<bool> HasActiveClassRegistrationAsync(int nguoiDungId, int lopHocId)
        {
            return await _unitOfWork.Context.DangKys
                .AnyAsync(d => d.NguoiDungId == nguoiDungId &&
                              d.LopHocId == lopHocId &&
                              d.TrangThai == "ACTIVE" &&
                              d.NgayKetThuc >= DateOnly.FromDateTime(DateTime.Today));
        }

        /// <summary>
        /// Kiểm tra xem thành viên có thể đăng ký lớp học với thời gian mới không
        /// Business Rule: Cho phép đăng ký cùng lớp nhưng PHẢI CHỜ KHÓA CŨ KẾT THÚC
        /// </summary>
        public async Task<bool> CanRegisterClassWithTimeAsync(int nguoiDungId, int lopHocId, DateOnly newStartDate, DateOnly newEndDate)
        {
            // Lấy tất cả đăng ký active của thành viên cho lớp học này
            var existingRegistrations = await _unitOfWork.Context.DangKys
                .Where(d => d.NguoiDungId == nguoiDungId &&
                           d.LopHocId == lopHocId &&
                           d.TrangThai == "ACTIVE")
                .ToListAsync();

            if (!existingRegistrations.Any())
            {
                return true; // Không có đăng ký nào → OK
            }

            // Kiểm tra overlap thời gian với các đăng ký hiện tại
            foreach (var existing in existingRegistrations)
            {
                // Kiểm tra overlap: newStart < existingEnd && newEnd > existingStart
                bool hasOverlap = newStartDate < existing.NgayKetThuc && newEndDate > existing.NgayBatDau;

                if (hasOverlap)
                {
                    return false; // Có overlap → KHÔNG được đăng ký
                }
            }

            return true; // Không có overlap → OK
        }

        /// <summary>
        /// Lấy thông tin chi tiết về đăng ký lớp học đang conflict
        /// </summary>
        public async Task<(bool CanRegister, string ErrorMessage)> ValidateClassRegistrationAsync(int nguoiDungId, int lopHocId, DateOnly newStartDate, DateOnly newEndDate)
        {
            // Lấy thông tin lớp học
            var lopHoc = await _lopHocRepository.GetByIdAsync(lopHocId);
            if (lopHoc == null)
            {
                return (false, "Lớp học không tồn tại.");
            }

            // Lấy đăng ký hiện tại của thành viên cho lớp này
            var existingRegistrations = await _unitOfWork.Context.DangKys
                .Include(d => d.LopHoc)
                .Where(d => d.NguoiDungId == nguoiDungId &&
                           d.LopHocId == lopHocId &&
                           d.TrangThai == "ACTIVE")
                .OrderByDescending(d => d.NgayKetThuc)
                .ToListAsync();

            if (!existingRegistrations.Any())
            {
                return (true, ""); // Không có đăng ký nào → OK
            }

            // Kiểm tra overlap với từng đăng ký hiện tại
            foreach (var existing in existingRegistrations)
            {
                bool hasOverlap = newStartDate < existing.NgayKetThuc && newEndDate > existing.NgayBatDau;

                if (hasOverlap)
                {
                    var errorMessage = $"Thành viên đã đăng ký lớp học \"{lopHoc.TenLop}\" " +
                                     $"từ {existing.NgayBatDau:dd/MM/yyyy} đến {existing.NgayKetThuc:dd/MM/yyyy}. " +
                                     $"Vui lòng chờ khóa học hiện tại kết thúc trước khi đăng ký khóa mới.";

                    return (false, errorMessage);
                }
            }

            return (true, ""); // Không có overlap → OK
        }

        public async Task ProcessExpiredRegistrationsAsync()
        {
            var expiredRegistrations = await _dangKyRepository.GetExpiredRegistrationsAsync();
            
            foreach (var dangKy in expiredRegistrations)
            {
                dangKy.TrangThai = "EXPIRED";
                
                // Send notification
                await _thongBaoService.CreateNotificationAsync(
                    dangKy.NguoiDungId,
                    "Đăng ký đã hết hạn",
                    "Đăng ký của bạn đã hết hạn. Vui lòng gia hạn để tiếp tục sử dụng dịch vụ.",
                    "APP"
                );
            }

            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<IEnumerable<DangKy>> GetActiveMemberRegistrationsAsync(int nguoiDungId)
        {
            var allRegistrations = await _dangKyRepository.GetByMemberIdAsync(nguoiDungId);
            return allRegistrations.Where(d => d.TrangThai == "ACTIVE" && d.NgayKetThuc >= DateOnly.FromDateTime(DateTime.Today));
        }

        public async Task<IEnumerable<DangKy>> GetActiveRegistrationsByMemberIdAsync(int nguoiDungId)
        {
            return await _unitOfWork.Context.DangKys
                .Include(d => d.GoiTap)
                .Include(d => d.LopHoc)
                .Include(d => d.NguoiDung)
                .Where(d => d.NguoiDungId == nguoiDungId &&
                           d.TrangThai == "ACTIVE" &&
                           d.NgayKetThuc >= DateOnly.FromDateTime(DateTime.Today))
                .ToListAsync();
        }

        public async Task<IEnumerable<DangKy>> GetRegistrationsByMemberIdAsync(int memberId)
        {
            return await _unitOfWork.Context.DangKys
                .Include(d => d.GoiTap)
                .Include(d => d.LopHoc)
                .Include(d => d.NguoiDung)
                .Where(d => d.NguoiDungId == memberId)
                .ToListAsync();
        }

        /// <summary>
        /// Kiểm tra xung đột thời gian giữa hai lớp học
        /// </summary>
        private bool HasTimeConflict(LopHoc newClass, LopHoc existingClass)
        {
            // Parse days of week for both classes
            var newClassDays = ParseDaysOfWeek(newClass.ThuTrongTuan);
            var existingClassDays = ParseDaysOfWeek(existingClass.ThuTrongTuan);

            // Check if there's any common day
            var commonDays = newClassDays.Intersect(existingClassDays);
            if (!commonDays.Any())
            {
                // No common days, no conflict
                return false;
            }

            // Check time overlap on common days
            var newStart = newClass.GioBatDau;
            var newEnd = newClass.GioKetThuc;
            var existingStart = existingClass.GioBatDau;
            var existingEnd = existingClass.GioKetThuc;

            // Time conflict if:
            // New class starts before existing ends AND new class ends after existing starts
            return newStart < existingEnd && newEnd > existingStart;
        }

        /// <summary>
        /// Parse days of week string to list of integers (1=Monday, 7=Sunday)
        /// </summary>
        private List<int> ParseDaysOfWeek(string thuTrongTuan)
        {
            var days = new List<int>();
            if (string.IsNullOrEmpty(thuTrongTuan)) return days;

            // Handle different formats: "2,4,6" or "Thứ 2, Thứ 4, Thứ 6" or "Monday,Tuesday,Wednesday"
            var dayStrings = thuTrongTuan.Split(',', StringSplitOptions.RemoveEmptyEntries);

            foreach (var dayStr in dayStrings)
            {
                var cleanDay = dayStr.Trim().ToLower();

                // Try to parse as number first
                if (int.TryParse(cleanDay, out int dayNum) && dayNum >= 1 && dayNum <= 7)
                {
                    days.Add(dayNum);
                }
                // Parse English day names
                else if (cleanDay.Contains("monday"))
                    days.Add(2);
                else if (cleanDay.Contains("tuesday"))
                    days.Add(3);
                else if (cleanDay.Contains("wednesday"))
                    days.Add(4);
                else if (cleanDay.Contains("thursday"))
                    days.Add(5);
                else if (cleanDay.Contains("friday"))
                    days.Add(6);
                else if (cleanDay.Contains("saturday"))
                    days.Add(7);
                else if (cleanDay.Contains("sunday"))
                    days.Add(1);
                // Parse Vietnamese day names
                else if (cleanDay.Contains("2") || cleanDay.Contains("hai"))
                    days.Add(2);
                else if (cleanDay.Contains("3") || cleanDay.Contains("ba"))
                    days.Add(3);
                else if (cleanDay.Contains("4") || cleanDay.Contains("tư"))
                    days.Add(4);
                else if (cleanDay.Contains("5") || cleanDay.Contains("năm"))
                    days.Add(5);
                else if (cleanDay.Contains("6") || cleanDay.Contains("sáu"))
                    days.Add(6);
                else if (cleanDay.Contains("7") || cleanDay.Contains("bảy"))
                    days.Add(7);
                else if (cleanDay.Contains("chủ nhật") || cleanDay.Contains("cn"))
                    days.Add(1);
            }

            return days.Distinct().ToList();
        }

        /// <summary>
        /// Kiểm tra xem member có gói tập đang hoạt động không
        /// </summary>
        public async Task<bool> HasActivePackageAsync(int nguoiDungId)
        {
            var registrations = await _dangKyRepository.GetByMemberIdAsync(nguoiDungId);
            return registrations.Any(d =>
                d.GoiTapId != null &&
                d.TrangThai == "ACTIVE" &&
                d.NgayKetThuc >= DateOnly.FromDateTime(DateTime.Today));
        }

        /// <summary>
        /// Kiểm tra xem member có thể đăng ký lớp học không (không trùng lịch và lớp chưa đầy)
        /// </summary>
        public async Task<bool> CanRegisterClassAsync(int nguoiDungId, int lopHocId)
        {
            // Check if class exists and is open
            var lopHoc = await _lopHocRepository.GetByIdAsync(lopHocId);
            if (lopHoc == null || lopHoc.TrangThai != "OPEN") return false;

            // Check if user already has active registration for this specific class
            if (await HasActiveClassRegistrationAsync(nguoiDungId, lopHocId)) return false;

            // Check capacity
            var currentCount = await GetActiveClassRegistrationCountAsync(lopHocId);
            if (currentCount >= lopHoc.SucChua) return false;

            return true;
        }

        /// <summary>
        /// Đếm số lượng đăng ký đang hoạt động cho một lớp học
        /// </summary>
        public async Task<int> GetActiveClassRegistrationCountAsync(int lopHocId)
        {
            return await _unitOfWork.Context.DangKys
                .Where(d => d.LopHocId == lopHocId &&
                           d.TrangThai == "ACTIVE" &&
                           d.NgayKetThuc >= DateOnly.FromDateTime(DateTime.Today))
                .CountAsync();
        }

        /// <summary>
        /// Đăng ký lớp học theo mô hình cố định (member tham gia từ đầu khóa)
        /// </summary>
        public async Task<bool> RegisterFixedClassAsync(int nguoiDungId, int lopHocId)
        {
            // Check if class exists and has fixed schedule
            var lopHoc = await _lopHocRepository.GetByIdAsync(lopHocId);
            if (lopHoc == null || lopHoc.TrangThai != "OPEN") return false;

            // Kiểm tra lớp học có lịch trình cố định không
            if (!lopHoc.NgayBatDauKhoa.HasValue || !lopHoc.NgayKetThucKhoa.HasValue)
                return false;

            // Kiểm tra còn thời gian đăng ký không (phải đăng ký trước ngày bắt đầu)
            if (lopHoc.NgayBatDauKhoa.Value <= DateOnly.FromDateTime(DateTime.Today))
                return false;

            // Check if user already registered for this class
            if (await _dangKyRepository.HasActiveRegistrationAsync(nguoiDungId, null, lopHocId))
                return false;

            // Check class capacity
            var currentCount = await GetActiveRegistrationCountAsync(lopHocId);
            if (currentCount >= lopHoc.SucChua) return false;

            // Check time conflicts with other active class registrations
            var existingActiveClasses = await _dangKyRepository.GetByMemberIdAsync(nguoiDungId);
            var activeClassRegistrations = existingActiveClasses.Where(d =>
                d.LopHocId != null &&
                d.TrangThai == "ACTIVE" &&
                d.NgayKetThuc >= DateOnly.FromDateTime(DateTime.Today)).ToList();

            foreach (var existingReg in activeClassRegistrations)
            {
                if (existingReg.LopHoc != null && HasTimeConflict(lopHoc, existingReg.LopHoc))
                {
                    return false;
                }
            }

            // Create registration with fixed schedule dates
            using var transaction = await _unitOfWork.Context.Database.BeginTransactionAsync();
            try
            {
                // Re-check capacity within transaction
                var currentCountInTransaction = await GetActiveRegistrationCountAsync(lopHocId);
                if (currentCountInTransaction >= lopHoc.SucChua)
                {
                    await transaction.RollbackAsync();
                    return false;
                }

                var dangKy = new DangKy
                {
                    NguoiDungId = nguoiDungId,
                    LopHocId = lopHocId,
                    NgayBatDau = lopHoc.NgayBatDauKhoa.Value,
                    NgayKetThuc = lopHoc.NgayKetThucKhoa.Value,
                    TrangThai = "ACTIVE",
                    NgayTao = DateTime.Now,
                    LoaiDangKy = "CLASS",
                    TrangThaiChiTiet = "ENROLLED"
                };

                await _dangKyRepository.AddAsync(dangKy);
                await _unitOfWork.SaveChangesAsync();
                await transaction.CommitAsync();

                // Send notification
                await _thongBaoService.CreateNotificationAsync(
                    nguoiDungId,
                    "Đăng ký lớp học thành công",
                    $"Bạn đã đăng ký thành công lớp {lopHoc.TenLop}. Khóa học: {lopHoc.NgayBatDauKhoa:dd/MM/yyyy} - {lopHoc.NgayKetThucKhoa:dd/MM/yyyy}",
                    "APP"
                );

                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                return false;
            }
        }

        /// <summary>
        /// Hủy đăng ký cho member
        /// </summary>
        public async Task<bool> CancelRegistrationAsync(int dangKyId, int nguoiDungId, string? lyDoHuy = null)
        {
            var dangKy = await _dangKyRepository.GetByIdAsync(dangKyId);
            if (dangKy == null || dangKy.NguoiDungId != nguoiDungId || dangKy.TrangThai != "ACTIVE")
                return false;

            // Kiểm tra có thể hủy không
            if (!dangKy.CanCancel) return false;

            dangKy.TrangThai = "CANCELLED";
            dangKy.TrangThaiChiTiet = "CANCELLED";
            dangKy.LyDoHuy = lyDoHuy ?? "Hủy bởi thành viên";

            await _dangKyRepository.UpdateAsync(dangKy);
            await _unitOfWork.SaveChangesAsync();

            // Send notification
            var message = dangKy.IsClassRegistration ?
                $"Bạn đã hủy đăng ký lớp {dangKy.LopHoc?.TenLop}" :
                $"Bạn đã hủy đăng ký gói {dangKy.GoiTap?.TenGoi}";

            await _thongBaoService.CreateNotificationAsync(
                nguoiDungId,
                "Hủy đăng ký thành công",
                message,
                "APP"
            );

            return true;
        }

        // New methods for payment-first registration flow

        public async Task<decimal> CalculatePackageFeeAsync(int goiTapId, int thoiHanThang, int? khuyenMaiId = null)
        {
            return await CalculateRegistrationFeeAsync(goiTapId, thoiHanThang, khuyenMaiId);
        }

        public async Task<decimal> CalculateClassFeeAsync(int lopHocId)
        {
            var lopHoc = await _lopHocRepository.GetByIdAsync(lopHocId);
            if (lopHoc == null) return 0;

            // Use custom price if set, otherwise use default class fee
            return lopHoc.GiaTuyChinh ?? 200000m; // Default class fee 200k VND
        }

        public async Task<bool> CreatePackageRegistrationAfterPaymentAsync(int nguoiDungId, int goiTapId, int thoiHanThang, int thanhToanId)
        {
            // Verify payment exists and is successful
            var thanhToan = await _unitOfWork.Context.ThanhToans.FindAsync(thanhToanId);
            if (thanhToan == null || thanhToan.TrangThai != "SUCCESS") return false;

            // Check if registration already exists for this payment
            var existingRegistration = await _unitOfWork.Context.DangKys
                .FirstOrDefaultAsync(d => d.ThanhToans.Any(t => t.ThanhToanId == thanhToanId));
            if (existingRegistration != null) return true; // Already created

            // ⚠️ BUSINESS RULE VALIDATION: Prevent duplicate active package registrations
            var hasActivePackage = await HasActivePackageAsync(nguoiDungId);
            if (hasActivePackage)
            {
                // Log the attempt but don't fail - payment was successful
                Console.WriteLine($"⚠️ User {nguoiDungId} already has active package but payment {thanhToanId} was successful. Skipping registration creation.");
                return false; // Don't create duplicate registration
            }

            // Check if user already has active package
            if (await HasActivePackageRegistrationAsync(nguoiDungId)) return false;

            var goiTap = await _goiTapRepository.GetByIdAsync(goiTapId);
            if (goiTap == null) return false;

            // Calculate dates
            var ngayBatDau = DateOnly.FromDateTime(DateTime.Today);
            var ngayKetThuc = DateOnly.FromDateTime(DateTime.Today.AddMonths(thoiHanThang));

            // Create registration
            var dangKy = new DangKy
            {
                NguoiDungId = nguoiDungId,
                GoiTapId = goiTapId,
                NgayBatDau = ngayBatDau,
                NgayKetThuc = ngayKetThuc,
                TrangThai = "ACTIVE",
                NgayTao = DateTime.Now
            };

            await _dangKyRepository.AddAsync(dangKy);
            await _unitOfWork.SaveChangesAsync();

            // Update payment to link with registration
            thanhToan.DangKyId = dangKy.DangKyId;
            await _unitOfWork.SaveChangesAsync();

            // Send notification
            await _thongBaoService.CreateNotificationAsync(
                nguoiDungId,
                "Đăng ký gói tập thành công",
                $"Bạn đã đăng ký thành công gói {goiTap.TenGoi}. Thời hạn: {ngayBatDau:dd/MM/yyyy} - {ngayKetThuc:dd/MM/yyyy}",
                "APP"
            );

            return true;
        }

        public async Task<bool> CreateClassRegistrationAfterPaymentAsync(int nguoiDungId, int lopHocId, DateTime ngayBatDau, DateTime ngayKetThuc, int thanhToanId)
        {
            // Verify payment exists and is successful
            var thanhToan = await _unitOfWork.Context.ThanhToans.FindAsync(thanhToanId);
            if (thanhToan == null || thanhToan.TrangThai != "SUCCESS") return false;

            // Check if registration already exists for this payment
            var existingRegistration = await _unitOfWork.Context.DangKys
                .FirstOrDefaultAsync(d => d.ThanhToans.Any(t => t.ThanhToanId == thanhToanId));
            if (existingRegistration != null) return true; // Already created

            // Check if class exists and is open
            var lopHoc = await _lopHocRepository.GetByIdAsync(lopHocId);
            if (lopHoc == null || lopHoc.TrangThai != "OPEN") return false;

            // ⚠️ BUSINESS RULE VALIDATION: Check for overlapping class registrations
            var (canRegister, errorMessage) = await ValidateClassRegistrationAsync(
                nguoiDungId,
                lopHocId,
                DateOnly.FromDateTime(ngayBatDau),
                DateOnly.FromDateTime(ngayKetThuc));

            if (!canRegister)
            {
                Console.WriteLine($"⚠️ Class registration validation failed for User {nguoiDungId}, Class {lopHocId}: {errorMessage}. Payment {thanhToanId} was successful but registration cannot be created.");
                return false; // Don't create overlapping registration
            }

            using var transaction = await _unitOfWork.Context.Database.BeginTransactionAsync();
            try
            {
                // Re-check capacity within transaction
                var currentCountInTransaction = await GetActiveRegistrationCountAsync(lopHocId);
                if (currentCountInTransaction >= lopHoc.SucChua)
                {
                    await transaction.RollbackAsync();
                    return false;
                }

                // Create registration within transaction
                var dangKy = new DangKy
                {
                    NguoiDungId = nguoiDungId,
                    LopHocId = lopHocId,
                    NgayBatDau = DateOnly.FromDateTime(ngayBatDau),
                    NgayKetThuc = DateOnly.FromDateTime(ngayKetThuc),
                    TrangThai = "ACTIVE",
                    NgayTao = DateTime.Now
                };

                await _dangKyRepository.AddAsync(dangKy);
                await _unitOfWork.SaveChangesAsync();

                // Update payment to link with registration
                thanhToan.DangKyId = dangKy.DangKyId;
                await _unitOfWork.SaveChangesAsync();

                await transaction.CommitAsync();

                // Send notification after successful registration
                await _thongBaoService.CreateNotificationAsync(
                    nguoiDungId,
                    "Đăng ký lớp học thành công",
                    $"Bạn đã đăng ký thành công lớp {lopHoc.TenLop}. Thời gian: {lopHoc.GioBatDau:HH:mm}-{lopHoc.GioKetThuc:HH:mm}",
                    "APP"
                );

                return true;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw; // Re-throw to be handled by calling method
            }
        }

        public async Task<bool> CreateFixedClassRegistrationAfterPaymentAsync(int nguoiDungId, int lopHocId, int thanhToanId)
        {
            // Verify payment exists and is successful
            var thanhToan = await _unitOfWork.Context.ThanhToans.FindAsync(thanhToanId);
            if (thanhToan == null || thanhToan.TrangThai != "SUCCESS") return false;

            // Check if registration already exists for this payment
            var existingRegistration = await _unitOfWork.Context.DangKys
                .FirstOrDefaultAsync(d => d.ThanhToans.Any(t => t.ThanhToanId == thanhToanId));
            if (existingRegistration != null) return true; // Already created

            var lopHoc = await _lopHocRepository.GetByIdAsync(lopHocId);
            if (lopHoc == null || lopHoc.TrangThai != "OPEN") return false;

            // Validate class has fixed schedule dates
            if (!lopHoc.NgayBatDauKhoa.HasValue || !lopHoc.NgayKetThucKhoa.HasValue) return false;

            // Check if class has already started
            if (lopHoc.NgayBatDauKhoa.Value < DateOnly.FromDateTime(DateTime.Today)) return false;

            // ⚠️ BUSINESS RULE VALIDATION: Check if user already has active registration for this specific class
            if (await HasActiveClassRegistrationAsync(nguoiDungId, lopHocId))
            {
                Console.WriteLine($"⚠️ User {nguoiDungId} already has active class registration for class {lopHocId} but payment {thanhToanId} was successful. Skipping registration creation.");
                return false; // Don't create duplicate registration
            }

            using var transaction = await _unitOfWork.Context.Database.BeginTransactionAsync();
            try
            {
                // Re-check capacity within transaction
                var currentCountInTransaction = await GetActiveRegistrationCountAsync(lopHocId);
                if (currentCountInTransaction >= lopHoc.SucChua)
                {
                    await transaction.RollbackAsync();
                    return false;
                }

                var dangKy = new DangKy
                {
                    NguoiDungId = nguoiDungId,
                    LopHocId = lopHocId,
                    NgayBatDau = lopHoc.NgayBatDauKhoa.Value,
                    NgayKetThuc = lopHoc.NgayKetThucKhoa.Value,
                    TrangThai = "ACTIVE",
                    NgayTao = DateTime.Now,
                    LoaiDangKy = "CLASS",
                    TrangThaiChiTiet = "ENROLLED"
                };

                await _dangKyRepository.AddAsync(dangKy);
                await _unitOfWork.SaveChangesAsync();

                // Update payment to link with registration
                thanhToan.DangKyId = dangKy.DangKyId;
                await _unitOfWork.SaveChangesAsync();

                await transaction.CommitAsync();

                // Send notification
                await _thongBaoService.CreateNotificationAsync(
                    nguoiDungId,
                    "Đăng ký lớp học thành công",
                    $"Bạn đã đăng ký thành công lớp {lopHoc.TenLop}. Lớp sẽ bắt đầu vào {lopHoc.NgayBatDauKhoa:dd/MM/yyyy}",
                    "APP"
                );

                return true;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}
