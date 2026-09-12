using MailKit.Net.Smtp;
using MimeKit;

namespace WebDeveloper.Services
{
    /// <summary>
    /// Gửi email thông báo (tương đương ContextEmail.java)
    /// </summary>
    public class EmailService : Interfaces.IEmailService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration config, ILogger<EmailService> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task SendDoctorApprovedEmail(string email, string fullName)
        {
            var subject = "DocBooking - Hồ sơ bác sĩ đã được duyệt";
            var body = $"<h3>Xin chào {fullName},</h3>" +
                       "<p>Hồ sơ bác sĩ của bạn đã được duyệt thành công. Bạn có thể bắt đầu tạo lịch khám ngay bây giờ!</p>" +
                       "<p>Trân trọng,<br/>DocBooking Team</p>";
            await SendEmailAsync(email, subject, body);
        }

        public async Task SendDoctorRejectedEmail(string email, string fullName, string reason)
        {
            var subject = "DocBooking - Hồ sơ bác sĩ bị từ chối";
            var body = $"<h3>Xin chào {fullName},</h3>" +
                       $"<p>Rất tiếc, hồ sơ bác sĩ của bạn đã bị từ chối.</p>" +
                       $"<p><strong>Lý do:</strong> {reason}</p>" +
                       "<p>Vui lòng cập nhật lại hồ sơ và gửi lại.</p>" +
                       "<p>Trân trọng,<br/>DocBooking Team</p>";
            await SendEmailAsync(email, subject, body);
        }

        public async Task SendPermanentBanEmail(string email, string fullName, string reason)
        {
            var subject = "DocBooking - Tài khoản đã bị khóa";
            var body = $"<h3>Xin chào {fullName},</h3>" +
                       $"<p>Tài khoản của bạn đã bị khóa vĩnh viễn.</p>" +
                       $"<p><strong>Lý do:</strong> {reason}</p>" +
                       "<p>Nếu bạn cho rằng đây là nhầm lẫn, vui lòng liên hệ admin.</p>" +
                       "<p>Trân trọng,<br/>DocBooking Team</p>";
            await SendEmailAsync(email, subject, body);
        }

        private async Task SendEmailAsync(string toEmail, string subject, string htmlBody)
        {
            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(
                    _config["EmailSettings:SenderName"],
                    _config["EmailSettings:SenderEmail"]));
                message.To.Add(MailboxAddress.Parse(toEmail));
                message.Subject = subject;
                message.Body = new TextPart("html") { Text = htmlBody };

                using var smtp = new SmtpClient();
                await smtp.ConnectAsync(
                    _config["EmailSettings:SmtpHost"],
                    int.Parse(_config["EmailSettings:SmtpPort"] ?? "587"),
                    MailKit.Security.SecureSocketOptions.StartTls);
                await smtp.AuthenticateAsync(
                    _config["EmailSettings:SenderEmail"],
                    _config["EmailSettings:SenderPassword"]);
                await smtp.SendAsync(message);
                await smtp.DisconnectAsync(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không thể gửi email đến {Email}", toEmail);
            }
        }
    }
}
