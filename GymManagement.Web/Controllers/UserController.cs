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
        private readonly IDangKyService _dangKyService;
        private readonly IEmailService _emailService;
        private readonly IThongBaoService _thongBaoService;

        public UserController(
            INguoiDungService nguoiDungService,
            ILogger<UserController> logger,
            IWebHostEnvironment webHostEnvironment,
            IUserSessionService userSessionService,
            IAuthService authService,
            IDangKyService dangKyService,
            IEmailService emailService,
            IThongBaoService thongBaoService)
            : base(userSessionService, logger)
        {
            _nguoiDungService = nguoiDungService;
            _webHostEnvironment = webHostEnvironment;
            _authService = authService;
            _dangKyService = dangKyService;
            _emailService = emailService;
            _thongBaoService = thongBaoService;
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

                // Get all users based on filter (with TaiKhoan information)
                var allUsers = await _nguoiDungService.GetAllWithTaiKhoanAsync();

                // Apply filters
                if (!string.IsNullOrEmpty(loaiNguoiDung))
                {
                    allUsers = allUsers.Where(u => u.LoaiNguoiDung == loaiNguoiDung);
                }

                if (!string.IsNullOrEmpty(searchTerm))
                {
                    searchTerm = searchTerm.ToLower();
                    allUsers = allUsers.Where(u =>
                        (u.Ho != null && u.Ho.ToLower().Contains(searchTerm)) ||
                        (u.Ten != null && u.Ten.ToLower().Contains(searchTerm)) ||
                        (u.Ho != null && u.Ten != null && (u.Ho + " " + u.Ten).ToLower().Contains(searchTerm)) ||
                        (u.Email?.ToLower().Contains(searchTerm) ?? false) ||
                        (u.SoDienThoai?.Contains(searchTerm) ?? false));
                }

                // Calculate pagination
                var totalItems = allUsers.Count();
                var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

                // Ensure page is within valid range
                if (page > totalPages && totalPages > 0) page = totalPages;

                var skip = (page - 1) * pageSize;
                var pagedUsers = allUsers.Skip(skip).Take(pageSize).ToList();

                // Convert to NguoiDungWithSubscriptionDto and get subscription info
                var usersWithSubscription = new List<NguoiDungWithSubscriptionDto>();
                foreach (var user in pagedUsers)
                {
                    var userWithSub = new NguoiDungWithSubscriptionDto
                    {
                        NguoiDungId = user.NguoiDungId,
                        LoaiNguoiDung = user.LoaiNguoiDung,
                        Ho = user.Ho,
                        Ten = user.Ten,
                        GioiTinh = user.GioiTinh,
                        NgaySinh = user.NgaySinh,
                        SoDienThoai = user.SoDienThoai,
                        Email = user.Email,
                        AnhDaiDien = user.AnhDaiDien,
                        TrangThai = user.TrangThai,
                        NgayThamGia = user.NgayThamGia,
                        NgayTao = user.NgayTao,
                        // Add account information
                        Username = user.Username,
                        HasAccount = user.HasAccount
                    };

                    // Get active registrations for this user
                    if (user.LoaiNguoiDung == "THANHVIEN")
                    {
                        var activeRegistrations = await _dangKyService.GetActiveRegistrationsByMemberIdAsync(user.NguoiDungId);

                        // Find active package registration (based on GoiTapId instead of LoaiDangKy)
                        var packageRegistration = activeRegistrations.FirstOrDefault(r =>
                            r.GoiTapId.HasValue &&
                            r.TrangThai == "ACTIVE" &&
                            r.NgayKetThuc >= DateOnly.FromDateTime(DateTime.Today));

                        if (packageRegistration != null)
                        {
                            userWithSub.ActivePackageRegistration = packageRegistration;
                            userWithSub.ActivePackage = packageRegistration.GoiTap;
                            userWithSub.PackageExpiryDate = packageRegistration.NgayKetThuc.ToDateTime(TimeOnly.MinValue);
                        }

                        // Count active class registrations (based on LopHocId)
                        var classRegistrations = activeRegistrations.Where(r =>
                            r.LopHocId.HasValue &&
                            r.TrangThai == "ACTIVE" &&
                            r.NgayKetThuc >= DateOnly.FromDateTime(DateTime.Today)).ToList();
                        userWithSub.ActiveClassRegistrations = classRegistrations;
                        userWithSub.ActiveClassCount = classRegistrations.Count;
                    }

                    // Calculate package status
                    userWithSub.CalculatePackageStatus();
                    usersWithSubscription.Add(userWithSub);
                }

                // Check for users with expiring packages (within 7 days) - Check ALL users, not just current page
                var allUsersForExpiryCheck = new List<NguoiDungWithSubscriptionDto>();
                foreach (var user in allUsers.Where(u => u.LoaiNguoiDung == "THANHVIEN"))
                {
                    var userForExpiry = new NguoiDungWithSubscriptionDto
                    {
                        NguoiDungId = user.NguoiDungId,
                        Ho = user.Ho,
                        Ten = user.Ten,
                        Email = user.Email
                    };

                    // Get active package registration for expiry check
                    var activeRegs = await _dangKyService.GetActiveRegistrationsByMemberIdAsync(user.NguoiDungId);
                    var packageReg = activeRegs.FirstOrDefault(r => r.GoiTapId != null);
                    if (packageReg != null)
                    {
                        userForExpiry.ActivePackageRegistration = packageReg;
                        userForExpiry.ActivePackage = packageReg.GoiTap;
                        userForExpiry.PackageExpiryDate = packageReg.NgayKetThuc.ToDateTime(TimeOnly.MinValue);
                    }

                    allUsersForExpiryCheck.Add(userForExpiry);
                }

                var expiringUsers = allUsersForExpiryCheck.Where(u =>
                    u.ActivePackageRegistration != null &&
                    u.PackageExpiryDate.HasValue &&
                    u.PackageExpiryDate.Value >= DateTime.Now &&
                    u.PackageExpiryDate.Value <= DateTime.Now.AddDays(7)
                ).ToList();

                // Set ViewBag data
                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = totalPages;
                ViewBag.PageSize = pageSize;
                ViewBag.TotalItems = totalItems;
                ViewBag.HasPreviousPage = page > 1;
                ViewBag.HasNextPage = page < totalPages;
                ViewBag.LoaiNguoiDung = loaiNguoiDung;
                ViewBag.SearchTerm = searchTerm;
                ViewBag.ExpiringUsersCount = expiringUsers.Count;
                ViewBag.ExpiringUsers = expiringUsers;

                _logger.LogInformation("SUCCESS - Returning {Count} users for page {Page}, {ExpiringCount} users expiring soon",
                    usersWithSubscription.Count, page, expiringUsers.Count);
                _logger.LogInformation("=== UserController.Index END ===");
                return View(usersWithSubscription);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting users for page {Page}", page);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải danh sách người dùng. Vui lòng thử lại sau.";
                return View(new List<NguoiDungWithSubscriptionDto>());
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
        /// Gửi thông báo gia hạn cho tất cả người dùng sắp hết hạn gói tập
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendExpiryNotifications()
        {
            try
            {
                _logger.LogInformation("=== SendExpiryNotifications START ===");

                // Check user authorization
                if (!IsInRoleSafe("Admin"))
                {
                    _logger.LogWarning("AUTHORIZATION FAILED - Non-admin user attempted to send expiry notifications");
                    return Json(new { success = false, message = "Bạn không có quyền thực hiện chức năng này." });
                }

                // Get all users with active packages (with TaiKhoan information)
                var allUsers = await _nguoiDungService.GetAllWithTaiKhoanAsync();
                var usersWithSubscription = new List<NguoiDungWithSubscriptionDto>();

                foreach (var user in allUsers.Where(u => u.LoaiNguoiDung == "THANHVIEN"))
                {
                    var userWithSub = new NguoiDungWithSubscriptionDto
                    {
                        NguoiDungId = user.NguoiDungId,
                        LoaiNguoiDung = user.LoaiNguoiDung,
                        Ho = user.Ho,
                        Ten = user.Ten,
                        GioiTinh = user.GioiTinh,
                        NgaySinh = user.NgaySinh,
                        SoDienThoai = user.SoDienThoai,
                        Email = user.Email,
                        AnhDaiDien = user.AnhDaiDien,
                        TrangThai = user.TrangThai,
                        NgayThamGia = user.NgayThamGia,
                        NgayTao = user.NgayTao,
                        // Add account information
                        Username = user.Username,
                        HasAccount = user.HasAccount
                    };

                    // Get active package registration
                    var activeRegistrations = await _dangKyService.GetActiveRegistrationsByMemberIdAsync(user.NguoiDungId);
                    var packageRegistration = activeRegistrations.FirstOrDefault(r => r.GoiTapId != null);
                    if (packageRegistration != null)
                    {
                        userWithSub.ActivePackageRegistration = packageRegistration;
                        userWithSub.ActivePackage = packageRegistration.GoiTap;
                        userWithSub.PackageExpiryDate = packageRegistration.NgayKetThuc.ToDateTime(TimeOnly.MinValue);
                    }

                    usersWithSubscription.Add(userWithSub);
                }

                // Find users with packages expiring within 7 days
                var expiringUsers = usersWithSubscription.Where(u =>
                    u.ActivePackageRegistration != null &&
                    u.PackageExpiryDate.HasValue &&
                    u.PackageExpiryDate.Value >= DateTime.Now &&
                    u.PackageExpiryDate.Value <= DateTime.Now.AddDays(7) &&
                    !string.IsNullOrEmpty(u.Email)
                ).ToList();

                if (!expiringUsers.Any())
                {
                    _logger.LogInformation("No users with expiring packages found");
                    return Json(new { success = true, message = "Hiện tại không có thành viên nào sắp hết hạn gói tập.", count = 0 });
                }

                _logger.LogInformation("Found {Count} users with expiring packages", expiringUsers.Count);

                // Send notifications
                var successCount = 0;
                var failedEmails = new List<string>();

                foreach (var user in expiringUsers)
                {
                    try
                    {
                        var daysRemaining = (int)(user.PackageExpiryDate!.Value - DateTime.Now).TotalDays;
                        var packageName = user.ActivePackage?.TenGoi ?? "Gói tập";
                        var memberName = $"{user.Ho} {user.Ten}".Trim();

                        // Send email using EmailService
                        await _emailService.SendMembershipExpiryReminderAsync(
                            user.Email!,
                            memberName,
                            packageName,
                            user.PackageExpiryDate.Value,
                            daysRemaining
                        );

                        // Create notification in database
                        await _thongBaoService.CreateNotificationAsync(
                            user.NguoiDungId,
                            $"⏳ Gói tập sắp hết hạn - còn {daysRemaining} ngày",
                            $"Xin chào {memberName},\n\nGói tập \"{packageName}\" của bạn sẽ hết hạn vào ngày {user.PackageExpiryDate.Value:dd/MM/yyyy} (còn {daysRemaining} ngày).\n\nVui lòng liên hệ để gia hạn gói tập và tiếp tục sử dụng dịch vụ.\n\nTrân trọng,\nĐội ngũ Gym Management",
                            "EMAIL"
                        );

                        successCount++;
                        _logger.LogInformation("Sent expiry notification to {Email} ({Name})", user.Email, memberName);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send expiry notification to {Email}", user.Email);
                        failedEmails.Add(user.Email!);
                    }
                }

                var message = $"Đã gửi thông báo gia hạn thành công cho {successCount}/{expiringUsers.Count} thành viên.";
                if (failedEmails.Any())
                {
                    message += $" Không thể gửi cho: {string.Join(", ", failedEmails)}";
                }

                _logger.LogInformation("SendExpiryNotifications completed - Success: {SuccessCount}, Failed: {FailedCount}",
                    successCount, failedEmails.Count);

                return Json(new {
                    success = true,
                    message = message,
                    count = successCount,
                    failed = failedEmails.Count,
                    details = expiringUsers.Select(u => new {
                        name = $"{u.Ho} {u.Ten}".Trim(),
                        email = u.Email,
                        packageName = u.ActivePackage?.TenGoi,
                        expiryDate = u.PackageExpiryDate?.ToString("dd/MM/yyyy"),
                        daysRemaining = u.PackageExpiryDate.HasValue ? (int)(u.PackageExpiryDate.Value - DateTime.Now).TotalDays : 0
                    })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while sending expiry notifications");
                return Json(new { success = false, message = "Có lỗi xảy ra khi gửi thông báo. Vui lòng thử lại sau." });
            }
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

                // Check for duplicate username/email in TaiKhoans table BEFORE creating NguoiDung
                if (!string.IsNullOrEmpty(createDto.Username) && !string.IsNullOrEmpty(createDto.Password))
                {
                    var existingAccount = await _authService.GetUserByUsernameAsync(createDto.Username);
                    if (existingAccount != null)
                    {
                        ModelState.AddModelError("Username", $"Tên đăng nhập '{createDto.Username}' đã tồn tại trong hệ thống.");
                        TempData["ErrorMessage"] = $"Tên đăng nhập '{createDto.Username}' đã tồn tại trong hệ thống.";
                        return View(createDto);
                    }

                    var existingEmailAccount = await _authService.GetUserByEmailAsync(createDto.Email);
                    if (existingEmailAccount != null)
                    {
                        ModelState.AddModelError("Email", $"Email '{createDto.Email}' đã được sử dụng cho tài khoản khác trong hệ thống.");
                        TempData["ErrorMessage"] = $"Email '{createDto.Email}' đã được sử dụng cho tài khoản khác. Vui lòng sử dụng email khác hoặc không tạo tài khoản đăng nhập.";
                        return View(createDto);
                    }
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

                    var accountCreated = await _authService.CreateAccountForExistingUserAsync(taiKhoan, createDto.Password);
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

                        // Show warning message to user
                        TempData["WarningMessage"] = $"Người dùng đã được tạo thành công, nhưng không thể tạo tài khoản đăng nhập. " +
                                                   $"Tên đăng nhập '{createDto.Username}' hoặc email '{createDto.Email}' có thể đã tồn tại trong hệ thống. " +
                                                   $"Bạn có thể tạo tài khoản đăng nhập sau với thông tin khác.";
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

        /// <summary>
        /// Đặt lại mật khẩu cho người dùng (chỉ Admin)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(int userId)
        {
            try
            {
                _logger.LogInformation("=== ResetPassword START for UserId: {UserId} ===", userId);

                // Check user authorization
                if (!IsInRoleSafe("Admin"))
                {
                    _logger.LogWarning("AUTHORIZATION FAILED - Non-admin user attempted to reset password for UserId: {UserId}", userId);
                    return Json(new { success = false, message = "Bạn không có quyền thực hiện chức năng này." });
                }

                // Get user with TaiKhoan information
                var user = await _nguoiDungService.GetByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("User not found with ID: {UserId}", userId);
                    return Json(new { success = false, message = "Không tìm thấy người dùng." });
                }

                // Check if user has account
                if (!user.HasAccount)
                {
                    _logger.LogWarning("User ID: {UserId} does not have an account", userId);
                    return Json(new { success = false, message = "Người dùng chưa có tài khoản. Vui lòng tạo tài khoản trước." });
                }

                // Generate new random password
                var newPassword = GenerateRandomPassword();
                var memberName = $"{user.Ho} {user.Ten}".Trim();

                _logger.LogInformation("Attempting to reset password for UserId: {UserId}, Username: {Username}", userId, user.Username);

                // Reset password using AuthService
                var resetResult = await _authService.ResetPasswordAsync(userId, newPassword);
                if (!resetResult)
                {
                    _logger.LogError("Failed to reset password for UserId: {UserId}", userId);
                    return Json(new { success = false, message = "Không thể đặt lại mật khẩu. Người dùng có thể chưa có tài khoản hoặc có lỗi hệ thống." });
                }

                // Send email notification if user has email
                bool emailSent = false;
                string emailError = null;

                if (!string.IsNullOrEmpty(user.Email))
                {
                    try
                    {
                        await _emailService.SendPasswordResetEmailAsync(
                            user.Email,
                            memberName,
                            "#" // Placeholder for reset link - not needed for admin reset
                        );
                        _logger.LogInformation("Password reset email sent to {Email}", user.Email);
                        emailSent = true;
                    }
                    catch (Exception emailEx)
                    {
                        _logger.LogError(emailEx, "Failed to send password reset email to {Email}", user.Email);
                        emailError = $"Không thể gửi email đến {user.Email}. Lỗi: {emailEx.Message}";
                        emailSent = false;
                    }
                }
                else
                {
                    emailError = "Người dùng không có địa chỉ email để gửi thông báo.";
                }

                // Create notification in system
                await _thongBaoService.CreateNotificationAsync(
                    userId,
                    "🔑 Mật khẩu đã được đặt lại",
                    $"Xin chào {memberName},\n\nMật khẩu của bạn đã được quản trị viên đặt lại.\nMật khẩu mới: {newPassword}\n\nVui lòng đăng nhập và đổi mật khẩu ngay lập tức.\n\nTrân trọng,\nĐội ngũ Gym Management",
                    "SYSTEM"
                );

                _logger.LogInformation("Password reset successful for UserId: {UserId}", userId);

                // Prepare response message
                var successMessage = $"Đã đặt lại mật khẩu thành công cho {memberName}.";
                if (emailSent)
                {
                    successMessage += " Mật khẩu mới đã được gửi qua email.";
                }
                else if (!string.IsNullOrEmpty(emailError))
                {
                    successMessage += $" Tuy nhiên, {emailError}";
                }

                return Json(new {
                    success = true,
                    message = successMessage,
                    newPassword = newPassword,
                    userEmail = user.Email,
                    fullName = memberName,
                    username = user.Username,
                    emailSent = emailSent,
                    emailError = emailError
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while resetting password for UserId: {UserId}", userId);
                return Json(new { success = false, message = "Có lỗi xảy ra khi đặt lại mật khẩu. Vui lòng thử lại sau." });
            }
        }

        /// <summary>
        /// Tạo tài khoản cho người dùng chưa có tài khoản
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAccount(int userId)
        {
            try
            {
                _logger.LogInformation("Admin attempting to create account for user ID: {UserId}", userId);

                // Get user information
                var nguoiDung = await _nguoiDungService.GetByIdAsync(userId);
                if (nguoiDung == null)
                {
                    _logger.LogWarning("User not found with ID: {UserId}", userId);
                    return Json(new { success = false, message = "Không tìm thấy người dùng." });
                }

                // Check if user already has an account
                var existingUser = await _nguoiDungService.GetByIdAsync(userId);
                if (existingUser?.HasAccount == true)
                {
                    _logger.LogWarning("User ID: {UserId} already has an account", userId);
                    return Json(new { success = false, message = "Người dùng đã có tài khoản." });
                }

                // Generate username and password
                var username = GenerateUsername(nguoiDung.Ho, nguoiDung.Ten, nguoiDung.SoDienThoai);
                var password = GenerateRandomPassword();

                // Create TaiKhoan
                var taiKhoan = new TaiKhoan
                {
                    TenDangNhap = username,
                    Email = nguoiDung.Email ?? $"{username}@gym.local",
                    NguoiDungId = nguoiDung.NguoiDungId,
                    KichHoat = nguoiDung.TrangThai == "ACTIVE",
                    EmailXacNhan = true
                };

                var accountCreated = await _authService.CreateAccountForExistingUserAsync(taiKhoan, password);
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

                    return Json(new {
                        success = true,
                        message = $"Tạo tài khoản thành công!\nTên đăng nhập: {username}\nMật khẩu: {password}\n\nVui lòng thông báo cho người dùng thông tin đăng nhập này.",
                        username = username,
                        password = password
                    });
                }
                else
                {
                    return Json(new { success = false, message = "Không thể tạo tài khoản. Tên đăng nhập có thể đã tồn tại." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating account for user ID: {UserId}", userId);
                return Json(new { success = false, message = "Có lỗi xảy ra khi tạo tài khoản. Vui lòng thử lại sau." });
            }
        }

        /// <summary>
        /// Vô hiệu hóa người dùng (xóa tạm thời)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deactivate(int id)
        {
            try
            {
                _logger.LogInformation("Admin attempting to deactivate user ID: {Id}", id);

                var result = await _nguoiDungService.DeactivateUserAsync(id);
                if (result)
                {
                    _logger.LogInformation("Successfully deactivated user ID: {Id}", id);
                    return Json(new { success = true, message = "Vô hiệu hóa người dùng thành công!" });
                }
                else
                {
                    _logger.LogWarning("Failed to deactivate user ID: {Id}", id);
                    return Json(new { success = false, message = "Không thể vô hiệu hóa người dùng. Người dùng không tồn tại." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deactivating user ID: {Id}", id);
                return Json(new { success = false, message = "Có lỗi xảy ra khi vô hiệu hóa người dùng. Vui lòng thử lại sau." });
            }
        }

        /// <summary>
        /// Tạo username từ thông tin người dùng
        /// </summary>
        private static string GenerateUsername(string ho, string? ten, string? soDienThoai)
        {
            // Remove Vietnamese accents and convert to lowercase
            var username = RemoveVietnameseAccents($"{ho}{ten}").ToLower().Replace(" ", "");

            // If username is too short or empty, use phone number
            if (username.Length < 3 && !string.IsNullOrEmpty(soDienThoai))
            {
                username = soDienThoai;
            }

            // Add random numbers if username is still too short
            if (username.Length < 3)
            {
                username += new Random().Next(100, 999);
            }

            return username;
        }

        /// <summary>
        /// Tạo tài khoản với username và password do admin nhập
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAccountWithCredentials(int userId, string username, string password)
        {
            try
            {
                _logger.LogInformation("Admin creating account with custom credentials for user ID: {UserId}", userId);

                // Get user information
                var nguoiDung = await _nguoiDungService.GetByIdAsync(userId);
                if (nguoiDung == null)
                {
                    _logger.LogWarning("User not found with ID: {UserId}", userId);
                    return Json(new { success = false, message = "Không tìm thấy người dùng." });
                }

                // Check if user already has an account
                if (nguoiDung.HasAccount)
                {
                    _logger.LogWarning("User ID: {UserId} already has an account", userId);
                    return Json(new { success = false, message = "Người dùng đã có tài khoản." });
                }

                // Validate input
                if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                {
                    return Json(new { success = false, message = "Tên đăng nhập và mật khẩu không được để trống." });
                }

                if (password.Length < 6)
                {
                    return Json(new { success = false, message = "Mật khẩu phải có ít nhất 6 ký tự." });
                }

                // Create TaiKhoan
                var taiKhoan = new TaiKhoan
                {
                    TenDangNhap = username.Trim(),
                    Email = nguoiDung.Email ?? $"{username.Trim()}@gym.local",
                    NguoiDungId = nguoiDung.NguoiDungId,
                    KichHoat = nguoiDung.TrangThai == "ACTIVE",
                    EmailXacNhan = true
                };

                var accountCreated = await _authService.CreateAccountForExistingUserAsync(taiKhoan, password);
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

                    return Json(new {
                        success = true,
                        message = $"Tạo tài khoản thành công!\nTên đăng nhập: {username}\nNgười dùng có thể đăng nhập ngay bây giờ."
                    });
                }
                else
                {
                    return Json(new { success = false, message = "Không thể tạo tài khoản. Tên đăng nhập hoặc email có thể đã tồn tại." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating account with credentials for user ID: {UserId}", userId);
                return Json(new { success = false, message = "Có lỗi xảy ra khi tạo tài khoản. Vui lòng thử lại sau." });
            }
        }

        /// <summary>
        /// Lấy danh sách người dùng chưa có tài khoản
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetUsersWithoutAccount()
        {
            try
            {
                _logger.LogInformation("Admin requesting users without account");

                var allUsers = await _nguoiDungService.GetAllWithTaiKhoanAsync();
                var usersWithoutAccount = allUsers.Where(u => !u.HasAccount).ToList();

                return Json(new {
                    success = true,
                    users = usersWithoutAccount.Select(u => new {
                        nguoiDungId = u.NguoiDungId,
                        ho = u.Ho,
                        ten = u.Ten,
                        hoTen = u.HoTen,
                        email = u.Email,
                        loaiNguoiDung = u.LoaiNguoiDung
                    })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting users without account");
                return Json(new { success = false, message = "Có lỗi xảy ra khi lấy danh sách người dùng." });
            }
        }

        /// <summary>
        /// Tạo tài khoản hàng loạt cho tất cả người dùng chưa có tài khoản
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkCreateAccount()
        {
            try
            {
                _logger.LogInformation("Admin attempting bulk create account");

                var allUsers = await _nguoiDungService.GetAllWithTaiKhoanAsync();
                var usersWithoutAccount = allUsers.Where(u => !u.HasAccount).ToList();

                if (!usersWithoutAccount.Any())
                {
                    return Json(new { success = false, message = "Không có người dùng nào cần tạo tài khoản." });
                }

                var createdAccounts = new List<object>();
                var failedAccounts = new List<object>();

                foreach (var user in usersWithoutAccount)
                {
                    try
                    {
                        // Generate username and password
                        var username = GenerateUsername(user.Ho, user.Ten, user.SoDienThoai);
                        var password = GenerateRandomPassword();

                        // Create TaiKhoan
                        var taiKhoan = new TaiKhoan
                        {
                            TenDangNhap = username,
                            Email = user.Email ?? $"{username}@gym.local",
                            NguoiDungId = user.NguoiDungId,
                            KichHoat = user.TrangThai == "ACTIVE",
                            EmailXacNhan = true
                        };

                        var accountCreated = await _authService.CreateAccountForExistingUserAsync(taiKhoan, password);
                        if (accountCreated)
                        {
                            // Assign role based on user type
                            string roleName = user.LoaiNguoiDung switch
                            {
                                "ADMIN" => "Admin",
                                "HLV" => "Trainer",
                                "THANHVIEN" => "Member",
                                "VANGLAI" => "Member",
                                _ => "Member"
                            };

                            await _authService.AssignRoleAsync(taiKhoan.Id, roleName);

                            createdAccounts.Add(new {
                                userId = user.NguoiDungId,
                                fullName = user.HoTen,
                                username = username,
                                password = password,
                                role = roleName
                            });

                            _logger.LogInformation("Successfully created account for user {Username} with role {Role}", username, roleName);
                        }
                        else
                        {
                            failedAccounts.Add(new {
                                userId = user.NguoiDungId,
                                fullName = user.HoTen,
                                reason = "Không thể tạo tài khoản"
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error creating account for user ID: {UserId}", user.NguoiDungId);
                        failedAccounts.Add(new {
                            userId = user.NguoiDungId,
                            fullName = user.HoTen,
                            reason = ex.Message
                        });
                    }
                }

                var message = $"Đã tạo thành công {createdAccounts.Count} tài khoản";
                if (failedAccounts.Any())
                {
                    message += $", {failedAccounts.Count} tài khoản tạo thất bại";
                }

                return Json(new {
                    success = true,
                    message = message,
                    accounts = createdAccounts,
                    failed = failedAccounts
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during bulk create account");
                return Json(new { success = false, message = "Có lỗi xảy ra khi tạo tài khoản hàng loạt." });
            }
        }

        /// <summary>
        /// Remove Vietnamese accents from string
        /// </summary>
        private static string RemoveVietnameseAccents(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;

            var accents = new Dictionary<char, char>
            {
                {'à', 'a'}, {'á', 'a'}, {'ạ', 'a'}, {'ả', 'a'}, {'ã', 'a'}, {'â', 'a'}, {'ầ', 'a'}, {'ấ', 'a'}, {'ậ', 'a'}, {'ẩ', 'a'}, {'ẫ', 'a'}, {'ă', 'a'}, {'ằ', 'a'}, {'ắ', 'a'}, {'ặ', 'a'}, {'ẳ', 'a'}, {'ẵ', 'a'},
                {'è', 'e'}, {'é', 'e'}, {'ẹ', 'e'}, {'ẻ', 'e'}, {'ẽ', 'e'}, {'ê', 'e'}, {'ề', 'e'}, {'ế', 'e'}, {'ệ', 'e'}, {'ể', 'e'}, {'ễ', 'e'},
                {'ì', 'i'}, {'í', 'i'}, {'ị', 'i'}, {'ỉ', 'i'}, {'ĩ', 'i'},
                {'ò', 'o'}, {'ó', 'o'}, {'ọ', 'o'}, {'ỏ', 'o'}, {'õ', 'o'}, {'ô', 'o'}, {'ồ', 'o'}, {'ố', 'o'}, {'ộ', 'o'}, {'ổ', 'o'}, {'ỗ', 'o'}, {'ơ', 'o'}, {'ờ', 'o'}, {'ớ', 'o'}, {'ợ', 'o'}, {'ở', 'o'}, {'ỡ', 'o'},
                {'ù', 'u'}, {'ú', 'u'}, {'ụ', 'u'}, {'ủ', 'u'}, {'ũ', 'u'}, {'ư', 'u'}, {'ừ', 'u'}, {'ứ', 'u'}, {'ự', 'u'}, {'ử', 'u'}, {'ữ', 'u'},
                {'ỳ', 'y'}, {'ý', 'y'}, {'ỵ', 'y'}, {'ỷ', 'y'}, {'ỹ', 'y'},
                {'đ', 'd'},
                {'À', 'A'}, {'Á', 'A'}, {'Ạ', 'A'}, {'Ả', 'A'}, {'Ã', 'A'}, {'Â', 'A'}, {'Ầ', 'A'}, {'Ấ', 'A'}, {'Ậ', 'A'}, {'Ẩ', 'A'}, {'Ẫ', 'A'}, {'Ă', 'A'}, {'Ằ', 'A'}, {'Ắ', 'A'}, {'Ặ', 'A'}, {'Ẳ', 'A'}, {'Ẵ', 'A'},
                {'È', 'E'}, {'É', 'E'}, {'Ẹ', 'E'}, {'Ẻ', 'E'}, {'Ẽ', 'E'}, {'Ê', 'E'}, {'Ề', 'E'}, {'Ế', 'E'}, {'Ệ', 'E'}, {'Ể', 'E'}, {'Ễ', 'E'},
                {'Ì', 'I'}, {'Í', 'I'}, {'Ị', 'I'}, {'Ỉ', 'I'}, {'Ĩ', 'I'},
                {'Ò', 'O'}, {'Ó', 'O'}, {'Ọ', 'O'}, {'Ỏ', 'O'}, {'Õ', 'O'}, {'Ô', 'O'}, {'Ồ', 'O'}, {'Ố', 'O'}, {'Ộ', 'O'}, {'Ổ', 'O'}, {'Ỗ', 'O'}, {'Ơ', 'O'}, {'Ờ', 'O'}, {'Ớ', 'O'}, {'Ợ', 'O'}, {'Ở', 'O'}, {'Ỡ', 'O'},
                {'Ù', 'U'}, {'Ú', 'U'}, {'Ụ', 'U'}, {'Ủ', 'U'}, {'Ũ', 'U'}, {'Ư', 'U'}, {'Ừ', 'U'}, {'Ứ', 'U'}, {'Ự', 'U'}, {'Ử', 'U'}, {'Ữ', 'U'},
                {'Ỳ', 'Y'}, {'Ý', 'Y'}, {'Ỵ', 'Y'}, {'Ỷ', 'Y'}, {'Ỹ', 'Y'},
                {'Đ', 'D'}
            };

            var result = new System.Text.StringBuilder();
            foreach (char c in text)
            {
                result.Append(accents.ContainsKey(c) ? accents[c] : c);
            }
            return result.ToString();
        }

        /// <summary>
        /// Tạo mật khẩu ngẫu nhiên
        /// </summary>
        private static string GenerateRandomPassword(int length = 8)
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}
