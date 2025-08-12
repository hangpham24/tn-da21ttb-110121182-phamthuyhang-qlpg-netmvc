using GymManagement.Web.Data.Models;
using GymManagement.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Text.Json;

namespace GymManagement.Web.Controllers
{
    [Authorize]
    public class LopHocController : BaseController
    {
        private readonly ILopHocService _lopHocService;
        private readonly INguoiDungService _nguoiDungService;
        private readonly IEmailService _emailService;

        public LopHocController(
            ILopHocService lopHocService,
            INguoiDungService nguoiDungService,
            IEmailService emailService,
            IUserSessionService userSessionService,
            ILogger<LopHocController> logger) : base(userSessionService, logger)
        {
            _lopHocService = lopHocService;
            _nguoiDungService = nguoiDungService;
            _emailService = emailService;
        }

        /// <summary>
        /// Hiển thị danh sách lớp học cho Admin
        /// </summary>
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index(int page = 1, int pageSize = 10)
        {
            try
            {
                _logger.LogInformation("Admin accessing class list page {Page}", page);
                
                // Validate pagination parameters
                if (page < 1) page = 1;
                if (pageSize < 5 || pageSize > 50) pageSize = 10;

                var allLopHocs = await _lopHocService.GetAllAsync();
                
                // Calculate pagination
                var totalItems = allLopHocs.Count();
                var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);
                
                // Ensure page is within valid range
                if (page > totalPages && totalPages > 0) page = totalPages;
                
                var skip = (page - 1) * pageSize;
                var lopHocs = allLopHocs.Skip(skip).Take(pageSize).ToList();

                // Pass pagination info to view
                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = totalPages;
                ViewBag.PageSize = pageSize;
                ViewBag.TotalItems = totalItems;
                ViewBag.HasPreviousPage = page > 1;
                ViewBag.HasNextPage = page < totalPages;

                _logger.LogInformation("Returning {Count} classes for page {Page}", lopHocs.Count, page);
                return View(lopHocs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting classes for page {Page}", page);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải danh sách lớp học. Vui lòng thử lại sau.";
                return View(new List<LopHoc>());
            }
        }

        /// <summary>
        /// Xem chi tiết lớp học
        /// </summary>
        [Authorize(Roles = "Admin,Trainer")]
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                _logger.LogInformation("User {UserId} accessing class details for ID: {ClassId}", 
                    User.Identity?.Name, id);

                var lopHoc = await _lopHocService.GetByIdAsync(id);
                if (lopHoc == null)
                {
                    _logger.LogWarning("Class not found with ID: {ClassId}", id);
                    TempData["ErrorMessage"] = "Không tìm thấy lớp học.";
                    return RedirectToAction(nameof(Index));
                }

                // Check authorization for trainers
                if (User.IsInRole("Trainer") && !User.IsInRole("Admin"))
                {
                    var currentUser = await GetCurrentUserSafeAsync();
                    if (currentUser?.NguoiDungId != lopHoc.HlvId)
                    {
                        _logger.LogWarning("Trainer {TrainerId} attempted to access class {ClassId} without permission", 
                            currentUser?.NguoiDungId, id);
                        return Forbid();
                    }
                }

                return View(lopHoc);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting class details for ID: {ClassId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải thông tin lớp học.";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// Hiển thị form tạo lớp học mới
        /// </summary>
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create()
        {
            try
            {
                await LoadTrainersSelectList();
                
                // Create empty model - no default values
                var model = new LopHoc();
                
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading create form");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải form tạo lớp học.";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// Xử lý tạo lớp học mới
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(LopHoc lopHoc)
        {
            try
            {
                _logger.LogInformation("Creating new class: {ClassName}", lopHoc.TenLop);

                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Invalid model state for class creation");
                    await LoadTrainersSelectList();
                    return View(lopHoc);
                }

                // Set default values for required fields if not provided
                if (string.IsNullOrEmpty(lopHoc.TrangThai))
                {
                    lopHoc.TrangThai = "OPEN";
                }
                
                // Set default capacity if not provided
                if (lopHoc.SucChua <= 0)
                {
                    lopHoc.SucChua = 20;
                }

                var createdClass = await _lopHocService.CreateAsync(lopHoc);
                
                _logger.LogInformation("Successfully created class with ID: {ClassId}", createdClass.LopHocId);
                TempData["SuccessMessage"] = $"Tạo lớp học '{lopHoc.TenLop}' thành công!";
                return RedirectToAction(nameof(Index));
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("⚠️ CẢNH BÁO"))
            {
                // Handle schedule conflict warnings
                _logger.LogWarning(ex, "Schedule conflict warning when creating class");
                
                // Add warning to ViewBag for special handling in view
                ViewBag.WarningMessage = ex.Message;
                ViewBag.ShowWarningConfirmation = true;
                
                await LoadTrainersSelectList();
                return View(lopHoc);
            }
            catch (ArgumentException ex)
            {
                // Handle validation errors
                _logger.LogWarning(ex, "Validation error when creating class: {Error}", ex.Message);
                ModelState.AddModelError("", ex.Message);
                await LoadTrainersSelectList();
                return View(lopHoc);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred while creating class");
                ModelState.AddModelError("", "Có lỗi không mong muốn xảy ra. Vui lòng thử lại sau.");
                await LoadTrainersSelectList();
                return View(lopHoc);
            }
        }

        /// <summary>
        /// Hiển thị form chỉnh sửa lớp học
        /// </summary>
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                _logger.LogInformation("Loading edit form for class ID: {ClassId}", id);

                var lopHoc = await _lopHocService.GetByIdAsync(id);
                if (lopHoc == null)
                {
                    _logger.LogWarning("Class not found for edit with ID: {ClassId}", id);
                    TempData["ErrorMessage"] = "Không tìm thấy lớp học cần chỉnh sửa.";
                    return RedirectToAction(nameof(Index));
                }

                await LoadTrainersSelectList(lopHoc.HlvId);
                return View(lopHoc);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while loading edit form for class ID: {ClassId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải thông tin lớp học.";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// Xử lý cập nhật lớp học
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, LopHoc lopHoc)
        {
            if (id != lopHoc.LopHocId)
            {
                _logger.LogWarning("ID mismatch in edit: URL ID {UrlId} vs Model ID {ModelId}", id, lopHoc.LopHocId);
                return BadRequest();
            }

            try
            {
                _logger.LogInformation("Updating class ID: {ClassId}", id);

                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Invalid model state for class update ID: {ClassId}", id);
                    await LoadTrainersSelectList(lopHoc.HlvId);
                    return View(lopHoc);
                }

                await _lopHocService.UpdateAsync(lopHoc);
                
                // Send schedule change notifications
                await SendScheduleChangeNotificationAsync(lopHoc);
                
                _logger.LogInformation("Successfully updated class ID: {ClassId}", id);
                TempData["SuccessMessage"] = $"Cập nhật lớp học '{lopHoc.TenLop}' thành công!";
                return RedirectToAction(nameof(Index));
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("⚠️ CẢNH BÁO"))
            {
                // Handle schedule conflict warnings
                _logger.LogWarning(ex, "Schedule conflict warning when updating class ID: {ClassId}", id);
                
                ViewBag.WarningMessage = ex.Message;
                ViewBag.ShowWarningConfirmation = true;
                
                await LoadTrainersSelectList(lopHoc.HlvId);
                return View(lopHoc);
            }
            catch (ArgumentException ex)
            {
                // Handle validation errors
                _logger.LogWarning(ex, "Validation error when updating class ID: {ClassId} - {Error}", id, ex.Message);
                ModelState.AddModelError("", ex.Message);
                await LoadTrainersSelectList(lopHoc.HlvId);
                return View(lopHoc);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred while updating class ID: {ClassId}", id);
                ModelState.AddModelError("", "Có lỗi không mong muốn xảy ra. Vui lòng thử lại sau.");
                await LoadTrainersSelectList(lopHoc.HlvId);
                return View(lopHoc);
            }
        }

        /// <summary>
        /// Hiển thị trang xác nhận xóa
        /// </summary>
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                _logger.LogInformation("Loading delete confirmation for class ID: {ClassId}", id);

                var lopHoc = await _lopHocService.GetByIdAsync(id);
                if (lopHoc == null)
                {
                    _logger.LogWarning("Class not found for delete with ID: {ClassId}", id);
                    TempData["ErrorMessage"] = "Không tìm thấy lớp học cần xóa.";
                    return RedirectToAction(nameof(Index));
                }

                // Check if class can be deleted
                var canDelete = await _lopHocService.CanDeleteClassAsync(id);
                ViewBag.CanDelete = canDelete.CanDelete;
                ViewBag.DeleteMessage = canDelete.Message;

                return View(lopHoc);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while loading delete confirmation for class ID: {ClassId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải thông tin lớp học.";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// Xử lý xóa lớp học (AJAX)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                _logger.LogInformation("Attempting to delete class ID: {ClassId}", id);

                // Check if class exists
                var lopHoc = await _lopHocService.GetByIdAsync(id);
                if (lopHoc == null)
                {
                    _logger.LogWarning("Class not found for deletion with ID: {ClassId}", id);
                    return Json(new { success = false, message = "Không tìm thấy lớp học cần xóa." });
                }

                // Check if class can be deleted
                var canDelete = await _lopHocService.CanDeleteClassAsync(id);
                if (!canDelete.CanDelete)
                {
                    _logger.LogWarning("Cannot delete class ID: {ClassId} - {Reason}", id, canDelete.Message);
                    return Json(new { success = false, message = canDelete.Message });
                }

                var result = await _lopHocService.DeleteAsync(id);
                if (result)
                {
                    _logger.LogInformation("Successfully deleted class ID: {ClassId}", id);
                    return Json(new { success = true, message = $"Đã xóa lớp học '{lopHoc.TenLop}' thành công!" });
                }
                else
                {
                    _logger.LogError("Failed to delete class ID: {ClassId}", id);
                    return Json(new { success = false, message = "Không thể xóa lớp học. Vui lòng thử lại sau." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting class ID: {ClassId}", id);
                return Json(new { success = false, message = "Có lỗi xảy ra khi xóa lớp học. Vui lòng thử lại sau." });
            }
        }

        /// <summary>
        /// API: Lấy danh sách lớp học đang hoạt động
        /// </summary>
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetActiveClasses()
        {
            try
            {
                var classes = await _lopHocService.GetActiveClassesAsync();
                
                var result = classes.Select(c => new {
                    id = c.LopHocId,
                    text = $"{c.TenLop} - {c.GioBatDau:HH:mm}-{c.GioKetThuc:HH:mm}",
                    trainer = c.Hlv != null ? $"{c.Hlv.Ho} {c.Hlv.Ten}" : "Chưa phân công",
                    capacity = c.SucChua,
                    registered = c.DangKys?.Count(d => d.TrangThai == "ACTIVE") ?? 0,
                    available = c.SucChua - (c.DangKys?.Count(d => d.TrangThai == "ACTIVE") ?? 0),
                    price = c.GiaTuyChinh,
                    schedule = c.ThuTrongTuan,
                    status = c.TrangThai
                });

                return Json(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting active classes");
                return Json(new { error = "Có lỗi xảy ra khi tải danh sách lớp học." });
            }
        }

        /// <summary>
        /// API: Kiểm tra khả năng đăng ký lớp học
        /// </summary>
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> CheckAvailability(int classId, DateTime? date = null)
        {
            try
            {
                // Input validation
                if (classId <= 0)
                {
                    return BadRequest(new { error = "ID lớp học không hợp lệ" });
                }

                var checkDate = date ?? DateTime.Today;
                if (checkDate < DateTime.Today)
                {
                    return BadRequest(new { error = "Không thể kiểm tra cho ngày trong quá khứ" });
                }

                var isAvailable = await _lopHocService.IsClassAvailableAsync(classId, checkDate);
                var availableSlots = await _lopHocService.GetAvailableSlotsAsync(classId, checkDate);
                var lopHoc = await _lopHocService.GetByIdAsync(classId);

                return Json(new {
                    available = isAvailable,
                    slots = availableSlots,
                    totalCapacity = lopHoc?.SucChua ?? 0,
                    className = lopHoc?.TenLop,
                    status = lopHoc?.TrangThai
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking availability for class {ClassId} on {Date}", classId, date);
                return Json(new { error = "Có lỗi xảy ra khi kiểm tra khả năng đăng ký." });
            }
        }

        /// <summary>
        /// API: Tạo lịch học tự động
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GenerateSchedule(int classId, DateTime startDate, DateTime endDate)
        {
            try
            {
                _logger.LogInformation("Generating schedule for class {ClassId} from {StartDate} to {EndDate}", 
                    classId, startDate, endDate);

                // Validate dates
                if (startDate >= endDate)
                {
                    return Json(new { success = false, message = "Ngày bắt đầu phải trước ngày kết thúc." });
                }

                if (startDate < DateTime.Today)
                {
                    return Json(new { success = false, message = "Không thể tạo lịch cho ngày trong quá khứ." });
                }

                if ((endDate - startDate).TotalDays > 365)
                {
                    return Json(new { success = false, message = "Khoảng thời gian tạo lịch không được vượt quá 1 năm." });
                }

                await _lopHocService.GenerateScheduleAsync(classId, startDate, endDate);
                
                _logger.LogInformation("Successfully generated schedule for class {ClassId}", classId);
                return Json(new { success = true, message = "Tạo lịch học thành công!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating schedule for class {ClassId}", classId);
                return Json(new { success = false, message = "Có lỗi xảy ra khi tạo lịch học. Vui lòng thử lại sau." });
            }
        }

        /// <summary>
        /// API: Lấy lịch học của lớp
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin,Trainer")]
        public async Task<IActionResult> GetSchedule(int classId, DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                // Default date range if not provided
                var start = startDate ?? DateTime.Today;
                var end = endDate ?? DateTime.Today.AddMonths(1);

                // Validate dates
                if (start > end)
                {
                    return BadRequest(new { error = "Ngày bắt đầu không thể sau ngày kết thúc" });
                }

                // Check authorization for trainers
                if (User.IsInRole("Trainer") && !User.IsInRole("Admin"))
                {
                    var currentUser = await GetCurrentUserSafeAsync();
                    if (currentUser?.NguoiDungId != null)
                    {
                        var lopHoc = await _lopHocService.GetByIdAsync(classId);
                        if (lopHoc?.HlvId != currentUser.NguoiDungId)
                        {
                            _logger.LogWarning("Trainer {TrainerId} attempted to access schedule for class {ClassId}", 
                                currentUser.NguoiDungId, classId);
                            return Forbid();
                        }
                    }
                }

                var schedule = await _lopHocService.GetClassScheduleAsync(classId, start, end);
                
                var result = schedule.Select(s => new {
                    id = s.LichLopId,
                    date = s.Ngay.ToString("yyyy-MM-dd"),
                    dayOfWeek = s.Ngay.DayOfWeek.ToString(),
                    startTime = s.GioBatDau.ToString("HH:mm"),
                    endTime = s.GioKetThuc.ToString("HH:mm"),
                    status = s.TrangThai,
                    className = s.LopHoc?.TenLop
                });

                return Json(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting schedule for class {ClassId}", classId);
                return Json(new { error = "Có lỗi xảy ra khi tải lịch học." });
            }
        }

        /// <summary>
        /// Hiển thị danh sách lớp học công khai (không cần đăng nhập)
        /// </summary>
        [AllowAnonymous]
        public async Task<IActionResult> PublicClasses()
        {
            try
            {
                var classes = await _lopHocService.GetActiveClassesAsync();
                return View(classes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting public classes");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải danh sách lớp học.";
                return View(new List<LopHoc>());
            }
        }

        /// <summary>
        /// API: Kiểm tra sức chứa của nhiều lớp
        /// </summary>
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CheckCapacities([FromBody] CheckCapacitiesRequest request)
        {
            try
            {
                if (request?.ClassIds == null || !request.ClassIds.Any())
                {
                    return BadRequest(new { error = "Danh sách ID lớp học không hợp lệ" });
                }

                var capacityData = new List<object>();

                foreach (var classId in request.ClassIds.Distinct())
                {
                    var lopHoc = await _lopHocService.GetByIdAsync(classId);
                    if (lopHoc != null)
                    {
                        var registeredCount = lopHoc.DangKys?.Count(d => d.TrangThai == "ACTIVE") ?? 0;
                        var availableSlots = Math.Max(0, lopHoc.SucChua - registeredCount);

                        capacityData.Add(new
                        {
                            classId = classId,
                            className = lopHoc.TenLop,
                            capacity = lopHoc.SucChua,
                            registeredCount = registeredCount,
                            availableSlots = availableSlots,
                            isFull = availableSlots == 0,
                            status = lopHoc.TrangThai,
                            percentFull = lopHoc.SucChua > 0 ? (registeredCount * 100.0 / lopHoc.SucChua) : 0
                        });
                    }
                }

                return Json(new { success = true, data = capacityData });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking capacities for classes: {ClassIds}", 
                    string.Join(",", request?.ClassIds ?? new List<int>()));
                return StatusCode(500, new { error = "Có lỗi xảy ra khi kiểm tra sức chứa lớp học." });
            }
        }

        /// <summary>
        /// Load danh sách huấn luyện viên cho dropdown
        /// </summary>
        private async Task LoadTrainersSelectList(int? selectedTrainerId = null)
        {
            try
            {
                var trainers = await _nguoiDungService.GetTrainersAsync();
                var trainerList = trainers
                    .OrderBy(t => t.Ho)
                    .ThenBy(t => t.Ten)
                    .Select(t => new {
                        NguoiDungId = t.NguoiDungId,
                        FullName = $"{t.Ho} {t.Ten}".Trim(),
                        IsActive = t.TrangThai == "ACTIVE"
                    })
                    .Where(t => t.IsActive || t.NguoiDungId == selectedTrainerId) // Include selected even if inactive
                    .ToList();

                ViewBag.Trainers = new SelectList(trainerList, "NguoiDungId", "FullName", selectedTrainerId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading trainers for select list");
                ViewBag.Trainers = new SelectList(new List<object>(), "NguoiDungId", "FullName");
            }
        }

        #region Email Helper Methods

        private async Task SendScheduleChangeNotificationAsync(LopHoc lopHoc)
        {
            try
            {
                // Get enrolled members for this class
                var enrolledMembers = await GetEnrolledMembersAsync(lopHoc.LopHocId);
                
                // Get trainer info
                var trainer = lopHoc.HlvId != null ? await _nguoiDungService.GetByIdAsync(lopHoc.HlvId.Value) : null;
                var trainerName = trainer != null ? $"{trainer.Ho} {trainer.Ten}" : "Đang cập nhật";

                var classInfo = $"Lớp: {lopHoc.TenLop}\nHuấn luyện viên: {trainerName}";
                var changeDetails = $"Lịch học của lớp '{lopHoc.TenLop}' đã được cập nhật.\n" +
                                  $"Vui lòng kiểm tra lại thời gian và địa điểm mới trên hệ thống.";

                // Send notifications to all enrolled members
                foreach (var member in enrolledMembers)
                {
                    if (!string.IsNullOrEmpty(member.Email))
                    {
                        await _emailService.SendScheduleChangeNotificationAsync(
                            member.Email,
                            $"{member.Ho} {member.Ten}",
                            changeDetails,
                            classInfo
                        );
                    }
                }

                // Send notification to trainer
                if (trainer != null && !string.IsNullOrEmpty(trainer.Email))
                {
                    await _emailService.SendInstructorScheduleChangeAsync(
                        trainer.Email,
                        trainerName,
                        $"Lịch dạy lớp '{lopHoc.TenLop}' đã được cập nhật.",
                        DateTime.Now
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending schedule change notifications for class {ClassId}", lopHoc.LopHocId);
            }
        }

        private async Task<List<NguoiDung>> GetEnrolledMembersAsync(int classId)
        {
            try
            {
                // This would typically come from a registration service
                // For now, return empty list - you might want to implement this based on your DangKy entity
                return new List<NguoiDung>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting enrolled members for class {ClassId}", classId);
                return new List<NguoiDung>();
            }
        }

        #endregion
    }

    /// <summary>
    /// Request model for checking multiple class capacities
    /// </summary>
    public class CheckCapacitiesRequest
    {
        public List<int> ClassIds { get; set; } = new List<int>();
    }
}
