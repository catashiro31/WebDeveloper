using System.ComponentModel.DataAnnotations;

namespace WebDeveloper.Models.DTOs.Auth
{
    public class SignInRequest
    {
        [Required(ErrorMessage = "Email không được để trống")]
        [EmailAddress(ErrorMessage = "Định dạng Email không hợp lệ")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mật khẩu không được để trống")]
        public string Password { get; set; } = string.Empty;
    }

    public class SignUpRequest
    {
        [Required(ErrorMessage = "Email không được để trống")]
        [EmailAddress(ErrorMessage = "Email không đúng định dạng")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mật khẩu không được để trống")]
        [MinLength(6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{6,}$", ErrorMessage = "Mật khẩu phải có tối thiểu 6 ký tự, gồm ít nhất 1 chữ hoa, 1 chữ thường và 1 chữ số")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Họ tên không được để trống")]
        [StringLength(60, MinimumLength = 2, ErrorMessage = "Họ và tên phải từ 2 đến 60 ký tự")]
        [RegularExpression(@"^[\p{L}\s]{2,60}$", ErrorMessage = "Họ và tên chỉ được chứa chữ cái tiếng Việt và khoảng trắng, không bao gồm số hay ký tự đặc biệt")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Số điện thoại không được để trống")]
        [RegularExpression(@"^(03|05|07|08|09)\d{8}$", ErrorMessage = "Số điện thoại phải đúng đầu số di động Việt Nam (03, 05, 07, 08, 09) và gồm 10 chữ số")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn vai trò người dùng")]
        public Enums.RoleStatus Role { get; set; }
    }

    public class SignInResponse
    {
        public string Token { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public string Message { get; set; } = "Đăng nhập thành công!";
        public UserDto User { get; set; } = new UserDto();
    }

    public class UserDto
    {
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? AvatarUrl { get; set; }
        public string? VerificationStatus { get; set; }
    }

    public class ChangePasswordRequest
    {
        [Required(ErrorMessage = "Mật khẩu cũ không được để trống")]
        public string OldPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mật khẩu mới không được để trống")]
        [MinLength(6, ErrorMessage = "Mật khẩu mới phải có ít nhất 6 ký tự")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{6,}$", ErrorMessage = "Mật khẩu mới phải có tối thiểu 6 ký tự, gồm ít nhất 1 chữ hoa, 1 chữ thường và 1 chữ số")]
        public string NewPassword { get; set; } = string.Empty;
    }

    public class ResetPasswordRequest
    {
        [Required(ErrorMessage = "Email không được để trống")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mã xác thực không được để trống")]
        public string Code { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mật khẩu mới không được để trống")]
        [MinLength(6, ErrorMessage = "Mật khẩu mới phải có ít nhất 6 ký tự")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{6,}$", ErrorMessage = "Mật khẩu mới phải có tối thiểu 6 ký tự, gồm ít nhất 1 chữ hoa, 1 chữ thường và 1 chữ số")]
        public string NewPassword { get; set; } = string.Empty;
    }
}
