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

        [HttpPost("relatives")]
        public async Task<IActionResult> CreateRelative([FromBody] RelativeRequest req)
        {
            var user = await SecurityHelper.GetCurrentUser(HttpContext, _db);
            return Ok(await _patientService.AddRelative(user, req));
        }

        [HttpGet("relatives/{id}")]
        public async Task<IActionResult> GetRelativeDetail(int id)
        {
            var user = await SecurityHelper.GetCurrentUser(HttpContext, _db);
            return Ok(await _patientService.GetRelativeById(user, id));
        }

        [HttpGet("relatives")]
        public async Task<IActionResult> GetAllRelatives()
        {
            var user = await SecurityHelper.GetCurrentUser(HttpContext, _db);
            return Ok(await _patientService.GetRelatives(user));
        }

        [HttpPut("relatives/{id}")]
        public async Task<IActionResult> UpdateRelative(int id, [FromBody] RelativeRequest req)
        {
            var user = await SecurityHelper.GetCurrentUser(HttpContext, _db);
            return Ok(await _patientService.UpdateRelative(user, id, req));
        }

        [HttpDelete("relatives/{id}")]
        public async Task<IActionResult> DeleteRelative(int id)
        {
            var user = await SecurityHelper.GetCurrentUser(HttpContext, _db);
            return Ok(await _patientService.DeleteRelative(user, id));
        }

        [HttpPost("appointments")]
        public async Task<IActionResult> BookAppointment([FromBody] AppointmentRequest req)
        {
            var user = await SecurityHelper.GetCurrentUser(HttpContext, _db);
            return Ok(await _patientService.BookAppointment(user, req));
        }

        [HttpGet("appointments")]
        public async Task<IActionResult> GetMyAppointments(
            [FromQuery] int page = 0,
            [FromQuery] int size = 10)
        {
            var user = await SecurityHelper.GetCurrentUser(HttpContext, _db);
            return Ok(await _patientService.GetAppointments(user, null, null, null, page, size));
        }

        [HttpPut("appointments/{id}/cancel")]
        public async Task<IActionResult> CancelAppointment(int id)
        {
            var user = await SecurityHelper.GetCurrentUser(HttpContext, _db);
            return Ok(await _patientService.CancelAppointment(user, id));
        }

        [HttpGet("history")]
        public async Task<IActionResult> GetHistory(
            [FromQuery] int page = 0,
            [FromQuery] int size = 10,
            [FromQuery] string? startDate = null,
            [FromQuery] string? endDate = null)
        {
            var user = await SecurityHelper.GetCurrentUser(HttpContext, _db);
            DateOnly? start = startDate != null ? DateOnly.Parse(startDate) : null;
            DateOnly? end = endDate != null ? DateOnly.Parse(endDate) : null;
            return Ok(await _patientService.GetAppointments(user, BookingStatus.COMPLETED, start, end, page, size));
        }

        [HttpGet("history/{id}")]
        public async Task<IActionResult> GetHistoryDetail(int id)
        {
            var user = await SecurityHelper.GetCurrentUser(HttpContext, _db);
            return Ok(await _patientService.GetAppointmentDetail(user, id));
        }

        [HttpPost("appointments/{id}/review")]
        public async Task<IActionResult> CreateReview(int id, [FromBody] ReviewRequest req)
        {
            var user = await SecurityHelper.GetCurrentUser(HttpContext, _db);
            return Ok(await _patientService.SubmitReview(user, id, req));
        }

        [HttpPut("appointments/{id}/review")]
        public async Task<IActionResult> UpdateReview(int id, [FromBody] ReviewRequest req)
        {
            var user = await SecurityHelper.GetCurrentUser(HttpContext, _db);
            return Ok(await _patientService.UpdateReview(user, id, req));
        }
    }
}
