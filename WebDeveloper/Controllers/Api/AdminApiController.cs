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
        private readonly IDoctorService _doctorService;

        public AdminApiController(IAdminService adminService, IDoctorService doctorService)
        {
            _adminService = adminService;
            _doctorService = doctorService;
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetStats(
            [FromQuery] DateOnly? startDate,
            [FromQuery] DateOnly? endDate)
        {
            return Ok(await _adminService.GetStats(startDate, endDate));
        }

        [HttpGet("doctor-pending")]
        public async Task<IActionResult> GetPendingDoctors()
        {
            return Ok(await _adminService.GetPendingDoctors());
        }

        [HttpGet("doctor-all")]
        public async Task<IActionResult> GetAllDoctors(
            [FromQuery] int page = 0,
            [FromQuery] int size = 10)
        {
            return Ok(await _adminService.GetAllDoctors(page, size));
        }

        [HttpGet("doctor/{id}")]
        public async Task<IActionResult> GetDoctorDetail(int id)
        {
            return Ok(await _adminService.GetDoctorDetail(id));
        }

        [HttpPut("{id}/approve")]
        public async Task<IActionResult> ApproveDoctor(int id)
        {
            try
            {
                return Ok(await _adminService.ApproveDoctor(id));
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpPut("{id}/reject")]
        public async Task<IActionResult> RejectDoctor(int id, [FromQuery] string reason)
        {
            try
            {
                return Ok(await _adminService.RejectDoctor(id, reason));
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        // Specialty
        [HttpPost("specialty")]
        public async Task<IActionResult> AddSpecialty([FromBody] SpecialtyRequest req)
            => Ok(await _adminService.AddSpecialty(req));

        [HttpPut("specialty/{id}")]
        public async Task<IActionResult> UpdateSpecialty(int id, [FromBody] SpecialtyRequest req)
            => Ok(await _adminService.UpdateSpecialty(id, req));

        [HttpDelete("specialty/{id}")]
        public async Task<IActionResult> DeleteSpecialty(int id)
            => Ok(await _adminService.DeleteSpecialty(id));

        // Facility
        [HttpPost("facility")]
        public async Task<IActionResult> AddFacility([FromForm] FacilityRequest req)
            => Ok(await _adminService.AddFacility(req));

        [HttpPut("facility/{id}")]
        public async Task<IActionResult> UpdateFacility(int id, [FromForm] FacilityRequest req)
            => Ok(await _adminService.UpdateFacility(id, req));

        [HttpPatch("facility/{id}/verify")]
        public async Task<IActionResult> VerifyFacility(int id)
        {
            try
            {
                return Ok(await _adminService.VerifyFacility(id));
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpDelete("facility/{id}")]
        public async Task<IActionResult> DeleteFacility(int id)
            => Ok(await _adminService.DeleteFacility(id));

        // Users
        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers(
            [FromQuery] int page = 0,
            [FromQuery] int size = 10)
            => Ok(await _adminService.GetAllUsers(page, size));

        [HttpPatch("users/{id}/block")]
        public async Task<IActionResult> BlockUser(int id, [FromQuery] string reason)
            => Ok(await _adminService.BlockUser(id, reason));

        [HttpPatch("users/{id}/unblock")]
        public async Task<IActionResult> UnblockUser(int id)
            => Ok(await _adminService.UnblockUser(id));

        // Appointments
        [HttpGet("appointments")]
        public async Task<IActionResult> GetAllAppointments(
            [FromQuery] DateOnly? dateFrom,
            [FromQuery] DateOnly? dateTo,
            [FromQuery] BookingStatus? status,
            [FromQuery] int page = 0,
            [FromQuery] int size = 10)
        {
            return Ok(await _adminService.GetAllAppointments(dateFrom, dateTo, status, page, size));
        }

        // Reviews
        [HttpGet("reviews")]
        public async Task<IActionResult> GetAllReviews(
            [FromQuery] int page = 0,
            [FromQuery] int size = 10)
        {
            return Ok(await _adminService.GetAllReviews(page, size));
        }

        [HttpPatch("reviews/{id}/hide")]
        public async Task<IActionResult> RejectReview(int id)
            => Ok(await _adminService.RejectReview(id));

        // Moderation
        [HttpPost("moderation/analyze")]
        public IActionResult AnalyzeComment([FromBody] Dictionary<string, string> request)
        {
            if (!request.TryGetValue("comment", out var commentText) || string.IsNullOrWhiteSpace(commentText))
                return BadRequest(new { error = "Bình luận không hợp lệ" });

            var results = _adminService.AnalyzeComment(commentText);
            return Ok(new { comment = commentText, labels = results });
        }

        [HttpGet("transfers")]
        public async Task<IActionResult> GetTransferRequests(
            [FromQuery] string status = "PENDING",
            [FromQuery] int page = 0,
            [FromQuery] int size = 10)
        {
            if (Enum.TryParse<TransferStatus>(status, true, out var transferStatus))
            {
                return Ok(await _doctorService.GetTransferRequests(transferStatus, page, size));
            }
            return BadRequest("Trạng thái không hợp lệ");
        }


        [HttpPut("transfers/{id}/approve")]
        public async Task<IActionResult> ApproveTransfer(int id, [FromBody] ProcessTransferRequest req)
        {
            await _doctorService.ApproveTransfer(id, req.AdminNote);
            return Ok("Đã duyệt chuyển công tác thành công!");
        }

        [HttpPut("transfers/{id}/reject")]
        public async Task<IActionResult> RejectTransfer(int id, [FromBody] ProcessTransferRequest req)
        {
            await _doctorService.RejectTransfer(id, req.AdminNote);
            return Ok("Đã từ chối chuyển công tác!");
        }
    }
}
