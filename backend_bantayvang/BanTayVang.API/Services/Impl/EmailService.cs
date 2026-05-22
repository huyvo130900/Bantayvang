using BanTayVang.API.Configuration;
using BanTayVang.API.Services.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace BanTayVang.API.Services.Impl
{
    /// <summary>
    /// Email service using MailKit with SMTP
    /// OWASP A02: Cryptographic Failures - Use TLS
    /// OWASP A09: Security Logging - Log email events
    /// </summary>
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _settings;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IOptions<EmailSettings> settings, ILogger<EmailService> logger)
        {
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task<bool> SendEmailAsync(string toEmail, string subject, string body, bool isHtml = true)
        {
            try
            {
                if (!_settings.EnableEmailSending)
                {
                    // Dev mode: just log instead of sending
                    _logger.LogInformation("EMAIL [DEV MODE - NOT SENT]: To={ToEmail}, Subject={Subject}", toEmail, subject);
                    return true;
                }

                if (string.IsNullOrWhiteSpace(toEmail))
                {
                    _logger.LogWarning("Email recipient is empty");
                    return false;
                }

                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(_settings.FromName, _settings.FromEmail));
                message.To.Add(MailboxAddress.Parse(toEmail));
                message.Subject = subject;

                var bodyBuilder = new BodyBuilder();
                if (isHtml)
                    bodyBuilder.HtmlBody = body;
                else
                    bodyBuilder.TextBody = body;

                message.Body = bodyBuilder.ToMessageBody();

                using var client = new SmtpClient();
                
                // OWASP A02: Use TLS
                var secureSocketOption = _settings.UseSsl 
                    ? SecureSocketOptions.StartTls 
                    : SecureSocketOptions.None;

                await client.ConnectAsync(_settings.SmtpHost, _settings.SmtpPort, secureSocketOption);
                
                if (!string.IsNullOrEmpty(_settings.SmtpUsername))
                {
                    await client.AuthenticateAsync(_settings.SmtpUsername, _settings.SmtpPassword);
                }

                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                _logger.LogInformation("Email sent successfully to {ToEmail}", toEmail);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {ToEmail}", toEmail);
                return false;
            }
        }

        public async Task<bool> SendPasswordResetEmailAsync(string toEmail, string fullName, string resetToken)
        {
            var resetUrl = $"https://localhost:7249/reset-password?token={Uri.EscapeDataString(resetToken)}";
            
            var subject = "[BanTayVang] Đặt lại mật khẩu";
            var body = $@"
<html>
<body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
    <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
        <h2 style='color: #2c5aa0;'>Yêu cầu đặt lại mật khẩu</h2>
        <p>Xin chào <strong>{fullName}</strong>,</p>
        <p>Chúng tôi nhận được yêu cầu đặt lại mật khẩu cho tài khoản của bạn.</p>
        <p>Nhấn vào nút bên dưới để đặt lại mật khẩu:</p>
        <p style='text-align: center; margin: 30px 0;'>
            <a href='{resetUrl}' style='background-color: #2c5aa0; color: white; padding: 12px 30px; text-decoration: none; border-radius: 5px; display: inline-block;'>
                Đặt lại mật khẩu
            </a>
        </p>
        <p>Hoặc copy link sau vào trình duyệt:</p>
        <p style='word-break: break-all; color: #666;'>{resetUrl}</p>
        <hr style='margin: 30px 0;'>
        <p style='color: #999; font-size: 12px;'>
            Link đặt lại mật khẩu sẽ hết hạn sau 1 giờ.<br>
            Nếu bạn không yêu cầu đặt lại mật khẩu, vui lòng bỏ qua email này.
        </p>
    </div>
</body>
</html>";

            return await SendEmailAsync(toEmail, subject, body, isHtml: true);
        }

        public async Task<bool> SendWelcomeEmailAsync(string toEmail, string username, string fullName)
        {
            var subject = "[BanTayVang] Chào mừng đến với hệ thống";
            var body = $@"
<html>
<body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
    <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
        <h2 style='color: #2c5aa0;'>Chào mừng đến với BanTayVang!</h2>
        <p>Xin chào <strong>{fullName}</strong>,</p>
        <p>Tài khoản của bạn đã được tạo thành công với thông tin:</p>
        <ul>
            <li><strong>Tên đăng nhập:</strong> {username}</li>
            <li><strong>Email:</strong> {toEmail}</li>
        </ul>
        <p>Bạn có thể đăng nhập vào hệ thống để bắt đầu sử dụng.</p>
        <p style='text-align: center; margin: 30px 0;'>
            <a href='https://localhost:7249' style='background-color: #28a745; color: white; padding: 12px 30px; text-decoration: none; border-radius: 5px; display: inline-block;'>
                Đăng nhập ngay
            </a>
        </p>
    </div>
</body>
</html>";

            return await SendEmailAsync(toEmail, subject, body, isHtml: true);
        }

        public async Task<bool> SendExamResultEmailAsync(string toEmail, string fullName, string examName, double score, bool pass)
        {
            var resultText = pass ? "<span style='color: #28a745;'>✓ Đạt</span>" : "<span style='color: #dc3545;'>✗ Không đạt</span>";
            var subject = $"[BanTayVang] Kết quả thi: {examName}";
            var body = $@"
<html>
<body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
    <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
        <h2 style='color: #2c5aa0;'>Kết quả bài thi</h2>
        <p>Xin chào <strong>{fullName}</strong>,</p>
        <p>Bạn vừa hoàn thành bài thi <strong>{examName}</strong></p>
        <div style='background-color: #f8f9fa; padding: 20px; border-radius: 8px; margin: 20px 0;'>
            <p style='margin: 5px 0;'><strong>Điểm:</strong> {score:F2}</p>
            <p style='margin: 5px 0;'><strong>Kết quả:</strong> {resultText}</p>
        </div>
        <p>Đăng nhập vào hệ thống để xem chi tiết.</p>
    </div>
</body>
</html>";

            return await SendEmailAsync(toEmail, subject, body, isHtml: true);
        }
    }
}