using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebDeveloper.Data;
using WebDeveloper.Helpers;
using WebDeveloper.Models.DTOs.Auth;
using WebDeveloper.Models.DTOs.User;
using WebDeveloper.Services.Interfaces;

namespace WebDeveloper.Controllers.Api
{
    [ApiController]
    [Route("api/v1/user")]
    [Authorize]
    public class UserApiController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ApplicationDbContext _db;

        public UserApiController(IUserService userService, ApplicationDbContext db)
        {
            _userService = userService;
            _db = db;
        }

        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            var user = await SecurityHelper.GetCurrentUser(HttpContext, _db);
            return Ok(await _userService.GetProfile(user));
        }

        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromForm] UpdateProfileRequest req)
        {
            var user = await SecurityHelper.GetCurrentUser(HttpContext, _db);
            return Ok(await _userService.UpdateProfile(user, req));
        }

        [HttpPut("password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest req)
        {
            var user = await SecurityHelper.GetCurrentUser(HttpContext, _db);
            await _userService.ChangePassword(user, req);
            return Ok(new { Message = "Đổi mật khẩu thành công!" });
        }
    }
}
