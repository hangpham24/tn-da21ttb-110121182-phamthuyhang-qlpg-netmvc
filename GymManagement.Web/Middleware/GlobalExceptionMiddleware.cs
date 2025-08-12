using GymManagement.Web.Services;
using System.Net;
using System.Text.Json;

namespace GymManagement.Web.Middleware
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;

        public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            _logger.LogError(exception, "An unhandled exception occurred. Path: {Path}, User: {User}", 
                context.Request.Path, context.User?.Identity?.Name ?? "Anonymous");

            // Clear potentially corrupted session
            try
            {
                var userSessionService = context.RequestServices.GetService<IUserSessionService>();
                if (userSessionService != null && context.User?.Identity?.IsAuthenticated == true)
                {
                    await userSessionService.ClearCurrentUserAsync();
                }
            }
            catch (Exception sessionEx)
            {
                _logger.LogError(sessionEx, "Error clearing user session during exception handling");
            }

            var response = context.Response;
            response.ContentType = "application/json";

            var responseModel = new
            {
                success = false,
                message = GetUserFriendlyMessage(exception),
                timestamp = DateTime.UtcNow,
                redirectUrl = (string?)null
            };

            switch (exception)
            {
                case UnauthorizedAccessException:
                    response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    responseModel = new
                    {
                        success = false,
                        message = "Bạn không có quyền truy cập.",
                        timestamp = DateTime.UtcNow,
                        redirectUrl = (string?)"/Auth/Login"
                    };
                    break;

                case ArgumentException or ArgumentNullException:
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    break;

                case KeyNotFoundException:
                    response.StatusCode = (int)HttpStatusCode.NotFound;
                    responseModel = new
                    {
                        success = false,
                        message = "Không tìm thấy dữ liệu yêu cầu.",
                        timestamp = DateTime.UtcNow,
                        redirectUrl = (string?)null
                    };
                    break;

                case InvalidOperationException when exception.Message.Contains("user"):
                case InvalidOperationException when exception.Message.Contains("session"):
                    response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    responseModel = new
                    {
                        success = false,
                        message = "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.",
                        timestamp = DateTime.UtcNow,
                        redirectUrl = (string?)"/Auth/Login"
                    };
                    break;

                default:
                    response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    responseModel = new
                    {
                        success = false,
                        message = "Đã xảy ra lỗi hệ thống. Vui lòng thử lại sau.",
                        timestamp = DateTime.UtcNow,
                        redirectUrl = (string?)null
                    };
                    break;
            }

            // Handle different response types
            if (IsAjaxRequest(context.Request))
            {
                // Return JSON for AJAX requests
                var jsonResponse = JsonSerializer.Serialize(responseModel);
                await response.WriteAsync(jsonResponse);
            }
            else
            {
                // Redirect to appropriate page for regular requests
                if (response.StatusCode == (int)HttpStatusCode.Unauthorized)
                {
                    response.Redirect("/Auth/Login?message=" + Uri.EscapeDataString(responseModel.message));
                }
                else
                {
                    response.Redirect("/Home/Error?message=" + Uri.EscapeDataString(responseModel.message));
                }
            }
        }

        private static string GetUserFriendlyMessage(Exception exception)
        {
            return exception switch
            {
                UnauthorizedAccessException => "Bạn không có quyền truy cập chức năng này.",
                ArgumentNullException => "Thiếu thông tin bắt buộc.",
                ArgumentException => "Dữ liệu đầu vào không hợp lệ.",
                KeyNotFoundException => "Không tìm thấy dữ liệu yêu cầu.",
                InvalidOperationException when exception.Message.Contains("user") => "Có lỗi với thông tin người dùng.",
                InvalidOperationException when exception.Message.Contains("session") => "Phiên đăng nhập đã hết hạn.",
                InvalidOperationException => "Có lỗi trong quá trình xử lý.",
                TimeoutException => "Yêu cầu đã hết thời gian chờ.",
                _ => "Đã xảy ra lỗi hệ thống. Vui lòng thử lại sau."
            };
        }

        private static bool IsAjaxRequest(HttpRequest request)
        {
            return request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
                   request.Headers["Accept"].ToString().Contains("application/json") ||
                   request.Path.StartsWithSegments("/api");
        }
    }

    public static class GlobalExceptionMiddlewareExtensions
    {
        public static IApplicationBuilder UseGlobalExceptionHandling(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<GlobalExceptionMiddleware>();
        }
    }
}
