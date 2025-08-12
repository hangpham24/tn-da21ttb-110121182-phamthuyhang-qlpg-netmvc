using GymManagement.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GymManagement.Web.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ThongBaoController : ControllerBase
    {
        private readonly IThongBaoService _thongBaoService;
        private readonly ILogger<ThongBaoController> _logger;

        public ThongBaoController(IThongBaoService thongBaoService, ILogger<ThongBaoController> logger)
        {
            _thongBaoService = thongBaoService;
            _logger = logger;
        }

        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst("NguoiDungId")?.Value;
            return int.TryParse(userIdClaim, out var userId) ? userId : null;
        }

        /// <summary>
        /// GET /api/thongbao/unread - Lấy thông báo chưa đọc
        /// </summary>
        [HttpGet("unread")]
        public async Task<IActionResult> GetUnreadNotifications()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (!userId.HasValue)
                {
                    return Unauthorized(new { message = "Không tìm thấy thông tin người dùng." });
                }

                var notifications = await _thongBaoService.GetUnreadByUserIdAsync(userId.Value);
                
                return Ok(new 
                { 
                    success = true,
                    count = notifications.Count(),
                    notifications = notifications.Select(n => new
                    {
                        id = n.ThongBaoId,
                        tieuDe = n.TieuDe,
                        noiDung = n.NoiDung,
                        thoiGianTao = n.NgayTao,
                        kenh = n.Kenh,
                        icon = GetNotificationIcon(n.Kenh, n.TieuDe)
                    })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting unread notifications");
                return StatusCode(500, new { success = false, message = "Có lỗi xảy ra khi tải thông báo." });
            }
        }

        /// <summary>
        /// GET /api/thongbao/all - Lấy tất cả thông báo
        /// </summary>
        [HttpGet("all")]
        public async Task<IActionResult> GetAllNotifications([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (!userId.HasValue)
                {
                    return Unauthorized(new { message = "Không tìm thấy thông tin người dùng." });
                }

                var allNotifications = await _thongBaoService.GetByUserIdAsync(userId.Value);
                var pagedNotifications = allNotifications
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(n => new
                    {
                        id = n.ThongBaoId,
                        tieuDe = n.TieuDe,
                        noiDung = n.NoiDung,
                        thoiGianTao = n.NgayTao,
                        daDoc = n.DaDoc,
                        kenh = n.Kenh,
                        icon = GetNotificationIcon(n.Kenh, n.TieuDe)
                    });

                return Ok(new 
                { 
                    success = true,
                    currentPage = page,
                    pageSize = pageSize,
                    totalCount = allNotifications.Count(),
                    notifications = pagedNotifications
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting all notifications");
                return StatusCode(500, new { success = false, message = "Có lỗi xảy ra khi tải thông báo." });
            }
        }

        /// <summary>
        /// POST /api/thongbao/{id}/mark-as-read - Đánh dấu đã đọc
        /// </summary>
        [HttpPost("{id}/mark-as-read")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (!userId.HasValue)
                {
                    return Unauthorized(new { message = "Không tìm thấy thông tin người dùng." });
                }

                // Verify notification belongs to current user
                var notification = await _thongBaoService.GetByIdAsync(id);
                if (notification == null || notification.NguoiDungId != userId.Value)
                {
                    return NotFound(new { success = false, message = "Không tìm thấy thông báo." });
                }

                var result = await _thongBaoService.MarkAsReadAsync(id);
                
                if (result)
                {
                    return Ok(new { success = true, message = "Đã đánh dấu thông báo là đã đọc." });
                }
                else
                {
                    return BadRequest(new { success = false, message = "Không thể đánh dấu thông báo." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while marking notification as read");
                return StatusCode(500, new { success = false, message = "Có lỗi xảy ra." });
            }
        }

        /// <summary>
        /// POST /api/thongbao/mark-all-as-read - Đánh dấu tất cả đã đọc
        /// </summary>
        [HttpPost("mark-all-as-read")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (!userId.HasValue)
                {
                    return Unauthorized(new { message = "Không tìm thấy thông tin người dùng." });
                }

                var result = await _thongBaoService.MarkAllAsReadAsync(userId.Value);
                
                if (result)
                {
                    return Ok(new { success = true, message = "Đã đánh dấu tất cả thông báo là đã đọc." });
                }
                else
                {
                    return BadRequest(new { success = false, message = "Không thể đánh dấu thông báo." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while marking all notifications as read");
                return StatusCode(500, new { success = false, message = "Có lỗi xảy ra." });
            }
        }

        /// <summary>
        /// GET /api/thongbao/count - Đếm số thông báo chưa đọc
        /// </summary>
        [HttpGet("count")]
        public async Task<IActionResult> GetUnreadCount()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (!userId.HasValue)
                {
                    return Unauthorized(new { message = "Không tìm thấy thông tin người dùng." });
                }

                var count = await _thongBaoService.CountUnreadNotificationsAsync(userId.Value);
                
                return Ok(new { success = true, unreadCount = count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting unread count");
                return StatusCode(500, new { success = false, message = "Có lỗi xảy ra." });
            }
        }

        private string GetNotificationIcon(string kenh, string tieuDe)
        {
            // Determine icon based on channel and title
            return kenh?.ToUpper() switch
            {
                "EMAIL" => "📧",
                "SMS" => "📱",
                "APP" => GetAppNotificationIcon(tieuDe),
                _ => "🔔"
            };
        }

        private string GetAppNotificationIcon(string tieuDe)
        {
            var title = tieuDe?.ToLower() ?? "";
            
            if (title.Contains("thanh toán") || title.Contains("payment"))
                return "💳";
            if (title.Contains("đăng ký") || title.Contains("registration"))
                return "📝";
            if (title.Contains("lớp học") || title.Contains("class"))
                return "🏃";
            if (title.Contains("check-in") || title.Contains("điểm danh"))
                return "✅";
            if (title.Contains("booking") || title.Contains("đặt lịch"))
                return "📅";
            if (title.Contains("khuyến mãi") || title.Contains("promotion"))
                return "🎉";
            if (title.Contains("thông báo hệ thống") || title.Contains("system"))
                return "⚙️";
            
            return "🔔";
        }


    }
}