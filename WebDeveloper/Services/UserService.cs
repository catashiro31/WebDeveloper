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

        // === Hồ sơ cá nhân dùng chung ba role: đọc và cập nhật thông tin từ database. ===
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

        // === Đổi mật khẩu: xác minh mật khẩu hiện tại, không nhận mật khẩu mới giống mật khẩu cũ. ===
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
