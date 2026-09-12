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

        public AuthService(ApplicationDbContext db, JwtTokenProvider jwt)
        {
            _db = db;
            _jwt = jwt;
        }

        public async Task<SignInResponse> SignIn(SignInRequest request)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (user == null)
                throw new InvalidOperationException("Tài khoản không tồn tại!");

            if (user.IsActive != true)
                throw new InvalidOperationException("Tài khoản đã bị khóa! Lý do: " + (user.ReasonBanned ?? "Không rõ"));

            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                throw new InvalidOperationException("Mật khẩu không chính xác!");

            var token = _jwt.CreateToken(user);
            return new SignInResponse
            {
                Token = token,
                User = new UserDto
                {
                    Email = user.Email ?? "",
                    FullName = user.FullName ?? "",
                    Role = user.Role.ToString() ?? "",
                    PhoneNumber = user.PhoneNumber,
                    AvatarUrl = user.AvatarUrl
                }
            };
        }

        public async Task<string> SignUp(SignUpRequest request)
        {
            if (await _db.Users.AnyAsync(u => u.Email == request.Email))
                throw new InvalidOperationException("Email đã được sử dụng!");

            var user = new User
            {
                Email = request.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                FullName = request.FullName,
                PhoneNumber = request.PhoneNumber,
                Role = request.Role,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();
            return "Đăng ký tài khoản thành công!";
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
