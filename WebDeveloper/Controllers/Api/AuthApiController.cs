using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebDeveloper.Models.DTOs.Auth;
using WebDeveloper.Services.Interfaces;

namespace WebDeveloper.Controllers.Api
{
    [ApiController]
    [Route("api/v1/auth")]
    public class AuthApiController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthApiController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("signin")]
        public async Task<IActionResult> SignIn([FromBody] SignInRequest req)
        {
            try
            {
                var res = await _authService.SignIn(req);

                // Set HttpOnly Cookie for XSS protection
                var cookieOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTime.UtcNow.AddMinutes(1440) // 1 day, matching Jwt config
                };
                Response.Cookies.Append("jwtToken", res.Token, cookieOptions);

                // Hide token from response body to prevent XSS (HttpOnly cookie is used)
                res.Token = "SECURE_HTTPONLY_COOKIE";
                
                return Ok(res);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPost("signup")]
        public async Task<IActionResult> SignUp([FromBody] SignUpRequest req)
        {
            try
            {
                var msg = await _authService.SignUp(req);
                return Ok(new { Message = msg });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPost("signout")]
        [Authorize]
        public async Task<IActionResult> SignOutApp()
        {
            // Try to get token from Cookie first, then fallback to Header
            var token = Request.Cookies["jwtToken"];
            
            if (string.IsNullOrEmpty(token))
            {
                var authHeader = Request.Headers["Authorization"].FirstOrDefault();
                if (authHeader != null && authHeader.StartsWith("Bearer "))
                {
                    token = authHeader["Bearer ".Length..];
                }
            }

            if (!string.IsNullOrEmpty(token))
            {
                await _authService.SignOut(token);
                Response.Cookies.Delete("jwtToken");
                return Ok(new { Message = "Đăng xuất thành công" });
            }
            
            return BadRequest(new { Message = "Token không hợp lệ" });
        }

        [HttpGet("verify")]
        public async Task<IActionResult> VerifyAccount([FromQuery] string email, [FromQuery] string code)
        {
            try
            {
                var result = await _authService.VerifyAccount(email, code);
                return Ok(new { Message = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromQuery] string email)
        {
            try
            {
                var result = await _authService.ForgotPassword(email);
                return Ok(new { Message = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest req)
        {
            try
            {
                var result = await _authService.ResetPassword(req);
                return Ok(new { Message = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }
    }
}
