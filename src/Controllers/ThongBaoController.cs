using GymManagement.Web.Data.Models;
using GymManagement.Web.Models.DTOs;
using GymManagement.Web.Services;
using GymManagement.Web.Data.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace GymManagement.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ThongBaoController : Controller
    {
        private readonly IThongBaoService _thongBaoService;
        private readonly INguoiDungRepository _nguoiDungRepository;
        private readonly ILogger<ThongBaoController> _logger;

        public ThongBaoController(
            IThongBaoService thongBaoService,
            INguoiDungRepository nguoiDungRepository,
            ILogger<ThongBaoController> logger)
        {
            _thongBaoService = thongBaoService;
            _nguoiDungRepository = nguoiDungRepository;
            _logger = logger;
        }

        // GET: ThongBao
        public async Task<IActionResult> Index(string searchTerm = "", string kenh = "", int page = 1, int pageSize = 10)
        {
            try
            {
                var allNotifications = await _thongBaoService.GetAllAsync();

                // Apply filters
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    allNotifications = allNotifications.Where(t => 
                        (t.TieuDe?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ?? false) ||
                        (t.NoiDung?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ?? false));
                }

                if (!string.IsNullOrWhiteSpace(kenh))
                {
                    allNotifications = allNotifications.Where(t => t.Kenh == kenh);
                }

                // Order by newest first
                allNotifications = allNotifications.OrderByDescending(t => t.NgayTao);

                // Pagination
                var totalCount = allNotifications.Count();
                var notifications = allNotifications
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                ViewBag.SearchTerm = searchTerm;
                ViewBag.Kenh = kenh;
                ViewBag.CurrentPage = page;
                ViewBag.PageSize = pageSize;
                ViewBag.TotalCount = totalCount;
                ViewBag.TotalPages = (int)Math.Ceiling((double)totalCount / pageSize);

                // Channel options for filter dropdown
                ViewBag.KenhOptions = new List<SelectListItem>
                {
                    new SelectListItem { Text = "Tất cả kênh", Value = "" },
                    new SelectListItem { Text = "📧 Email", Value = "EMAIL" },
                    // new SelectListItem { Text = "📱 SMS", Value = "SMS" },
                    new SelectListItem { Text = "📱 App", Value = "APP" }
                };

                return View(notifications);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading notifications");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải danh sách thông báo.";
                return View(new List<ThongBao>());
            }
        }

        // GET: ThongBao/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var notification = await _thongBaoService.GetByIdAsync(id);
            if (notification == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy thông báo.";
                return RedirectToAction(nameof(Index));
            }

            return View(notification);
        }

        // GET: ThongBao/Create
        public async Task<IActionResult> Create()
        {
            await PrepareViewBags();
            return View();
        }

        // POST: ThongBao/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateThongBaoDto dto)
        {
            if (!ModelState.IsValid)
            {
                await PrepareViewBags();
                return View(dto);
            }

            try
            {
                if (dto.SendToAll)
                {
                    // Send to all members
                    await _thongBaoService.SendNotificationToAllMembersAsync(dto.TieuDe, dto.NoiDung, dto.Kenh);
                    TempData["SuccessMessage"] = "Đã gửi thông báo đến tất cả thành viên thành công!";
                }
                else if (dto.NguoiDungIds != null && dto.NguoiDungIds.Any())
                {
                    // Send to selected users
                    await _thongBaoService.SendBulkNotificationAsync(dto.NguoiDungIds, dto.TieuDe, dto.NoiDung, dto.Kenh);
                    TempData["SuccessMessage"] = $"Đã gửi thông báo đến {dto.NguoiDungIds.Count()} người dùng thành công!";
                }
                else if (dto.NguoiDungId.HasValue)
                {
                    // Send to single user
                    await _thongBaoService.CreateNotificationAsync(dto.NguoiDungId.Value, dto.TieuDe, dto.NoiDung, dto.Kenh);
                    TempData["SuccessMessage"] = "Đã tạo thông báo thành công!";
                }
                else
                {
                    ModelState.AddModelError("", "Vui lòng chọn người dùng hoặc chọn gửi đến tất cả thành viên.");
                    await PrepareViewBags();
                    return View(dto);
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating notification");
                ModelState.AddModelError("", "Có lỗi xảy ra khi tạo thông báo.");
                await PrepareViewBags();
                return View(dto);
            }
        }

        // GET: ThongBao/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var notification = await _thongBaoService.GetByIdAsync(id);
            if (notification == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy thông báo.";
                return RedirectToAction(nameof(Index));
            }

            var dto = new EditThongBaoDto
            {
                ThongBaoId = notification.ThongBaoId,
                TieuDe = notification.TieuDe,
                NoiDung = notification.NoiDung,
                Kenh = notification.Kenh,
                NguoiDungId = notification.NguoiDungId,
                DaDoc = notification.DaDoc
            };

            await PrepareViewBags();
            return View(dto);
        }

        // POST: ThongBao/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, EditThongBaoDto dto)
        {
            if (id != dto.ThongBaoId)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                await PrepareViewBags();
                return View(dto);
            }

            try
            {
                var notification = await _thongBaoService.GetByIdAsync(id);
                if (notification == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy thông báo.";
                    return RedirectToAction(nameof(Index));
                }

                // Update properties
                notification.TieuDe = dto.TieuDe;
                notification.NoiDung = dto.NoiDung;
                notification.Kenh = dto.Kenh;
                notification.DaDoc = dto.DaDoc;

                await _thongBaoService.UpdateAsync(notification);
                TempData["SuccessMessage"] = "Đã cập nhật thông báo thành công!";

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating notification with id {Id}", id);
                ModelState.AddModelError("", "Có lỗi xảy ra khi cập nhật thông báo.");
                await PrepareViewBags();
                return View(dto);
            }
        }

        // GET: ThongBao/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var notification = await _thongBaoService.GetByIdAsync(id);
            if (notification == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy thông báo.";
                return RedirectToAction(nameof(Index));
            }

            return View(notification);
        }

        // POST: ThongBao/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var result = await _thongBaoService.DeleteAsync(id);
                if (result)
                {
                    TempData["SuccessMessage"] = "Đã xóa thông báo thành công!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Không thể xóa thông báo.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting notification with id {Id}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi xóa thông báo.";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: ThongBao/DeleteOld
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteOld(int daysOld = 30)
        {
            try
            {
                var result = await _thongBaoService.DeleteOldNotificationsAsync(daysOld);
                if (result)
                {
                    TempData["SuccessMessage"] = $"Đã xóa các thông báo cũ hơn {daysOld} ngày thành công!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Không thể xóa các thông báo cũ.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting old notifications");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi xóa thông báo cũ.";
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: ThongBao/Send - Quick send notification
        public async Task<IActionResult> Send()
        {
            await PrepareViewBags();
            return View();
        }

        // POST: ThongBao/Send
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Send(CreateThongBaoDto dto)
        {
            if (!ModelState.IsValid)
            {
                await PrepareViewBags();
                return View(dto);
            }

            try
            {
                if (dto.SendToAll)
                {
                    await _thongBaoService.SendNotificationToAllMembersAsync(dto.TieuDe, dto.NoiDung, dto.Kenh);
                    TempData["SuccessMessage"] = "Đã gửi thông báo đến tất cả thành viên thành công!";
                }
                else if (dto.NguoiDungIds != null && dto.NguoiDungIds.Any())
                {
                    await _thongBaoService.SendBulkNotificationAsync(dto.NguoiDungIds, dto.TieuDe, dto.NoiDung, dto.Kenh);
                    TempData["SuccessMessage"] = $"Đã gửi thông báo đến {dto.NguoiDungIds.Count()} người dùng thành công!";
                }
                else
                {
                    ModelState.AddModelError("", "Vui lòng chọn người dùng hoặc chọn gửi đến tất cả thành viên.");
                    await PrepareViewBags();
                    return View(dto);
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending notification");
                ModelState.AddModelError("", "Có lỗi xảy ra khi gửi thông báo.");
                await PrepareViewBags();
                return View(dto);
            }
        }

        private async Task PrepareViewBags()
        {
            // Get all active users for user selection
            var users = await _nguoiDungRepository.GetAllAsync();
            var activeUsers = users.Where(u => u.TrangThai == "ACTIVE").ToList();

            ViewBag.NguoiDungs = activeUsers.Select(u => new SelectListItem
            {
                Value = u.NguoiDungId.ToString(),
                Text = $"{u.Ho} {u.Ten} ({u.LoaiNguoiDung})"
            }).ToList();

            // Channel options
            ViewBag.KenhOptions = new List<SelectListItem>
            {
                new SelectListItem { Text = "📧 Email", Value = "EMAIL" },
                new SelectListItem { Text = "📱 SMS", Value = "SMS" },
                new SelectListItem { Text = "📱 App", Value = "APP" }
            };

            // User type options for filtering
            ViewBag.LoaiNguoiDungOptions = new List<SelectListItem>
            {
                new SelectListItem { Text = "Tất cả", Value = "" },
                new SelectListItem { Text = "👤 Thành viên", Value = "THANHVIEN" },
                new SelectListItem { Text = "💪 Huấn luyện viên", Value = "HLV" },
                new SelectListItem { Text = "👔 Admin", Value = "ADMIN" },
                new SelectListItem { Text = "🚶 Vãng lai", Value = "VANGLAI" }
            };
        }
    }
}
