using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace GymManagement.Web.Services
{
    public interface IAuditLogService
    {
        void LogSalaryAction(string action, int? salaryId, string? month, string? details, string? userId);
    }

    public class AuditLogService : IAuditLogService
    {
        private readonly ILogger<AuditLogService> _logger;

        public AuditLogService(ILogger<AuditLogService> logger)
        {
            _logger = logger;
        }

        public void LogSalaryAction(string action, int? salaryId, string? month, string? details, string? userId)
        {
            var logMessage = "SALARY_AUDIT: Action={Action}, SalaryId={SalaryId}, Month={Month}, UserId={UserId}, Details={Details}, Timestamp={Timestamp}";
            
            _logger.LogInformation(logMessage, 
                action, 
                salaryId, 
                month, 
                userId, 
                details, 
                DateTime.UtcNow);
        }
    }

    // Extension methods for common audit actions
    public static class AuditLogExtensions
    {
        public static void LogSalaryCreated(this IAuditLogService auditLog, int salaryId, string month, string userId)
        {
            auditLog.LogSalaryAction("CREATE", salaryId, month, "Salary record created", userId);
        }

        public static void LogSalaryPaid(this IAuditLogService auditLog, int salaryId, string month, decimal amount, string userId)
        {
            auditLog.LogSalaryAction("PAY", salaryId, month, $"Salary paid: {amount:N0} VND", userId);
        }

        public static void LogSalaryDeleted(this IAuditLogService auditLog, int salaryId, string month, string userId)
        {
            auditLog.LogSalaryAction("DELETE", salaryId, month, "Salary record deleted", userId);
        }

        public static void LogMonthlySalaryGenerated(this IAuditLogService auditLog, string month, int count, string userId)
        {
            auditLog.LogSalaryAction("GENERATE_MONTHLY", null, month, $"Generated {count} salary records", userId);
        }

        public static void LogSalaryViewed(this IAuditLogService auditLog, int? salaryId, string month, string userId)
        {
            auditLog.LogSalaryAction("VIEW", salaryId, month, "Salary data viewed", userId);
        }

        public static void LogSalaryExported(this IAuditLogService auditLog, string month, string format, string userId)
        {
            auditLog.LogSalaryAction("EXPORT", null, month, $"Salary data exported as {format}", userId);
        }
    }
}
