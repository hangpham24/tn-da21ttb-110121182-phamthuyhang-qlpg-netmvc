using GymManagement.Web.Services;
using System.Security.Claims;

namespace GymManagement.Web.Middleware
{
    /// <summary>
    /// Middleware để kiểm tra bảo mật cho các request của Trainer
    /// </summary>
    public class TrainerSecurityMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<TrainerSecurityMiddleware> _logger;

        public TrainerSecurityMiddleware(RequestDelegate next, ILogger<TrainerSecurityMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, ITrainerSecurityService trainerSecurityService)
        {
            try
            {
                // Chỉ áp dụng cho các request đến TrainerController
                if (context.Request.Path.StartsWithSegments("/Trainer") && context.User.Identity?.IsAuthenticated == true)
                {
                    await ValidateTrainerRequest(context, trainerSecurityService);
                }

                await _next(context);
            }
            catch (UnauthorizedAccessException)
            {
                await HandleUnauthorizedAccess(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in TrainerSecurityMiddleware");
                await _next(context);
            }
        }

        private async Task ValidateTrainerRequest(HttpContext context, ITrainerSecurityService trainerSecurityService)
        {
            var user = context.User;
            var path = context.Request.Path.Value?.ToLower();

            // Log tất cả các request của Trainer
            trainerSecurityService.LogSecurityEvent("TRAINER_REQUEST", user, new { 
                Path = path,
                Method = context.Request.Method,
                QueryString = context.Request.QueryString.Value,
                UserAgent = context.Request.Headers["User-Agent"].ToString()
            });

            // Kiểm tra các request có tham số classId
            if (context.Request.Query.ContainsKey("classId"))
            {
                if (int.TryParse(context.Request.Query["classId"], out int classId))
                {
                    var hasAccess = await trainerSecurityService.ValidateTrainerClassAccessAsync(classId, user);
                    if (!hasAccess)
                    {
                        throw new UnauthorizedAccessException($"Trainer không có quyền truy cập lớp học {classId}");
                    }
                }
            }

            // Kiểm tra các request có tham số studentId
            if (context.Request.Query.ContainsKey("studentId"))
            {
                if (int.TryParse(context.Request.Query["studentId"], out int studentId))
                {
                    var hasAccess = await trainerSecurityService.ValidateTrainerStudentAccessAsync(studentId, user);
                    if (!hasAccess)
                    {
                        throw new UnauthorizedAccessException($"Trainer không có quyền truy cập học viên {studentId}");
                    }
                }
            }

            // Kiểm tra rate limiting cho Trainer (tránh spam request)
            await CheckRateLimit(context, user);
        }

        private async Task CheckRateLimit(HttpContext context, ClaimsPrincipal user)
        {
            // Implement simple rate limiting
            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return;

            var cacheKey = $"trainer_rate_limit_{userId}";
            var requestCount = context.Session.GetInt32(cacheKey) ?? 0;
            
            if (requestCount > 100) // Max 100 requests per session
            {
                _logger.LogWarning("Rate limit exceeded for trainer {UserId}", userId);
                throw new UnauthorizedAccessException("Quá nhiều request. Vui lòng thử lại sau.");
            }

            context.Session.SetInt32(cacheKey, requestCount + 1);
        }

        private async Task HandleUnauthorizedAccess(HttpContext context)
        {
            context.Response.StatusCode = 403;
            context.Response.ContentType = "application/json";

            var response = new
            {
                success = false,
                message = "Bạn không có quyền truy cập tài nguyên này.",
                timestamp = DateTime.UtcNow,
                path = context.Request.Path.Value
            };

            await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(response));
        }
    }

    /// <summary>
    /// Extension method để đăng ký middleware
    /// </summary>
    public static class TrainerSecurityMiddlewareExtensions
    {
        public static IApplicationBuilder UseTrainerSecurity(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<TrainerSecurityMiddleware>();
        }
    }
}
