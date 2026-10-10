using CloudinaryDotNet;
using CloudinaryDotNet.Actions;

namespace WebDeveloper.Services
{
    /// <summary>
    /// Upload file lên Cloudinary (tương đương ConvertUrl.java)
    /// </summary>
    public class FileUploadService : Interfaces.IFileUploadService
    {
        private readonly Cloudinary _cloudinary;

        public FileUploadService(IConfiguration config)
        {
            var account = new Account(
                config["Cloudinary:CloudName"],
                config["Cloudinary:ApiKey"],
                config["Cloudinary:ApiSecret"]
            );
            _cloudinary = new Cloudinary(account);
        }

        public async Task<string?> UploadFileAsync(IFormFile? file)
        {
            if (file == null || file.Length == 0)
                return null;

            using var stream = file.OpenReadStream();
            var uploadParams = new RawUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                PublicId = Guid.NewGuid().ToString()
            };

            var result = await _cloudinary.UploadAsync(uploadParams);
            return result.SecureUrl?.ToString();
        }

        public WebDeveloper.Models.DTOs.Upload.CloudinarySignatureResponse GenerateUploadSignature()
        {
            var timestamp = Math.Round((DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds).ToString();
            
            var parameters = new SortedDictionary<string, object>
            {
                { "timestamp", timestamp },
                { "folder", "docbooking" }
            };

            var signature = _cloudinary.Api.SignParameters(parameters);

            var account = _cloudinary.Api.Account;
            return new WebDeveloper.Models.DTOs.Upload.CloudinarySignatureResponse
            {
                Signature = signature,
                Timestamp = timestamp,
                ApiKey = account.ApiKey,
                CloudName = account.Cloud
            };
        }

        public void ValidateFile(IFormFile? file, string fieldName, params string[] supportedTypes)
        {
            if (file == null || file.Length == 0) return;

            if (file.Length > 5 * 1024 * 1024)
                throw new InvalidOperationException($"{fieldName} không được vượt quá 5 MB!");

            var contentType = file.ContentType;
            if (string.IsNullOrEmpty(contentType) || !supportedTypes.Contains(contentType))
            {
                var typeNames = string.Join(", ", supportedTypes.Select(GetTypeDescription));
                throw new InvalidOperationException($"{fieldName} chỉ hỗ trợ định dạng {typeNames}!");
            }
        }

        private static string GetTypeDescription(string type) => type switch
        {
            "image/jpeg" => "JPG",
            "image/png" => "PNG",
            "application/pdf" => "PDF",
            _ => type
        };
    }
}
