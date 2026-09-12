using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebDeveloper.Data;
using WebDeveloper.Helpers;
using WebDeveloper.Models.DTOs.Patient;
using WebDeveloper.Models.Enums;
using WebDeveloper.Services.Interfaces;

namespace WebDeveloper.Controllers.Api
{
    [ApiController]
    [Route("api/v1/patient")]
    [Authorize(Roles = "PATIENT")]
    public class PatientApiController : ControllerBase
    {
        private readonly IPatientService _patientService;
        private readonly ApplicationDbContext _db;

        public PatientApiController(IPatientService patientService, ApplicationDbContext db)
        {
            _patientService = patientService;
            _db = db;
        }

        [HttpGet("relative")]
        public async Task<IActionResult> GetRelatives()
        {
            var user = await SecurityHelper.GetCurrentUser(HttpContext, _db);
            return Ok(await _patientService.GetRelatives(user));
        }

        [HttpPost("relative")]
        public async Task<IActionResult> AddRelative([FromBody] RelativeRequest req)
        {
            var user = await SecurityHelper.GetCurrentUser(HttpContext, _db);
            return Ok(await _patientService.AddRelative(user, req));
        }

        [HttpPut("relative/{id}")]
        public async Task<IActionResult> UpdateRelative(int id, [FromBody] RelativeRequest req)
        {
            var user = await SecurityHelper.GetCurrentUser(HttpContext, _db);
            return Ok(await _patientService.UpdateRelative(user, id, req));
        }

        [HttpDelete("relative/{id}")]
        public async Task<IActionResult> DeleteRelative(int id)
        {
            var user = await SecurityHelper.GetCurrentUser(HttpContext, _db);
            return Ok(await _patientService.DeleteRelative(user, id));
        }

        [HttpPost("appointment")]
        public async Task<IActionResult> BookAppointment([FromBody] AppointmentRequest req)
        {
            var user = await SecurityHelper.GetCurrentUser(HttpContext, _db);
            return Ok(await _patientService.BookAppointment(user, req));
        }

        [HttpPut("appointment/cancel/{id}")]
        public async Task<IActionResult> CancelAppointment(int id)
        {
            var user = await SecurityHelper.GetCurrentUser(HttpContext, _db);
            return Ok(await _patientService.CancelAppointment(user, id));
        }

        [HttpGet("appointments")]
        public async Task<IActionResult> GetAppointments(
            [FromQuery] BookingStatus? status, 
            [FromQuery] DateOnly? startDate, 
            [FromQuery] DateOnly? endDate, 
            [FromQuery] int page = 0, 
            [FromQuery] int size = 10)
        {
            var user = await SecurityHelper.GetCurrentUser(HttpContext, _db);
            return Ok(await _patientService.GetAppointments(user, status, startDate, endDate, page, size));
        }

        [HttpGet("appointment/{id}")]
        public async Task<IActionResult> GetAppointmentDetail(int id)
        {
            var user = await SecurityHelper.GetCurrentUser(HttpContext, _db);
            return Ok(await _patientService.GetAppointmentDetail(user, id));
        }

        [HttpPost("appointment/{id}/review")]
        public async Task<IActionResult> SubmitReview(int id, [FromBody] ReviewRequest req)
        {
            var user = await SecurityHelper.GetCurrentUser(HttpContext, _db);
            return Ok(await _patientService.SubmitReview(user, id, req));
        }
    }
}
