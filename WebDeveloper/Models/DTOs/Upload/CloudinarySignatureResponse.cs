namespace WebDeveloper.Models.DTOs.Upload
{
    public class CloudinarySignatureResponse
    {
        public string Signature { get; set; } = string.Empty;
        public string Timestamp { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
        public string CloudName { get; set; } = string.Empty;
    }
}
