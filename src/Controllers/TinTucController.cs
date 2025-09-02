using GymManagement.Web.Models.DTOs;
using GymManagement.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace GymManagement.Web.Controllers
{
    [Authorize]
    public class TinTucController : BaseController
    {
        private readonly ITinTucService _tinTucService;
        private readonly new ILogger<TinTucController> _logger;

        public TinTucController(
            ITinTucService tinTucService,
            IUserSessionService userSessionService,
            ILogger<TinTucController> logger) : base(userSessionService, logger)
        {
            _tinTucService = tinTucService;
            _logger = logger;
        }

        // GET: TinTuc
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index(string searchTerm = "", string trangThai = "", int page = 1, int pageSize = 10)
        {
            try
            {
                var allTinTuc = await _tinTucService.GetAllAsync();

                // Apply filters
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    allTinTuc = allTinTuc.Where(t => 
                        t.TieuDe.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                        t.MoTaNgan.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));
                }

                if (!string.IsNullOrWhiteSpace(trangThai))
                {
                    allTinTuc = allTinTuc.Where(t => t.TrangThai == trangThai);
                }

                // Order by newest first
                allTinTuc = allTinTuc.OrderByDescending(t => t.NgayTao);

                // Pagination
                var totalCount = allTinTuc.Count();
                var tinTucs = allTinTuc
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                ViewBag.SearchTerm = searchTerm;
                ViewBag.TrangThai = trangThai;
                ViewBag.CurrentPage = page;
                ViewBag.PageSize = pageSize;
                ViewBag.TotalCount = totalCount;
                ViewBag.TotalPages = (int)Math.Ceiling((double)totalCount / pageSize);

                // Status options for filter dropdown
                ViewBag.TrangThaiOptions = new List<SelectListItem>
                {
                    new SelectListItem { Text = "Tất cả trạng thái", Value = "" },
                    new SelectListItem { Text = "📝 Nháp", Value = "DRAFT" },
                    new SelectListItem { Text = "✅ Đã xuất bản", Value = "PUBLISHED" },
                    // new SelectListItem { Text = "📁 Lưu trữ", Value = "ARCHIVED" }
                };

                return View(tinTucs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading tin tuc list");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải danh sách tin tức.";
                return View(new List<TinTucListDto>());
            }
        }

        // GET: TinTuc/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var tinTuc = await _tinTucService.GetByIdAsync(id);
            if (tinTuc == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy tin tức.";
                return RedirectToAction("Index", "Home");
            }

            // Determine layout based on user role
            if (User.Identity?.IsAuthenticated == true)
            {
                if (User.IsInRole("Admin"))
                {
                    ViewData["Layout"] = "_Layout";
                }
                else if (User.IsInRole("Trainer"))
                {
                    ViewData["Layout"] = "_TrainerLayout";
                }
                else
                {
                    ViewData["Layout"] = "_MemberLayout";
                }
            }
            else
            {
                ViewData["Layout"] = "_MemberLayout"; // Default for anonymous users
            }

            return View(tinTuc);
        }

        // GET: TinTuc/Create
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            PrepareViewBags();
            return View();
        }

        // POST: TinTuc/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(CreateTinTucDto dto)
        {
            if (!ModelState.IsValid)
            {
                PrepareViewBags();
                return View(dto);
            }

            try
            {
                var currentUser = await GetCurrentUserSafeAsync();
                if (currentUser == null)
                if (!currentUser.NguoiDungId.HasValue)
                {
                    return HandleUserNotFound();
                }

                await _tinTucService.CreateAsync(dto, currentUser.NguoiDungId.Value, currentUser.HoTen ?? "");
                TempData["SuccessMessage"] = "Đã tạo tin tức thành công!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating tin tuc");
                ModelState.AddModelError("", "Có lỗi xảy ra khi tạo tin tức.");
                PrepareViewBags();
                return View(dto);
            }
        }

        // GET: TinTuc/Edit/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var tinTuc = await _tinTucService.GetByIdAsync(id);
            if (tinTuc == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy tin tức.";
                return RedirectToAction(nameof(Index));
            }

            var dto = new EditTinTucDto
            {
                TinTucId = tinTuc.TinTucId,
                TieuDe = tinTuc.TieuDe,
                MoTaNgan = tinTuc.MoTaNgan,
                NoiDung = tinTuc.NoiDung,
                CurrentAnhDaiDien = tinTuc.AnhDaiDien,
                NgayXuatBan = tinTuc.NgayXuatBan,
                TrangThai = tinTuc.TrangThai,
                NoiBat = tinTuc.NoiBat,
                MetaTitle = tinTuc.MetaTitle,
                MetaDescription = tinTuc.MetaDescription,
                MetaKeywords = tinTuc.MetaKeywords
            };

            PrepareViewBags();
            return View(dto);
        }

        // POST: TinTuc/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, EditTinTucDto dto)
        {
            if (id != dto.TinTucId)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                PrepareViewBags();
                return View(dto);
            }

            try
            {
                var currentUser = await GetCurrentUserSafeAsync();
                if (currentUser == null)
                {
                    return HandleUserNotFound();
                }

                if (!currentUser.NguoiDungId.HasValue)
                {
                    return HandleUserNotFound();
                }

                await _tinTucService.UpdateAsync(id, dto, currentUser.NguoiDungId.Value);
                TempData["SuccessMessage"] = "Đã cập nhật tin tức thành công!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating tin tuc with id {Id}", id);
                ModelState.AddModelError("", "Có lỗi xảy ra khi cập nhật tin tức.");
                PrepareViewBags();
                return View(dto);
            }
        }

        // GET: TinTuc/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var tinTuc = await _tinTucService.GetByIdAsync(id);
            if (tinTuc == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy tin tức.";
                return RedirectToAction(nameof(Index));
            }

            return View(tinTuc);
        }

        // POST: TinTuc/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var result = await _tinTucService.DeleteAsync(id);
                if (result)
                {
                    TempData["SuccessMessage"] = "Đã xóa tin tức thành công!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Không thể xóa tin tức.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting tin tuc with id {Id}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi xóa tin tức.";
            }

            return RedirectToAction(nameof(Index));
        }

        private void PrepareViewBags()
        {
            // Status options
            ViewBag.TrangThaiOptions = new List<SelectListItem>
            {
                new SelectListItem { Text = "📝 Nháp", Value = "DRAFT" },
                new SelectListItem { Text = "✅ Xuất bản", Value = "PUBLISHED" },
                // new SelectListItem { Text = "📁 Lưu trữ", Value = "ARCHIVED" }
            };
        }
    }
}
