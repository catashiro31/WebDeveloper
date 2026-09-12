using WebDeveloper.Models.DTOs.Auth;
using WebDeveloper.Models.Entities;

namespace WebDeveloper.Services.Interfaces
{
    public interface IAuthService
    {
        Task<SignInResponse> SignIn(SignInRequest request);
        Task<string> SignUp(SignUpRequest request);
        Task SignOut(string token);
    }

    public interface IUserService
    {
        Task<Models.DTOs.User.UserProfileResponse> GetProfile(User user);
        Task<Models.DTOs.User.UserProfileResponse> UpdateProfile(User user, Models.DTOs.User.UpdateProfileRequest request);
        Task ChangePassword(User user, ChangePasswordRequest request);
    }

    public interface IFileUploadService
    {
        Task<string?> UploadFileAsync(IFormFile? file);
        void ValidateFile(IFormFile? file, string fieldName, params string[] supportedTypes);
    }

    public interface IEmailService
    {
        Task SendDoctorApprovedEmail(string email, string fullName);
        Task SendDoctorRejectedEmail(string email, string fullName, string reason);
        Task SendPermanentBanEmail(string email, string fullName, string reason);
    }
}
