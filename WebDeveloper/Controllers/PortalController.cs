using Microsoft.AspNetCore.Mvc;
using WebDeveloper.Services.Interfaces;

namespace WebDeveloper.Controllers
{
    public class PortalController : Controller
    {
        private readonly IPublicService _publicService;

        public PortalController(IPublicService publicService)
        {
            _publicService = publicService;
        }

        public IActionResult Doctors()
        {
            return View();
        }

        public async Task<IActionResult> DoctorDetail(int id)
        {
            try
            {
                var doctor = await _publicService.GetDoctorById(id);
                var reviews = await _publicService.GetReviewsByDoctorId(id);
                var topRated = await _publicService.GetDoctors(null, null, null, null, null, null, "rating", 0, 10);
                
                ViewBag.Reviews = reviews.Take(2).ToList();
                ViewBag.TopRated = topRated.Content.ToList();
                
                return View(doctor);
            }
            catch (Exception)
            {
                return RedirectToAction("Doctors");
            }
        }

        public IActionResult Facilities()
        {
            return View();
        }

        public IActionResult Booking(int scheduleId)
        {
            ViewBag.ScheduleId = scheduleId;
            return View();
        }
    }
}
