using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Microsoft.Extensions.Options;

namespace GymManagement.Web.Services
{
    public class EmailSettings
    {
        public string SmtpServer { get; set; } = string.Empty;
        public int SmtpPort { get; set; }
        public string SmtpUsername { get; set; } = string.Empty;
        public string SmtpPassword { get; set; } = string.Empty;
        public string SenderEmail { get; set; } = string.Empty;
        public string SenderName { get; set; } = string.Empty;
    }

    public class EmailService : IEmailService
    {
        private readonly EmailSettings _emailSettings;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _emailSettings = configuration.GetSection("EmailSettings").Get<EmailSettings>() ?? new EmailSettings();
            _logger = logger;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            await SendEmailAsync(toEmail, "", subject, body);
        }

        public async Task SendEmailAsync(string toEmail, string toName, string subject, string body)
        {
            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(_emailSettings.SenderName, _emailSettings.SenderEmail));
                message.To.Add(new MailboxAddress(toName, toEmail));
                message.Subject = subject;

                var bodyBuilder = new BodyBuilder
                {
                    HtmlBody = body
                };
                message.Body = bodyBuilder.ToMessageBody();

                using var client = new SmtpClient();

                // Bypass SSL certificate validation (đã được thêm)
                client.ServerCertificateValidationCallback = (s, c, h, e) => true;

                await client.ConnectAsync(_emailSettings.SmtpServer, _emailSettings.SmtpPort, SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(_emailSettings.SmtpUsername, _emailSettings.SmtpPassword);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                _logger.LogInformation("Email sent successfully to {Email}", toEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {Email}", toEmail);
                throw;
            }
        }

        public async Task SendBulkEmailAsync(IEnumerable<string> toEmails, string subject, string body)
        {
            var tasks = toEmails.Select(email => SendEmailAsync(email, subject, body));
            await Task.WhenAll(tasks);
        }

        public async Task SendWelcomeEmailAsync(string toEmail, string memberName, string username, string tempPassword)
        {
            var subject = "Chào mừng bạn đến với Gym Management System";
            var body = $@"
                <html>
                <body>
                    <h2>Chào mừng {memberName}!</h2>
                    <p>Tài khoản của bạn đã được tạo thành công.</p>
                    <p><strong>Thông tin đăng nhập:</strong></p>
                    <ul>
                        <li>Tên đăng nhập: {username}</li>
                        <li>Mật khẩu tạm thời: {tempPassword}</li>
                    </ul>
                    <p>Vui lòng đăng nhập và đổi mật khẩu ngay lập tức.</p>
                    <p>Trân trọng,<br/>Đội ngũ Gym Management</p>
                </body>
                </html>";

            await SendEmailAsync(toEmail, memberName, subject, body);
        }

        public async Task SendPasswordResetEmailAsync(string toEmail, string memberName, string resetLink)
        {
            var subject = "Đặt lại mật khẩu - Gym Management System";
            var body = $@"
                <html>
                <body>
                    <h2>Đặt lại mật khẩu</h2>
                    <p>Xin chào {memberName},</p>
                    <p>Bạn đã yêu cầu đặt lại mật khẩu. Vui lòng click vào link bên dưới để đặt lại mật khẩu:</p>
                    <p><a href='{resetLink}'>Đặt lại mật khẩu</a></p>
                    <p>Link này sẽ hết hạn sau 24 giờ.</p>
                    <p>Nếu bạn không yêu cầu đặt lại mật khẩu, vui lòng bỏ qua email này.</p>
                    <p>Trân trọng,<br/>Đội ngũ Gym Management</p>
                </body>
                </html>";

            await SendEmailAsync(toEmail, memberName, subject, body);
        }

        public async Task SendRegistrationConfirmationEmailAsync(string toEmail, string memberName, string packageName, DateTime expiryDate)
        {
            var subject = "Xác nhận đăng ký gói tập - Gym Management System";
            var body = $@"
                <html>
                <body>
                    <h2>Xác nhận đăng ký thành công</h2>
                    <p>Xin chào {memberName},</p>
                    <p>Bạn đã đăng ký thành công gói tập: <strong>{packageName}</strong></p>
                    <p>Thông tin gói tập:</p>
                    <ul>
                        <li>Tên gói: {packageName}</li>
                        <li>Ngày hết hạn: {expiryDate:dd/MM/yyyy}</li>
                    </ul>
                    <p>Cảm ơn bạn đã tin tưởng và sử dụng dịch vụ của chúng tôi!</p>
                    <p>Trân trọng,<br/>Đội ngũ Gym Management</p>
                </body>
                </html>";

            await SendEmailAsync(toEmail, memberName, subject, body);
        }

        public async Task SendPaymentConfirmationEmailAsync(string toEmail, string memberName, decimal amount, string paymentMethod)
        {
            var subject = "Xác nhận thanh toán - Gym Management System";
            var body = $@"
                <html>
                <body>
                    <h2>Xác nhận thanh toán thành công</h2>
                    <p>Xin chào {memberName},</p>
                    <p>Chúng tôi đã nhận được thanh toán của bạn với thông tin sau:</p>
                    <ul>
                        <li>Số tiền: {amount:N0} VNĐ</li>
                        <li>Phương thức: {paymentMethod}</li>
                        <li>Thời gian: {DateTime.Now:dd/MM/yyyy HH:mm}</li>
                    </ul>
                    <p>Cảm ơn bạn đã thanh toán!</p>
                    <p>Trân trọng,<br/>Đội ngũ Gym Management</p>
                </body>
                </html>";

            await SendEmailAsync(toEmail, memberName, subject, body);
        }

        #region New Email Templates

        public async Task SendScheduleChangeNotificationAsync(string toEmail, string memberName, string changeDetails, string classOrSessionInfo)
        {
            var subject = "🔔 Thông báo thay đổi lịch - Gym Management";
            var body = $@"
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; }}
                        .header {{ background-color: #3498db; color: white; padding: 20px; text-align: center; }}
                        .content {{ padding: 20px; }}
                        .alert {{ background-color: #fff3cd; border: 1px solid #ffeeba; padding: 15px; border-radius: 5px; margin: 15px 0; }}
                        .footer {{ background-color: #f8f9fa; padding: 15px; text-align: center; font-size: 12px; }}
                    </style>
                </head>
                <body>
                    <div class='header'>
                        <h2>📅 Thông báo thay đổi lịch</h2>
                    </div>
                    <div class='content'>
                        <p>Xin chào <strong>{memberName}</strong>,</p>
                        <div class='alert'>
                            <strong>⚠️ THÔNG BÁO QUAN TRỌNG:</strong> Có thay đổi trong lịch của bạn
                        </div>
                        <p><strong>Thông tin lớp/buổi tập:</strong></p>
                        <p>{classOrSessionInfo}</p>
                        <p><strong>Chi tiết thay đổi:</strong></p>
                        <p>{changeDetails}</p>
                        <p>Vui lòng kiểm tra lại lịch trình và sắp xếp thời gian phù hợp.</p>
                        <p>Nếu có thắc mắc, vui lòng liên hệ với chúng tôi.</p>
                    </div>
                    <div class='footer'>
                        <p>Trân trọng,<br/>Đội ngũ Gym Management<br/>📞 Hotline: 1900-xxxx | 📧 Email: clbhtsvtvu@gmail.com</p>
                    </div>
                </body>
                </html>";

            await SendEmailAsync(toEmail, memberName, subject, body);
        }

        public async Task SendClassReminderEmailAsync(string toEmail, string memberName, string className, DateTime classTime, string instructorName, string location)
        {
            var subject = "⏰ Nhắc nhở lớp học sắp diễn ra";
            var body = $@"
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; }}
                        .header {{ background-color: #28a745; color: white; padding: 20px; text-align: center; }}
                        .content {{ padding: 20px; }}
                        .class-info {{ background-color: #d4edda; border: 1px solid #c3e6cb; padding: 15px; border-radius: 5px; margin: 15px 0; }}
                        .footer {{ background-color: #f8f9fa; padding: 15px; text-align: center; font-size: 12px; }}
                    </style>
                </head>
                <body>
                    <div class='header'>
                        <h2>⏰ Nhắc nhở lớp học</h2>
                    </div>
                    <div class='content'>
                        <p>Xin chào <strong>{memberName}</strong>,</p>
                        <p>Đây là lời nhắc về lớp học sắp diễn ra của bạn:</p>
                        <div class='class-info'>
                            <table style='width: 100%; border-collapse: collapse;'>
                                <tr><td style='padding: 5px;'><strong>🏃‍♂️ Lớp học:</strong></td><td style='padding: 5px;'>{className}</td></tr>
                                <tr><td style='padding: 5px;'><strong>🕐 Thời gian:</strong></td><td style='padding: 5px;'>{classTime:dddd, dd/MM/yyyy lúc HH:mm}</td></tr>
                                <tr><td style='padding: 5px;'><strong>👨‍🏫 Huấn luyện viên:</strong></td><td style='padding: 5px;'>{instructorName}</td></tr>
                                <tr><td style='padding: 5px;'><strong>📍 Địa điểm:</strong></td><td style='padding: 5px;'>{location}</td></tr>
                            </table>
                        </div>
                        <p>📝 <strong>Lưu ý:</strong> Vui lòng có mặt sớm 10-15 phút để chuẩn bị.</p>
                        <p>Chúc bạn có buổi tập thể dục hiệu quả! 💪</p>
                    </div>
                    <div class='footer'>
                        <p>Trân trọng,<br/>Đội ngũ Gym Management</p>
                    </div>
                </body>
                </html>";

            await SendEmailAsync(toEmail, memberName, subject, body);
        }

        public async Task SendClassCancellationEmailAsync(string toEmail, string memberName, string className, DateTime originalTime, string reason)
        {
            var subject = "❌ Thông báo hủy lớp học";
            var body = $@"
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; }}
                        .header {{ background-color: #dc3545; color: white; padding: 20px; text-align: center; }}
                        .content {{ padding: 20px; }}
                        .cancel-info {{ background-color: #f8d7da; border: 1px solid #f5c6cb; padding: 15px; border-radius: 5px; margin: 15px 0; }}
                        .footer {{ background-color: #f8f9fa; padding: 15px; text-align: center; font-size: 12px; }}
                    </style>
                </head>
                <body>
                    <div class='header'>
                        <h2>❌ Thông báo hủy lớp học</h2>
                    </div>
                    <div class='content'>
                        <p>Xin chào <strong>{memberName}</strong>,</p>
                        <p>Chúng tôi rất tiếc phải thông báo rằng lớp học sau đây đã bị hủy:</p>
                        <div class='cancel-info'>
                            <p><strong>🏃‍♂️ Lớp học:</strong> {className}</p>
                            <p><strong>🕐 Thời gian dự kiến:</strong> {originalTime:dddd, dd/MM/yyyy lúc HH:mm}</p>
                            <p><strong>📝 Lý do:</strong> {reason}</p>
                        </div>
                        <p>Chúng tôi sẽ thông báo lịch học bù hoặc lịch thay thế sớm nhất có thể.</p>
                        <p>Xin lỗi về sự bất tiện này và cảm ơn sự thông hiểu của bạn.</p>
                    </div>
                    <div class='footer'>
                        <p>Trân trọng,<br/>Đội ngũ Gym Management<br/>📞 Liên hệ: 1900-xxxx để biết thêm chi tiết</p>
                    </div>
                </body>
                </html>";

            await SendEmailAsync(toEmail, memberName, subject, body);
        }

        public async Task SendInstructorScheduleChangeAsync(string toEmail, string instructorName, string changeDetails, DateTime effectiveDate)
        {
            var subject = "📋 Thông báo thay đổi lịch dạy";
            var body = $@"
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; }}
                        .header {{ background-color: #6f42c1; color: white; padding: 20px; text-align: center; }}
                        .content {{ padding: 20px; }}
                        .schedule-info {{ background-color: #e2e3e5; border: 1px solid #d6d8db; padding: 15px; border-radius: 5px; margin: 15px 0; }}
                        .footer {{ background-color: #f8f9fa; padding: 15px; text-align: center; font-size: 12px; }}
                    </style>
                </head>
                <body>
                    <div class='header'>
                        <h2>📋 Thông báo thay đổi lịch dạy</h2>
                    </div>
                    <div class='content'>
                        <p>Xin chào HLV <strong>{instructorName}</strong>,</p>
                        <p>Có thay đổi trong lịch dạy của bạn:</p>
                        <div class='schedule-info'>
                            <p><strong>📅 Có hiệu lực từ:</strong> {effectiveDate:dddd, dd/MM/yyyy}</p>
                            <p><strong>📝 Chi tiết thay đổi:</strong></p>
                            <p>{changeDetails}</p>
                        </div>
                        <p>Vui lòng xem xét và xác nhận việc tiếp nhận thay đổi này.</p>
                        <p>Nếu có thắc mắc, vui lòng liên hệ với bộ phận lập lịch.</p>
                    </div>
                    <div class='footer'>
                        <p>Trân trọng,<br/>Ban quản lý Gym<br/>📞 Nội bộ: ext-xxx</p>
                    </div>
                </body>
                </html>";

            await SendEmailAsync(toEmail, instructorName, subject, body);
        }

        public async Task SendMembershipExpiryReminderAsync(string toEmail, string memberName, string packageName, DateTime expiryDate, int daysRemaining)
        {
            var subject = $"⏳ Gói tập sắp hết hạn - còn {daysRemaining} ngày";
            var urgencyClass = daysRemaining <= 3 ? "urgent" : daysRemaining <= 7 ? "warning" : "info";
            var urgencyColor = daysRemaining <= 3 ? "#dc3545" : daysRemaining <= 7 ? "#ffc107" : "#17a2b8";
            
            var body = $@"
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; }}
                        .header {{ background-color: {urgencyColor}; color: white; padding: 20px; text-align: center; }}
                        .content {{ padding: 20px; }}
                        .expiry-info {{ background-color: #fff3cd; border: 1px solid #ffeeba; padding: 15px; border-radius: 5px; margin: 15px 0; }}
                        .footer {{ background-color: #f8f9fa; padding: 15px; text-align: center; font-size: 12px; }}
                        .renewal-btn {{ background-color: #28a745; color: white; padding: 12px 24px; text-decoration: none; border-radius: 5px; display: inline-block; margin: 10px 0; }}
                    </style>
                </head>
                <body>
                    <div class='header'>
                        <h2>⏳ Nhắc nhở gia hạn gói tập</h2>
                    </div>
                    <div class='content'>
                        <p>Xin chào <strong>{memberName}</strong>,</p>
                        <p>Gói tập của bạn sắp hết hạn:</p>
                        <div class='expiry-info'>
                            <p><strong>📦 Gói tập:</strong> {packageName}</p>
                            <p><strong>📅 Ngày hết hạn:</strong> {expiryDate:dddd, dd/MM/yyyy}</p>
                            <p><strong>⏰ Còn lại:</strong> <span style='color: {urgencyColor}; font-weight: bold;'>{daysRemaining} ngày</span></p>
                        </div>
                        <p>Để không bị gián đoạn việc tập luyện, vui lòng gia hạn gói tập sớm.</p>
                        <p style='text-align: center;'>
                            <a href='#' class='renewal-btn'>🔄 Gia hạn ngay</a>
                        </p>
                        <p>📞 Liên hệ hotline hoặc đến trực tiếp gym để được hỗ trợ gia hạn.</p>
                    </div>
                    <div class='footer'>
                        <p>Trân trọng,<br/>Đội ngũ Gym Management</p>
                    </div>
                </body>
                </html>";

            await SendEmailAsync(toEmail, memberName, subject, body);
        }

        public async Task SendBookingConfirmationEmailAsync(string toEmail, string memberName, string sessionType, DateTime sessionTime, string instructorName)
        {
            var subject = "✅ Xác nhận đặt lịch thành công";
            var body = $@"
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; }}
                        .header {{ background-color: #28a745; color: white; padding: 20px; text-align: center; }}
                        .content {{ padding: 20px; }}
                        .booking-info {{ background-color: #d4edda; border: 1px solid #c3e6cb; padding: 15px; border-radius: 5px; margin: 15px 0; }}
                        .footer {{ background-color: #f8f9fa; padding: 15px; text-align: center; font-size: 12px; }}
                    </style>
                </head>
                <body>
                    <div class='header'>
                        <h2>✅ Xác nhận đặt lịch</h2>
                    </div>
                    <div class='content'>
                        <p>Xin chào <strong>{memberName}</strong>,</p>
                        <p>Bạn đã đặt lịch thành công:</p>
                        <div class='booking-info'>
                            <table style='width: 100%; border-collapse: collapse;'>
                                <tr><td style='padding: 5px;'><strong>🏋️‍♂️ Loại buổi tập:</strong></td><td style='padding: 5px;'>{sessionType}</td></tr>
                                <tr><td style='padding: 5px;'><strong>🕐 Thời gian:</strong></td><td style='padding: 5px;'>{sessionTime:dddd, dd/MM/yyyy lúc HH:mm}</td></tr>
                                <tr><td style='padding: 5px;'><strong>👨‍🏫 Huấn luyện viên:</strong></td><td style='padding: 5px;'>{instructorName}</td></tr>
                            </table>
                        </div>
                        <p>📝 <strong>Lưu ý quan trọng:</strong></p>
                        <ul>
                            <li>Vui lòng có mặt đúng giờ hoặc sớm 5-10 phút</li>
                            <li>Mang theo đồ tập và nước uống</li>
                            <li>Nếu cần hủy, vui lòng thông báo trước ít nhất 2 giờ</li>
                        </ul>
                    </div>
                    <div class='footer'>
                        <p>Trân trọng,<br/>Đội ngũ Gym Management</p>
                    </div>
                </body>
                </html>";

            await SendEmailAsync(toEmail, memberName, subject, body);
        }

        public async Task SendBookingCancellationEmailAsync(string toEmail, string memberName, string sessionType, DateTime sessionTime, string reason)
        {
            var subject = "❌ Xác nhận hủy lịch đặt";
            var body = $@"
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; }}
                        .header {{ background-color: #dc3545; color: white; padding: 20px; text-align: center; }}
                        .content {{ padding: 20px; }}
                        .cancel-info {{ background-color: #f8d7da; border: 1px solid #f5c6cb; padding: 15px; border-radius: 5px; margin: 15px 0; }}
                        .footer {{ background-color: #f8f9fa; padding: 15px; text-align: center; font-size: 12px; }}
                    </style>
                </head>
                <body>
                    <div class='header'>
                        <h2>❌ Xác nhận hủy lịch</h2>
                    </div>
                    <div class='content'>
                        <p>Xin chào <strong>{memberName}</strong>,</p>
                        <p>Lịch đặt của bạn đã được hủy:</p>
                        <div class='cancel-info'>
                            <p><strong>🏋️‍♂️ Loại buổi tập:</strong> {sessionType}</p>
                            <p><strong>🕐 Thời gian đã hủy:</strong> {sessionTime:dddd, dd/MM/yyyy lúc HH:mm}</p>
                            <p><strong>📝 Lý do:</strong> {reason}</p>
                        </div>
                        <p>Bạn có thể đặt lịch mới bất kỳ lúc nào thông qua hệ thống.</p>
                        <p>Cảm ơn bạn đã thông báo trước!</p>
                    </div>
                    <div class='footer'>
                        <p>Trân trọng,<br/>Đội ngũ Gym Management</p>
                    </div>
                </body>
                </html>";

            await SendEmailAsync(toEmail, memberName, subject, body);
        }

        public async Task SendImportantChangeConfirmationAsync(string toEmail, string memberName, string changeType, string changeDetails, DateTime effectiveDate)
        {
            var subject = "🔔 Xác nhận thay đổi quan trọng";
            var body = $@"
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; }}
                        .header {{ background-color: #fd7e14; color: white; padding: 20px; text-align: center; }}
                        .content {{ padding: 20px; }}
                        .change-info {{ background-color: #fff3cd; border: 1px solid #ffeeba; padding: 15px; border-radius: 5px; margin: 15px 0; }}
                        .footer {{ background-color: #f8f9fa; padding: 15px; text-align: center; font-size: 12px; }}
                        .important {{ color: #dc3545; font-weight: bold; }}
                    </style>
                </head>
                <body>
                    <div class='header'>
                        <h2>🔔 Xác nhận thay đổi quan trọng</h2>
                    </div>
                    <div class='content'>
                        <p>Xin chào <strong>{memberName}</strong>,</p>
                        <p class='important'>Đây là email xác nhận về thay đổi quan trọng trong tài khoản của bạn:</p>
                        <div class='change-info'>
                            <p><strong>📝 Loại thay đổi:</strong> {changeType}</p>
                            <p><strong>📅 Có hiệu lực từ:</strong> {effectiveDate:dddd, dd/MM/yyyy lúc HH:mm}</p>
                            <p><strong>🔍 Chi tiết:</strong></p>
                            <p>{changeDetails}</p>
                        </div>
                        <p>Nếu bạn không thực hiện thay đổi này hoặc có thắc mắc, vui lòng liên hệ ngay với chúng tôi.</p>
                        <p class='important'>📞 Hotline khẩn cấp: 1900-xxxx (24/7)</p>
                    </div>
                    <div class='footer'>
                        <p>Trân trọng,<br/>Đội ngũ Gym Management<br/>🔒 Email này được gửi tự động để đảm bảo bảo mật</p>
                    </div>
                </body>
                </html>";

            await SendEmailAsync(toEmail, memberName, subject, body);
        }

        #endregion
    }
}
