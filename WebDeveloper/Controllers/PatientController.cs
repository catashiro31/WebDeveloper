using Microsoft.AspNetCore.Mvc;

namespace WebDeveloper.Controllers
{
    public class PatientController : Controller
    {
        public IActionResult Index()
        {
            return RedirectToAction("Appointments");
        }

        public IActionResult Appointments()
        {
            return View();
        }

        public IActionResult History()
        {
            return View();
        }

        public IActionResult Relatives()
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
