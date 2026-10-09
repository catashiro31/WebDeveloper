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
        private readonly JwtTokenProvider _jwt;
        private readonly IEmailService _emailService;

        public AuthService(ApplicationDbContext db, JwtTokenProvider jwt, IEmailService emailService)
        {
            _db = db;
            _jwt = jwt;
            _emailService = emailService;
        }

        public async Task<SignInResponse> SignIn(SignInRequest request)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (user == null)
                throw new InvalidOperationException("Tài khoản không tồn tại!");

            if (user.IsActive != true)
            {
                if (user.VerificationCode != null)
                    throw new InvalidOperationException("Tài khoản chưa được xác thực. Vui lòng kiểm tra email để xác thực!");
                throw new InvalidOperationException("Tài khoản đã bị khóa. Lý do: " + (user.ReasonBanned ?? "Vi phạm chính sách"));
            }

            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                throw new InvalidOperationException("Mật khẩu không chính xác!");

            var token = _jwt.CreateToken(user);
            
            string? verificationStatus = null;
            if (user.Role == WebDeveloper.Models.Enums.RoleStatus.DOCTOR)
            {
                var doctorDetail = await _db.DoctorDetails.FirstOrDefaultAsync(d => d.UserId == user.UserId);
                verificationStatus = doctorDetail?.VerificationStatus.ToString() ?? "UNREGISTERED";
            }

            return new SignInResponse
            {
                Token = token,
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

        public async Task<string> SignUp(SignUpRequest request)
        {
            if (request.Role == WebDeveloper.Models.Enums.RoleStatus.ADMIN)
                throw new InvalidOperationException("Không thể đăng ký tài khoản với quyền quản trị!");

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
            
            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // Bắt lỗi khi có Unique Index từ database để chống Race Condition hoàn toàn
                throw new InvalidOperationException("Email đã được sử dụng!");
            }

            await _emailService.SendSignUpConfirmationAsync(user.Email, user.FullName ?? "", user.VerificationCode);

            return "Vui lòng kiểm tra email để xác thực tài khoản!";
        }

        public async Task SignOut(string token)
        {
            var expiry = _jwt.GetExpiryDateFromToken(token);
            var blacklist = new TokenBlacklist
            {
                Token = token,
                ExpiryDate = expiry ?? DateTime.UtcNow.AddDays(1),
                CreatedAt = DateTime.UtcNow
            };
            _db.TokenBlacklists.Add(blacklist);
            await _db.SaveChangesAsync();
        }

        public async Task<string> VerifyAccount(string email, string code)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email)
                ?? throw new InvalidOperationException("Người dùng không tồn tại!");

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

        public async Task<string> ForgotPassword(string email)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email)
                ?? throw new InvalidOperationException("Email không tồn tại trong hệ thống!");

            if (user.IsActive != true)
            {
                if (user.VerificationCode != null)
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

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.NewPassword);
            user.VerificationCode = null; // Clear the code after successful reset
            user.CodeExpiry = null;

            await _db.SaveChangesAsync();

            return "Mật khẩu đã được đặt lại thành công. Bạn có thể đăng nhập bằng mật khẩu mới!";
        }
    }

    /// <summary>
    /// Quản lý profile người dùng (tương đương UserService.java)
    /// </summary>
    public class UserService : IUserService
    {
        private readonly ApplicationDbContext _db;
        private readonly IFileUploadService _fileUpload;

        public UserService(ApplicationDbContext db, IFileUploadService fileUpload)
        {
            _db = db;
            _fileUpload = fileUpload;
        }

        public async Task<UserProfileResponse> GetProfile(User user)
        {
            var dbUser = await _db.Users.FindAsync(user.UserId)
                ?? throw new InvalidOperationException("Không tìm thấy người dùng!");

            return new UserProfileResponse
            {
                Email = dbUser.Email,
                FullName = dbUser.FullName,
                PhoneNumber = dbUser.PhoneNumber,
                AvatarUrl = dbUser.AvatarUrl,
                Role = dbUser.Role?.ToString()
            };
        }

        public async Task<UserProfileResponse> UpdateProfile(User user, UpdateProfileRequest req)
        {
            var dbUser = await _db.Users.FindAsync(user.UserId)
                ?? throw new InvalidOperationException("Không tìm thấy người dùng!");

            if (!string.IsNullOrWhiteSpace(req.FullName))
                dbUser.FullName = req.FullName;

            if (!string.IsNullOrWhiteSpace(req.PhoneNumber))
                dbUser.PhoneNumber = req.PhoneNumber;

            if (req.File != null && req.File.Length > 0)
            {
                _fileUpload.ValidateFile(req.File, "Ảnh đại diện", "image/jpeg", "image/png");
                dbUser.AvatarUrl = await _fileUpload.UploadFileAsync(req.File);
            }

            await _db.SaveChangesAsync();

            return new UserProfileResponse
            {
                Email = dbUser.Email,
                FullName = dbUser.FullName,
                PhoneNumber = dbUser.PhoneNumber,
                AvatarUrl = dbUser.AvatarUrl,
                Role = dbUser.Role?.ToString()
            };
        }

        public async Task ChangePassword(User user, ChangePasswordRequest req)
        {
            var dbUser = await _db.Users.FindAsync(user.UserId)
                ?? throw new InvalidOperationException("Không tìm thấy người dùng!");

            if (!BCrypt.Net.BCrypt.Verify(req.OldPassword, dbUser.PasswordHash))
                throw new InvalidOperationException("Mật khẩu cũ không chính xác");

            if (BCrypt.Net.BCrypt.Verify(req.NewPassword, dbUser.PasswordHash))
                throw new InvalidOperationException("Mật khẩu mới phải khác mật khẩu cũ!");

            dbUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.NewPassword);
            await _db.SaveChangesAsync();
        }
    }
}
