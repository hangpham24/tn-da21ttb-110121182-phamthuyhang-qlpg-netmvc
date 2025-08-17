using Microsoft.AspNetCore.Mvc;
using GymManagement.Web.Services;
using GymManagement.Web.Data.Models;
using GymManagement.Web.Data;

namespace GymManagement.Web.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    public class KhuyenMaiController : ControllerBase
    {
        private readonly IKhuyenMaiService _khuyenMaiService;
        private readonly IDangKyService _dangKyService;
        private readonly ILogger<KhuyenMaiController> _logger;
        private readonly IUnitOfWork _unitOfWork;

        public KhuyenMaiController(
            IKhuyenMaiService khuyenMaiService,
            IDangKyService dangKyService,
            ILogger<KhuyenMaiController> logger,
            IUnitOfWork unitOfWork)
        {
            _khuyenMaiService = khuyenMaiService;
            _dangKyService = dangKyService;
            _logger = logger;
            _unitOfWork = unitOfWork;
        }

        [HttpPost("validate")]
        public async Task<IActionResult> ValidatePromotionCode([FromBody] ValidatePromotionRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.PromotionCode))
                {
                    return Ok(new { success = false, message = "Mã khuyến mãi không được để trống" });
                }

                var promotion = await _khuyenMaiService.GetByCodeAsync(request.PromotionCode);
                if (promotion == null)
                {
                    return Ok(new { success = false, message = "Mã khuyến mãi không tồn tại" });
                }

                // Calculate original price first (WITHOUT promotion applied)
                var originalPrice = await _dangKyService.CalculatePackageFeeAsync(request.PackageId, request.Duration, null);

                // 🐛 DEBUG: Log calculation
                _logger.LogInformation("🐛 DEBUG API: PackageId={PackageId}, Duration={Duration}, OriginalPrice={OriginalPrice}",
                    request.PackageId, request.Duration, originalPrice);

                // Validate promotion with order amount
                var validationResult = await _khuyenMaiService.ValidatePromotionAsync(request.PromotionCode, originalPrice);

                // 🐛 DEBUG: Log validation result
                _logger.LogInformation("🐛 DEBUG Validation: IsValid={IsValid}, DiscountAmount={DiscountAmount}, FinalAmount={FinalAmount}",
                    validationResult.IsValid, validationResult.DiscountAmount, validationResult.FinalAmount);

                if (!validationResult.IsValid)
                {
                    return Ok(new { success = false, message = validationResult.ErrorMessage });
                }

                return Ok(new
                {
                    success = true,
                    promotionId = promotion.KhuyenMaiId,
                    promotionName = promotion.MaCode,
                    promotionDescription = promotion.MoTa,
                    discountPercent = promotion.PhanTramGiam,
                    originalPrice = originalPrice,
                    finalPrice = validationResult.FinalAmount,
                    discountAmount = validationResult.DiscountAmount
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating promotion code: {Code}", request.PromotionCode);
                return Ok(new { success = false, message = "Có lỗi xảy ra khi kiểm tra mã khuyến mãi" });
            }
        }

        [HttpGet("debug/{packageId}/{duration}")]
        public async Task<IActionResult> DebugPackagePrice(int packageId, int duration)
        {
            try
            {
                // Get package info
                var package = await _unitOfWork.Context.GoiTaps.FindAsync(packageId);
                if (package == null)
                {
                    return Ok(new { error = "Package not found" });
                }

                // Calculate prices
                var originalPrice = await _dangKyService.CalculatePackageFeeAsync(packageId, duration, null);

                // Get WELCOME2024 promotion
                var promotion = await _khuyenMaiService.GetByCodeAsync("WELCOME2024");

                return Ok(new
                {
                    package = new
                    {
                        id = package.GoiTapId,
                        name = package.TenGoi,
                        monthlyPrice = package.Gia,
                        duration = duration,
                        totalPrice = originalPrice, // ✅ FIXED: Use calculated price instead of multiplication
                        calculatedPrice = originalPrice
                    },
                    promotion = promotion != null ? new
                    {
                        id = promotion.KhuyenMaiId,
                        code = promotion.MaCode,
                        description = promotion.MoTa,
                        discountPercent = promotion.PhanTramGiam,
                        isActive = promotion.KichHoat,
                        startDate = promotion.NgayBatDau,
                        endDate = promotion.NgayKetThuc
                    } : null
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in debug endpoint");
                return Ok(new { error = ex.Message });
            }
        }
    }

    public class ValidatePromotionRequest
    {
        public string PromotionCode { get; set; } = string.Empty;
        public int PackageId { get; set; }
        public int Duration { get; set; }
    }
}
