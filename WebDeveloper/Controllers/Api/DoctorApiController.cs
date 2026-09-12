using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebDeveloper.Data;
using WebDeveloper.Helpers;
using WebDeveloper.Models.DTOs.Doctor;
using WebDeveloper.Services.Interfaces;

namespace WebDeveloper.Controllers.Api
{
    [ApiController]
    [Route("api/v1/doctor")]
    [Authorize(Roles = "DOCTOR")]
    public class DoctorApiController : ControllerBase
    {
        private readonly IDoctorService _doctorService;
        private readonly ApplicationDbContext _db;

        public DoctorApiController(IDoctorService doctorService, ApplicationDbContext db)
        {
            _doctorService = doctorService;
            _db = db;
        }

        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            var user = await SecurityHelper.GetCurrentUser(HttpContext, _db);
            return Ok(await _doctorService.GetProfile(user));
        }

        [HttpPost("register")]
        [Authorize(Roles = "PATIENT")] // Bệnh nhân đăng ký thành bác sĩ
        public async Task<IActionResult> RegisterDoctor([FromForm] DoctorProfileRequest req)
        {
            var user = await SecurityHelper.GetCurrentUser(HttpContext, _db);
            var msg = await _doctorService.RegisterDoctor(user, req);
            return Ok(msg);
        }

        [HttpPut("change-profile")]
        public async Task<IActionResult> ChangeProfile([FromBody] ChangeProfileRequest req)
        {
            var user = await SecurityHelper.GetCurrentUser(HttpContext, _db);
            return Ok(await _doctorService.ChangeProfile(user, req));
        }

        [HttpGet("schedules")]
        public async Task<IActionResult> GetSchedules()
        {
            var user = await SecurityHelper.GetCurrentUser(HttpContext, _db);
            return Ok(await _doctorService.GetSchedules(user));
        }

        [HttpPost("schedule")]
        public async Task<IActionResult> CreateSchedule([FromBody] ScheduleRequest req)
        {
            var user = await SecurityHelper.GetCurrentUser(HttpContext, _db);
            return Ok(await _doctorService.CreateSchedule(user, req));
        }

        [HttpDelete("schedule/{id}")]
        public async Task<IActionResult> DeleteSchedule(int id)
        {
            var user = await SecurityHelper.GetCurrentUser(HttpContext, _db);
            return Ok(await _doctorService.DeleteSchedule(user, id));
        }

        [HttpGet("appointments")]
        public async Task<IActionResult> GetAppointments([FromQuery] int page = 0, [FromQuery] int size = 10)
        {
            var user = await SecurityHelper.GetCurrentUser(HttpContext, _db);
            return Ok(await _doctorService.GetAppointments(user, page, size));
        }

        [HttpPut("appointment/confirm/{id}")]
        public async Task<IActionResult> ConfirmAppointment(int id)
        {
            var user = await SecurityHelper.GetCurrentUser(HttpContext, _db);
            return Ok(await _doctorService.ConfirmAppointment(user, id));
        }

        [HttpPut("appointment/complete/{id}")]
        public async Task<IActionResult> CompleteAppointment(int id)
        {
            var user = await SecurityHelper.GetCurrentUser(HttpContext, _db);
            return Ok(await _doctorService.CompleteAppointment(user, id));
        }

        [HttpPost("appointment/result/{id}")]
        public async Task<IActionResult> SaveMedicalResult(int id, [FromForm] MedicalResultRequest req)
        {
            var user = await SecurityHelper.GetCurrentUser(HttpContext, _db);
            return Ok(await _doctorService.SaveMedicalResult(user, id, req));
        }

        [HttpGet("reviews")]
        public async Task<IActionResult> GetReviews([FromQuery] int page = 0, [FromQuery] int size = 10)
        {
            var user = await SecurityHelper.GetCurrentUser(HttpContext, _db);
            return Ok(await _doctorService.GetReviews(user, page, size));
        }

        [HttpPost("transfer")]
        public async Task<IActionResult> RequestTransfer([FromBody] TransferRequestDto req)
        {
            var user = await SecurityHelper.GetCurrentUser(HttpContext, _db);
            return Ok(await _doctorService.CreateTransferRequest(user, req));
        }
    }
}
