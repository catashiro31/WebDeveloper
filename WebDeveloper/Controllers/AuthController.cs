using Microsoft.AspNetCore.Mvc;

namespace WebDeveloper.Controllers
{
    [Route("Auth")]
    public class AuthController : Controller
    {
        [HttpGet("Login")]
        public IActionResult Login()
        {
            // If already logged in, redirect to home
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        [HttpGet("Register")]
        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        [HttpGet("ForgotPassword")]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpGet("VerifyAccount")]
        public IActionResult VerifyAccount()
        {
            return View();
        }

        [HttpGet("ResetPassword")]
        public IActionResult ResetPassword([FromQuery] string email, [FromQuery] string code)
        {
            ViewBag.Email = email;
            ViewBag.Code = code;
            return View();
        }
    }
}
