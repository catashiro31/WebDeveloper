using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using WebDeveloper.Data;
using WebDeveloper.Helpers;
using WebDeveloper.Models.DTOs.Admin;
using WebDeveloper.Models.DTOs.Doctor;
using WebDeveloper.Models.DTOs.Patient;
using WebDeveloper.Models.DTOs.Upload;
using WebDeveloper.Models.Entities;
using WebDeveloper.Models.Enums;
using WebDeveloper.Services;
using WebDeveloper.Services.Interfaces;

var connection = Environment.GetEnvironmentVariable("DELETE_RECREATE_TEST_CONNECTION")
    ?? throw new Exception("Set DELETE_RECREATE_TEST_CONNECTION to an isolated test database.");
await using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(connection).Options);
await db.Database.MigrateAsync();
var config = new ConfigurationBuilder().Build();
var upload = new TestUpload();
var admin = new AdminService(db, upload, new EmailService(config, NullLogger<EmailService>.Instance));
var doctorService = new DoctorService(db, upload);
using var cache = new MemoryCache(new MemoryCacheOptions());
var catalog = new PublicService(db, cache);
var prefix = "Delete-test-" + Guid.NewGuid().ToString("N");
int checks = 0;
void Check(bool ok, string label) { if (!ok) throw new Exception(label); checks++; }
async Task Reject(Func<Task> action, string message)
{
    try { await action(); }
    catch (InvalidOperationException e) when (e.Message.Contains(message)) { checks++; return; }
    throw new Exception("Expected rejection: " + message);
}
IFormFile File(string mime) => new FormFile(new MemoryStream(new byte[]{1,2,3}), 0, 3, "file", "test") {Headers=new HeaderDictionary(), ContentType=mime};
FacilityRequest Facility(string name) => new() { FacilityName=name, Address="Địa chỉ thử nghiệm", LicenseFile=File("application/pdf") };
SpecialtyRequest Specialty(string name) => new() { SpecialtyName=name };

await admin.AddSpecialty(Specialty(prefix));
await admin.AddFacility(Facility(prefix));
var oldSpecialty = await db.Specialties.SingleAsync(s => s.SpecialtyName == prefix);
var oldFacility = await db.Facilities.SingleAsync(f => f.FacilityName == prefix);
await admin.DeleteSpecialty(oldSpecialty.SpecialtyId);
await admin.DeleteFacility(oldFacility.FacilityId);
Check(!(await catalog.GetAllSpecialties()).Any(s => s.Id == oldSpecialty.SpecialtyId), "Deleted specialty hidden");
Check(!(await catalog.GetAllFacilities()).Any(f => f.Id == oldFacility.FacilityId), "Deleted facility hidden");
await admin.AddSpecialty(Specialty("  " + prefix + "  "));
await admin.AddFacility(Facility("  " + prefix + "  "));
var specialty = await db.Specialties.SingleAsync(s => s.SpecialtyName == prefix && s.IsActive == true);
var facility = await db.Facilities.SingleAsync(f => f.FacilityName == prefix && f.IsActive);
Check(specialty.SpecialtyId != oldSpecialty.SpecialtyId && oldSpecialty.IsActive == false, "New specialty retains old inactive row");
Check(facility.FacilityId != oldFacility.FacilityId && !oldFacility.IsActive, "New facility retains old inactive row");
Check((await catalog.GetAllSpecialties()).Count(s => s.Name == prefix) == 1, "Only recreated specialty listed");
Check((await catalog.GetAllFacilities()).Count(f => f.Name == prefix) == 1, "Only recreated facility listed");
await Reject(() => admin.AddSpecialty(Specialty(" " + prefix.ToUpperInvariant() + " ")), "đã tồn tại");
await Reject(() => admin.AddFacility(Facility(" " + prefix.ToUpperInvariant() + " ")), "đã tồn tại");
await admin.UpdateSpecialty(specialty.SpecialtyId, Specialty(prefix));
await admin.UpdateFacility(facility.FacilityId, Facility(prefix));
await Reject(() => admin.UpdateSpecialty(oldSpecialty.SpecialtyId, Specialty(prefix+"-stale")), "đã bị xóa");
await Reject(() => admin.UpdateFacility(oldFacility.FacilityId, Facility(prefix+"-stale")), "đã bị xóa");
await admin.AddSpecialty(Specialty(prefix+"-other"));
await admin.AddFacility(Facility(prefix+"-other"));
var otherSpecialty = await db.Specialties.SingleAsync(s => s.SpecialtyName == prefix+"-other");
var otherFacility = await db.Facilities.SingleAsync(f => f.FacilityName == prefix+"-other");
await Reject(() => admin.UpdateSpecialty(otherSpecialty.SpecialtyId, Specialty(" " + prefix.ToUpperInvariant() + " ")), "đã được sử dụng");
await Reject(() => admin.UpdateFacility(otherFacility.FacilityId, Facility(" " + prefix.ToUpperInvariant() + " ")), "đã được sử dụng");
await admin.DeleteSpecialty(specialty.SpecialtyId);
await admin.DeleteFacility(facility.FacilityId);
await admin.UpdateSpecialty(otherSpecialty.SpecialtyId, Specialty(prefix));
await admin.UpdateFacility(otherFacility.FacilityId, Facility(prefix));
Check(otherSpecialty.SpecialtyName == prefix && otherFacility.FacilityName == prefix, "Rename to deleted names succeeds");

var doctor = new User {Email=prefix+"@example.test", FullName="Bác sĩ thử nghiệm", Role=RoleStatus.DOCTOR, IsActive=true};
db.Users.Add(doctor); await db.SaveChangesAsync();
DoctorProfileRequest Profile(int specialtyId) => new() {SpecialtyId=specialtyId, NewFacilityName="  "+prefix+"  ", FacilityAddress="Địa chỉ mới", FacilityLicensePdf=File("application/pdf"), Bio="Hồ sơ thử nghiệm", Degree="Bác sĩ", Price=100000, IdCardImage=File("image/png"), CertificatePdf=File("application/pdf"), DiplomaPdf=File("application/pdf")};
await Reject(() => doctorService.RegisterDoctor(doctor, Profile(oldSpecialty.SpecialtyId)), "Chuyên khoa không tồn tại");
await Reject(() => doctorService.RegisterDoctor(doctor, Profile(otherSpecialty.SpecialtyId)), "đã tồn tại");
await admin.DeleteFacility(otherFacility.FacilityId);
await doctorService.RegisterDoctor(doctor, Profile(otherSpecialty.SpecialtyId));
var detail = await db.DoctorDetails.SingleAsync(d => d.UserId == doctor.UserId);
Check(detail.FacilityId != oldFacility.FacilityId && detail.Facility.FacilityName == prefix && !detail.Facility.IsVerified, "Doctor creates fresh unverified facility after deletion");
await Reject(() => admin.DeleteSpecialty(detail.SpecialtyId), "đang có bác sĩ");
await Reject(() => admin.DeleteFacility(detail.FacilityId), "đang có bác sĩ");
detail.VerificationStatus=VerificationStatus.APPROVED; await db.SaveChangesAsync();
var scheduleRequest = new ScheduleRequest {Date=DateOnly.FromDateTime(VietnamTime.Now).AddDays(8), SlotIds=new(){Enum.GetNames<TimeSlot>()[0]}};
await doctorService.CreateSchedule(doctor, scheduleRequest);
var schedule = await db.DoctorSchedules.SingleAsync(s => s.DoctorId == detail.DoctorId);
await doctorService.DeleteSchedule(doctor, schedule.ScheduleId);
Check(schedule.SlotStatus == SlotStatus.CLOSED, "Schedule closes");
await doctorService.CreateSchedule(doctor, scheduleRequest);
Check(await db.DoctorSchedules.CountAsync(s => s.DoctorId == detail.DoctorId) == 1 && schedule.SlotStatus == SlotStatus.AVAILABLE, "Recreating schedule reopens existing slot");

var patient = new User {Email=prefix+"-patient@example.test", FullName="Bệnh nhân thử nghiệm", Role=RoleStatus.PATIENT, IsActive=true};
db.Users.Add(patient); await db.SaveChangesAsync();
var patients = new PatientService(db, null!, config);
var relativeRequest = new RelativeRequest {FullName="Nguyễn Thị An", PhoneNumber="0912345678", DateOfBirth=new DateOnly(1980,1,1), Gender=GenderStatus.FEMALE, Relationship="Mẹ"};
await patients.AddRelative(patient, relativeRequest);
var oldRelative = await db.PatientProfiles.SingleAsync(p => p.UserId == patient.UserId);
await patients.DeleteRelative(patient, oldRelative.PatientId);
await patients.AddRelative(patient, relativeRequest);
Check((await patients.GetRelatives(patient)).Count == 1, "Relative recreated after soft delete");
await Reject(() => patients.UpdateRelative(patient, oldRelative.PatientId, relativeRequest), "đã bị xóa");
await Reject(() => patients.AddRelative(patient, relativeRequest), "đã tồn tại");
// Deletion guards must keep booked appointments, self profiles and unread notifications intact.
var relative = await db.PatientProfiles.SingleAsync(p => p.UserId == patient.UserId && p.IsActive);
var self = new PatientProfile {UserId=patient.UserId, FullName=patient.FullName!, Relationship="SELF", IsActive=true};
db.PatientProfiles.Add(self);
schedule.SlotStatus=SlotStatus.BOOKED;
var appointment = new Appointment {PatientId=relative.PatientId, ScheduleId=schedule.ScheduleId, BookingStatus=BookingStatus.PENDING};
db.Appointments.Add(appointment); await db.SaveChangesAsync();
await Reject(() => patients.DeleteRelative(patient, self.PatientId), "hồ sơ chính");
await Reject(() => patients.DeleteRelative(patient, relative.PatientId), "lịch hẹn chưa hoàn thành");
await Reject(() => doctorService.DeleteSchedule(doctor, schedule.ScheduleId), "đã có người đặt");
var readNotification = new Notification {UserId=patient.UserId, Title="Đã đọc", ReadAt=DateTime.UtcNow};
var unreadNotification = new Notification {UserId=patient.UserId, Title="Chưa đọc"};
var otherNotification = new Notification {UserId=doctor.UserId, Title="Thông báo bác sĩ", ReadAt=DateTime.UtcNow};
db.Notifications.AddRange(readNotification, unreadNotification, otherNotification); await db.SaveChangesAsync();
var controller = new WebDeveloper.Controllers.Api.NotificationsController(db) {
    ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext { HttpContext = new DefaultHttpContext {
        User = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(new[]{new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, patient.UserId.ToString())}, "test"))
    }}
};
Check(await controller.Delete(unreadNotification.Id, default) is Microsoft.AspNetCore.Mvc.NotFoundResult, "Unread notification cannot be deleted");
Check(await controller.Delete(otherNotification.Id, default) is Microsoft.AspNetCore.Mvc.NotFoundResult, "Cannot delete another user's notification");
Check(await controller.Delete(readNotification.Id, default) is Microsoft.AspNetCore.Mvc.NoContentResult, "Read notification deleted");
await controller.DeleteRead(default);
Check(await db.Notifications.AnyAsync(n => n.Id == unreadNotification.Id) && await db.Notifications.AnyAsync(n => n.Id == otherNotification.Id), "Bulk delete preserves unread and other user's notifications");
// Numeric input must search phone numbers, not switch to a user-ID-only branch.
patient.PhoneNumber = "0912345678";
await db.SaveChangesAsync();
Check((await admin.GetAllUsers(0, 10, "0912345678")).TotalElements == 1, "Full numeric phone search failed");
Check((await admin.GetAllUsers(0, 10, "09123")).TotalElements == 1, "Partial numeric phone search failed");
Check((await admin.GetAllUsers(0, 10, "0912345678", RoleStatus.DOCTOR)).TotalElements == 0, "Phone search ignored role filter");
Check((await admin.GetAllUsers(0, 10, "benh nhan")).TotalElements == 1, "Accent-insensitive multiword search regressed");
Check((await admin.GetAllUsers(0, 10, "%")).TotalElements == 0, "Search treated a literal percent as a wildcard");
Console.WriteLine($"PASS: {checks} checks for delete/recreate, duplicate names, renaming, stale edits, doctor registration, linked-record protection, schedules and relatives.");

sealed class TestUpload : IFileUploadService
{
 public Task<string?> UploadFileAsync(IFormFile? file) => Task.FromResult<string?>("/test-assets/file.pdf");
 public void ValidateFile(IFormFile? file, string fieldName, params string[] supportedTypes) { if(file == null || !supportedTypes.Contains(file.ContentType)) throw new InvalidOperationException("Invalid test upload"); }
 public CloudinarySignatureResponse GenerateUploadSignature() => throw new NotSupportedException();
}
