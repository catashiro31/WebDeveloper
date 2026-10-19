using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebDeveloper.Data;
using WebDeveloper.Services;
using WebDeveloper.Models.Enums;

namespace WebDeveloper.Controllers.Api;

[ApiController, Authorize(Roles = "ADMIN"), Route("api/v1/admin")]
public class AdminCatalogController(ApplicationDbContext db) : ControllerBase
{

    [HttpGet("specialties")]
    public async Task<IActionResult> Specialties(string? search)
    {
        return Ok(await db.ActiveSpecialties(search)
            .OrderBy(s => s.SpecialtyName).Select(s => new { id = s.SpecialtyId, name = s.SpecialtyName, s.Description, canDelete = !db.DoctorDetails.Any(d => d.SpecialtyId == s.SpecialtyId) }).ToListAsync());
    }

    [HttpGet("facilities")]
    public async Task<IActionResult> Facilities([FromQuery] string? search = null, [FromQuery] string? province = null)
    {
        return Ok(await db.ActiveFacilities(search, province)
            .OrderBy(f => f.FacilityName).ToFacilityResponses().ToListAsync());
    }

    [HttpGet("doctors/catalog")]
    public async Task<IActionResult> DoctorsCatalog()
    {
        return Ok(await db.DoctorDetails.AsNoTracking()
            .Where(d => d.VerificationStatus == VerificationStatus.APPROVED)
            .OrderBy(d => d.User.FullName)
            .Select(d => new { id = d.DoctorId, name = d.User.FullName, specialty = d.Specialty.SpecialtyName, facility = d.Facility.FacilityName })
            .ToListAsync());
    }
}
