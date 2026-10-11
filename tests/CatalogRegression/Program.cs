using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using WebDeveloper.Data;
using WebDeveloper.Models.DTOs.Admin;
using WebDeveloper.Models.DTOs.Portal;
using WebDeveloper.Models.DTOs.Upload;
using WebDeveloper.Services;
using WebDeveloper.Services.Interfaces;

// Use a development/test database only. No files are uploaded to Cloudinary.
var connection = Environment.GetEnvironmentVariable("CATALOG_TEST_CONNECTION")
    ?? throw new InvalidOperationException("Set CATALOG_TEST_CONNECTION to a migrated test PostgreSQL database.");
var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(connection).Options;
using var cache = new MemoryCache(new MemoryCacheOptions());
var email = new EmailService(new ConfigurationBuilder().Build(), NullLogger<EmailService>.Instance);
var upload = new TestUploadService();
var unique = "catalog-regression-" + Guid.NewGuid().ToString("N");
int? specialtyId = null, facilityId = null;

async Task<List<SpecialtyResponse>> Specialties()
{
    await using var db = new ApplicationDbContext(options);
    return await new PublicService(db, cache).GetAllSpecialties();
}

async Task<List<FacilityResponse>> Facilities()
{
    await using var db = new ApplicationDbContext(options);
    return await new PublicService(db, cache).GetAllFacilities();
}

async Task Write(Func<AdminService, Task> action)
{
    await using var db = new ApplicationDbContext(options);
    await action(new AdminService(db, upload, email));
}

void Check(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

try
{
    // Warm the same cache used by later requests before creating either record.
    await Specialties();
    await Facilities();
    await Write(admin => admin.AddSpecialty(new SpecialtyRequest
    {
        SpecialtyName = unique, Description = "Initial description"
    }));
    using var licenseStream = new MemoryStream("%PDF-1.4\n%%EOF\n"u8.ToArray());
    var license = new FormFile(licenseStream, 0, licenseStream.Length, "LicenseFile", "license.pdf")
    {
        Headers = new HeaderDictionary(), ContentType = "application/pdf"
    };
    await Write(admin => admin.AddFacility(new FacilityRequest
    {
        FacilityName = unique, Address = "Initial address", LicenseFile = license
    }));
    await using (var db = new ApplicationDbContext(options))
    {
        specialtyId = await db.Specialties.Where(s => s.SpecialtyName == unique).Select(s => s.SpecialtyId).SingleAsync();
        facilityId = await db.Facilities.Where(f => f.FacilityName == unique).Select(f => f.FacilityId).SingleAsync();
    }
    var specialtyVisible = (await Specialties()).Any(s => s.Id == specialtyId);
    var facilityVisible = (await Facilities()).Any(f => f.Id == facilityId);
    Check(specialtyVisible && facilityVisible,
        $"Created records missing after prior list read: specialty visible = {specialtyVisible}, facility visible = {facilityVisible}.");
    Console.WriteLine("PASS: both created records appear immediately after a prior list read.");

    await Write(admin => admin.UpdateSpecialty(specialtyId.Value, new SpecialtyRequest
    {
        SpecialtyName = unique + "-updated", Description = "Updated description"
    }));
    await Write(admin => admin.UpdateFacility(facilityId.Value, new FacilityRequest
    {
        FacilityName = unique + "-updated", Address = "Updated address"
    }));
    Check((await Specialties()).Single(s => s.Id == specialtyId).Description == "Updated description", "Specialty update missing.");
    Check((await Facilities()).Single(f => f.Id == facilityId).Address == "Updated address", "Facility update missing.");
    Console.WriteLine("PASS: both updates appear on the next list read.");

    await using (var db = new ApplicationDbContext(options))
    {
        await db.Facilities.Where(f => f.FacilityId == facilityId).ExecuteUpdateAsync(s => s.SetProperty(f => f.IsVerified, false));
    }
    Check(!(await Facilities()).Single(f => f.Id == facilityId).Verified, "Pending verification state missing.");
    await Write(admin => admin.VerifyFacility(facilityId.Value));
    Check((await Facilities()).Single(f => f.Id == facilityId).Verified, "Verified facility state stale.");
    Console.WriteLine("PASS: facility verification state updates immediately.");

    await Write(admin => admin.DeleteSpecialty(specialtyId.Value));
    await Write(admin => admin.DeleteFacility(facilityId.Value));
    Check(!(await Specialties()).Any(s => s.Id == specialtyId), "Deleted specialty still appears.");
    Check(!(await Facilities()).Any(f => f.Id == facilityId), "Deleted facility still appears.");
    Console.WriteLine("PASS: inactive records disappear on the next list read.");
}
finally
{
    await using var db = new ApplicationDbContext(options);
    // Restrict cleanup to this run's two unique names, including renamed rows.
    await db.Specialties.Where(s => s.SpecialtyName == unique || s.SpecialtyName == unique + "-updated").ExecuteDeleteAsync();
    await db.Facilities.Where(f => f.FacilityName == unique || f.FacilityName == unique + "-updated").ExecuteDeleteAsync();
}

sealed class TestUploadService : IFileUploadService
{
    public Task<string?> UploadFileAsync(IFormFile? file) => Task.FromResult<string?>("/test-assets/license.pdf");
    public void ValidateFile(IFormFile? file, string fieldName, params string[] supportedTypes)
    {
        if (file == null || file.Length == 0 || !supportedTypes.Contains(file.ContentType))
            throw new InvalidOperationException("Invalid regression-test license file.");
    }
    public CloudinarySignatureResponse GenerateUploadSignature() => throw new NotSupportedException();
}
