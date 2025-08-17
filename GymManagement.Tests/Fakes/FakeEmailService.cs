using GymManagement.Web.Services;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

namespace GymManagement.Tests.Fakes
{
    /// <summary>
    /// 📧 FAKE EMAIL SERVICE
    /// Replaces real email service for testing
    /// Captures email operations for verification
    /// </summary>
    public class FakeEmailService : IEmailService
    {
        #region Properties

        public List<EmailRecord> SentEmails { get; private set; } = new();
        public bool ShouldThrowException { get; set; } = false;
        public string ExceptionMessage { get; set; } = "Email service error";
        public bool IsEnabled { get; set; } = true;

        #endregion

        #region IEmailService Implementation

        // Core email methods
        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            if (ShouldThrowException)
                throw new InvalidOperationException(ExceptionMessage);

            if (!IsEnabled)
                return;

            var emailRecord = new EmailRecord
            {
                To = toEmail,
                Subject = subject,
                Body = body,
                SentAt = DateTime.Now,
                IsHtml = false
            };

            SentEmails.Add(emailRecord);
            await Task.Delay(10); // Simulate async operation
        }

        public async Task SendEmailAsync(string toEmail, string toName, string subject, string body)
        {
            if (ShouldThrowException)
                throw new InvalidOperationException(ExceptionMessage);

            if (!IsEnabled)
                return;

            var emailRecord = new EmailRecord
            {
                To = toEmail,
                ToName = toName,
                Subject = subject,
                Body = body,
                SentAt = DateTime.Now,
                IsHtml = false
            };

            SentEmails.Add(emailRecord);
            await Task.Delay(10); // Simulate async operation
        }

        public async Task SendBulkEmailAsync(IEnumerable<string> toEmails, string subject, string body)
        {
            if (ShouldThrowException)
                throw new InvalidOperationException(ExceptionMessage);

            if (!IsEnabled)
                return;

            foreach (var email in toEmails)
            {
                var emailRecord = new EmailRecord
                {
                    To = email,
                    Subject = subject,
                    Body = body,
                    SentAt = DateTime.Now,
                    IsHtml = false,
                    IsBulk = true
                };

                SentEmails.Add(emailRecord);
            }

            await Task.Delay(toEmails.Count() * 5); // Simulate bulk operation
        }

        // Existing templates
        public async Task SendWelcomeEmailAsync(string toEmail, string memberName, string username, string tempPassword)
        {
            await SendEmailAsync(toEmail, memberName, "Chào mừng đến với Gym!",
                $"Xin chào {memberName}, chào mừng bạn đến với hệ thống quản lý phòng gym! Username: {username}, Mật khẩu tạm: {tempPassword}");
        }

        public async Task SendPasswordResetEmailAsync(string toEmail, string memberName, string resetLink)
        {
            await SendEmailAsync(toEmail, memberName, "Đặt lại mật khẩu",
                $"Xin chào {memberName}, click vào link sau để đặt lại mật khẩu: {resetLink}");
        }

        public async Task SendRegistrationConfirmationEmailAsync(string toEmail, string memberName, string packageName, DateTime expiryDate)
        {
            await SendEmailAsync(toEmail, memberName, "Xác nhận đăng ký",
                $"Xin chào {memberName}, bạn đã đăng ký thành công gói {packageName}. Hết hạn: {expiryDate:dd/MM/yyyy}");
        }

        public async Task SendPaymentConfirmationEmailAsync(string toEmail, string memberName, decimal amount, string paymentMethod)
        {
            await SendEmailAsync(toEmail, memberName, "Xác nhận thanh toán",
                $"Xin chào {memberName}, thanh toán {amount:N0} VNĐ qua {paymentMethod} đã được xử lý thành công!");
        }

        // New notification templates
        public async Task SendScheduleChangeNotificationAsync(string toEmail, string memberName, string changeDetails, string classOrSessionInfo)
        {
            await SendEmailAsync(toEmail, memberName, "Thay đổi lịch trình",
                $"Xin chào {memberName}, có thay đổi lịch trình: {changeDetails} cho {classOrSessionInfo}");
        }

        public async Task SendClassReminderEmailAsync(string toEmail, string memberName, string className, DateTime classTime, string instructorName, string location)
        {
            await SendEmailAsync(toEmail, memberName, "Nhắc nhở lớp học",
                $"Xin chào {memberName}, nhắc nhở lớp {className} vào {classTime:dd/MM/yyyy HH:mm} với HLV {instructorName} tại {location}");
        }

        public async Task SendClassCancellationEmailAsync(string toEmail, string memberName, string className, DateTime originalTime, string reason)
        {
            await SendEmailAsync(toEmail, memberName, "Hủy lớp học",
                $"Xin chào {memberName}, lớp {className} vào {originalTime:dd/MM/yyyy HH:mm} đã bị hủy. Lý do: {reason}");
        }

        public async Task SendInstructorScheduleChangeAsync(string toEmail, string instructorName, string changeDetails, DateTime effectiveDate)
        {
            await SendEmailAsync(toEmail, instructorName, "Thay đổi lịch dạy",
                $"Xin chào {instructorName}, có thay đổi lịch dạy: {changeDetails}. Có hiệu lực từ {effectiveDate:dd/MM/yyyy}");
        }

        public async Task SendMembershipExpiryReminderAsync(string toEmail, string memberName, string packageName, DateTime expiryDate, int daysRemaining)
        {
            await SendEmailAsync(toEmail, memberName, "Nhắc nhở hết hạn thành viên",
                $"Xin chào {memberName}, gói {packageName} sẽ hết hạn vào {expiryDate:dd/MM/yyyy} (còn {daysRemaining} ngày)");
        }

        public async Task SendBookingConfirmationEmailAsync(string toEmail, string memberName, string sessionType, DateTime sessionTime, string instructorName)
        {
            await SendEmailAsync(toEmail, memberName, "Xác nhận đặt lịch",
                $"Xin chào {memberName}, bạn đã đặt lịch thành công {sessionType} vào {sessionTime:dd/MM/yyyy HH:mm} với HLV {instructorName}");
        }

        public async Task SendBookingCancellationEmailAsync(string toEmail, string memberName, string sessionType, DateTime sessionTime, string reason)
        {
            await SendEmailAsync(toEmail, memberName, "Hủy đặt lịch",
                $"Xin chào {memberName}, lịch {sessionType} vào {sessionTime:dd/MM/yyyy HH:mm} đã bị hủy. Lý do: {reason}");
        }

        public async Task SendImportantChangeConfirmationAsync(string toEmail, string memberName, string changeType, string changeDetails, DateTime effectiveDate)
        {
            await SendEmailAsync(toEmail, memberName, "Xác nhận thay đổi quan trọng",
                $"Xin chào {memberName}, thay đổi {changeType}: {changeDetails}. Có hiệu lực từ {effectiveDate:dd/MM/yyyy}");
        }

        public async Task SendPasswordResetNotificationAsync(string toEmail, string memberName, string newPassword)
        {
            await SendEmailAsync(toEmail, memberName, "Mật khẩu đã được đặt lại",
                $"Xin chào {memberName}, mật khẩu của bạn đã được đặt lại. Mật khẩu mới: {newPassword}. Vui lòng đăng nhập và đổi mật khẩu ngay lập tức.");
        }

        #endregion

        #region Test Helper Methods

        /// <summary>
        /// Reset all captured data
        /// </summary>
        public void Reset()
        {
            SentEmails.Clear();
            ShouldThrowException = false;
            ExceptionMessage = "Email service error";
            IsEnabled = true;
        }

        /// <summary>
        /// Get emails sent to specific recipient
        /// </summary>
        public List<EmailRecord> GetEmailsTo(string recipient)
        {
            return SentEmails.Where(e => e.To.Equals(recipient, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        /// <summary>
        /// Get emails with specific subject
        /// </summary>
        public List<EmailRecord> GetEmailsWithSubject(string subject)
        {
            return SentEmails.Where(e => e.Subject.Contains(subject, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        /// <summary>
        /// Check if email was sent
        /// </summary>
        public bool WasEmailSent(string to, string subjectContains = null)
        {
            return SentEmails.Any(e => 
                e.To.Equals(to, StringComparison.OrdinalIgnoreCase) &&
                (subjectContains == null || e.Subject.Contains(subjectContains, StringComparison.OrdinalIgnoreCase)));
        }

        /// <summary>
        /// Get total emails sent
        /// </summary>
        public int GetTotalEmailsSent() => SentEmails.Count;

        /// <summary>
        /// Get emails sent in date range
        /// </summary>
        public List<EmailRecord> GetEmailsInDateRange(DateTime from, DateTime to)
        {
            return SentEmails.Where(e => e.SentAt >= from && e.SentAt <= to).ToList();
        }

        #endregion
    }

    #region Supporting Classes

    /// <summary>
    /// Record of sent email for testing verification
    /// </summary>
    public class EmailRecord
    {
        public string To { get; set; } = string.Empty;
        public string ToName { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public DateTime SentAt { get; set; }
        public bool IsHtml { get; set; }
        public bool IsBulk { get; set; }

        public override string ToString()
        {
            return $"Email to {To} ({ToName}): {Subject} (Sent: {SentAt:yyyy-MM-dd HH:mm:ss})";
        }
    }

    #endregion
}
