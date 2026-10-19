using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebDeveloper.Data;
using WebDeveloper.Helpers;
using WebDeveloper.Models.DTOs.Doctor;
using WebDeveloper.Services;

namespace WebDeveloper.Controllers.Api;

[ApiController, Authorize, Route("api/v1")]
public class ProfessionalUpdatesController(ProfessionalProfileService service, ApplicationDbContext db) : ControllerBase
{
    [HttpGet("doctor/professional-updates/current"), Authorize(Roles = "DOCTOR")]
    public async Task<IActionResult> OwnCurrent()
    {
        var actor = await SecurityHelper.GetCurrentUser(HttpContext, db);
        return Ok(await service.Current(actor, await service.OwnDoctorId(actor)));
    }
    [HttpGet("doctor/professional-updates"), Authorize(Roles = "DOCTOR")]
    public async Task<IActionResult> OwnList(int page = 0, int size = 10)
        => Ok(await service.List(await SecurityHelper.GetCurrentUser(HttpContext, db), null, null, page, size));
    [HttpPost("doctor/professional-updates"), Authorize(Roles = "DOCTOR")]
    public async Task<IActionResult> Submit([FromForm] ProfessionalUpdateRequest req)
    {
        var actor = await SecurityHelper.GetCurrentUser(HttpContext, db);
        return Ok(new { id = await service.Submit(actor, await service.OwnDoctorId(actor), req, false), message = "Đã gửi yêu cầu. Hồ sơ đã duyệt vẫn được sử dụng trong lúc chờ." });
    }
    [HttpGet("admin/professional-updates"), Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> List(int? doctorId = null, string? status = null, int page = 0, int size = 10)
        => Ok(await service.List(await SecurityHelper.GetCurrentUser(HttpContext, db), doctorId, status, page, size));
    [HttpGet("admin/professional-updates/doctors"), Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> Doctors()
        => Ok(await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(
            db.DoctorDetails.OrderBy(d => d.User.FullName).Select(d => new { id = d.DoctorId, name = d.User.FullName })));
    [HttpGet("admin/professional-updates/doctors/{doctorId:int}"), Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> Current(int doctorId)
        => Ok(await service.Current(await SecurityHelper.GetCurrentUser(HttpContext, db), doctorId));
    [HttpPost("admin/professional-updates/doctors/{doctorId:int}"), Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> Direct(int doctorId, [FromForm] ProfessionalUpdateRequest req)
        => Ok(new { id = await service.Submit(await SecurityHelper.GetCurrentUser(HttpContext, db), doctorId, req, true), message = "Đã cập nhật và lưu lịch sử thay đổi." });
    [HttpPut("admin/professional-updates/{id:long}"), Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> Decide(long id, [FromBody] ProfessionalDecision decision)
    {
        await service.Decide(await SecurityHelper.GetCurrentUser(HttpContext, db), id, decision);
        return Ok(new { message = decision.Approve ? "Đã duyệt yêu cầu." : "Đã từ chối và gửi lý do cho bác sĩ." });
    }
}
