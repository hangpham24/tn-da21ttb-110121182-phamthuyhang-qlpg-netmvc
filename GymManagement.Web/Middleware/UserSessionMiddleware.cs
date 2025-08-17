using GymManagement.Web.Services;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;

namespace GymManagement.Web.Middleware
{
    public class UserSessionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<UserSessionMiddleware> _logger;

        public UserSessionMiddleware(RequestDelegate next, ILogger<UserSessionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, IUserSessionService userSessionService)
        {
            try
            {
                // Skip for non-authenticated requests or static files
                if (!context.User.Identity?.IsAuthenticated == true || 
                    context.Request.Path.StartsWithSegments("/css") ||
                    context.Request.Path.StartsWithSegments("/js") ||
                    context.Request.Path.StartsWithSegments("/images") ||
                    context.Request.Path.StartsWithSegments("/lib"))
                {
                    await _next(context);
                    return;
                }

                // Check if this is a login/logout action or Google OAuth callback to avoid infinite loops
                var path = context.Request.Path.Value?.ToLower();
                if (path != null && (path.Contains("/auth/login") || 
                                   path.Contains("/auth/logout") || 
                                   path.Contains("/auth/loginwithgoogle") ||
                                   path.Contains("/auth/googlecallback") ||
                                   path.Contains("/signin-google")))
                {
                    await _next(context);
                    return;
                }

                // Validate and refresh user session if needed
                var currentUser = await userSessionService.GetCurrentUserAsync();
                
                if (currentUser == null)
                {
                    _logger.LogWarning("User session not found for authenticated user: {Username} on path: {Path}", 
                        context.User.FindFirst(ClaimTypes.Name)?.Value, context.Request.Path);
                    
                    // Try to rebuild session from claims
                    await userSessionService.RefreshUserClaimsAsync();
                    currentUser = await userSessionService.GetCurrentUserAsync();
                    
                    if (currentUser == null)
                    {
                        _logger.LogError("Failed to rebuild user session, redirecting to login");
                        context.Response.Redirect("/Auth/Login?message=" + Uri.EscapeDataString("Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại."));
                        return;
                    }
                }

                // Check if user account is still active
                if (!currentUser.KichHoat)
                {
                    _logger.LogWarning("Inactive user attempted access: {Username}", currentUser.TenDangNhap);
                    await userSessionService.ClearCurrentUserAsync();
                    await context.SignOutAsync("Cookies");
                    context.Response.Redirect("/Auth/Login?message=" + Uri.EscapeDataString("Tài khoản của bạn đã bị vô hiệu hóa."));
                    return;
                }

                // Add user info to HttpContext for easy access
                context.Items["CurrentUser"] = currentUser;
                
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UserSessionMiddleware for path: {Path}", context.Request.Path);
                
                // Clear potentially corrupted session
                try
                {
                    await userSessionService.ClearCurrentUserAsync();
                }
                catch (Exception clearEx)
                {
                    _logger.LogError(clearEx, "Error clearing user session in middleware");
                }
                
                // Redirect to login for authenticated users, otherwise continue
                if (context.User.Identity?.IsAuthenticated == true)
                {
                    context.Response.Redirect("/Auth/Login?message=" + Uri.EscapeDataString("Đã xảy ra lỗi với phiên đăng nhập. Vui lòng đăng nhập lại."));
                    return;
                }
                
                await _next(context);
            }
        }
    }

    public static class UserSessionMiddlewareExtensions
    {
        public static IApplicationBuilder UseUserSession(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<UserSessionMiddleware>();
        }
    }
}
