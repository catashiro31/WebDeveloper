using WebDeveloper.Models.DTOs.Auth;
using WebDeveloper.Models.Entities;

namespace WebDeveloper.Services.Interfaces
{
    /// <summary>Hợp đồng hồ sơ cá nhân và đổi mật khẩu. Xem Function_detail.md.</summary>
    public interface IUserService
    {
        Task<Models.DTOs.User.UserProfileResponse> GetProfile(User user);
        Task<Models.DTOs.User.UserProfileResponse> UpdateProfile(User user, Models.DTOs.User.UpdateProfileRequest request);
        Task ChangePassword(User user, ChangePasswordRequest request);
    }
}
