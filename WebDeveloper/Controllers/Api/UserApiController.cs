using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebDeveloper.Data;
using WebDeveloper.Helpers;
using WebDeveloper.Models.DTOs.Auth;
using WebDeveloper.Models.DTOs.User;
using WebDeveloper.Services.Interfaces;
using WebDeveloper.Security;

namespace WebDeveloper.Controllers.Api
{
    [ApiController]
    [Route("api/v1/user")]
    [Authorize]
    public class UserApiController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ApplicationDbContext _db;
        private readonly AccessTokenProvider _tokens;
        private readonly AuthCookieSettings _cookieSettings;

        public UserApiController(IUserService userService, ApplicationDbContext db,
            AccessTokenProvider tokens, AuthCookieSettings cookieSettings)
        {
            _userService = userService;
            _db = db;
            _tokens = tokens;
            _cookieSettings = cookieSettings;
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
            var profile = await _userService.UpdateProfile(user, req);
            // Name and avatar are embedded in the access JWT. Replace it after
            // saving so server-rendered headers show the new values on reload.
            Response.Cookies.Append("accessToken", _tokens.CreateToken(user), new CookieOptions
            {
                HttpOnly = true,
                Secure = _cookieSettings.Secure,
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddMinutes(15)
            });
            return Ok(profile);
        }

        [HttpPut("password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest req)
        {
            var user = await SecurityHelper.GetCurrentUser(HttpContext, _db);
            await _userService.ChangePassword(user, req);
            return Ok(new { Message = "Đổi mật khẩu thành công!" });
        }

        [HttpGet("upload-signature")]
        public IActionResult GetUploadSignature([FromServices] IFileUploadService fileUploadService)
        {
            var signature = fileUploadService.GenerateUploadSignature();
            return Ok(signature);
        }
    }
}
