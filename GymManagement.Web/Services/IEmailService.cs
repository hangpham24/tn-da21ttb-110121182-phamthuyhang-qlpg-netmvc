namespace GymManagement.Web.Services
{
    public interface IEmailService
    {
        // Core email methods
        Task SendEmailAsync(string toEmail, string subject, string body);
        Task SendEmailAsync(string toEmail, string toName, string subject, string body);
        Task SendBulkEmailAsync(IEnumerable<string> toEmails, string subject, string body);
        
        // Existing templates
        Task SendWelcomeEmailAsync(string toEmail, string memberName, string username, string tempPassword);
        Task SendPasswordResetEmailAsync(string toEmail, string memberName, string resetLink);
        Task SendRegistrationConfirmationEmailAsync(string toEmail, string memberName, string packageName, DateTime expiryDate);
        Task SendPaymentConfirmationEmailAsync(string toEmail, string memberName, decimal amount, string paymentMethod);
        
        // New notification templates
        Task SendScheduleChangeNotificationAsync(string toEmail, string memberName, string changeDetails, string classOrSessionInfo);
        Task SendClassReminderEmailAsync(string toEmail, string memberName, string className, DateTime classTime, string instructorName, string location);
        Task SendClassCancellationEmailAsync(string toEmail, string memberName, string className, DateTime originalTime, string reason);
        Task SendInstructorScheduleChangeAsync(string toEmail, string instructorName, string changeDetails, DateTime effectiveDate);
        Task SendMembershipExpiryReminderAsync(string toEmail, string memberName, string packageName, DateTime expiryDate, int daysRemaining);
        Task SendBookingConfirmationEmailAsync(string toEmail, string memberName, string sessionType, DateTime sessionTime, string instructorName);
        Task SendBookingCancellationEmailAsync(string toEmail, string memberName, string sessionType, DateTime sessionTime, string reason);
        Task SendImportantChangeConfirmationAsync(string toEmail, string memberName, string changeType, string changeDetails, DateTime effectiveDate);
    }
}
