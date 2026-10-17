using System.ComponentModel.DataAnnotations;

namespace WebDeveloper.Models.DTOs.User
{
    public class UpdateProfileRequest
    {
        [Required(ErrorMessage = "Họ tên không được để trống")]
        [StringLength(60, MinimumLength = 2, ErrorMessage = "Họ và tên phải từ 2 đến 60 ký tự")]
        [RegularExpression(@"^[\p{L}\s]{2,60}$", ErrorMessage = "Họ và tên chỉ được chứa chữ cái tiếng Việt và khoảng trắng, không bao gồm số hay ký tự đặc biệt")]
        public string FullName { get; set; } = string.Empty;

        [RegularExpression(@"^(03|05|07|08|09)\d{8}$", ErrorMessage = "Số điện thoại phải đúng đầu số di động Việt Nam (03, 05, 07, 08, 09) và gồm 10 chữ số")]
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
