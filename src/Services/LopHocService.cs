using GymManagement.Web.Data;
using GymManagement.Web.Data.Models;
using GymManagement.Web.Data.Repositories;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.EntityFrameworkCore;

namespace GymManagement.Web.Services
{
    public class LopHocService : ILopHocService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILopHocRepository _lopHocRepository;
        private readonly IBookingRepository _bookingRepository;
        private readonly IMemoryCache _cache;

        public LopHocService(
            IUnitOfWork unitOfWork,
            ILopHocRepository lopHocRepository,
            IBookingRepository bookingRepository,
            IMemoryCache cache)
        {
            _unitOfWork = unitOfWork;
            _lopHocRepository = lopHocRepository;
            _bookingRepository = bookingRepository;
            _cache = cache;
        }

        public async Task<IEnumerable<LopHoc>> GetAllAsync()
        {
            const string cacheKey = "all_classes";
            return await _cache.GetOrCreateAsync(cacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
                entry.Priority = CacheItemPriority.High;
                entry.SetSlidingExpiration(TimeSpan.FromMinutes(5));

                return await _lopHocRepository.GetAllAsync();
            }) ?? new List<LopHoc>();
        }

        public void ClearCache()
        {
            _cache.Remove("all_classes");
            _cache.Remove("active_classes");
            _cache.Remove("active_classes_detailed");

            // Clear individual class caches
            // Note: In production, consider using cache tags for better management
        }

        public async Task<LopHoc?> GetByIdAsync(int id)
        {
            string cacheKey = $"class_{id}";
            return await _cache.GetOrCreateAsync(cacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15);
                entry.Priority = CacheItemPriority.Normal;

                return await _lopHocRepository.GetByIdAsync(id);
            });
        }

        public async Task<LopHoc> CreateAsync(LopHoc lopHoc)
        {
            try
            {
                // Validate business rules
                await ValidateClassBusinessRulesAsync(lopHoc);

                var created = await _lopHocRepository.AddAsync(lopHoc);
                await _unitOfWork.SaveChangesAsync();

                // Clear cache efficiently
                ClearCache();

                return created;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to create class: {ex.Message}", ex);
            }
        }

        public async Task<LopHoc> UpdateAsync(LopHoc lopHoc)
        {
            try
            {
                // Validate business rules
                await ValidateClassBusinessRulesAsync(lopHoc);

                await _lopHocRepository.UpdateAsync(lopHoc);
                await _unitOfWork.SaveChangesAsync();

                // Clear cache efficiently
                ClearCache();
                _cache.Remove($"class_{lopHoc.LopHocId}");

                return lopHoc;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to update class: {ex.Message}", ex);
            }
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var lopHoc = await _lopHocRepository.GetByIdAsync(id);
            if (lopHoc == null) return false;

            await _lopHocRepository.DeleteAsync(lopHoc);
            await _unitOfWork.SaveChangesAsync();
            
            // Clear cache
            ClearCache();
            
            return true;
        }

        /// <summary>
        /// Check if a class can be deleted
        /// </summary>
        public async Task<(bool CanDelete, string Message)> CanDeleteClassAsync(int lopHocId)
        {
            try
            {
                var lopHoc = await _lopHocRepository.GetByIdAsync(lopHocId);
                if (lopHoc == null)
                {
                    return (false, "Không tìm thấy lớp học.");
                }

                // Check for active registrations
                var activeRegistrations = lopHoc.DangKys?.Count(d => d.TrangThai == "ACTIVE") ?? 0;
                if (activeRegistrations > 0)
                {
                    return (false, $"Không thể xóa lớp học này vì đang có {activeRegistrations} học viên đang hoạt động.");
                }

                // Check for future bookings
                var futureBookings = lopHoc.Bookings?.Count(b => b.Ngay >= DateOnly.FromDateTime(DateTime.Today)) ?? 0;
                if (futureBookings > 0)
                {
                    return (false, $"Không thể xóa lớp học này vì đang có {futureBookings} lịch đặt trong tương lai.");
                }

                // Note: LichLops table has been removed - no need to check schedules

                // Check for completed sessions that need to be preserved
                var completedSessions = lopHoc.BuoiTaps?.Count() ?? 0;
                if (completedSessions > 0)
                {
                    return (false, $"Không thể xóa lớp học này vì đã có {completedSessions} buổi tập đã hoàn thành. Bạn có thể đổi trạng thái thành 'Đã đóng' thay vì xóa.");
                }

                return (true, "Lớp học có thể xóa được.");
            }
            catch (Exception ex)
            {
                return (false, $"Có lỗi xảy ra khi kiểm tra: {ex.Message}");
            }
        }

        public async Task<IEnumerable<LopHoc>> GetActiveClassesAsync()
        {
            const string cacheKey = "active_classes_detailed";
            return await _cache.GetOrCreateAsync(cacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(3);
                entry.Priority = CacheItemPriority.High;
                entry.SetSlidingExpiration(TimeSpan.FromMinutes(1));

                // Use optimized method to prevent N+1 queries
                return await _lopHocRepository.GetActiveClassesWithDetailsAsync();
            }) ?? new List<LopHoc>();
        }

        public async Task<IEnumerable<LopHoc>> GetClassesByTrainerAsync(int hlvId)
        {
            return await _lopHocRepository.GetClassesByTrainerAsync(hlvId);
        }

        public async Task<bool> IsClassAvailableAsync(int lopHocId, DateTime date)
        {
            var lopHoc = await _lopHocRepository.GetByIdAsync(lopHocId);
            if (lopHoc == null || lopHoc.TrangThai != "OPEN") return false;

            var bookingCount = await _bookingRepository.CountBookingsForClassAsync(lopHocId, date);
            return bookingCount < lopHoc.SucChua;
        }

        public async Task<int> GetAvailableSlotsAsync(int lopHocId, DateTime date)
        {
            var lopHoc = await _lopHocRepository.GetByIdAsync(lopHocId);
            if (lopHoc == null) return 0;

            var bookingCount = await _bookingRepository.CountBookingsForClassAsync(lopHocId, date);
            return Math.Max(0, lopHoc.SucChua - bookingCount);
        }



        private string GetVietnameseDayOfWeek(DayOfWeek dayOfWeek)
        {
            return dayOfWeek switch
            {
                DayOfWeek.Monday => "Thứ 2",
                DayOfWeek.Tuesday => "Thứ 3",
                DayOfWeek.Wednesday => "Thứ 4",
                DayOfWeek.Thursday => "Thứ 5",
                DayOfWeek.Friday => "Thứ 6",
                DayOfWeek.Saturday => "Thứ 7",
                DayOfWeek.Sunday => "Chủ nhật",
                _ => ""
            };
        }

        /// <summary>
        /// Validate business rules for class creation/update
        /// </summary>
        private async Task ValidateClassBusinessRulesAsync(LopHoc lopHoc)
        {
            // Validate time range
            if (lopHoc.GioKetThuc <= lopHoc.GioBatDau)
            {
                throw new ArgumentException("Giờ kết thúc phải sau giờ bắt đầu");
            }

            // Validate capacity
            if (lopHoc.SucChua <= 0 || lopHoc.SucChua > 100)
            {
                throw new ArgumentException("Sức chứa phải từ 1 đến 100 người");
            }

            // Validate duration if provided
            if (lopHoc.ThoiLuong.HasValue && (lopHoc.ThoiLuong < 15 || lopHoc.ThoiLuong > 300))
            {
                throw new ArgumentException("Thời lượng phải từ 15 đến 300 phút");
            }

            // Validate trainer assignment
            if (lopHoc.HlvId.HasValue)
            {
                var trainer = await _unitOfWork.NguoiDungs.GetByIdAsync(lopHoc.HlvId.Value);
                if (trainer == null || trainer.LoaiNguoiDung != "HLV")
                {
                    throw new ArgumentException("Huấn luyện viên không hợp lệ");
                }
            }

            // Check for time conflicts with existing classes (same trainer)
            if (lopHoc.HlvId.HasValue)
            {
                var existingClasses = await _lopHocRepository.GetByHuanLuyenVienAsync(lopHoc.HlvId.Value);
                var conflictingClass = existingClasses.FirstOrDefault(c =>
                    c.LopHocId != lopHoc.LopHocId && // Exclude current class for updates
                    HasTimeConflict(lopHoc, c));

                if (conflictingClass != null)
                {
                    throw new InvalidOperationException($"Huấn luyện viên đã có lịch dạy lớp '{conflictingClass.TenLop}' trùng thời gian");
                }
            }

            // Check for general time conflicts with all existing classes
            // This helps prevent creating classes that members cannot register for due to schedule conflicts
            await ValidateGeneralTimeConflictsAsync(lopHoc);
        }

        /// <summary>
        /// Validate general time conflicts with all existing classes
        /// Provides warnings about potential member registration conflicts
        /// </summary>
        private async Task ValidateGeneralTimeConflictsAsync(LopHoc lopHoc)
        {
            var allClasses = await _lopHocRepository.GetAllAsync();
            var conflictingClasses = allClasses.Where(c =>
                c.LopHocId != lopHoc.LopHocId && // Exclude current class for updates
                c.TrangThai == "OPEN" && // Only check active classes
                HasTimeConflict(lopHoc, c)).ToList();

            if (conflictingClasses.Any())
            {
                var conflictNames = string.Join(", ", conflictingClasses.Select(c => $"'{c.TenLop}'"));

                // For now, we'll throw a warning exception
                // In the future, this could be changed to just log a warning
                throw new InvalidOperationException(
                    $"⚠️ CẢNH BÁO: Lớp học này có thời gian trùng với {conflictingClasses.Count} lớp khác: {conflictNames}. " +
                    "Điều này có thể khiến thành viên không thể đăng ký cả hai lớp cùng lúc. " +
                    "Bạn có chắc chắn muốn tạo lớp này không?");
            }
        }

        /// <summary>
        /// Check if two classes have time conflicts
        /// </summary>
        private bool HasTimeConflict(LopHoc class1, LopHoc class2)
        {
            // Parse days of week
            var days1 = ParseDaysOfWeek(class1.ThuTrongTuan);
            var days2 = ParseDaysOfWeek(class2.ThuTrongTuan);

            // Check if there's any common day
            if (!days1.Intersect(days2).Any())
                return false;

            // Check time overlap
            return class1.GioBatDau < class2.GioKetThuc && class1.GioKetThuc > class2.GioBatDau;
        }

        /// <summary>
        /// Parse days of week string to list of integers
        /// </summary>
        private List<int> ParseDaysOfWeek(string thuTrongTuan)
        {
            var days = new List<int>();
            if (string.IsNullOrEmpty(thuTrongTuan)) return days;

            var dayStrings = thuTrongTuan.Split(',', StringSplitOptions.RemoveEmptyEntries);
            foreach (var dayStr in dayStrings)
            {
                var trimmed = dayStr.Trim();
                if (int.TryParse(trimmed, out int dayNum))
                {
                    days.Add(dayNum);
                }
                else
                {
                    // Handle Vietnamese day names
                    var dayNum2 = trimmed switch
                    {
                        "Thứ 2" => 2,
                        "Thứ 3" => 3,
                        "Thứ 4" => 4,
                        "Thứ 5" => 5,
                        "Thứ 6" => 6,
                        "Thứ 7" => 7,
                        "Chủ nhật" => 8,
                        _ => 0
                    };
                    if (dayNum2 > 0) days.Add(dayNum2);
                }
            }
            return days;
        }
    }
}
