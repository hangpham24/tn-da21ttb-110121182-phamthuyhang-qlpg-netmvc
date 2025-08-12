using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

namespace GymManagement.Web.Controllers.Api
{
    /// <summary>
    /// ✅ REAL-TIME NOTIFICATIONS API
    /// Provides Server-Sent Events for real-time notifications
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class NotificationsController : ControllerBase
    {
        private readonly ILogger<NotificationsController> _logger;
        private static readonly Dictionary<string, List<StreamWriter>> _connections = new();
        private static readonly object _lock = new object();

        public NotificationsController(ILogger<NotificationsController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Server-Sent Events stream endpoint
        /// </summary>
        [HttpGet("stream")]
        public async Task Stream()
        {
            var userId = User.Identity?.Name ?? "anonymous";
            
            Response.Headers["Content-Type"] = "text/event-stream";
            Response.Headers["Cache-Control"] = "no-cache";
            Response.Headers["Connection"] = "keep-alive";
            Response.Headers["Access-Control-Allow-Origin"] = "*";

            var writer = new StreamWriter(Response.Body, Encoding.UTF8);
            
            // Add connection to user's connection list
            lock (_lock)
            {
                if (!_connections.ContainsKey(userId))
                {
                    _connections[userId] = new List<StreamWriter>();
                }
                _connections[userId].Add(writer);
            }

            try
            {
                // Send initial connection message
                await SendMessage(writer, new
                {
                    type = "connection",
                    message = "Kết nối thành công",
                    timestamp = DateTime.Now
                });

                // Keep connection alive
                while (!HttpContext.RequestAborted.IsCancellationRequested)
                {
                    // Send heartbeat every 30 seconds
                    await SendHeartbeat(writer);
                    await Task.Delay(30000, HttpContext.RequestAborted);
                }
            }
            catch (OperationCanceledException)
            {
                // Client disconnected
                _logger.LogInformation("Client {UserId} disconnected from notification stream", userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in notification stream for user {UserId}", userId);
            }
            finally
            {
                // Remove connection
                lock (_lock)
                {
                    if (_connections.ContainsKey(userId))
                    {
                        _connections[userId].Remove(writer);
                        if (_connections[userId].Count == 0)
                        {
                            _connections.Remove(userId);
                        }
                    }
                }
                
                writer?.Dispose();
            }
        }

        /// <summary>
        /// Send notification to specific user
        /// </summary>
        [HttpPost("send/{userId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SendToUser(string userId, [FromBody] NotificationRequest request)
        {
            try
            {
                await SendNotificationToUser(userId, request);
                return Ok(new { success = true, message = "Notification sent successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending notification to user {UserId}", userId);
                return BadRequest(new { success = false, message = "Failed to send notification" });
            }
        }

        /// <summary>
        /// Broadcast notification to all connected users
        /// </summary>
        [HttpPost("broadcast")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Broadcast([FromBody] NotificationRequest request)
        {
            try
            {
                await BroadcastNotification(request);
                return Ok(new { success = true, message = "Notification broadcasted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error broadcasting notification");
                return BadRequest(new { success = false, message = "Failed to broadcast notification" });
            }
        }

        /// <summary>
        /// Send revenue notification
        /// </summary>
        [HttpPost("revenue")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SendRevenueNotification([FromBody] RevenueNotificationRequest request)
        {
            try
            {
                var notification = new NotificationRequest
                {
                    Type = "revenue",
                    Title = "Doanh thu mới",
                    Message = $"Doanh thu {request.Period}: {request.Amount:N0} VNĐ",
                    Duration = 8000
                };

                await BroadcastNotification(notification);
                return Ok(new { success = true, message = "Revenue notification sent" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending revenue notification");
                return BadRequest(new { success = false, message = "Failed to send revenue notification" });
            }
        }

        /// <summary>
        /// Send payment notification
        /// </summary>
        [HttpPost("payment")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SendPaymentNotification([FromBody] PaymentNotificationRequest request)
        {
            try
            {
                var notification = new NotificationRequest
                {
                    Type = "payment",
                    Title = "Thanh toán mới",
                    Message = $"Thanh toán {request.Method}: {request.Amount:N0} VNĐ từ {request.CustomerName}",
                    Duration = 6000
                };

                await BroadcastNotification(notification);
                return Ok(new { success = true, message = "Payment notification sent" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending payment notification");
                return BadRequest(new { success = false, message = "Failed to send payment notification" });
            }
        }

        #region Private Methods

        private async Task SendMessage(StreamWriter writer, object data)
        {
            try
            {
                var json = JsonSerializer.Serialize(data);
                await writer.WriteAsync($"data: {json}\n\n");
                await writer.FlushAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending SSE message");
            }
        }

        private async Task SendHeartbeat(StreamWriter writer)
        {
            try
            {
                await writer.WriteAsync(": heartbeat\n\n");
                await writer.FlushAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending heartbeat");
            }
        }

        private async Task SendNotificationToUser(string userId, NotificationRequest request)
        {
            lock (_lock)
            {
                if (_connections.ContainsKey(userId))
                {
                    var connections = _connections[userId].ToList();
                    foreach (var writer in connections)
                    {
                        try
                        {
                            _ = Task.Run(async () => await SendMessage(writer, request));
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error sending notification to user {UserId}", userId);
                        }
                    }
                }
            }
        }

        private async Task BroadcastNotification(NotificationRequest request)
        {
            lock (_lock)
            {
                foreach (var userConnections in _connections.Values)
                {
                    foreach (var writer in userConnections.ToList())
                    {
                        try
                        {
                            _ = Task.Run(async () => await SendMessage(writer, request));
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error broadcasting notification");
                        }
                    }
                }
            }
        }

        #endregion
    }

    #region Request Models

    public class NotificationRequest
    {
        public string Type { get; set; } = "info";
        public string Title { get; set; } = "";
        public string Message { get; set; } = "";
        public int Duration { get; set; } = 5000;
    }

    public class RevenueNotificationRequest
    {
        public decimal Amount { get; set; }
        public string Period { get; set; } = "";
    }

    public class PaymentNotificationRequest
    {
        public decimal Amount { get; set; }
        public string Method { get; set; } = "";
        public string CustomerName { get; set; } = "";
    }

    #endregion
}
