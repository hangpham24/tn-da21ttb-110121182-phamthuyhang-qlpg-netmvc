using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using GymManagement.Web.Services;

namespace GymManagement.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class WalkInController : Controller
    {
        private readonly IWalkInService _walkInService;
        private readonly ILogger<WalkInController> _logger;

        public WalkInController(IWalkInService walkInService, ILogger<WalkInController> logger)
        {
            _walkInService = walkInService;
            _logger = logger;
        }

        /// <summary>
        /// Dashboard tổng quan khách vãng lai
        /// </summary>
        public IActionResult Dashboard()
        {
            ViewData["Title"] = "Quản lý khách vãng lai";
            return View();
        }

        /// <summary>
        /// Trang quản lý gói vé
        /// </summary>
        public IActionResult Packages()
        {
            ViewData["Title"] = "Quản lý gói vé khách vãng lai";
            return View();
        }

        /// <summary>
        /// Trang lịch sử giao dịch
        /// </summary>
        public IActionResult History()
        {
            ViewData["Title"] = "Lịch sử khách vãng lai";
            return View();
        }

        /// <summary>
        /// API: Lấy thống kê dashboard
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetDashboardStats()
        {
            try
            {
                var today = DateTime.Today;
                var startOfWeek = today.AddDays(-(int)today.DayOfWeek);
                var startOfMonth = new DateTime(today.Year, today.Month, 1);

                // Thống kê hôm nay
                var todayStats = await _walkInService.GetWalkInStatsAsync(today, today.AddDays(1));
                
                // Thống kê tuần này
                var weekStats = await _walkInService.GetWalkInStatsAsync(startOfWeek, today.AddDays(1));
                
                // Thống kê tháng này
                var monthStats = await _walkInService.GetWalkInStatsAsync(startOfMonth, today.AddDays(1));

                // Khách đang tập hiện tại
                var activeSessions = await _walkInService.GetTodayWalkInsAsync();
                var currentlyActive = activeSessions.Where(s => s.IsActive).ToList();

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        today = new
                        {
                            sessions = todayStats.TotalSessions,
                            revenue = todayStats.TotalRevenue,
                            uniqueGuests = todayStats.UniqueGuests,
                            averageValue = todayStats.AverageSessionValue
                        },
                        week = new
                        {
                            sessions = weekStats.TotalSessions,
                            revenue = weekStats.TotalRevenue,
                            uniqueGuests = weekStats.UniqueGuests
                        },
                        month = new
                        {
                            sessions = monthStats.TotalSessions,
                            revenue = monthStats.TotalRevenue,
                            uniqueGuests = monthStats.UniqueGuests
                        },
                        currentlyActive = currentlyActive.Count,
                        paymentMethodBreakdown = todayStats.PaymentMethodBreakdown,
                        activeSessions = currentlyActive.Select(s => new
                        {
                            guestName = s.GuestName,
                            phoneNumber = s.PhoneNumber,
                            packageName = s.PackageName,
                            checkInTime = s.CheckInTime.ToString("HH:mm"),
                            duration = s.Duration?.ToString(@"hh\:mm") ?? "00:00"
                        })
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting walk-in dashboard stats");
                return Json(new { success = false, message = "Có lỗi xảy ra khi tải thống kê." });
            }
        }

        /// <summary>
        /// API: Lấy xu hướng theo ngày (7 ngày gần nhất)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetWeeklyTrend()
        {
            try
            {
                var endDate = DateTime.Today.AddDays(1);
                var startDate = DateTime.Today.AddDays(-6);
                
                var dailyStats = new List<object>();
                
                for (var date = startDate; date < endDate; date = date.AddDays(1))
                {
                    var dayStats = await _walkInService.GetWalkInStatsAsync(date, date.AddDays(1));
                    dailyStats.Add(new
                    {
                        date = date.ToString("yyyy-MM-dd"),
                        dayName = date.ToString("dddd", new System.Globalization.CultureInfo("vi-VN")),
                        sessions = dayStats.TotalSessions,
                        revenue = dayStats.TotalRevenue,
                        uniqueGuests = dayStats.UniqueGuests
                    });
                }

                return Json(new { success = true, data = dailyStats });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting weekly trend");
                return Json(new { success = false, message = "Có lỗi xảy ra khi tải xu hướng tuần." });
            }
        }

        /// <summary>
        /// API: Lấy danh sách gói vé có sẵn
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAvailablePackages()
        {
            try
            {
                var packages = await _walkInService.GetAvailablePackagesAsync();
                
                var result = packages.Select(p => new
                {
                    id = p.GoiTapId,
                    name = p.TenGoi,
                    price = p.Gia,
                    description = p.MoTa,
                    formattedPrice = p.Gia.ToString("N0") + " VNĐ",
                    isActive = true // Gói cố định luôn active
                }).ToList();

                return Json(new { success = true, packages = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available packages");
                return Json(new { success = false, message = "Có lỗi xảy ra khi tải danh sách gói vé." });
            }
        }

        /// <summary>
        /// API: Lấy lịch sử giao dịch với phân trang
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetTransactionHistory(DateTime? startDate = null, DateTime? endDate = null, int page = 1, int pageSize = 20)
        {
            try
            {
                var start = startDate ?? DateTime.Today.AddDays(-30);
                var end = endDate ?? DateTime.Today.AddDays(1);
                
                var sessions = await _walkInService.GetTodayWalkInsAsync(start);
                
                // Filter by date range
                var filteredSessions = sessions.Where(s => s.CheckInTime >= start && s.CheckInTime < end)
                                             .OrderByDescending(s => s.CheckInTime)
                                             .ToList();

                // Pagination
                var totalCount = filteredSessions.Count;
                var pagedSessions = filteredSessions.Skip((page - 1) * pageSize)
                                                   .Take(pageSize)
                                                   .ToList();

                var result = pagedSessions.Select(s => new
                {
                    guestName = s.GuestName,
                    phoneNumber = s.PhoneNumber,
                    packageName = s.PackageName,
                    checkInTime = s.CheckInTime.ToString("dd/MM/yyyy HH:mm"),
                    checkOutTime = s.CheckOutTime?.ToString("dd/MM/yyyy HH:mm"),
                    duration = s.Duration?.ToString(@"hh\:mm"),
                    status = s.Status
                }).ToList();

                return Json(new
                {
                    success = true,
                    data = result,
                    pagination = new
                    {
                        currentPage = page,
                        pageSize = pageSize,
                        totalCount = totalCount,
                        totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting transaction history");
                return Json(new { success = false, message = "Có lỗi xảy ra khi tải lịch sử giao dịch." });
            }
        }
    }
}
