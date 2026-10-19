using System.Data.Common;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using StackExchange.Redis;
using WebDeveloper.Controllers.Api;
using WebDeveloper.Data;
using WebDeveloper.Helpers;
using WebDeveloper.Models.DTOs.Doctor;
using WebDeveloper.Models.DTOs.Patient;
using WebDeveloper.Models.Entities;
using WebDeveloper.Models.Enums;
using WebDeveloper.Services;
using WebDeveloper.Services.Interfaces;

var connection = Environment.GetEnvironmentVariable("AUDIT_TEST_CONNECTION")
    ?? throw new Exception("Set AUDIT_TEST_CONNECTION to a new isolated PostgreSQL database.");
var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(connection).Options;
await using var db = new ApplicationDbContext(options);
await db.Database.MigrateAsync();
GlobalConfiguration.Configuration.UsePostgreSqlStorage(o => o.UseNpgsqlConnection(connection));
using var redis = await ConnectionMultiplexer.ConnectAsync(Environment.GetEnvironmentVariable("AUDIT_TEST_REDIS") ?? "127.0.0.1:6379");
var config = new ConfigurationBuilder().Build();
var email = new FakeEmail();
var adminService = new AdminService(db, null!, email);
var doctors = new DoctorService(db, null!);
var patients = new PatientService(db, redis, config);
var jobs = new AppointmentJobService(db);
var today = DateOnly.FromDateTime(VietnamTime.Now);
var key = Guid.NewGuid().ToString("N");
int assertions = 0;
void Check(bool condition, string message) { if (!condition) throw new Exception(message); assertions++; }
async Task Reject(Func<Task> call, string text)
{
    try { await call(); }
    catch (InvalidOperationException e) when (e.Message.Contains(text)) { assertions++; return; }
    throw new Exception("Expected rejection containing: " + text);
}
User User(string name, RoleStatus role) => new() { Email = name + key + "@example.test", FullName = name,
    Role = role, IsActive = true, PasswordHash = BCrypt.Net.BCrypt.HashPassword("AuditTest123!") };
var admin = User("admin", RoleStatus.ADMIN);
var patient = User("patient", RoleStatus.PATIENT);
var doctorUser = User("doctor", RoleStatus.DOCTOR);
var doctor2User = User("doctor2", RoleStatus.DOCTOR);
var spec = new Specialty { SpecialtyName = key, IsActive = true };
var facility = new Facility { FacilityName = key, IsActive = true, IsVerified = true };
var doctor = new DoctorDetail { User = doctorUser, Specialty = spec, Facility = facility, VerificationStatus = VerificationStatus.APPROVED, Price = 150000 };
var doctor2 = new DoctorDetail { User = doctor2User, Specialty = spec, Facility = facility, VerificationStatus = VerificationStatus.APPROVED, Price = 200000 };
var profile = new PatientProfile { User = patient, FullName = "Audit patient" };
db.AddRange(admin, doctor, doctor2, profile);
await db.SaveChangesAsync();
DoctorSchedule Slot(DoctorDetail d, int day, TimeSlot time = TimeSlot.SLOT_09_00) => new() {
    Doctor = d, Facility = facility, DateWorking = today.AddDays(day), TimeSlot = time, SlotStatus = SlotStatus.AVAILABLE };
Appointment Appointment(DoctorSchedule s, BookingStatus status, DateTime? created = null) => new() {
    Patient = profile, Schedule = s, BookingStatus = status, CreatedAt = created ?? DateTime.UtcNow, Reason = "Audit regression fixture" };
async Task Book(int scheduleId)
{
    await using var fresh = new ApplicationDbContext(options);
    await new PatientService(fresh, redis, config).BookAppointment(patient,
        new AppointmentRequest { PatientId = profile.PatientId, ScheduleId = scheduleId, Reason = "Kiểm tra lịch khám đồng thời" });
}

// Pending doctors and blocked doctors cannot receive new bookings.
var pendingSlot = Slot(doctor, 8);
db.Add(pendingSlot); doctor.VerificationStatus = VerificationStatus.PENDING; await db.SaveChangesAsync();
await Reject(() => Book(pendingSlot.ScheduleId), "chưa được xác minh");
var pendingAppointment = Appointment(pendingSlot, BookingStatus.PENDING);
db.Add(pendingAppointment); await db.SaveChangesAsync();
await Reject(() => doctors.ConfirmAppointment(doctorUser, pendingAppointment.Id), "chưa được duyệt");
pendingAppointment.BookingStatus = BookingStatus.CANCELLED;
doctor.VerificationStatus = VerificationStatus.APPROVED; doctorUser.IsActive = false; await db.SaveChangesAsync();
await Reject(() => Book(pendingSlot.ScheduleId), "bị khóa");
doctorUser.IsActive = true; await db.SaveChangesAsync();

// Same patient/time across doctors, same doctor/day, and concurrent different-slot requests.
var first = Slot(doctor, 9); var sameTime = Slot(doctor2, 9); var sameDoctorDay = Slot(doctor, 9, TimeSlot.SLOT_10_00);
db.AddRange(first, sameTime, sameDoctorDay); await db.SaveChangesAsync();
await Book(first.ScheduleId);
await Reject(() => Book(sameTime.ScheduleId), "cùng thời gian");
await Reject(() => Book(sameDoctorDay.ScheduleId), "trong ngày");
var race1 = Slot(doctor, 10); var race2 = Slot(doctor2, 10); db.AddRange(race1, race2); await db.SaveChangesAsync();
async Task<bool> TryBook(int id) { try { await Book(id); return true; } catch (InvalidOperationException) { return false; } }
var race = await Task.WhenAll(TryBook(race1.ScheduleId), TryBook(race2.ScheduleId));
Check(race.Count(x => x) == 1, "Concurrent overlap accepted");
Check(await db.Appointments.CountAsync(a => a.PatientId == profile.PatientId && a.Schedule.DateWorking == today.AddDays(10) && a.BookingStatus == BookingStatus.PENDING) == 1, "Concurrent booking persisted duplicates");
Console.WriteLine("PASS: pending/blocked doctors rejected; same time, same doctor/day and concurrent overlaps rejected.");

// Time guards and result presence are independent of COMPLETED state.
var future = Appointment(Slot(doctor, 11), BookingStatus.CONFIRMED);
var past = Appointment(Slot(doctor, -1), BookingStatus.CONFIRMED);
db.AddRange(future, past); await db.SaveChangesAsync();
await Reject(() => doctors.DeclineAppointment(doctorUser, future.Id, BookingStatus.NO_SHOW), "Chưa đến giờ");
await Reject(() => doctors.CompleteAppointment(doctorUser, future.Id), "Chưa đến giờ");
await Reject(() => doctors.SaveMedicalResult(doctorUser, future.Id, new MedicalResultRequest { Diagnosis = "Test" }), "Chưa đến giờ");
await doctors.CompleteAppointment(doctorUser, past.Id);
Check(!(await patients.GetAppointmentDetail(patient, past.Id)).HasResult, "Completion fabricated a result");
await doctors.SaveMedicalResult(doctorUser, past.Id, new MedicalResultRequest { Diagnosis = "Kết quả kiểm thử" });
Check((await patients.GetAppointmentDetail(patient, past.Id)).HasResult, "Saved result is missing");
var noResult = Appointment(Slot(doctor2, -1), BookingStatus.COMPLETED); db.Add(noResult); await db.SaveChangesAsync();
Check(!(await patients.GetAppointments(patient, BookingStatus.COMPLETED, null, null, 0, 100)).Content.Single(a => a.AppointmentId == noResult.Id).HasResult, "History result flag wrong");
Console.WriteLine("PASS: early no-show/completion/result rejected; history distinguishes missing and saved results.");

// Sweep recovers jobs not enqueued, does not reopen elapsed slots or cancel confirmed/new bookings.
var expired = Appointment(Slot(doctor, 12), BookingStatus.PENDING, DateTime.UtcNow.AddMinutes(-31));
var elapsed = Appointment(Slot(doctor, -2), BookingStatus.PENDING);
var freshPending = Appointment(Slot(doctor, 13), BookingStatus.PENDING);
var confirmed = Appointment(Slot(doctor, 14), BookingStatus.CONFIRMED, DateTime.UtcNow.AddHours(-1));
foreach (var a in new[] {expired, elapsed, freshPending, confirmed}) { a.Schedule.SlotStatus = SlotStatus.BOOKED; db.Add(a); }
await db.SaveChangesAsync();
await Reject(() => doctors.ConfirmAppointment(doctorUser, expired.Id), "quá thời hạn");
await jobs.CancelOverdueAppointments();
Check(expired.BookingStatus == BookingStatus.CANCELLED && expired.Schedule.SlotStatus == SlotStatus.AVAILABLE, "Expired booking not released");
Check(elapsed.BookingStatus == BookingStatus.CANCELLED && elapsed.Schedule.SlotStatus == SlotStatus.CLOSED, "Past slot reopened");
Check(freshPending.BookingStatus == BookingStatus.PENDING && confirmed.BookingStatus == BookingStatus.CONFIRMED, "Sweep cancelled valid booking");
var notificationCount = await db.Notifications.CountAsync();
await jobs.CancelOverdueAppointments();
Check(notificationCount == await db.Notifications.CountAsync(), "Sweep duplicated notifications");
Console.WriteLine("PASS: overdue sweep, time-bound slot release, fresh/confirmed preservation and repeatability.");

// Soft deletion must reject an in-use specialty; unused entries remain stored but inactive.
await Reject(() => adminService.DeleteSpecialty(spec.SpecialtyId), "đang có bác sĩ");
var unused = new Specialty { SpecialtyName = key + "unused", IsActive = true }; db.Add(unused); await db.SaveChangesAsync();
await adminService.DeleteSpecialty(unused.SpecialtyId);
Check(await db.Specialties.AnyAsync(s => s.SpecialtyId == unused.SpecialtyId && s.IsActive == false), "Specialty physically deleted");
var catalog = (OkObjectResult)await new AdminCatalogController(db).Specialties(key);
var json = System.Text.Json.JsonSerializer.SerializeToElement(catalog.Value);
Check(json.EnumerateArray().Single(s => s.GetProperty("id").GetInt32() == spec.SpecialtyId).GetProperty("canDelete").GetBoolean() == false, "In-use specialty deletion enabled in catalog");

// All statuses must be returned when the admin omits the status filter.
var all = await adminService.GetAllAppointments(null, null, null, 0, 1000);
var cancelled = await adminService.GetAllAppointments(null, null, BookingStatus.CANCELLED, 0, 1000);
Check(all.TotalElements == await db.Appointments.CountAsync(), "All tab omits appointments");
Check(cancelled.Content.All(a => a.BookingStatus == BookingStatus.CANCELLED) && all.TotalElements > cancelled.TotalElements, "Admin status filter broken");
Console.WriteLine("PASS: in-use specialty protected, unused specialty soft deleted, admin All/filter counts.");

// Deterministic period statistics; no fabricated collected payment amount.
var metricDate = new DateOnly(2050, 1, 1);
foreach (var status in new[] {BookingStatus.COMPLETED, BookingStatus.CANCELLED, BookingStatus.NO_SHOW, BookingStatus.CONFIRMED}) {
    var slot = Slot(doctor, 1); slot.DateWorking = metricDate.AddDays((int)status);
    db.Add(Appointment(slot, status));
}
await db.SaveChangesAsync();
var stats = await adminService.GetStats(metricDate, metricDate.AddDays(10));
Check(stats.PeriodAppointments == 4 && stats.CancellationRate == 25 && stats.NoShowRate == 25, "Wrong rate denominator");
Check(stats.EstimatedRevenue == 150000 && stats.DoctorPerformance.Single().CompletionRate == 25, "Wrong estimated revenue/performance");
var empty = await adminService.GetStats(metricDate.AddYears(1), metricDate.AddYears(1));
Check(empty.EstimatedRevenue == 0 && empty.CancellationRate == 0 && empty.NoShowRate == 0 && empty.DoctorPerformance.Count == 0, "Empty period not handled");
await Reject(() => adminService.GetStats(metricDate.AddDays(1), metricDate), "Ngày bắt đầu");
Console.WriteLine("PASS: period filtering, estimated revenue, cancellation/no-show/completion rates and empty periods.");

// Blocking a doctor cancels active appointments, closes booked and available slots, keeps history.
var completedId = past.Id;
foreach (var entry in db.ChangeTracker.Entries<DoctorSchedule>().ToList()) await entry.ReloadAsync();
await adminService.BlockUser(doctorUser.UserId, "Kiểm thử khóa bác sĩ");
Check(!await db.Appointments.AnyAsync(a => a.Schedule.DoctorId == doctor.DoctorId && (a.BookingStatus == BookingStatus.PENDING || a.BookingStatus == BookingStatus.CONFIRMED)), "Doctor block left active appointments");
Check(!await db.DoctorSchedules.AnyAsync(s => s.DoctorId == doctor.DoctorId && s.SlotStatus == SlotStatus.AVAILABLE), "Doctor block left available slots");
Check(confirmed.Schedule.SlotStatus == SlotStatus.CLOSED && confirmed.CancelledBy == "ADMIN", "Doctor booked slot left open or cancellation missing attribution");
Check((await db.Appointments.FindAsync(completedId))!.BookingStatus == BookingStatus.COMPLETED, "Doctor block modified medical history");
Check(await db.Notifications.AnyAsync(n => n.UserId == patient.UserId && n.Url != null), "Patient has no cancellation notification");
// A patient block must close past slots and release future ones.
var pastPatient = Appointment(Slot(doctor2, -3), BookingStatus.CONFIRMED);
var futurePatient = Appointment(Slot(doctor2, 15), BookingStatus.CONFIRMED);
db.AddRange(pastPatient, futurePatient); await db.SaveChangesAsync();
await adminService.BlockUser(patient.UserId, "Kiểm thử khóa bệnh nhân");
Check(pastPatient.Schedule.SlotStatus == SlotStatus.CLOSED && futurePatient.Schedule.SlotStatus == SlotStatus.AVAILABLE, "Patient block slot timing wrong");
Console.WriteLine("PASS: doctor/patient blocking cancels appointments, preserves history and updates slot availability.");
// Leave isolated browser fixtures usable. No real email is sent.
await adminService.UnblockUser(patient.UserId, "Kết thúc kiểm thử khóa");
await adminService.UnblockUser(doctorUser.UserId, "Kết thúc kiểm thử khóa");
// Patient booking horizon matches the doctor's inclusive 30-day schedule limit.
var lastDaySlot = Slot(doctor, BookingWindow.MaxAdvanceDays);
var outsideWindow = Slot(doctor, BookingWindow.MaxAdvanceDays + 1);
db.AddRange(lastDaySlot, outsideWindow); await db.SaveChangesAsync();
using var cache = new Microsoft.Extensions.Caching.Memory.MemoryCache(new Microsoft.Extensions.Caching.Memory.MemoryCacheOptions());
var portal = new PublicService(db, cache);
Check((await portal.GetAvailableSlots(doctor.DoctorId, today.AddDays(30))).Any(s => s.ScheduleId == lastDaySlot.ScheduleId), "Day 30 unavailable");
Check((await portal.GetAvailableSlots(doctor.DoctorId, today.AddDays(31))).Count == 0, "Day 31 exposed");
await Reject(() => Book(outsideWindow.ScheduleId), "30 ngày");
await Book(lastDaySlot.ScheduleId);
Check(await db.Appointments.AnyAsync(a => a.ScheduleId == lastDaySlot.ScheduleId && a.BookingStatus == BookingStatus.PENDING), "Day 30 booking failed");
// Keep another slot available on the last day for the browser's calendar check.
var browserLastSlot = Slot(doctor, 30, TimeSlot.SLOT_17_00); db.Add(browserLastSlot); await db.SaveChangesAsync();
Console.WriteLine("PASS: day 30 listing/booking accepted; day 31 listing/booking rejected.");
// Verify the scoped FindAsync contract used by middleware, helpers and _AuthState.
var reads = new ReadCounter();
await using (var requestDb = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
    .UseNpgsql(connection).AddInterceptors(reads).Options))
{
    var context = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(
        new[] { new Claim(ClaimTypes.NameIdentifier, patient.UserId.ToString()) }, "test")) };
    var middlewareUser = await requestDb.Users.FindAsync(patient.UserId);
    var helperUser = await SecurityHelper.GetCurrentUser(context, requestDb);
    await new UserService(requestDb, null!).GetProfile(helperUser);
    var layoutUser = await requestDb.Users.FindAsync(patient.UserId);
    Check(ReferenceEquals(middlewareUser, helperUser) && ReferenceEquals(helperUser, layoutUser), "Current user was not reused within request");
    Check(reads.Count == 1, "Repeated current-user access executed redundant SQL");
}
Console.WriteLine("PASS: middleware/helper/service/layout current-user lookup shares one SQL query per request.");
await File.WriteAllTextAsync("/tmp/audit-browser-fixtures.json", System.Text.Json.JsonSerializer.Serialize(new {
    admin = admin.Email, patient = patient.Email, doctor = doctorUser.Email, noResultId = noResult.Id, resultId = past.Id,
    specialtyId = spec.SpecialtyId, doctorId = doctor.DoctorId, lastDate = today.AddDays(30).ToString("yyyy-MM-dd"), allCount = await db.Appointments.CountAsync()
}));
Console.WriteLine($"PASS: {assertions} assertions completed.");

sealed class FakeEmail : IEmailService
{
    public Task SendDoctorApprovedEmail(string a, string b) => Task.CompletedTask;
    public Task SendDoctorRejectedEmail(string a, string b, string c) => Task.CompletedTask;
    public Task SendPermanentBanEmail(string a, string b, string c) => Task.CompletedTask;
    public Task SendSignUpConfirmationAsync(string a, string b, string c) => Task.CompletedTask;
    public Task SendPasswordResetLinkAsync(string a, string b, string c) => Task.CompletedTask;
}

sealed class ReadCounter : DbCommandInterceptor
{
    public int Count { get; private set; }
    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        Count++;
        return ValueTask.FromResult(result);
    }
}
