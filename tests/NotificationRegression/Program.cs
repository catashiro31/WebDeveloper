using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WebDeveloper.Controllers.Api;
using WebDeveloper.Data;
using WebDeveloper.Models.Entities;
using WebDeveloper.Models.Enums;

var connection = Environment.GetEnvironmentVariable("NOTIFICATION_TEST_CONNECTION")
    ?? throw new Exception("Set NOTIFICATION_TEST_CONNECTION to a migrated test database.");
var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(connection).Options;
await using var db = new ApplicationDbContext(options);
await using var transaction = await db.Database.BeginTransactionAsync();
void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
var key = Guid.NewGuid().ToString("N");
var admin = new User { Email = key + "a@example.test", Role = RoleStatus.ADMIN, IsActive = true };
var doctorUser = new User { Email = key + "d@example.test", Role = RoleStatus.DOCTOR, IsActive = true };
var patient = new User { Email = key + "p@example.test", Role = RoleStatus.PATIENT, IsActive = true };
db.Users.AddRange(admin, doctorUser, patient);
await db.SaveChangesAsync();
var doctor = new DoctorDetail { User = doctorUser,
    Specialty = new Specialty { SpecialtyName = key, IsActive = true },
    Facility = new Facility { FacilityName = key, Address = "Test", IsActive = true }, VerificationStatus = VerificationStatus.PENDING };
db.DoctorDetails.Add(doctor);
await db.SaveChangesAsync();
Check(await db.Notifications.AnyAsync(n => n.UserId == admin.UserId && n.Url == "/Admin/Doctors"), "Admin missing doctor application");
doctor.VerificationStatus = VerificationStatus.APPROVED;
await db.SaveChangesAsync();
Check(await db.Notifications.AnyAsync(n => n.UserId == doctorUser.UserId && n.Title.Contains("Hồ sơ")), "Doctor missing approval");
var schedule = new DoctorSchedule { Doctor = doctor, DateWorking = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(2), TimeSlot = TimeSlot.SLOT_09_00, SlotStatus = SlotStatus.BOOKED };
var profile = new PatientProfile { User = patient, FullName = "Test" };
db.AddRange(schedule, profile);
await db.SaveChangesAsync();
var appointment = new Appointment { Patient = profile, Schedule = schedule, BookingStatus = BookingStatus.PENDING };
db.Appointments.Add(appointment);
await db.SaveChangesAsync();
Check(await db.Notifications.AnyAsync(n => n.UserId == patient.UserId && n.Title.Contains("Đặt lịch thành công")), "Patient booking notice missing");
Check(await db.Notifications.AnyAsync(n => n.UserId == doctorUser.UserId && n.Title.Contains("đặt lịch mới")), "Doctor booking notice missing");
var count = await db.Notifications.CountAsync();
await db.SaveChangesAsync();
Check(await db.Notifications.CountAsync() == count, "Repeated save duplicated notices");
appointment.BookingStatus = BookingStatus.CONFIRMED;
await db.SaveChangesAsync();
Check(await db.Notifications.CountAsync() == count + 2, "Status event must notify both users once");
db.MedicalResults.Add(new MedicalResult { AppointmentId = appointment.Id, Diagnosis = "Test" });
appointment.BookingStatus = BookingStatus.COMPLETED;
await db.SaveChangesAsync();
Check(await db.Notifications.AnyAsync(n => n.UserId == patient.UserId && n.Url == "/Patient/History"), "Result notice missing");
var transfer = new DoctorTransferRequest { Doctor = doctor, TargetFacilityId = doctor.FacilityId, Status = TransferStatus.PENDING };
db.DoctorTransferRequests.Add(transfer);
await db.SaveChangesAsync();
Check(await db.Notifications.AnyAsync(n => n.UserId == admin.UserId && n.Url == "/Admin/Transfers"), "Transfer not sent to admin");
transfer.Status = TransferStatus.REJECTED;
await db.SaveChangesAsync();
Check(await db.Notifications.AnyAsync(n => n.UserId == doctorUser.UserId && n.Title.Contains("Chuyển công tác bị từ chối")), "Transfer result missing");
var context = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, patient.UserId.ToString()) }, "Test")) };
var api = new NotificationsController(db) { ControllerContext = new ControllerContext { HttpContext = context } };
var mine = await db.Notifications.FirstAsync(n => n.UserId == patient.UserId);
var other = await db.Notifications.FirstAsync(n => n.UserId == doctorUser.UserId);
Check(await api.Read(other.Id, default) is NotFoundResult, "Cross-user read allowed");
Check(await api.Read(mine.Id, default) is NoContentResult, "Read failed");
Check(await api.Read(mine.Id, default) is NoContentResult, "Repeated read failed");
await api.ReadAll(default);
Check(!await db.Notifications.AnyAsync(n => n.UserId == patient.UserId && n.ReadAt == null), "Mark all missed own notifications");
Check(await db.Notifications.AnyAsync(n => n.UserId == doctorUser.UserId && n.ReadAt == null), "Mark all affected another user");
// A failed business save must not leave an extra notification in the database.
count = await db.Notifications.CountAsync();
doctor.VerificationStatus = VerificationStatus.REJECTED;
doctor.Degree = new string('x', 51); // Database varchar(50) rejection.
try { await db.SaveChangesAsync(); throw new Exception("Expected save failure"); }
catch (DbUpdateException) { }
Check(await db.Notifications.CountAsync() == count, "Failed business write persisted notice");
await transaction.RollbackAsync();
Console.WriteLine("PASS: booking/status/results/profile/transfer notifications; no duplicate saves; read ownership/idempotency; atomic failure rollback. Fixtures rolled back.");
