using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebDeveloper.Controllers
{
    [Authorize(Roles = "DOCTOR")]
    public class DoctorController : Controller
    {
        public IActionResult Index()
        {
            return RedirectToAction("Appointments");
        }

        public IActionResult Appointments()
        {
            return View();
        }

        public IActionResult Schedules()
        {
            return View();
        }

        public IActionResult Overdue()
        {
            return View();
        }

        public IActionResult Reviews()
        {
            return View();
        }

        public IActionResult Profile()
        {
            return View();
        }

        public IActionResult Personal()
        {
            return View();
        }

        public IActionResult ChangePassword()
        {
            return View();
        }
    }
}
