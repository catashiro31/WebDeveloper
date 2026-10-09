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

        public async Task SendSignUpConfirmationAsync(string email, string fullName, string code)
        {
            var subject = "DocBooking - Xác nhận đăng ký tài khoản";
            var verificationLink = $"{_config["App:ClientUrl"]}/Auth/VerifyAccount?email={email}&code={code}";
            var body = $"<h3>Xin chào {fullName},</h3>" +
                       $"<p>Cảm ơn bạn đã đăng ký tài khoản tại DocBooking.</p>" +
                       $"<p>Vui lòng click vào link bên dưới để xác thực tài khoản của bạn:</p>" +
                       $"<p><a href='{verificationLink}'>{verificationLink}</a></p>" +
                       "<p>Trân trọng,<br/>DocBooking Team</p>";
            await SendEmailAsync(email, subject, body);
        }



        public async Task SendPasswordResetLinkAsync(string email, string fullName, string code)
        {
            var subject = "DocBooking - Yêu cầu đặt lại mật khẩu";
            var resetLink = $"{_config["App:ClientUrl"]}/Auth/ResetPassword?email={email}&code={code}";
            var body = $"<h3>Xin chào {fullName},</h3>" +
                       $"<p>Bạn (hoặc ai đó) vừa yêu cầu đặt lại mật khẩu cho tài khoản DocBooking.</p>" +
                       $"<p>Vui lòng click vào link bên dưới để đặt lại mật khẩu (link sẽ hết hạn sau 15 phút):</p>" +
                       $"<p><a href='{resetLink}'>{resetLink}</a></p>" +
                       $"<p>Nếu bạn không yêu cầu, vui lòng bỏ qua email này.</p>" +
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
