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
            var authHeader = Request.Headers["Authorization"].FirstOrDefault();
            if (authHeader != null && authHeader.StartsWith("Bearer "))
            {
                var token = authHeader["Bearer ".Length..];
                await _authService.SignOut(token);
                return Ok(new { Message = "Đăng xuất thành công" });
            }
            return BadRequest(new { Message = "Token không hợp lệ" });
        }
    }
}
