using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebDeveloper.Data;
using WebDeveloper.Helpers;
using WebDeveloper.Models.DTOs.Doctor;
using WebDeveloper.Models.Enums;
using WebDeveloper.Services.Interfaces;

namespace WebDeveloper.Controllers.Api
{
    [ApiController]
    [Route("api/v1/doctor")]
    [Authorize]
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
        [Authorize(Roles = "DOCTOR,PATIENT")]
        public async Task<IActionResult> GetProfile()
        {
            var user = await SecurityHelper.GetCurrentUser(HttpContext, _db);
            return Ok(await _doctorService.GetProfile(user));
        }

        [HttpPost("profile")]
        [Authorize(Roles = "PATIENT")]
        public async Task<IActionResult> RegisterDoctor([FromForm] DoctorProfileRequest req)
        {
            var user = await SecurityHelper.GetCurrentUser(HttpContext, _db);
            var msg = await _doctorService.RegisterDoctor(user, req);
            return Ok(msg);
        }

        [HttpPut("profile")]
        [Authorize(Roles = "DOCTOR")]
        public async Task<IActionResult> ChangeProfile([FromBody] ChangeProfileRequest req)
        {
            var user = await SecurityHelper.GetCurrentUser(HttpContext, _db);
            return Ok(await _doctorService.ChangeProfile(user, req));
        }

        [HttpGet("schedules")]
        [Authorize(Roles = "DOCTOR")]
        public async Task<IActionResult> GetSchedules()
        {
            var user = await SecurityHelper.GetCurrentUser(HttpContext, _db);
            return Ok(await _doctorService.GetSchedules(user));
        }

        [HttpPost("schedules")]
        [Authorize(Roles = "DOCTOR")]
        public async Task<IActionResult> CreateSchedule([FromBody] ScheduleRequest req)
        {
            var user = await SecurityHelper.GetCurrentUser(HttpContext, _db);
            return Ok(await _doctorService.CreateSchedule(user, req));
        }

        [HttpDelete("schedules/{id}")]
        [Authorize(Roles = "DOCTOR")]
        public async Task<IActionResult> DeleteSchedule(int id)
        {
            var user = await SecurityHelper.GetCurrentUser(HttpContext, _db);
            return Ok(await _doctorService.DeleteSchedule(user, id));
        }

        [HttpGet("appointment")]
        [Authorize(Roles = "DOCTOR")]
        public async Task<IActionResult> GetAppointments([FromQuery] int page = 0, [FromQuery] int size = 10)
        {
            var user = await SecurityHelper.GetCurrentUser(HttpContext, _db);
            return Ok(await _doctorService.GetAppointments(user, page, size));
        }

        [HttpPut("appointment/{id}/status")]
        [Authorize(Roles = "DOCTOR")]
        public async Task<IActionResult> UpdateBookStatus(int id, [FromQuery] BookingStatus status)
        {
            var user = await SecurityHelper.GetCurrentUser(HttpContext, _db);
            if (status == BookingStatus.CONFIRMED)
                return Ok(await _doctorService.ConfirmAppointment(user, id));
            else if (status == BookingStatus.COMPLETED)
                return Ok(await _doctorService.CompleteAppointment(user, id));
            
            return BadRequest("Lỗi cập nhật trạng thái");
        }

        [HttpPost("appointment/{id}/result")]
        [Authorize(Roles = "DOCTOR")]
        public async Task<IActionResult> SaveMedicalResult(int id, [FromForm] MedicalResultRequest req)
        {
            var user = await SecurityHelper.GetCurrentUser(HttpContext, _db);
            return Ok(await _doctorService.SaveMedicalResult(user, id, req));
        }

        [HttpPut("appointment/{id}/result")]
        [Authorize(Roles = "DOCTOR")]
        public async Task<IActionResult> UpdateMedicalResult(int id, [FromForm] MedicalResultRequest req)
        {
            var user = await SecurityHelper.GetCurrentUser(HttpContext, _db);
            return Ok(await _doctorService.SaveMedicalResult(user, id, req));
        }

        [HttpGet("reviews")]
        [Authorize(Roles = "DOCTOR")]
        public async Task<IActionResult> GetReviews([FromQuery] int page = 0, [FromQuery] int size = 10)
        {
            var user = await SecurityHelper.GetCurrentUser(HttpContext, _db);
            return Ok(await _doctorService.GetReviews(user, page, size));
        }

        [HttpPost("transfer")]
        [Authorize(Roles = "DOCTOR")]
        public async Task<IActionResult> RequestTransfer([FromBody] TransferRequestDto req)
        {
            var user = await SecurityHelper.GetCurrentUser(HttpContext, _db);
            return Ok(await _doctorService.CreateTransferRequest(user, req));
        }
    }
}
