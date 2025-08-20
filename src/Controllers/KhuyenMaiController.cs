using GymManagement.Web.Data.Models;
using GymManagement.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymManagement.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class KhuyenMaiController : Controller
    {
        private readonly IKhuyenMaiService _khuyenMaiService;
        private readonly ILogger<KhuyenMaiController> _logger;

        public KhuyenMaiController(IKhuyenMaiService khuyenMaiService, ILogger<KhuyenMaiController> logger)
        {
            _khuyenMaiService = khuyenMaiService;
            _logger = logger;
        }

        // GET: KhuyenMai
        public async Task<IActionResult> Index(string? search, string? status, int page = 1, int pageSize = 5)
        {
            try
            {
                var allKhuyenMais = await _khuyenMaiService.GetAllAsync();

                // Auto-deactivate expired promotions
                await _khuyenMaiService.DeactivateExpiredPromotionsAsync();

                // Apply filters
                var filteredKhuyenMais = allKhuyenMais.AsQueryable();

                // Search filter
                if (!string.IsNullOrWhiteSpace(search))
                {
                    filteredKhuyenMais = filteredKhuyenMais.Where(k =>
                        k.MaCode.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                        (k.MoTa != null && k.MoTa.Contains(search, StringComparison.OrdinalIgnoreCase)));
                }

                // Status filter
                var today = DateOnly.FromDateTime(DateTime.Today);
                if (!string.IsNullOrWhiteSpace(status))
                {
                    filteredKhuyenMais = status.ToLower() switch
                    {
                        "active" => filteredKhuyenMais.Where(k => k.KichHoat && k.NgayBatDau <= today && k.NgayKetThuc >= today),
                        "expired" => filteredKhuyenMais.Where(k => k.NgayKetThuc < today),
                        "upcoming" => filteredKhuyenMais.Where(k => k.NgayBatDau > today),
                        "disabled" => filteredKhuyenMais.Where(k => !k.KichHoat),
                        "expiring" => filteredKhuyenMais.Where(k => k.KichHoat && k.NgayKetThuc >= today && k.NgayKetThuc <= today.AddDays(7)),
                        _ => filteredKhuyenMais
                    };
                }

                // Order by creation date (newest first)
                filteredKhuyenMais = filteredKhuyenMais.OrderByDescending(k => k.NgayTao);

                // Pagination
                var totalItems = filteredKhuyenMais.Count();
                var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);
                var pagedKhuyenMais = filteredKhuyenMais
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                // Pass data to view
                ViewBag.CurrentSearch = search;
                ViewBag.CurrentStatus = status;
                ViewBag.CurrentPage = page;
                ViewBag.PageSize = pageSize;
                ViewBag.TotalPages = totalPages;
                ViewBag.TotalItems = totalItems;
                ViewBag.HasPreviousPage = page > 1;
                ViewBag.HasNextPage = page < totalPages;
                ViewBag.AllKhuyenMais = allKhuyenMais; // For statistics

                return View(pagedKhuyenMais);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting promotions list");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải danh sách khuyến mãi.";
                return View(new List<KhuyenMai>());
            }
        }

        // GET: KhuyenMai/Details/5
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var khuyenMai = await _khuyenMaiService.GetByIdAsync(id);
                if (khuyenMai == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy khuyến mãi.";
                    return RedirectToAction(nameof(Index));
                }

                return View(khuyenMai);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting promotion details for ID: {Id}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải thông tin khuyến mãi.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: KhuyenMai/Create
        public IActionResult Create()
        {
            var model = new KhuyenMai
            {
                NgayBatDau = DateOnly.FromDateTime(DateTime.Today),
                NgayKetThuc = DateOnly.FromDateTime(DateTime.Today.AddDays(30)),
                KichHoat = true
            };
            return View(model);
        }

        // POST: KhuyenMai/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(KhuyenMai khuyenMai)
        {
            try
            {
                // Custom validation
                if (khuyenMai.NgayKetThuc <= khuyenMai.NgayBatDau)
                {
                    ModelState.AddModelError("NgayKetThuc", "Ngày kết thúc phải sau ngày bắt đầu.");
                }

                if (!await _khuyenMaiService.IsCodeUniqueAsync(khuyenMai.MaCode))
                {
                    ModelState.AddModelError("MaCode", "Mã khuyến mãi đã tồn tại.");
                }

                if (ModelState.IsValid)
                {
                    var success = await _khuyenMaiService.CreateAsync(khuyenMai);
                    if (success)
                    {
                        TempData["SuccessMessage"] = "Tạo khuyến mãi thành công!";
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        ModelState.AddModelError("", "Có lỗi xảy ra khi tạo khuyến mãi.");
                    }
                }

                return View(khuyenMai);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating promotion");
                ModelState.AddModelError("", "Có lỗi xảy ra khi tạo khuyến mãi.");
                return View(khuyenMai);
            }
        }

        // GET: KhuyenMai/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var khuyenMai = await _khuyenMaiService.GetByIdAsync(id);
                if (khuyenMai == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy khuyến mãi.";
                    return RedirectToAction(nameof(Index));
                }

                return View(khuyenMai);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting promotion for edit: {Id}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải thông tin khuyến mãi.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: KhuyenMai/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, KhuyenMai khuyenMai)
        {
            if (id != khuyenMai.KhuyenMaiId)
            {
                return NotFound();
            }

            try
            {
                // Custom validation
                if (khuyenMai.NgayKetThuc <= khuyenMai.NgayBatDau)
                {
                    ModelState.AddModelError("NgayKetThuc", "Ngày kết thúc phải sau ngày bắt đầu.");
                }

                if (!await _khuyenMaiService.IsCodeUniqueAsync(khuyenMai.MaCode, khuyenMai.KhuyenMaiId))
                {
                    ModelState.AddModelError("MaCode", "Mã khuyến mãi đã tồn tại.");
                }

                if (ModelState.IsValid)
                {
                    var success = await _khuyenMaiService.UpdateAsync(khuyenMai);
                    if (success)
                    {
                        TempData["SuccessMessage"] = "Cập nhật khuyến mãi thành công!";
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        ModelState.AddModelError("", "Có lỗi xảy ra khi cập nhật khuyến mãi.");
                    }
                }

                return View(khuyenMai);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating promotion: {Id}", id);
                ModelState.AddModelError("", "Có lỗi xảy ra khi cập nhật khuyến mãi.");
                return View(khuyenMai);
            }
        }

        // GET: KhuyenMai/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var khuyenMai = await _khuyenMaiService.GetByIdAsync(id);
                if (khuyenMai == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy khuyến mãi.";
                    return RedirectToAction(nameof(Index));
                }

                return View(khuyenMai);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting promotion for delete: {Id}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải thông tin khuyến mãi.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: KhuyenMai/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var success = await _khuyenMaiService.DeleteAsync(id);
                if (success)
                {
                    TempData["SuccessMessage"] = "Xóa khuyến mãi thành công!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Không thể xóa khuyến mãi.";
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting promotion: {Id}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi xóa khuyến mãi.";
                return RedirectToAction(nameof(Index));
            }
        }

        // AJAX: Validate promotion code
        [HttpPost]
        public async Task<IActionResult> ValidateCode(string code, decimal amount = 0)
        {
            try
            {
                var result = await _khuyenMaiService.ValidatePromotionAsync(code, amount);
                return Json(new
                {
                    isValid = result.IsValid,
                    errorMessage = result.ErrorMessage,
                    discountAmount = result.DiscountAmount,
                    finalAmount = result.FinalAmount,
                    promotion = result.Promotion != null ? new
                    {
                        maCode = result.Promotion.MaCode,
                        moTa = result.Promotion.MoTa,
                        phanTramGiam = result.Promotion.PhanTramGiam,
                        ngayKetThuc = result.Promotion.NgayKetThuc.ToString("dd/MM/yyyy")
                    } : null
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while validating promotion code: {Code}", code);
                return Json(new
                {
                    isValid = false,
                    errorMessage = "Có lỗi xảy ra khi kiểm tra mã khuyến mãi"
                });
            }
        }

        // AJAX: Check code uniqueness
        [HttpPost]
        public async Task<IActionResult> CheckCodeUnique(string code, int? excludeId = null)
        {
            try
            {
                var isUnique = await _khuyenMaiService.IsCodeUniqueAsync(code, excludeId);
                return Json(new { isUnique });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while checking code uniqueness: {Code}", code);
                return Json(new { isUnique = false });
            }
        }

        // GET: Active promotions for dropdown
        [HttpGet]
        public async Task<IActionResult> GetActivePromotions()
        {
            try
            {
                var promotions = await _khuyenMaiService.GetActivePromotionsAsync();
                var result = promotions.Select(p => new
                {
                    id = p.KhuyenMaiId,
                    code = p.MaCode,
                    description = p.MoTa,
                    discount = p.PhanTramGiam,
                    endDate = p.NgayKetThuc.ToString("dd/MM/yyyy")
                });

                return Json(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting active promotions");
                return Json(new List<object>());
            }
        }
    }
}
