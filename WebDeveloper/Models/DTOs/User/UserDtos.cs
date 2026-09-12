using System.ComponentModel.DataAnnotations;

namespace WebDeveloper.Models.DTOs.User
{
    public class UpdateProfileRequest
    {
        [Required(ErrorMessage = "Họ tên không được để trống")]
        public string FullName { get; set; } = string.Empty;

        [RegularExpression(@"^[0-9]{10}$", ErrorMessage = "Số điện thoại phải gồm 10 chữ số")]
        public string? PhoneNumber { get; set; }

        public IFormFile? File { get; set; }
    }

    public class UserProfileResponse
    {
        public string? Email { get; set; }
        public string? FullName { get; set; }
        public string? PhoneNumber { get; set; }
        public string? AvatarUrl { get; set; }
        public string? Role { get; set; }
    }
}
