using WebDeveloper.Models.DTOs.Auth;
using WebDeveloper.Models.Entities;

namespace WebDeveloper.Services.Interfaces
{
    /// <summary>Hợp đồng xác thực, phiên đăng nhập và khôi phục mật khẩu. Xem Function_detail.md.</summary>
    public interface IAuthService
    {
        Task<SignInResponse> SignIn(SignInRequest request);
        Task<SignInResponse> RefreshToken(string refreshToken, bool rotateRefreshToken = true);
        Task<string> SignUp(SignUpRequest request);
        Task SignOut(string? token, string? refreshToken);
        Task<string> VerifyAccount(string email, string code);
        Task<string> ForgotPassword(string email);
        Task<string> ResetPassword(ResetPasswordRequest request);
    }
}
