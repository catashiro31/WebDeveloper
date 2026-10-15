using WebDeveloper.Models.DTOs.Auth;
using WebDeveloper.Models.Entities;

namespace WebDeveloper.Services.Interfaces
{
    /// <summary>Hợp đồng gửi email nghiệp vụ. Xem Function_detail.md.</summary>
    public interface IEmailService
    {
        Task SendDoctorApprovedEmail(string email, string fullName);
        Task SendDoctorRejectedEmail(string email, string fullName, string reason);
        Task SendPermanentBanEmail(string email, string fullName, string reason);
        Task SendSignUpConfirmationAsync(string email, string fullName, string code);
        Task SendPasswordResetLinkAsync(string email, string fullName, string code);
    }
}
