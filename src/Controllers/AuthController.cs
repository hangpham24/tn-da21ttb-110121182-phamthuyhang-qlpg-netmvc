using GymManagement.Web.Data.Models;
using GymManagement.Web.Models.ViewModels;
using GymManagement.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GymManagement.Web.Controllers
{
    public class AuthController : Controller
    {
        private readonly IAuthService _authService;
        private readonly IUserSessionService _userSessionService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            IAuthService authService,
            IUserSessionService userSessionService,
            ILogger<AuthController> logger)
        {
            _authService = authService;
            _userSessionService = userSessionService;
            _logger = logger;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null, string? message = null)
        {
            // Skip authentication check for Google callback
            var path = Request.Path.Value?.ToLower();
            if (User.Identity?.IsAuthenticated == true && !Request.Path.StartsWithSegments("/signin-google"))
            {
                // Only redirect if not coming from session expired message
                if (string.IsNullOrEmpty(message) || !message.Contains("hết hạn"))
                {
                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    // Force logout if session expired
                    HttpContext.SignOutAsync("Cookies").Wait();
                }
            }

            if (!string.IsNullOrEmpty(message))
            {
                TempData["ErrorMessage"] = message;
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _authService.AuthenticateAsync(model.Username, model.Password);

            if (user != null)
            {
                try
                {
                    var principal = await _authService.CreateClaimsPrincipalAsync(user);
                    await HttpContext.SignInAsync("Cookies", principal);

                    // Set user session
                    await _userSessionService.SetCurrentUserAsync(user);

                    _logger.LogInformation("User {Username} logged in successfully.", model.Username);

                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    {
                        return Redirect(returnUrl);
                    }

                    // Get user roles to determine redirect destination
                    var userRoles = await _authService.GetUserRolesAsync(user.Id);
                    
                    // Redirect trainers to their dashboard
                    if (userRoles.Contains("Trainer"))
                    {
                        return RedirectToAction("Dashboard", "Trainer");
                    }

                    return RedirectToAction("Index", "Home");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during login process for user {Username}", model.Username);
                    ModelState.AddModelError(string.Empty, "Đã xảy ra lỗi trong quá trình đăng nhập. Vui lòng thử lại.");
                    return View(model);
                }
            }

            ModelState.AddModelError(string.Empty, "Tên đăng nhập hoặc mật khẩu không đúng.");
            return View(model);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }

            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = new TaiKhoan
            {
                TenDangNhap = model.Username,
                Email = model.Email,
                KichHoat = true
            };

            var result = await _authService.CreateUserAsync(user, model.Password);

            if (result)
            {
                try
                {
                    _logger.LogInformation("User {Username} created a new account with password.", model.Username);

                    // Sign in the user
                    var createdUser = await _authService.GetUserByUsernameAsync(model.Username);
                    if (createdUser != null)
                    {
                        var principal = await _authService.CreateClaimsPrincipalAsync(createdUser);
                        await HttpContext.SignInAsync("Cookies", principal);

                        // Set user session
                        await _userSessionService.SetCurrentUserAsync(createdUser);
                    }

                    return RedirectToAction("Index", "Home");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during registration process for user {Username}", model.Username);
                    ModelState.AddModelError(string.Empty, "Đã xảy ra lỗi trong quá trình đăng ký. Vui lòng thử lại.");
                    return View(model);
                }
            }

            ModelState.AddModelError(string.Empty, "Không thể tạo tài khoản. Tên đăng nhập hoặc email có thể đã tồn tại.");

            return View(model);
        }

        [HttpPost]
        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            try
            {
                var username = _userSessionService.GetUserName();

                // Clear user session
                await _userSessionService.ClearCurrentUserAsync();

                // Sign out from authentication
                await HttpContext.SignOutAsync("Cookies");

                _logger.LogInformation("User {Username} logged out.", username);

                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout process");
                // Still try to sign out even if there's an error
                await HttpContext.SignOutAsync("Cookies");
                return RedirectToAction("Index", "Home");
            }
        }



        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPassword()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }

            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                ModelState.AddModelError(string.Empty, "Email là bắt buộc.");
                return View();
            }

            // TODO: Implement forgot password logic
            // For now, just show success message
            TempData["SuccessMessage"] = "Nếu email tồn tại trong hệ thống, chúng tôi đã gửi link đặt lại mật khẩu.";

            return RedirectToAction("Login");
        }

        // Google Login
        [HttpGet]
        [AllowAnonymous]
        public IActionResult LoginWithGoogle(string? returnUrl = null)
        {
            var redirectUrl = Url.Action("GoogleCallback", "Auth", new { returnUrl });
            var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GoogleCallback(string? returnUrl = null)
        {
            try
            {
                // Authenticate với Google scheme, không phải Cookies
                var result = await HttpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);
                if (!result.Succeeded)
                {
                    _logger.LogWarning("Google authentication failed");
                    TempData["ErrorMessage"] = "Đăng nhập với Google thất bại. Vui lòng thử lại.";
                    return RedirectToAction("Login");
                }

                var claims = result.Principal?.Claims;
                var email = claims?.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
                var googleId = claims?.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
                var name = claims?.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;
                var givenName = claims?.FirstOrDefault(c => c.Type == ClaimTypes.GivenName)?.Value;
                var surname = claims?.FirstOrDefault(c => c.Type == ClaimTypes.Surname)?.Value;

                if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(googleId))
                {
                    _logger.LogWarning("Invalid Google user data");
                    TempData["ErrorMessage"] = "Không thể lấy thông tin từ Google. Vui lòng thử lại.";
                    return RedirectToAction("Login");
                }

                // Kiểm tra xem user đã tồn tại chưa
                var existingUser = await _authService.GetUserByEmailAsync(email);
                
                if (existingUser == null)
                {
                    // Tạo user mới từ Google account
                    var username = email.Split('@')[0] + "_" + DateTime.Now.Ticks.ToString().Substring(10, 5);
                    
                    var newUser = new TaiKhoan
                    {
                        TenDangNhap = username,
                        Email = email,
                        KichHoat = true,
                        EmailXacNhan = true // Google đã xác nhận email
                    };

                    // Tạo mật khẩu ngẫu nhiên (user sẽ không sử dụng)
                    var randomPassword = Guid.NewGuid().ToString("N").Substring(0, 16) + "@1Aa";
                    var created = await _authService.CreateUserAsync(newUser, randomPassword, true); // true = isGoogleUser

                    if (!created)
                    {
                        _logger.LogError("Failed to create user from Google account: {Email}", email);
                        TempData["ErrorMessage"] = "Không thể tạo tài khoản. Vui lòng thử lại.";
                        return RedirectToAction("Login");
                    }

                    existingUser = await _authService.GetUserByEmailAsync(email);
                    
                    // Lưu thông tin Google login
                    await _authService.SaveExternalLoginAsync(existingUser!.Id, "Google", googleId, name);

                    _logger.LogInformation("Created new user from Google account: {Email}", email);
                }
                else
                {
                    // Kiểm tra và cập nhật external login info
                    await _authService.SaveExternalLoginAsync(existingUser.Id, "Google", googleId, name);
                }

                // Đăng nhập user
                var principal = await _authService.CreateClaimsPrincipalAsync(existingUser!);
                await HttpContext.SignOutAsync("Cookies"); // Sign out current
                await HttpContext.SignInAsync("Cookies", principal);

                // Set user session
                await _userSessionService.SetCurrentUserAsync(existingUser!);

                _logger.LogInformation("User {Email} logged in via Google.", email);

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }

                // Kiểm tra role để redirect
                var userRoles = await _authService.GetUserRolesAsync(existingUser!.Id);
                if (userRoles.Contains("Trainer"))
                {
                    return RedirectToAction("Dashboard", "Trainer");
                }

                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during Google callback");
                TempData["ErrorMessage"] = "Đã xảy ra lỗi trong quá trình đăng nhập với Google.";
                return RedirectToAction("Login");
            }
        }

    }
}
