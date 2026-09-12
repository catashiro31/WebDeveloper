using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebDeveloper.Models.DTOs.Admin;
using WebDeveloper.Models.Enums;
using WebDeveloper.Services.Interfaces;

namespace WebDeveloper.Controllers.Api
{
    [ApiController]
    [Route("api/v1/admin")]
    [Authorize(Roles = "ADMIN")]
    public class AdminApiController : ControllerBase
    {
        private readonly IAdminService _adminService;
        private readonly IDoctorService _doctorService; // needed for transfers

        public AdminApiController(IAdminService adminService, IDoctorService doctorService)
        {
            _adminService = adminService;
            _doctorService = doctorService;
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetStats([FromQuery] DateOnly start, [FromQuery] DateOnly end)
        {
            return Ok(await _adminService.GetStats(start, end));
        }

        [HttpGet("doctor/all")]
        public async Task<IActionResult> GetAllDoctors([FromQuery] int page = 0, [FromQuery] int size = 10)
        {
            return Ok(await _adminService.GetAllDoctors(page, size));
        }

        [HttpGet("doctor/pending")]
        public async Task<IActionResult> GetPendingDoctors()
        {
            return Ok(await _adminService.GetPendingDoctors());
        }

        [HttpGet("doctor/{id}")]
        public async Task<IActionResult> GetDoctorDetail(int id)
        {
            return Ok(await _adminService.GetDoctorDetail(id));
        }

        [HttpPut("doctor/approve/{id}")]
        public async Task<IActionResult> ApproveDoctor(int id)
        {
            return Ok(await _adminService.ApproveDoctor(id));
        }

        [HttpPut("doctor/reject/{id}")]
        public async Task<IActionResult> RejectDoctor(int id, [FromBody] string reason)
        {
            return Ok(await _adminService.RejectDoctor(id, reason));
        }

        // Specialty
        [HttpPost("specialty")]
        public async Task<IActionResult> AddSpecialty([FromBody] SpecialtyRequest req) => Ok(await _adminService.AddSpecialty(req));

        [HttpPut("specialty/{id}")]
        public async Task<IActionResult> UpdateSpecialty(int id, [FromBody] SpecialtyRequest req) => Ok(await _adminService.UpdateSpecialty(id, req));

        [HttpDelete("specialty/{id}")]
        public async Task<IActionResult> DeleteSpecialty(int id) => Ok(await _adminService.DeleteSpecialty(id));

        // Facility
        [HttpPost("facility")]
        public async Task<IActionResult> AddFacility([FromForm] FacilityRequest req) => Ok(await _adminService.AddFacility(req));

        [HttpPut("facility/{id}")]
        public async Task<IActionResult> UpdateFacility(int id, [FromForm] FacilityRequest req) => Ok(await _adminService.UpdateFacility(id, req));

        [HttpPut("facility/verify/{id}")]
        public async Task<IActionResult> VerifyFacility(int id) => Ok(await _adminService.VerifyFacility(id));

        [HttpDelete("facility/{id}")]
        public async Task<IActionResult> DeleteFacility(int id) => Ok(await _adminService.DeleteFacility(id));

        // Users
        [HttpGet("user/all")]
        public async Task<IActionResult> GetAllUsers([FromQuery] int page = 0, [FromQuery] int size = 10) => Ok(await _adminService.GetAllUsers(page, size));

        [HttpPut("user/block/{id}")]
        public async Task<IActionResult> BlockUser(int id, [FromBody] string reason) => Ok(await _adminService.BlockUser(id, reason));

        [HttpPut("user/unblock/{id}")]
        public async Task<IActionResult> UnblockUser(int id) => Ok(await _adminService.UnblockUser(id));

        // Appointments & Reviews
        [HttpGet("appointment/all")]
        public async Task<IActionResult> GetAllAppointments(
            [FromQuery] DateOnly? dateFrom, [FromQuery] DateOnly? dateTo, [FromQuery] BookingStatus? status, [FromQuery] int page = 0, [FromQuery] int size = 10)
        {
            return Ok(await _adminService.GetAllAppointments(dateFrom, dateTo, status, page, size));
        }

        [HttpGet("review/all")]
        public async Task<IActionResult> GetAllReviews([FromQuery] int page = 0, [FromQuery] int size = 10)
        {
            return Ok(await _adminService.GetAllReviews(page, size));
        }

        [HttpPut("review/reject/{id}")]
        public async Task<IActionResult> RejectReview(int id) => Ok(await _adminService.RejectReview(id));

        // Doctor Transfers
        [HttpGet("transfer")]
        public async Task<IActionResult> GetTransferRequests([FromQuery] string status, [FromQuery] int page = 0, [FromQuery] int size = 10)
        {
            if (Enum.TryParse<TransferStatus>(status, true, out var transferStatus))
            {
                return Ok(await _doctorService.GetTransferRequests(transferStatus, page, size));
            }
            return BadRequest("Status không hợp lệ");
        }

        [HttpPut("transfer/approve/{id}")]
        public async Task<IActionResult> ApproveTransfer(int id, [FromBody] ProcessTransferRequest req)
        {
            await _doctorService.ApproveTransfer(id, req.AdminNote);
            return Ok("Đã duyệt chuyển công tác");
        }

        [HttpPut("transfer/reject/{id}")]
        public async Task<IActionResult> RejectTransfer(int id, [FromBody] ProcessTransferRequest req)
        {
            await _doctorService.RejectTransfer(id, req.AdminNote);
            return Ok("Đã từ chối chuyển công tác");
        }
    }
}
