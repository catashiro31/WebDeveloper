using WebDeveloper.Models.DTOs.Auth;
using WebDeveloper.Models.Entities;

namespace WebDeveloper.Services.Interfaces
{
    /// <summary>Hợp đồng kiểm tra tệp và lưu trữ Cloudinary. Xem Function_detail.md.</summary>
    public interface IFileUploadService
    {
        Task<string?> UploadFileAsync(IFormFile? file);
        void ValidateFile(IFormFile? file, string fieldName, params string[] supportedTypes);
        WebDeveloper.Models.DTOs.Upload.CloudinarySignatureResponse GenerateUploadSignature();
    }
}
