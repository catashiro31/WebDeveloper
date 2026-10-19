using Microsoft.EntityFrameworkCore;
using WebDeveloper.Data;
using WebDeveloper.Models.Entities;
using WebDeveloper.Models.DTOs.Portal;

namespace WebDeveloper.Services;

// Shared active-record/filter rules. Callers retain their own ordering and
// projections (admin specialties additionally expose canDelete).
public static class CatalogQueries
{
    private static string Pattern(string search) => "%" + search.Trim()
        .Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%";

    public static IQueryable<Specialty> ActiveSpecialties(this ApplicationDbContext db, string? search = null)
    {
        var query = db.Specialties.AsNoTracking().Where(s => s.IsActive == true);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = Pattern(search);
            query = query.Where(s => EF.Functions.ILike(EF.Functions.Unaccent(s.SpecialtyName),
                EF.Functions.Unaccent(pattern), "\\"));
        }
        return query;
    }

    public static IQueryable<Facility> ActiveFacilities(this ApplicationDbContext db, string? search = null, string? province = null)
    {
        var query = db.Facilities.AsNoTracking().Where(f => f.IsActive);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = Pattern(search);
            query = query.Where(f => EF.Functions.ILike(EF.Functions.Unaccent(f.FacilityName ?? ""),
                EF.Functions.Unaccent(pattern), "\\"));
        }
        if (!string.IsNullOrWhiteSpace(province)) query = query.Where(f => f.Province == province);
        return query;
    }

    public static IQueryable<FacilityResponse> ToFacilityResponses(this IQueryable<Facility> query) => query.Select(f => new FacilityResponse
    {
        Id = f.FacilityId, Name = f.FacilityName, Address = f.Address,
        Description = f.Description, ImageUrl = f.ImageUrl, LicenseUrl = f.LicenseUrl,
        MapUrl = f.MapUrl, Province = f.Province, Verified = f.IsVerified
    });
}
