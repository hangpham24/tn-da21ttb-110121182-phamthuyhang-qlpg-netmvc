using GymManagement.Web.Data.Models;
using GymManagement.Web.Models.DTOs;

namespace GymManagement.Web.Services
{
    public interface IKhuyenMaiService
    {
        Task<IEnumerable<KhuyenMai>> GetAllAsync();
        Task<KhuyenMai?> GetByIdAsync(int id);
        Task<KhuyenMai?> GetByCodeAsync(string code);
        Task<bool> CreateAsync(KhuyenMai khuyenMai);
        Task<bool> UpdateAsync(KhuyenMai khuyenMai);
        Task<bool> DeleteAsync(int id);
        Task<bool> ValidateCodeAsync(string code);
        Task<decimal> CalculateDiscountAsync(string code, decimal originalAmount);
        Task<bool> IsCodeUniqueAsync(string code, int? excludeId = null);
        Task<IEnumerable<KhuyenMai>> GetActivePromotionsAsync();
        Task<bool> DeactivateExpiredPromotionsAsync();
        Task<KhuyenMaiValidationResult> ValidatePromotionAsync(string code, decimal orderAmount = 0);
        Task<bool> TrackUsageAsync(int khuyenMaiId, int nguoiDungId, decimal soTienGoc, decimal soTienGiam, decimal soTienCuoi, int? thanhToanId = null, int? dangKyId = null, string? ghiChu = null);
        Task<IEnumerable<KhuyenMaiUsage>> GetUsageHistoryAsync(int khuyenMaiId);
        Task<IEnumerable<KhuyenMaiUsage>> GetUserUsageHistoryAsync(int nguoiDungId);
        Task<int> GetUsageCountAsync(int khuyenMaiId);
        Task<decimal> GetTotalDiscountAmountAsync(int khuyenMaiId);
    }

    public class KhuyenMaiValidationResult
    {
        public bool IsValid { get; set; }
        public string? ErrorMessage { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal FinalAmount { get; set; }
        public KhuyenMai? Promotion { get; set; }
    }
}
