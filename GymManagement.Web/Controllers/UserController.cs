using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using GymManagement.Web.Services;
using GymManagement.Web.Models.DTOs;
using GymManagement.Web.Data.Models;

namespace GymManagement.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UserController : BaseController
    {
        private readonly INguoiDungService _nguoiDungService;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IAuthService _authService;

        public UserController(
            INguoiDungService nguoiDungService, 
            ILogger<UserController> logger, 
            IWebHostEnvironment webHostEnvironment,
            IUserSessionService userSessionService,
            IAuthService authService)
            : base(userSessionService, logger)
        {
            _nguoiDungService = nguoiDungService;
            _webHostEnvironment = webHostEnvironment;
            _authService = authService;
        }

        /// <summary>
        /// Hiển thị danh sách người dùng với pagination và filter
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index(
            string? loaiNguoiDung = null,
            string? searchTerm = null,
            int page = 1,
            int pageSize = 5)
        {
            try
            {
                _logger.LogInformation("=== UserController.Index START ===");
                _logger.LogInformation("Parameters - Page: {Page}, Filter: {Filter}, SearchTerm: {SearchTerm}",
                    page, loaiNguoiDung, searchTerm);

                // Log current user info
                var currentUser = _userSessionService.GetUserName();
                var userRoles = _userSessionService.GetUserRoles();
                _logger.LogInformation("Current user: {User}, Roles: {Roles}", currentUser, string.Join(",", userRoles ?? new List<string>()));

                // Check user authorization
                if (!IsInRoleSafe("Admin"))
                {
                    _logger.LogWarning("AUTHORIZATION FAILED - Non-admin user attempted to access user management");
                    _logger.LogWarning("User: {User}, Roles: {Roles}", currentUser, string.Join(",", userRoles ?? new List<string>()));
                    return HandleUnauthorized();
                }

                _logger.LogInformation("Authorization passed - proceeding with user list");

                // Validate pagination parameters
                if (page < 1) page = 1;
                if (pageSize < 5 || pageSize > 50) pageSize = 5;

                // Get all users based on filter
                var allUsers = await _nguoiDungService.GetAllAsync();
                
                // Apply filters
                if (!string.IsNullOrEmpty(loaiNguoiDung))
                {
                    allUsers = allUsers.Where(u => u.LoaiNguoiDung == loaiNguoiDung);
                }
                
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    searchTerm = searchTerm.ToLower();
                    allUsers = allUsers.Where(u => 
                        u.HoTen.ToLower().Contains(searchTerm) ||
                        (u.Email?.ToLower().Contains(searchTerm) ?? false) ||
                        (u.SoDienThoai?.Contains(searchTerm) ?? false));
                }
                
                // Calculate pagination
                var totalItems = allUsers.Count();
                var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);
                
                // Ensure page is within valid range
                if (page > totalPages && totalPages > 0) page = totalPages;
                
                var skip = (page - 1) * pageSize;
                var users = allUsers.Skip(skip).Take(pageSize).ToList();

                // Set ViewBag data
                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = totalPages;
                ViewBag.PageSize = pageSize;
                ViewBag.TotalItems = totalItems;
                ViewBag.HasPreviousPage = page > 1;
                ViewBag.HasNextPage = page < totalPages;
                ViewBag.LoaiNguoiDung = loaiNguoiDung;
                ViewBag.SearchTerm = searchTerm;

                _logger.LogInformation("SUCCESS - Returning {Count} users for page {Page}", users.Count, page);
                _logger.LogInformation("=== UserController.Index END ===");
                return View(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting users for page {Page}", page);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải danh sách người dùng. Vui lòng thử lại sau.";
                return View(new List<NguoiDungDto>());
            }
        }

        /// <summary>
        /// Shortcut routes for user types
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Members(int page = 1, int pageSize = 10)
        {
            return await Index("THANHVIEN", null, page, pageSize);
        }

        [HttpGet]
        public async Task<IActionResult> Trainers(int page = 1, int pageSize = 10)
        {
            return await Index("HLV", null, page, pageSize);
        }

        [HttpGet]
        public async Task<IActionResult> Staff(int page = 1, int pageSize = 10)
        {
            return await Index("ADMIN", null, page, pageSize);
        }

        [HttpGet]
        public async Task<IActionResult> Guests(int page = 1, int pageSize = 10)
        {
            return await Index("VANGLAI", null, page, pageSize);
        }

        /// <summary>
        /// Hiển thị chi tiết người dùng
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                _logger.LogInformation("Admin viewing user details for ID: {Id}", id);
                
                var nguoiDung = await _nguoiDungService.GetByIdAsync(id);
                if (nguoiDung == null)
                {
                    _logger.LogWarning("User not found with ID: {Id}", id);
                    TempData["ErrorMessage"] = "Không tìm thấy người dùng.";
                    return RedirectToAction(nameof(Index));
                }

                // Get related data
                ViewBag.CanDelete = await _nguoiDungService.CanDeleteUserAsync(id);
                
                return View(nguoiDung);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting user details for ID: {Id}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải thông tin người dùng.";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// Hiển thị form tạo người dùng mới
        /// </summary>
        [HttpGet]
        public IActionResult Create()
        {
            _logger.LogInformation("Admin accessing create user form");
            return View(new CreateNguoiDungDto());
        }

        /// <summary>
        /// Xử lý tạo người dùng mới
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateNguoiDungDto createDto, IFormFile? avatarFile)
        {
            try
            {
                _logger.LogInformation("Admin creating new user with type: {Type}", createDto.LoaiNguoiDung);
                
                if (!ModelState.IsValid)
                {
                    return View(createDto);
                }

                // Create NguoiDung first
                var nguoiDung = await _nguoiDungService.CreateAsync(createDto);
                
                // Process avatar after user creation (need user ID)
                if (avatarFile != null && avatarFile.Length > 0)
                {
                    var avatarPath = await ProcessAvatarUpload(avatarFile, nguoiDung.NguoiDungId);
                    if (!string.IsNullOrEmpty(avatarPath))
                    {
                        await _nguoiDungService.UpdateAvatarAsync(nguoiDung.NguoiDungId, avatarPath);
                    }
                }

                // Create TaiKhoan if username and password are provided
                if (!string.IsNullOrEmpty(createDto.Username) && !string.IsNullOrEmpty(createDto.Password))
                {
                    var taiKhoan = new TaiKhoan
                    {
                        TenDangNhap = createDto.Username,
                        Email = createDto.Email ?? "",
                        NguoiDungId = nguoiDung.NguoiDungId,
                        KichHoat = createDto.TrangThai, // Use status from form
                        EmailXacNhan = true
                    };

                    var accountCreated = await _authService.CreateUserAsync(taiKhoan, createDto.Password);
                    if (accountCreated)
                    {
                        // Assign role based on user type
                                            string roleName = createDto.LoaiNguoiDung switch
                    {
                        "ADMIN" => "Admin",
                        "HLV" => "Trainer", 
                        "THANHVIEN" => "Member",
                        "VANGLAI" => "Member",
                        _ => "Member"
                    };
                        
                        await _authService.AssignRoleAsync(taiKhoan.Id, roleName);
                        _logger.LogInformation("Successfully created account for user {Username} with role {Role}", createDto.Username, roleName);
                    }
                    else
                    {
                        _logger.LogWarning("Failed to create account for user {Username}, but NguoiDung was created", createDto.Username);
                    }
                }
                
                _logger.LogInformation("Successfully created user with ID: {Id}", nguoiDung.NguoiDungId);
                TempData["SuccessMessage"] = "Tạo người dùng thành công!";
                return RedirectToAction(nameof(Details), new { id = nguoiDung.NguoiDungId });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Business rule violation while creating user");
                ModelState.AddModelError("", ex.Message);
                return View(createDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating user");
                ModelState.AddModelError("", "Có lỗi xảy ra khi tạo người dùng. Vui lòng thử lại.");
                return View(createDto);
            }
        }

        /// <summary>
        /// Tạo tài khoản đăng nhập cho người dùng đã tồn tại
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAccount(int nguoiDungId, string username, string password)
        {
            try
            {
                _logger.LogInformation("Admin creating account for existing user ID: {Id}", nguoiDungId);

                // Get existing NguoiDung
                var nguoiDung = await _nguoiDungService.GetByIdAsync(nguoiDungId);
                if (nguoiDung == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy người dùng." });
                }

                // Check if account already exists
                var existingAccount = await _authService.GetUserByUsernameAsync(username);
                if (existingAccount != null)
                {
                    return Json(new { success = false, message = "Tên đăng nhập đã tồn tại." });
                }

                // Create TaiKhoan
                var taiKhoan = new TaiKhoan
                {
                    TenDangNhap = username,
                    Email = nguoiDung.Email ?? "",
                    NguoiDungId = nguoiDung.NguoiDungId,
                    KichHoat = nguoiDung.TrangThai == "ACTIVE",
                    EmailXacNhan = true
                };

                var accountCreated = await _authService.CreateUserAsync(taiKhoan, password);
                if (accountCreated)
                {
                    // Assign role based on user type
                    string roleName = nguoiDung.LoaiNguoiDung switch
                    {
                        "ADMIN" => "Admin",
                        "HLV" => "Trainer", 
                        "THANHVIEN" => "Member",
                        "VANGLAI" => "Member",
                        _ => "Member"
                    };
                    
                    await _authService.AssignRoleAsync(taiKhoan.Id, roleName);
                    _logger.LogInformation("Successfully created account for user {Username} with role {Role}", username, roleName);
                    
                    return Json(new { success = true, message = "Tạo tài khoản thành công! Người dùng có thể đăng nhập với tên đăng nhập: " + username });
                }
                else
                {
                    return Json(new { success = false, message = "Không thể tạo tài khoản. Vui lòng thử lại." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating account for user ID: {Id}", nguoiDungId);
                return Json(new { success = false, message = "Có lỗi xảy ra khi tạo tài khoản." });
            }
        }

        /// <summary>
        /// Hiển thị form chỉnh sửa người dùng
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                _logger.LogInformation("Admin accessing edit form for user ID: {Id}", id);
                
                var nguoiDung = await _nguoiDungService.GetByIdAsync(id);
                if (nguoiDung == null)
                {
                    _logger.LogWarning("User not found for edit with ID: {Id}", id);
                    return NotFound();
                }

                var updateDto = new UpdateNguoiDungDto
                {
                    NguoiDungId = nguoiDung.NguoiDungId,
                    LoaiNguoiDung = nguoiDung.LoaiNguoiDung,
                    Ho = nguoiDung.Ho,
                    Ten = nguoiDung.Ten,
                    GioiTinh = nguoiDung.GioiTinh,
                    NgaySinh = nguoiDung.NgaySinh,
                    SoDienThoai = nguoiDung.SoDienThoai,
                    Email = nguoiDung.Email,
                    TrangThai = nguoiDung.TrangThai,
                    NgayThamGia = nguoiDung.NgayThamGia,
                    NgayTao = nguoiDung.NgayTao
                };

                ViewBag.CurrentAvatar = nguoiDung.AnhDaiDien;
                return View(updateDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while loading edit user page for ID: {Id}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải trang chỉnh sửa người dùng.";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// Xử lý cập nhật thông tin người dùng
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, UpdateNguoiDungDto updateDto, IFormFile? avatarFile)
        {
            if (id != updateDto.NguoiDungId)
            {
                return BadRequest();
            }

            try
            {
                _logger.LogInformation("Admin updating user ID: {Id}", id);
                
                if (!ModelState.IsValid)
                {
                    var currentUser = await _nguoiDungService.GetByIdAsync(id);
                    ViewBag.CurrentAvatar = currentUser?.AnhDaiDien;
                    return View(updateDto);
                }

                // Process avatar if provided
                if (avatarFile != null && avatarFile.Length > 0)
                {
                    var avatarPath = await ProcessAvatarUpload(avatarFile, updateDto.NguoiDungId);
                    if (!string.IsNullOrEmpty(avatarPath))
                    {
                        // Update avatar separately first
                        await _nguoiDungService.UpdateAvatarAsync(id, avatarPath);
                    }
                    else
                    {
                        ModelState.AddModelError("", "Có lỗi xảy ra khi tải lên ảnh đại diện.");
                        var user = await _nguoiDungService.GetByIdAsync(id);
                        ViewBag.CurrentAvatar = user?.AnhDaiDien;
                        return View(updateDto);
                    }
                }

                var nguoiDung = await _nguoiDungService.UpdateAsync(updateDto);
                
                _logger.LogInformation("Successfully updated user ID: {Id}", id);
                TempData["SuccessMessage"] = "Cập nhật người dùng thành công!";
                return RedirectToAction(nameof(Details), new { id = nguoiDung.NguoiDungId });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Business rule violation while updating user ID: {Id}", id);
                ModelState.AddModelError("", ex.Message);
                var currentUser = await _nguoiDungService.GetByIdAsync(id);
                ViewBag.CurrentAvatar = currentUser?.AnhDaiDien;
                return View(updateDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating user ID: {Id}", id);
                ModelState.AddModelError("", "Có lỗi xảy ra khi cập nhật người dùng. Vui lòng thử lại.");
                var currentUser = await _nguoiDungService.GetByIdAsync(id);
                ViewBag.CurrentAvatar = currentUser?.AnhDaiDien;
                return View(updateDto);
            }
        }

        /// <summary>
        /// Xóa người dùng (AJAX)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                _logger.LogInformation("Admin attempting to delete user ID: {Id}", id);
                
                // Check if can delete
                var (canDelete, message) = await _nguoiDungService.CanDeleteUserAsync(id);
                if (!canDelete)
                {
                    _logger.LogWarning("Cannot delete user ID: {Id}. Reason: {Reason}", id, message);
                    return Json(new { success = false, message });
                }

                var result = await _nguoiDungService.DeleteAsync(id);
                if (result)
                {
                    _logger.LogInformation("Successfully deleted user ID: {Id}", id);
                    return Json(new { success = true, message = "Xóa người dùng thành công!" });
                }
                else
                {
                    return Json(new { success = false, message = "Không thể xóa người dùng." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting user ID: {Id}", id);
                return Json(new { success = false, message = "Có lỗi xảy ra khi xóa người dùng." });
            }
        }

        /// <summary>
        /// API: Lấy thống kê người dùng
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetUserStats()
        {
            try
            {
                var allUsers = await _nguoiDungService.GetAllAsync();
                
                var stats = new
                {
                    total = allUsers.Count(),
                    byType = allUsers.GroupBy(u => u.LoaiNguoiDung)
                        .Select(g => new { type = g.Key, count = g.Count() }),
                    byStatus = allUsers.GroupBy(u => u.TrangThai)
                        .Select(g => new { status = g.Key, count = g.Count() }),
                    newThisMonth = allUsers.Count(u => 
                        u.NgayThamGia >= DateOnly.FromDateTime(DateTime.Today.AddDays(-30)))
                };

                return Json(new { success = true, data = stats });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user statistics");
                return Json(new { success = false, message = "Không thể tải thống kê người dùng." });
            }
        }

        /// <summary>
        /// API: Kiểm tra email/username tồn tại
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CheckEmailExists(string email, int? excludeUserId = null)
        {
            try
            {
                var exists = await _nguoiDungService.IsEmailExistsAsync(email, excludeUserId);
                return Json(new { exists });
            }
            catch
            {
                return Json(new { exists = false });
            }
        }

        #region Private Methods

        private async Task<string?> ProcessAvatarUpload(IFormFile avatarFile, int userId)
        {
            try
            {
                // Validate file type
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                var fileExtension = Path.GetExtension(avatarFile.FileName).ToLowerInvariant();

                if (!allowedExtensions.Contains(fileExtension))
                {
                    _logger.LogWarning("Invalid file extension for avatar upload: {Extension}", fileExtension);
                    return null;
                }

                // Validate file size (max 5MB)
                if (avatarFile.Length > 5 * 1024 * 1024)
                {
                    _logger.LogWarning("Avatar file too large: {Size} bytes", avatarFile.Length);
                    return null;
                }

                // Create upload directory
                var uploadsPath = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "avatars");
                if (!Directory.Exists(uploadsPath))
                {
                    Directory.CreateDirectory(uploadsPath);
                    _logger.LogInformation("Created uploads directory: {Path}", uploadsPath);
                }

                // Generate unique filename
                var fileName = $"avatar_{userId}_{DateTime.Now:yyyyMMddHHmmss}{fileExtension}";
                var filePath = Path.Combine(uploadsPath, fileName);

                // Delete old avatar if exists
                if (userId > 0)
                {
                    await DeleteOldAvatar(userId);
                }

                // Save new file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await avatarFile.CopyToAsync(stream);
                }

                _logger.LogInformation("Successfully uploaded avatar for user {UserId}: {FileName} at {Path}", 
                    userId, fileName, filePath);
                
                // Return relative path for database storage
                var relativePath = $"/uploads/avatars/{fileName}";
                _logger.LogInformation("Avatar URL path: {RelativePath}", relativePath);
                
                return relativePath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while processing avatar upload for user {UserId}", userId);
                return null;
            }
        }

        private async Task DeleteOldAvatar(int userId)
        {
            try
            {
                var user = await _nguoiDungService.GetByIdAsync(userId);
                if (user?.AnhDaiDien != null && user.AnhDaiDien.StartsWith("/uploads/avatars/"))
                {
                    var oldFilePath = Path.Combine(_webHostEnvironment.WebRootPath, 
                        user.AnhDaiDien.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                    
                    if (System.IO.File.Exists(oldFilePath))
                    {
                        System.IO.File.Delete(oldFilePath);
                        _logger.LogInformation("Deleted old avatar for user {UserId}: {Path}", 
                            userId, oldFilePath);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting old avatar for user {UserId}", userId);
                // Don't throw - this is not critical
            }
        }

        #endregion
    }
}
