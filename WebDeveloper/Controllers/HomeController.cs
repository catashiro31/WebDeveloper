using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using WebDeveloper.Models;
using WebDeveloper.Services.Interfaces;
using System.Threading.Tasks;

namespace WebDeveloper.Controllers
{
    public class HomeController : Controller
    {
        private readonly IPublicService _publicService;

        public HomeController(IPublicService publicService)
        {
            _publicService = publicService;
        }

        public async Task<IActionResult> Index()
        {
            var doctors = await _publicService.GetDoctors(null, null, null, null, null, null, "rating", 0, 8);
            var specialties = await _publicService.GetAllSpecialties();
            var facilities = await _publicService.GetAllFacilities();
            var stats = await _publicService.GetPortalStats();

            ViewBag.TopDoctors = doctors.Content;
            ViewBag.Specialties = specialties;
            ViewBag.Facilities = facilities;
            ViewBag.Stats = stats;

            return View();
        }

        public IActionResult About()
        {
            return View();
        }

        public IActionResult Contact()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
