using Microsoft.AspNetCore.Mvc;

namespace WebDeveloper.Controllers
{
    public class AdminController : Controller
    {
        public IActionResult Dashboard()
        {
            return View();
        }

        public IActionResult Users()
        {
            return View();
        }

        public IActionResult Doctors()
        {
            return View();
        }

        public IActionResult Appointments()
        {
            return View();
        }

        public IActionResult Reviews()
        {
            return View();
        }

        public IActionResult Specialties()
        {
            return View();
        }

        public IActionResult Facilities()
        {
            return View();
        }
        
        public IActionResult Transfers()
        {
            return View();
        }

        public IActionResult Profile()
        {
            return View();
        }

        public IActionResult ChangePassword()
        {
            return View();
        }
    }
}
