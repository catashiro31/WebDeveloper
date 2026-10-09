using WebDeveloper.Models.DTOs.Auth;
using WebDeveloper.Models.Entities;

namespace WebDeveloper.Services.Interfaces
{
    public interface IAuthService
    {
        Task<SignInResponse> SignIn(SignInRequest request);
        Task<SignInResponse> RefreshToken(string refreshToken);
        Task<string> SignUp(SignUpRequest request);
        Task SignOut(string token);
        Task<string> VerifyAccount(string email, string code);
        Task<string> ForgotPassword(string email);
        Task<string> ResetPassword(ResetPasswordRequest request);
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
        Task SendSignUpConfirmationAsync(string email, string fullName, string code);
        Task SendPasswordResetLinkAsync(string email, string fullName, string code);
    }
}
