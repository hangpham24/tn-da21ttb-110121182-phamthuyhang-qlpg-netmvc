using GymManagement.Web.Data;
using GymManagement.Web.Data.Models;
using GymManagement.Web.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace GymManagement.Web.Services
{
    public class KhuyenMaiService : IKhuyenMaiService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<KhuyenMaiService> _logger;

        public KhuyenMaiService(IUnitOfWork unitOfWork, ILogger<KhuyenMaiService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<IEnumerable<KhuyenMai>> GetAllAsync()
        {
            try
            {
                return await _unitOfWork.Context.KhuyenMais
                    .OrderByDescending(k => k.NgayTao)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting all promotions");
                return new List<KhuyenMai>();
            }
        }

        public async Task<KhuyenMai?> GetByIdAsync(int id)
        {
            try
            {
                return await _unitOfWork.Context.KhuyenMais.FindAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting promotion by ID: {Id}", id);
                return null;
            }
        }

        public async Task<KhuyenMai?> GetByCodeAsync(string code)
        {
            try
            {
                return await _unitOfWork.Context.KhuyenMais
                    .FirstOrDefaultAsync(k => k.MaCode == code);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting promotion by code: {Code}", code);
                return null;
            }
        }

        public async Task<bool> CreateAsync(KhuyenMai khuyenMai)
        {
            try
            {
                // Validate unique code
                if (!await IsCodeUniqueAsync(khuyenMai.MaCode))
                {
                    _logger.LogWarning("Attempted to create promotion with duplicate code: {Code}", khuyenMai.MaCode);
                    return false;
                }

                _unitOfWork.Context.KhuyenMais.Add(khuyenMai);
                await _unitOfWork.SaveChangesAsync();
                
                _logger.LogInformation("Created new promotion: {Code}", khuyenMai.MaCode);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating promotion: {Code}", khuyenMai.MaCode);
                return false;
            }
        }

        public async Task<bool> UpdateAsync(KhuyenMai khuyenMai)
        {
            try
            {
                // Validate unique code (excluding current record)
                if (!await IsCodeUniqueAsync(khuyenMai.MaCode, khuyenMai.KhuyenMaiId))
                {
                    _logger.LogWarning("Attempted to update promotion with duplicate code: {Code}", khuyenMai.MaCode);
                    return false;
                }

                _unitOfWork.Context.KhuyenMais.Update(khuyenMai);
                await _unitOfWork.SaveChangesAsync();
                
                _logger.LogInformation("Updated promotion: {Code}", khuyenMai.MaCode);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating promotion: {Code}", khuyenMai.MaCode);
                return false;
            }
        }

        public async Task<bool> DeleteAsync(int id)
        {
            try
            {
                var khuyenMai = await GetByIdAsync(id);
                if (khuyenMai == null) return false;

                _unitOfWork.Context.KhuyenMais.Remove(khuyenMai);
                await _unitOfWork.SaveChangesAsync();
                
                _logger.LogInformation("Deleted promotion: {Code}", khuyenMai.MaCode);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting promotion with ID: {Id}", id);
                return false;
            }
        }

        public async Task<bool> ValidateCodeAsync(string code)
        {
            try
            {
                var khuyenMai = await GetByCodeAsync(code);
                if (khuyenMai == null) return false;

                var today = DateOnly.FromDateTime(DateTime.Today);
                return khuyenMai.KichHoat && 
                       today >= khuyenMai.NgayBatDau && 
                       today <= khuyenMai.NgayKetThuc;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while validating code: {Code}", code);
                return false;
            }
        }

        public async Task<decimal> CalculateDiscountAsync(string code, decimal originalAmount)
        {
            try
            {
                var khuyenMai = await GetByCodeAsync(code);
                if (khuyenMai == null || !await ValidateCodeAsync(code))
                    return 0;

                if (khuyenMai.PhanTramGiam.HasValue)
                {
                    return originalAmount * khuyenMai.PhanTramGiam.Value / 100;
                }

                return 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while calculating discount for code: {Code}", code);
                return 0;
            }
        }

        public async Task<bool> IsCodeUniqueAsync(string code, int? excludeId = null)
        {
            try
            {
                var query = _unitOfWork.Context.KhuyenMais.Where(k => k.MaCode == code);
                
                if (excludeId.HasValue)
                {
                    query = query.Where(k => k.KhuyenMaiId != excludeId.Value);
                }

                return !await query.AnyAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while checking code uniqueness: {Code}", code);
                return false;
            }
        }

        public async Task<IEnumerable<KhuyenMai>> GetActivePromotionsAsync()
        {
            try
            {
                var today = DateOnly.FromDateTime(DateTime.Today);
                return await _unitOfWork.Context.KhuyenMais
                    .Where(k => k.KichHoat && 
                               k.NgayBatDau <= today && 
                               k.NgayKetThuc >= today)
                    .OrderBy(k => k.NgayKetThuc)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting active promotions");
                return new List<KhuyenMai>();
            }
        }

        public async Task<bool> DeactivateExpiredPromotionsAsync()
        {
            try
            {
                var today = DateOnly.FromDateTime(DateTime.Today);
                var expiredPromotions = await _unitOfWork.Context.KhuyenMais
                    .Where(k => k.KichHoat && k.NgayKetThuc < today)
                    .ToListAsync();

                foreach (var promotion in expiredPromotions)
                {
                    promotion.KichHoat = false;
                }

                if (expiredPromotions.Any())
                {
                    await _unitOfWork.SaveChangesAsync();
                    _logger.LogInformation("Deactivated {Count} expired promotions", expiredPromotions.Count);
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deactivating expired promotions");
                return false;
            }
        }

        public async Task<KhuyenMaiValidationResult> ValidatePromotionAsync(string code, decimal orderAmount = 0)
        {
            var result = new KhuyenMaiValidationResult
            {
                IsValid = false,
                DiscountAmount = 0,
                FinalAmount = orderAmount
            };

            try
            {
                if (string.IsNullOrWhiteSpace(code))
                {
                    result.ErrorMessage = "Vui lòng nhập mã khuyến mãi";
                    return result;
                }

                var khuyenMai = await GetByCodeAsync(code);
                if (khuyenMai == null)
                {
                    result.ErrorMessage = "Mã khuyến mãi không tồn tại";
                    return result;
                }

                if (!khuyenMai.KichHoat)
                {
                    result.ErrorMessage = "Mã khuyến mãi đã bị vô hiệu hóa";
                    return result;
                }

                var today = DateOnly.FromDateTime(DateTime.Today);
                if (today < khuyenMai.NgayBatDau)
                {
                    result.ErrorMessage = $"Mã khuyến mãi chưa có hiệu lực. Có hiệu lực từ {khuyenMai.NgayBatDau:dd/MM/yyyy}";
                    return result;
                }

                if (today > khuyenMai.NgayKetThuc)
                {
                    result.ErrorMessage = $"Mã khuyến mãi đã hết hạn. Hết hạn vào {khuyenMai.NgayKetThuc:dd/MM/yyyy}";
                    return result;
                }

                // Calculate discount
                if (khuyenMai.PhanTramGiam.HasValue && orderAmount > 0)
                {
                    result.DiscountAmount = orderAmount * khuyenMai.PhanTramGiam.Value / 100;
                    result.FinalAmount = orderAmount - result.DiscountAmount;
                }

                result.IsValid = true;
                result.Promotion = khuyenMai;
                result.ErrorMessage = null;

                _logger.LogInformation("Validated promotion code: {Code} for amount: {Amount}", code, orderAmount);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while validating promotion: {Code}", code);
                result.ErrorMessage = "Có lỗi xảy ra khi kiểm tra mã khuyến mãi";
                return result;
            }
        }

        public async Task<bool> TrackUsageAsync(int khuyenMaiId, int nguoiDungId, decimal soTienGoc, decimal soTienGiam, decimal soTienCuoi, int? thanhToanId = null, int? dangKyId = null, string? ghiChu = null)
        {
            try
            {
                var usage = new KhuyenMaiUsage
                {
                    KhuyenMaiId = khuyenMaiId,
                    NguoiDungId = nguoiDungId,
                    ThanhToanId = thanhToanId,
                    DangKyId = dangKyId,
                    SoTienGoc = soTienGoc,
                    SoTienGiam = soTienGiam,
                    SoTienCuoi = soTienCuoi,
                    NgaySuDung = DateTime.Now,
                    GhiChu = ghiChu
                };

                _unitOfWork.Context.KhuyenMaiUsages.Add(usage);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Tracked promotion usage: KhuyenMaiId={KhuyenMaiId}, NguoiDungId={NguoiDungId}, Amount={Amount}",
                    khuyenMaiId, nguoiDungId, soTienGiam);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while tracking promotion usage");
                return false;
            }
        }

        public async Task<IEnumerable<KhuyenMaiUsage>> GetUsageHistoryAsync(int khuyenMaiId)
        {
            try
            {
                return await _unitOfWork.Context.KhuyenMaiUsages
                    .Where(u => u.KhuyenMaiId == khuyenMaiId)
                    .Include(u => u.NguoiDung)
                    .Include(u => u.ThanhToan)
                    .Include(u => u.DangKy)
                    .OrderByDescending(u => u.NgaySuDung)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting usage history for promotion: {KhuyenMaiId}", khuyenMaiId);
                return new List<KhuyenMaiUsage>();
            }
        }

        public async Task<IEnumerable<KhuyenMaiUsage>> GetUserUsageHistoryAsync(int nguoiDungId)
        {
            try
            {
                return await _unitOfWork.Context.KhuyenMaiUsages
                    .Where(u => u.NguoiDungId == nguoiDungId)
                    .Include(u => u.KhuyenMai)
                    .Include(u => u.ThanhToan)
                    .Include(u => u.DangKy)
                    .OrderByDescending(u => u.NgaySuDung)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting user usage history: {NguoiDungId}", nguoiDungId);
                return new List<KhuyenMaiUsage>();
            }
        }

        public async Task<int> GetUsageCountAsync(int khuyenMaiId)
        {
            try
            {
                return await _unitOfWork.Context.KhuyenMaiUsages
                    .CountAsync(u => u.KhuyenMaiId == khuyenMaiId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting usage count for promotion: {KhuyenMaiId}", khuyenMaiId);
                return 0;
            }
        }

        public async Task<decimal> GetTotalDiscountAmountAsync(int khuyenMaiId)
        {
            try
            {
                return await _unitOfWork.Context.KhuyenMaiUsages
                    .Where(u => u.KhuyenMaiId == khuyenMaiId)
                    .SumAsync(u => u.SoTienGiam);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting total discount amount for promotion: {KhuyenMaiId}", khuyenMaiId);
                return 0;
            }
        }
    }
}
