using Microsoft.Extensions.Caching.Memory;
using System.Net;

namespace GymManagement.Web.Middleware
{
    public class RateLimitingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IMemoryCache _cache;
        private readonly ILogger<RateLimitingMiddleware> _logger;
        
        // Rate limiting configuration
        private readonly Dictionary<string, RateLimitConfig> _rateLimits = new()
        {
            { "/FaceTest/TestRegisterFace", new RateLimitConfig { MaxRequests = 10, WindowMinutes = 1 } },
            { "/FaceTest/TestRecognizeFace", new RateLimitConfig { MaxRequests = 30, WindowMinutes = 1 } },
            { "/FaceTest/BulkTestRecognition", new RateLimitConfig { MaxRequests = 5, WindowMinutes = 5 } },
            { "/Reception/AutoCheckIn", new RateLimitConfig { MaxRequests = 60, WindowMinutes = 1 } }
        };

        public RateLimitingMiddleware(RequestDelegate next, IMemoryCache cache, ILogger<RateLimitingMiddleware> logger)
        {
            _next = next;
            _cache = cache;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var path = context.Request.Path.Value;
            var method = context.Request.Method;

            // Only apply rate limiting to specific face recognition endpoints
            if (method == "POST" && _rateLimits.ContainsKey(path))
            {
                var clientId = GetClientIdentifier(context);
                var rateLimitConfig = _rateLimits[path];
                
                if (!await IsRequestAllowed(clientId, path, rateLimitConfig))
                {
                    _logger.LogWarning("Rate limit exceeded for client {ClientId} on endpoint {Path}", clientId, path);
                    
                    context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
                    context.Response.ContentType = "application/json";
                    
                    var response = new
                    {
                        success = false,
                        message = "Quá nhiều yêu cầu. Vui lòng thử lại sau.",
                        retryAfter = rateLimitConfig.WindowMinutes * 60
                    };
                    
                    await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(response));
                    return;
                }
            }

            await _next(context);
        }

        private string GetClientIdentifier(HttpContext context)
        {
            // Use combination of IP address and user ID for identification
            var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var userId = context.User?.Identity?.Name ?? "anonymous";
            return $"{ipAddress}:{userId}";
        }

        private async Task<bool> IsRequestAllowed(string clientId, string endpoint, RateLimitConfig config)
        {
            var cacheKey = $"rate_limit:{clientId}:{endpoint}";
            var windowStart = DateTime.UtcNow.AddMinutes(-config.WindowMinutes);
            
            // Get existing requests in the current window
            var requests = _cache.Get<List<DateTime>>(cacheKey) ?? new List<DateTime>();
            
            // Remove old requests outside the window
            requests = requests.Where(r => r > windowStart).ToList();
            
            // Check if limit exceeded
            if (requests.Count >= config.MaxRequests)
            {
                return false;
            }
            
            // Add current request
            requests.Add(DateTime.UtcNow);
            
            // Update cache with sliding expiration
            var cacheOptions = new MemoryCacheEntryOptions
            {
                SlidingExpiration = TimeSpan.FromMinutes(config.WindowMinutes),
                Priority = CacheItemPriority.Low
            };
            
            _cache.Set(cacheKey, requests, cacheOptions);
            return true;
        }
    }

    public class RateLimitConfig
    {
        public int MaxRequests { get; set; }
        public int WindowMinutes { get; set; }
    }
}
