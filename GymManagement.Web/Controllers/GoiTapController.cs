using GymManagement.Web.Data.Models;
using GymManagement.Web.Models.DTOs;
using GymManagement.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymManagement.Web.Controllers
{
    [Authorize]
    public class GoiTapController : Controller
    {
        private readonly IGoiTapService _goiTapService;
        private readonly ILogger<GoiTapController> _logger;

        public GoiTapController(IGoiTapService goiTapService, ILogger<GoiTapController> logger)
        {
            _goiTapService = goiTapService;
            _logger = logger;
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index()
        {
            try
            {
                var goiTaps = await _goiTapService.GetAllAsync();
                return View(goiTaps);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting packages");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải danh sách gói tập.";
                return View(new List<GoiTapDto>());
            }
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var goiTap = await _goiTapService.GetByIdAsync(id);
                if (goiTap == null)
                {
                    return NotFound();
                }
                return View(goiTap);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting package details for ID: {Id}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải thông tin gói tập.";
                return RedirectToAction(nameof(Index));
            }
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            _logger.LogInformation("GoiTap Create GET called");
            return View();
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(CreateGoiTapDto createDto)
        {
            _logger.LogInformation("GoiTap Create POST called with data: {@CreateDto}", createDto);

            try
            {
                if (ModelState.IsValid)
                {
                    _logger.LogInformation("ModelState is valid, creating package");
                    await _goiTapService.CreateAsync(createDto);
                    TempData["SuccessMessage"] = "Tạo gói tập thành công!";
                    return RedirectToAction(nameof(Index));
                }

                _logger.LogWarning("ModelState is invalid: {@ModelState}", ModelState);
                return View(createDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating package");
                ModelState.AddModelError("", "Có lỗi xảy ra khi tạo gói tập.");
                return View(createDto);
            }
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var goiTap = await _goiTapService.GetByIdAsync(id);
                if (goiTap == null)
                {
                    return NotFound();
                }

                // Convert GoiTapDto to UpdateGoiTapDto
                var updateDto = new UpdateGoiTapDto
                {
                    GoiTapId = goiTap.GoiTapId,
                    TenGoi = goiTap.TenGoi,
                    ThoiHanThang = goiTap.ThoiHanThang,
                    SoBuoiToiDa = goiTap.SoBuoiToiDa,
                    Gia = goiTap.Gia,
                    MoTa = goiTap.MoTa
                };

                return View(updateDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting package for edit, ID: {Id}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải thông tin gói tập.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Edit(int id, UpdateGoiTapDto updateDto)
        {
            if (id != updateDto.GoiTapId)
            {
                return NotFound();
            }

            try
            {
                if (ModelState.IsValid)
                {
                    await _goiTapService.UpdateAsync(updateDto);
                    TempData["SuccessMessage"] = "Cập nhật gói tập thành công!";
                    return RedirectToAction(nameof(Index));
                }
                return View(updateDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating package ID: {Id}", id);
                ModelState.AddModelError("", "Có lỗi xảy ra khi cập nhật gói tập.");
                return View(updateDto);
            }
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var goiTap = await _goiTapService.GetByIdAsync(id);
                if (goiTap == null)
                {
                    return NotFound();
                }
                return View(goiTap);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting package for delete, ID: {Id}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải thông tin gói tập.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        // [ValidateAntiForgeryToken] // Temporarily disabled for testing
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var result = await _goiTapService.DeleteAsync(id);
                if (result)
                {
                    return Json(new { success = true, message = "Xóa gói tập thành công!" });
                }
                else
                {
                    return Json(new { success = false, message = "Không thể xóa gói tập." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting package ID: {Id}", id);
                return Json(new { success = false, message = "Có lỗi xảy ra khi xóa gói tập." });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetPackages()
        {
            try
            {
                var packages = await _goiTapService.GetAllAsync();
                return Json(packages.Select(p => new { 
                    id = p.GoiTapId, 
                    text = $"{p.TenGoi} - {p.Gia:N0} VNĐ",
                    price = p.Gia,
                    duration = p.ThoiHanThang
                }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting packages for API");
                return Json(new List<object>());
            }
        }

        [HttpGet]
        public async Task<IActionResult> CalculatePrice(int packageId, int months)
        {
            try
            {
                var package = await _goiTapService.GetByIdAsync(packageId);
                if (package == null)
                {
                    return Json(new { success = false, message = "Gói tập không tồn tại." });
                }

                var totalPrice = package.Gia * months;
                return Json(new { 
                    success = true, 
                    price = totalPrice,
                    formattedPrice = totalPrice.ToString("N0") + " VNĐ"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while calculating price");
                return Json(new { success = false, message = "Có lỗi xảy ra khi tính giá." });
            }
        }

        public async Task<IActionResult> PublicPackages()
        {
            try
            {
                var packages = await _goiTapService.GetAllAsync();
                return View(packages);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting public packages");
                return View(new List<GoiTap>());
            }
        }
    }
}
