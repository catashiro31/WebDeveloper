using WebDeveloper.Helpers;
using Microsoft.EntityFrameworkCore;
using WebDeveloper.Data;
using WebDeveloper.Models.DTOs.Auth;
using WebDeveloper.Models.DTOs.User;
using WebDeveloper.Models.Entities;
using WebDeveloper.Security;
using WebDeveloper.Services.Interfaces;

namespace WebDeveloper.Services
{
    /// <summary>
    /// Xác thực người dùng (tương đương AuthService.java)
    /// </summary>
    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _db;
        private readonly AccessTokenProvider _jwt;
        private readonly IEmailService _emailService;

        public AuthService(ApplicationDbContext db, AccessTokenProvider jwt, IEmailService emailService)
        {
            _db = db;
            _jwt = jwt;
            _emailService = emailService;
        }

        // === Đăng nhập: kiểm tra mật khẩu và trạng thái tài khoản trước khi cấp cặp token. ===
        public async Task<SignInResponse> SignIn(SignInRequest request)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (user == null)
                throw new InvalidOperationException("Tài khoản không tồn tại!");

            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                throw new InvalidOperationException("Mật khẩu không chính xác!");

            if (user.IsActive != true)
            {
                if (AccountState.AwaitingEmailVerification(user))
                    throw new InvalidOperationException("Tài khoản chưa được xác thực. Vui lòng kiểm tra email để xác thực!");
                throw new InvalidOperationException("Tài khoản đã bị khóa. Lý do: " + (user.ReasonBanned ?? "Vi phạm chính sách"));
            }


            var token = _jwt.CreateToken(user);
            var refreshToken = _jwt.GenerateRefreshToken();
            
            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            await _db.SaveChangesAsync();
            
            string? verificationStatus = null;
            if (user.Role == WebDeveloper.Models.Enums.RoleStatus.DOCTOR)
            {
                var doctorDetail = await _db.DoctorDetails.FirstOrDefaultAsync(d => d.UserId == user.UserId);
                verificationStatus = doctorDetail?.VerificationStatus.ToString() ?? "UNREGISTERED";
            }

            return new SignInResponse
            {
                Token = token,
                RefreshToken = refreshToken,
                User = new UserDto
                {
                    Email = user.Email ?? "",
                    FullName = user.FullName ?? "",
                    Role = user.Role.ToString() ?? "",
                    PhoneNumber = user.PhoneNumber,
                    AvatarUrl = user.AvatarUrl,
                    VerificationStatus = verificationStatus
                }
            };
        }

        // === Gia hạn phiên: kiểm tra token đã lưu; xoay token có điều kiện để tránh dùng lại. ===
        public async Task<SignInResponse> RefreshToken(string refreshToken, bool rotateRefreshToken = true)
        {
            var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.RefreshToken == refreshToken);

            if (user == null || user.IsActive != true || user.RefreshTokenExpiryTime == null || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
                throw new InvalidOperationException("Refresh token không hợp lệ hoặc đã hết hạn.");

            var newAccessToken = _jwt.CreateToken(user);
            var newRefreshToken = refreshToken;
            if (rotateRefreshToken)
            {
                newRefreshToken = _jwt.GenerateRefreshToken();
                var now = DateTime.UtcNow;
                var updated = await _db.Users
                    .Where(u => u.UserId == user.UserId && u.RefreshToken == refreshToken &&
                        u.IsActive == true && u.RefreshTokenExpiryTime > now)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(u => u.RefreshToken, newRefreshToken)
                        .SetProperty(u => u.RefreshTokenExpiryTime, now.AddDays(7)));
                if (updated != 1)
                    throw new InvalidOperationException("Refresh token không hợp lệ hoặc đã hết hạn.");
            }

            string? verificationStatus = null;
            if (user.Role == WebDeveloper.Models.Enums.RoleStatus.DOCTOR)
            {
                var doctorDetail = await _db.DoctorDetails.FirstOrDefaultAsync(d => d.UserId == user.UserId);
                verificationStatus = doctorDetail?.VerificationStatus.ToString() ?? "UNREGISTERED";
            }

            return new SignInResponse
            {
                Token = newAccessToken,
                RefreshToken = newRefreshToken,
                User = new UserDto
                {
                    Email = user.Email ?? "",
                    FullName = user.FullName ?? "",
                    Role = user.Role.ToString() ?? "",
                    PhoneNumber = user.PhoneNumber,
                    AvatarUrl = user.AvatarUrl,
                    VerificationStatus = verificationStatus
                }
            };
        }

        // === Đăng ký: lưu tài khoản chưa kích hoạt và gửi mã xác thực email. ===
        public async Task<string> SignUp(SignUpRequest request)
        {
            if (request.Role == WebDeveloper.Models.Enums.RoleStatus.ADMIN)
                throw new InvalidOperationException("Không thể đăng ký tài khoản với quyền quản trị!");

            if (request.PhoneNumber.Distinct().Count() <= 2 || System.Text.RegularExpressions.Regex.IsMatch(request.PhoneNumber, @"^0[35789](\d)\1{7}$"))
                throw new InvalidOperationException("Số điện thoại không hợp lệ (không được là chuỗi số lặp)!");

            if (!System.Text.RegularExpressions.Regex.IsMatch(request.Password, @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{6,}$"))
                throw new InvalidOperationException("Mật khẩu phải có tối thiểu 6 ký tự, gồm ít nhất 1 chữ hoa, 1 chữ thường và 1 chữ số!");

            if (await _db.Users.AnyAsync(u => u.Email == request.Email))
                throw new InvalidOperationException("Email đã được sử dụng!");

            var randomCode = Guid.NewGuid().ToString();

            var user = new User
            {
                Email = request.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                FullName = request.FullName,
                PhoneNumber = request.PhoneNumber,
                Role = request.Role,
                IsActive = false,
                VerificationCode = randomCode,
                CodeExpiry = DateTime.UtcNow.AddHours(24),
                CreatedAt = DateTime.UtcNow
            };

            _db.Users.Add(user);
            _db.EmailOutbox.Add(new EmailOutbox { User = user, Code = randomCode });
            
            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // Bắt lỗi khi có Unique Index từ database để chống Race Condition hoàn toàn
                throw new InvalidOperationException("Email đã được sử dụng!");
            }

            // Email được ghi cùng tài khoản và gửi bởi worker; request không chờ SMTP.

            return "Đã tạo tài khoản. Email xác thực đang được gửi; vui lòng kiểm tra hộp thư hoặc chọn gửi lại nếu chưa nhận được.";
        }

        // === Đăng xuất: thu hồi token; controller chịu trách nhiệm xóa cookie trình duyệt. ===
        public async Task SignOut(string? token, string? refreshToken)
        {
            if (!string.IsNullOrEmpty(token) && _jwt.GetUserIdFromToken(token) != null)
            {
                var tokenKey = TokenBlacklistKey.FromToken(token);
                if (!await _db.TokenBlacklists.AnyAsync(t => t.Token == tokenKey || t.Token == token))
                {
                    _db.TokenBlacklists.Add(new TokenBlacklist
                    {
                        Token = tokenKey,
                        ExpiryDate = _jwt.GetExpiryDateFromToken(token) ?? DateTime.UtcNow.AddDays(1),
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }
            if (!string.IsNullOrEmpty(refreshToken))
            {
                var user = await _db.Users.FirstOrDefaultAsync(u => u.RefreshToken == refreshToken);
                if (user != null)
                {
                    user.RefreshToken = null;
                    user.RefreshTokenExpiryTime = null;
                }
            }
            await _db.SaveChangesAsync();
        }

        // === Xác thực email: không cho link xác thực vượt qua quyết định khóa của admin. ===
        public async Task<string> VerifyAccount(string email, string code)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email)
                ?? throw new InvalidOperationException("Người dùng không tồn tại!");

            if (!string.IsNullOrWhiteSpace(user.ReasonBanned))
                throw new InvalidOperationException("Tài khoản đã bị khóa. Vui lòng gửi phiếu xin mở khóa.");
            if (user.IsActive == true)
                throw new InvalidOperationException("Tài khoản đã được xác thực.");
            if (user.VerificationCode == null || user.VerificationCode != code)
                throw new InvalidOperationException("Đường link xác thực không hợp lệ!");

            if (user.CodeExpiry == null || DateTime.UtcNow > user.CodeExpiry)
                throw new InvalidOperationException("Đường link xác thực đã hết hạn! Vui lòng đăng ký lại.");

            user.IsActive = true;
            user.VerificationCode = null;
            user.CodeExpiry = null;

            if (user.Role == WebDeveloper.Models.Enums.RoleStatus.PATIENT)
            {
                var selfProfile = new PatientProfile
                {
                    FullName = user.FullName ?? "",
                    PhoneNumber = user.PhoneNumber,
                    Relationship = "SELF",
                    UserId = user.UserId
                };
                _db.PatientProfiles.Add(selfProfile);
            }

            await _db.SaveChangesAsync();
            return "Xác thực tài khoản thành công! Bây giờ bạn có thể đăng nhập.";
        }

        // === Khôi phục mật khẩu: trường mã dùng chung; không suy ra trạng thái khóa chỉ từ mã. ===
        public async Task<string> ForgotPassword(string email)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email)
                ?? throw new InvalidOperationException("Email không tồn tại trong hệ thống!");

            if (user.IsActive != true)
            {
                if (AccountState.AwaitingEmailVerification(user))
                    throw new InvalidOperationException("Tài khoản chưa được xác thực. Vui lòng xác thực email trước!");
                throw new InvalidOperationException("Tài khoản đã bị khóa. Không thể đặt lại mật khẩu!");
            }

            var resetCode = Guid.NewGuid().ToString();
            user.VerificationCode = resetCode; // Reuse verification code field
            user.CodeExpiry = DateTime.UtcNow.AddMinutes(15);
            
            await _db.SaveChangesAsync();

            await _emailService.SendPasswordResetLinkAsync(user.Email, user.FullName ?? "", resetCode);

            return "Link khôi phục mật khẩu đã được gửi đến email của bạn. Vui lòng kiểm tra hộp thư!";
        }

        public async Task<string> ResetPassword(ResetPasswordRequest req)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == req.Email)
                ?? throw new InvalidOperationException("Email không hợp lệ!");

            if (user.VerificationCode == null || user.VerificationCode != req.Code)
                throw new InvalidOperationException("Mã khôi phục không hợp lệ hoặc đã được sử dụng!");

            if (user.CodeExpiry == null || DateTime.UtcNow > user.CodeExpiry)
                throw new InvalidOperationException("Đường link khôi phục đã hết hạn! Vui lòng yêu cầu lại.");

            if (!System.Text.RegularExpressions.Regex.IsMatch(req.NewPassword, @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{6,}$"))
                throw new InvalidOperationException("Mật khẩu mới phải có tối thiểu 6 ký tự, gồm ít nhất 1 chữ hoa, 1 chữ thường và 1 chữ số!");

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.NewPassword);
            user.VerificationCode = null; // Clear the code after successful reset
            user.CodeExpiry = null;

            await _db.SaveChangesAsync();

            return "Mật khẩu đã được đặt lại thành công. Bạn có thể đăng nhập bằng mật khẩu mới!";
        }
    }
}
