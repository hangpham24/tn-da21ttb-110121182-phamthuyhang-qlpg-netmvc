using GymManagement.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace GymManagement.Web.Helpers
{
    public static class UserSessionHelper
    {
        /// <summary>
        /// Extension method to get current user safely from any controller
        /// </summary>
        public static async Task<UserSessionInfo?> GetCurrentUserSafeAsync(this Controller controller, IUserSessionService userSessionService)
        {
            try
            {
                return await userSessionService.GetCurrentUserAsync();
            }
            catch (Exception ex)
            {
                // Log error if logger is available
                var logger = controller.HttpContext.RequestServices.GetService<ILogger<Controller>>();
                logger?.LogError(ex, "Error getting current user safely in controller: {Controller}", controller.GetType().Name);
                return null;
            }
        }

        /// <summary>
        /// Extension method to check if user is in role safely
        /// </summary>
        public static bool IsInRoleSafe(this Controller controller, IUserSessionService userSessionService, string role)
        {
            try
            {
                return userSessionService.IsInRole(role);
            }
            catch (Exception ex)
            {
                var logger = controller.HttpContext.RequestServices.GetService<ILogger<Controller>>();
                logger?.LogError(ex, "Error checking user role safely: {Role}", role);
                return false;
            }
        }

        /// <summary>
        /// Extension method to get user ID safely
        /// </summary>
        public static string? GetUserIdSafe(this Controller controller, IUserSessionService userSessionService)
        {
            try
            {
                return userSessionService.GetUserId();
            }
            catch (Exception ex)
            {
                var logger = controller.HttpContext.RequestServices.GetService<ILogger<Controller>>();
                logger?.LogError(ex, "Error getting user ID safely");
                return null;
            }
        }

        /// <summary>
        /// Extension method to get NguoiDungId safely
        /// </summary>
        public static int? GetNguoiDungIdSafe(this Controller controller, IUserSessionService userSessionService)
        {
            try
            {
                return userSessionService.GetNguoiDungId();
            }
            catch (Exception ex)
            {
                var logger = controller.HttpContext.RequestServices.GetService<ILogger<Controller>>();
                logger?.LogError(ex, "Error getting NguoiDungId safely");
                return null;
            }
        }

        /// <summary>
        /// Create standardized error response for AJAX requests
        /// </summary>
        public static JsonResult CreateErrorResponse(string message, Exception? ex = null, ILogger? logger = null)
        {
            if (ex != null && logger != null)
            {
                logger.LogError(ex, "AJAX Error: {Message}", message);
            }

            return new JsonResult(new
            {
                success = false,
                message = message,
                timestamp = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Create standardized success response for AJAX requests
        /// </summary>
        public static JsonResult CreateSuccessResponse(object? data = null, string? message = null)
        {
            var response = new
            {
                success = true,
                message = message,
                data = data,
                timestamp = DateTime.UtcNow
            };

            return new JsonResult(response);
        }

        /// <summary>
        /// Validate user session and return appropriate response for AJAX
        /// </summary>
        public static async Task<(bool IsValid, JsonResult? ErrorResponse)> ValidateUserSessionForAjax(
            IUserSessionService userSessionService, 
            ILogger logger,
            string? requiredRole = null)
        {
            try
            {
                if (!userSessionService.IsUserAuthenticated())
                {
                    return (false, CreateErrorResponse("Bạn chưa đăng nhập.", logger: logger));
                }

                var currentUser = await userSessionService.GetCurrentUserAsync();
                if (currentUser == null)
                {
                    return (false, CreateErrorResponse("Không tìm thấy thông tin người dùng.", logger: logger));
                }

                if (!string.IsNullOrEmpty(requiredRole) && !userSessionService.IsInRole(requiredRole))
                {
                    return (false, CreateErrorResponse("Bạn không có quyền thực hiện hành động này.", logger: logger));
                }

                return (true, null);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error validating user session for AJAX");
                return (false, CreateErrorResponse("Có lỗi xảy ra khi xác thực người dùng.", ex, logger));
            }
        }
    }

    /// <summary>
    /// Attribute to automatically validate user session for API endpoints
    /// </summary>
    public class ValidateUserSessionAttribute : ActionFilterAttribute
    {
        private readonly string? _requiredRole;

        public ValidateUserSessionAttribute(string? requiredRole = null)
        {
            _requiredRole = requiredRole;
        }

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var userSessionService = context.HttpContext.RequestServices.GetRequiredService<IUserSessionService>();
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<ValidateUserSessionAttribute>>();

            var (isValid, errorResponse) = await UserSessionHelper.ValidateUserSessionForAjax(
                userSessionService, logger, _requiredRole);

            if (!isValid && errorResponse != null)
            {
                context.Result = errorResponse;
                return;
            }

            await next();
        }
    }
}
