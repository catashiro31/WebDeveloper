using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text.Json;
using WebDeveloper.Data;
using WebDeveloper.Models.DTOs.Admin;
using WebDeveloper.Models.DTOs.Doctor;
using WebDeveloper.Models.DTOs.Portal;
using WebDeveloper.Models.DTOs.Upload;
using WebDeveloper.Models.Entities;
using WebDeveloper.Models.Enums;
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

    // Registration must persist the selected catalog IDs. Pending profiles are
    // intentionally private; approval makes the same record publicly visible.
    using var imageStream = new MemoryStream(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });
    var image = new FormFile(imageStream, 0, imageStream.Length, "IdCardImage", "id.png")
    {
        Headers = new HeaderDictionary(), ContentType = "image/png"
    };
    await using (var db = new ApplicationDbContext(options))
    {
        var doctorUser = new User
        {
            Email = unique + "@example.test", FullName = unique,
            Role = RoleStatus.DOCTOR, IsActive = true, CreatedAt = DateTime.UtcNow
        };
        db.Users.Add(doctorUser);
        await db.SaveChangesAsync();
        await new DoctorService(db, upload).RegisterDoctor(doctorUser, new DoctorProfileRequest
        {
            SpecialtyId = specialtyId.Value, FacilityId = facilityId.Value,
            Bio = "Regression profile", Degree = "Bác sĩ", ExperienceYears = 3,
            Price = 200000, IdCardImage = image, CertificatePdf = license
        });
    }
    await using (var db = new ApplicationDbContext(options))
    {
        var doctorUser = await db.Users.SingleAsync(u => u.Email == unique + "@example.test");
        var profile = await new DoctorService(db, upload).GetProfile(doctorUser);
        Check(profile.SpecialtyId == specialtyId && profile.FacilityId == facilityId,
            "Doctor registration did not persist the selected catalog IDs.");
        Check(profile.SpecialtyName == unique + "-updated" && profile.FacilityName == unique + "-updated",
            "Doctor profile did not resolve catalog names from the database.");
        var publicService = new PublicService(db, cache);
        var pending = await publicService.GetDoctors(unique, null, null, null, null, null, null, 0, 10);
        Check(pending.TotalElements == 0, "Pending doctor incorrectly listed publicly.");
        var adminPending = JsonSerializer.SerializeToElement(await new AdminService(db, upload, email).GetPendingDoctors());
        var pendingRow = adminPending.EnumerateArray().Single(row => row.GetProperty("FullName").GetString() == unique);
        Check(pendingRow.GetProperty("VerificationStatus").GetString() == "PENDING", "Admin pending list omitted verification status.");
        await db.DoctorDetails.Where(d => d.UserId == doctorUser.UserId)
            .ExecuteUpdateAsync(s => s.SetProperty(d => d.VerificationStatus, VerificationStatus.APPROVED));
    }
    await using (var db = new ApplicationDbContext(options))
    {
        var approved = await new PublicService(db, cache)
            .GetDoctors(unique, specialtyId, facilityId, null, null, null, null, 0, 10);
        Check(approved.TotalElements == 1, "Approved doctor missing with matching catalog filters.");
        var card = approved.Content.Single();
        Check(card.SpecialtyName == unique + "-updated" && card.FacilityName == unique + "-updated",
            "Public doctor card is missing its specialty/facility name.");
        var adminApproved = await new AdminService(db, upload, email).GetAllDoctors(0, 100);
        var approvedRows = JsonSerializer.SerializeToElement(adminApproved.Content);
        var approvedRow = approvedRows.EnumerateArray().Single(row => row.GetProperty("FullName").GetString() == unique);
        Check(approvedRow.GetProperty("VerificationStatus").GetString() == "APPROVED", "Admin approved list omitted verification status.");
        var doctor = await db.DoctorDetails.Include(d => d.User).SingleAsync(d => d.User.Email == unique + "@example.test");
        doctor.User.FullName = "Đặng Lê Văn";
        doctor.Bio = "Tư vấn đau đầu";
        await db.SaveChangesAsync();
        foreach (var keyword in new[] { "  dang   VAN  ", "ĐẶNG", "dau dau", unique + "-updated" })
        {
            var found = await new PublicService(db, cache).GetDoctors(keyword, specialtyId, facilityId, null, null, null, null, 0, 10);
            Check(found.Content.Count() == 1, "Search missed keyword: " + keyword);
        }
        foreach (var keyword in new[] { "%", "_", "unmatched-keyword" })
            Check((await new PublicService(db, cache).GetDoctors(keyword, specialtyId, facilityId, null, null, null, null, 0, 10)).TotalElements == 0, "Search treated literal keyword as wildcard.");
        Console.WriteLine("PASS: case/accent insensitive multiword doctor search, specialty/facility/bio keywords, literal wildcard escaping.");
        var patient = new PatientProfile { User = doctor.User, FullName = "Người thân kiểm thử" };
        var schedule = new DoctorSchedule { Doctor = doctor, DateWorking = DateOnly.FromDateTime(DateTime.UtcNow), TimeSlot = TimeSlot.SLOT_09_00, SlotStatus = SlotStatus.BOOKED };
        var appointment = new Appointment { Patient = patient, Schedule = schedule, BookingStatus = BookingStatus.CONFIRMED };
        db.Appointments.Add(appointment);
        await db.SaveChangesAsync();
        var service = new DoctorService(db, upload);
        foreach (var invalid in new[] {
            new MedicalResultRequest { Diagnosis = "" },
            new MedicalResultRequest { Diagnosis = "Test", PrescriptionLink = "javascript:alert(1)" },
            new MedicalResultRequest { Diagnosis = "Test", FollowUpDate = schedule.DateWorking.AddDays(-1) }
        })
        {
            bool rejected = false;
            try { await service.SaveMedicalResult(doctor.User, appointment.Id, invalid); }
            catch (InvalidOperationException) { rejected = true; }
            Check(rejected && appointment.BookingStatus == BookingStatus.CONFIRMED, "Invalid result changed appointment state.");
        }
        var nextVisit = schedule.DateWorking.AddDays(7);
        await service.SaveMedicalResult(doctor.User, appointment.Id, new MedicalResultRequest {
            Diagnosis = "Chẩn đoán", DoctorNotes = "Dặn dò", PrescriptionLink = "https://example.test/result.docx", FollowUpDate = nextVisit
        });
        db.ChangeTracker.Clear();
        var saved = await db.MedicalResults.SingleAsync(r => r.AppointmentId == appointment.Id);
        Check(saved.FollowUpDate == nextVisit && saved.PrescriptionUrl == "https://example.test/result.docx", "Result fields not persisted.");
        Check(await db.Appointments.Where(a => a.Id == appointment.Id).Select(a => a.BookingStatus).SingleAsync() == BookingStatus.COMPLETED, "Result did not complete appointment.");
        foreach (var mime in new[] { "application/msword", "application/vnd.openxmlformats-officedocument.wordprocessingml.document" })
        {
            using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
            var document = new FormFile(stream, 0, stream.Length, "PrescriptionFile", "result.docx") { Headers = new HeaderDictionary(), ContentType = mime };
            await service.SaveMedicalResult(doctor.User, appointment.Id, new MedicalResultRequest { Diagnosis = "Cập nhật", PrescriptionFile = document });
        }
        db.ChangeTracker.Clear();
        saved = await db.MedicalResults.SingleAsync(r => r.AppointmentId == appointment.Id);
        Check(saved.FollowUpDate == null && saved.DoctorNotes == null && saved.PrescriptionUrl == "/test-assets/license.pdf", "Editing result did not clear optional fields or replace attachment.");
        Console.WriteLine("PASS: result validation, atomic completion, link/follow-up persistence, DOC/DOCX uploads through fake storage, optional field clearing.");
        await db.Appointments.Where(a => a.Patient.User.Email == unique + "@example.test").ExecuteDeleteAsync();
        await db.Users.Where(u => u.Email == unique + "@example.test").ExecuteDeleteAsync();
    }
    Console.WriteLine("PASS: doctor registration stores catalog IDs/names; pending stays private and approved appears with correct filters/names.");

    await Write(admin => admin.DeleteSpecialty(specialtyId.Value));
    await Write(admin => admin.DeleteFacility(facilityId.Value));
    Check(!(await Specialties()).Any(s => s.Id == specialtyId), "Deleted specialty still appears.");
    Check(!(await Facilities()).Any(f => f.Id == facilityId), "Deleted facility still appears.");
    Console.WriteLine("PASS: inactive records disappear on the next list read.");
}
finally
{
    await using var db = new ApplicationDbContext(options);
    await db.Appointments.Where(a => a.Patient.User.Email == unique + "@example.test").ExecuteDeleteAsync();
    await db.Users.Where(u => u.Email == unique + "@example.test").ExecuteDeleteAsync();
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
