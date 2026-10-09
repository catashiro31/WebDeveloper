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

                var accessCookieOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTime.UtcNow.AddMinutes(15) 
                };
                Response.Cookies.Append("accessToken", res.Token, accessCookieOptions);

                var refreshCookieOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTime.UtcNow.AddDays(7)
                };
                Response.Cookies.Append("refreshToken", res.RefreshToken, refreshCookieOptions);

                // Hide tokens from response body to prevent XSS
                res.Token = "SECURE_HTTPONLY_COOKIE";
                res.RefreshToken = "";
                
                return Ok(res);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken()
        {
            try
            {
                var refreshToken = Request.Cookies["refreshToken"];

                if (string.IsNullOrEmpty(refreshToken))
                {
                    return Unauthorized(new { Message = "Tokens are missing." });
                }

                var res = await _authService.RefreshToken(refreshToken);

                var accessCookieOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTime.UtcNow.AddMinutes(15)
                };
                Response.Cookies.Append("accessToken", res.Token, accessCookieOptions);

                var refreshCookieOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTime.UtcNow.AddDays(7)
                };
                Response.Cookies.Append("refreshToken", res.RefreshToken, refreshCookieOptions);

                return Ok(new { Message = "Token refreshed successfully" });
            }
            catch (Exception ex)
            {
                return Unauthorized(new { Message = ex.Message });
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
            var token = Request.Cookies["accessToken"];
            
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
                Response.Cookies.Delete("accessToken");
                Response.Cookies.Delete("refreshToken");
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
