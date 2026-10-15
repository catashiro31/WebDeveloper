using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebDeveloper.Data;

namespace WebDeveloper.Controllers.Api;

[ApiController, Authorize(Roles = "ADMIN"), Route("api/v1/admin")]
public class AdminCatalogController(ApplicationDbContext db) : ControllerBase
{
    private static string Pattern(string? search) => "%" + (search ?? "").Trim().Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%";
    [HttpGet("specialties")]
    public async Task<IActionResult> Specialties(string? search)
    {
        var pattern = Pattern(search);
        return Ok(await db.Specialties.AsNoTracking().Where(s => s.IsActive == true &&
            EF.Functions.ILike(EF.Functions.Unaccent(s.SpecialtyName), EF.Functions.Unaccent(pattern), "\\"))
            .OrderBy(s => s.SpecialtyName).Select(s => new { id = s.SpecialtyId, name = s.SpecialtyName, s.Description }).ToListAsync());
    }
    [HttpGet("facilities")]
    public async Task<IActionResult> Facilities(string? search)
    {
        var pattern = Pattern(search);
        return Ok(await db.Facilities.AsNoTracking().Where(f => f.IsActive &&
            EF.Functions.ILike(EF.Functions.Unaccent(f.FacilityName ?? ""), EF.Functions.Unaccent(pattern), "\\"))
            .OrderBy(f => f.FacilityName).Select(f => new { id = f.FacilityId, name = f.FacilityName, f.Address, f.Description, f.ImageUrl, f.LicenseUrl, f.MapUrl, f.Province, verified = f.IsVerified }).ToListAsync());
    }
}
