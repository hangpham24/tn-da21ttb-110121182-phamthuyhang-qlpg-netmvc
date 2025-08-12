using GymManagement.Web.Data.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using System.Text.Json;

namespace GymManagement.Web.Services
{
    public interface IUserSessionService
    {
        Task<UserSessionInfo?> GetCurrentUserAsync();
        Task SetCurrentUserAsync(TaiKhoan user);
        Task UpdateCurrentUserAsync(TaiKhoan user);
        Task ClearCurrentUserAsync();
        Task RefreshUserClaimsAsync();
        bool IsUserAuthenticated();
        string? GetUserId();
        string? GetUserName();
        string? GetUserEmail();
        int? GetNguoiDungId();
        string? GetFullName();
        List<string> GetUserRoles();
        bool IsInRole(string role);
    }

    public class UserSessionInfo
    {
        public string Id { get; set; } = string.Empty;
        public string TenDangNhap { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int? NguoiDungId { get; set; }
        public string? HoTen { get; set; }
        public List<string> Roles { get; set; } = new List<string>();
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
        public bool KichHoat { get; set; } = true;
    }

    public class UserSessionService : IUserSessionService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IAuthService _authService;
        private readonly ILogger<UserSessionService> _logger;
        private const string USER_SESSION_KEY = "CurrentUser";
        private const int SESSION_TIMEOUT_MINUTES = 60;

        public UserSessionService(
            IHttpContextAccessor httpContextAccessor,
            IAuthService authService,
            ILogger<UserSessionService> logger)
        {
            _httpContextAccessor = httpContextAccessor;
            _authService = authService;
            _logger = logger;
        }

        private HttpContext? HttpContext => _httpContextAccessor.HttpContext;
        private ClaimsPrincipal? User => HttpContext?.User;

        public async Task<UserSessionInfo?> GetCurrentUserAsync()
        {
            try
            {
                if (HttpContext?.Session == null || User?.Identity?.IsAuthenticated != true)
                {
                    return null;
                }

                // Try to get from session first
                var sessionData = HttpContext.Session.GetString(USER_SESSION_KEY);
                if (!string.IsNullOrEmpty(sessionData))
                {
                    var userSession = JsonSerializer.Deserialize<UserSessionInfo>(sessionData);
                    if (userSession != null && IsSessionValid(userSession))
                    {
                        return userSession;
                    }
                }

                // If not in session or expired, rebuild from claims and database
                return await RebuildUserSessionAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current user from session");
                return await RebuildUserSessionAsync();
            }
        }

        public async Task SetCurrentUserAsync(TaiKhoan user)
        {
            try
            {
                if (HttpContext?.Session == null)
                {
                    _logger.LogWarning("Session is not available");
                    return;
                }

                var roles = await _authService.GetUserRolesAsync(user.Id);
                var userSession = new UserSessionInfo
                {
                    Id = user.Id,
                    TenDangNhap = user.TenDangNhap,
                    Email = user.Email,
                    NguoiDungId = user.NguoiDungId,
                    HoTen = user.NguoiDung != null ? $"{user.NguoiDung.Ho} {user.NguoiDung.Ten}" : null,
                    Roles = roles,
                    LastUpdated = DateTime.UtcNow,
                    KichHoat = user.KichHoat
                };

                var sessionData = JsonSerializer.Serialize(userSession);
                HttpContext.Session.SetString(USER_SESSION_KEY, sessionData);

                _logger.LogInformation("User session set for user: {Username}", user.TenDangNhap);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting user session for user: {Username}", user.TenDangNhap);
            }
        }

        public async Task UpdateCurrentUserAsync(TaiKhoan user)
        {
            await SetCurrentUserAsync(user);
        }

        public async Task ClearCurrentUserAsync()
        {
            try
            {
                if (HttpContext?.Session != null)
                {
                    HttpContext.Session.Remove(USER_SESSION_KEY);
                    _logger.LogInformation("User session cleared");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing user session");
            }
        }

        public async Task RefreshUserClaimsAsync()
        {
            try
            {
                var userId = GetUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    return;
                }

                var user = await _authService.GetUserByIdAsync(userId);
                if (user != null)
                {
                    await SetCurrentUserAsync(user);
                    
                    // Also refresh the authentication cookie with new claims
                    var principal = await _authService.CreateClaimsPrincipalAsync(user);
                    await HttpContext!.SignInAsync("Cookies", principal);
                    
                    _logger.LogInformation("User claims refreshed for user: {Username}", user.TenDangNhap);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing user claims");
            }
        }

        public bool IsUserAuthenticated()
        {
            return User?.Identity?.IsAuthenticated == true;
        }

        public string? GetUserId()
        {
            return User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }

        public string? GetUserName()
        {
            return User?.FindFirst(ClaimTypes.Name)?.Value;
        }

        public string? GetUserEmail()
        {
            return User?.FindFirst(ClaimTypes.Email)?.Value;
        }

        public int? GetNguoiDungId()
        {
            var nguoiDungIdClaim = User?.FindFirst("NguoiDungId")?.Value;
            if (int.TryParse(nguoiDungIdClaim, out int nguoiDungId))
            {
                return nguoiDungId;
            }
            return null;
        }

        public string? GetFullName()
        {
            return User?.FindFirst("HoTen")?.Value;
        }

        public List<string> GetUserRoles()
        {
            return User?.FindAll(ClaimTypes.Role)?.Select(c => c.Value).ToList() ?? new List<string>();
        }

        public bool IsInRole(string role)
        {
            return User?.IsInRole(role) == true;
        }

        private async Task<UserSessionInfo?> RebuildUserSessionAsync()
        {
            try
            {
                var userId = GetUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    return null;
                }

                var user = await _authService.GetUserByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("User not found in database: {UserId}", userId);
                    return null;
                }

                await SetCurrentUserAsync(user);
                return await GetCurrentUserAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rebuilding user session");
                return null;
            }
        }

        private static bool IsSessionValid(UserSessionInfo userSession)
        {
            return userSession.LastUpdated.AddMinutes(SESSION_TIMEOUT_MINUTES) > DateTime.UtcNow;
        }
    }
}
