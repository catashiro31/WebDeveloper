using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using WebDeveloper.Data;
using WebDeveloper.Models.Entities;
using WebDeveloper.Models.Enums;
using WebDeveloper.Services;
using WebDeveloper.Services.Interfaces;
using WebDeveloper.Models.DTOs.Upload;

var connection = Environment.GetEnvironmentVariable("CREDENTIAL_TEST_CONNECTION") ?? throw new Exception("Use an isolated PostgreSQL database");
await using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(connection).Options);
await db.Database.MigrateAsync();
void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
async Task Reject(Func<Task> action) { try { await action(); } catch (InvalidOperationException) { return; } throw new Exception("Expected rejection"); }
var key = Guid.NewGuid().ToString("N");
var user = new User { Email = key + "@test.invalid", Role = RoleStatus.DOCTOR, IsActive = true };
var doctor = new DoctorDetail { User = user, Degree = "Bác sĩ", CertificateUrl = "https://test.invalid/old.pdf", VerificationStatus = VerificationStatus.APPROVED,
 Specialty = new Specialty { SpecialtyName = key, IsActive = true }, Facility = new Facility { FacilityName = key, IsActive = true } };
db.Add(doctor); await db.SaveChangesAsync();
var service = new DoctorService(db, new Upload());
IFormFile File() => new FormFile(new MemoryStream("%PDF-1.4 test"u8.ToArray()), 0, 13, "document", "degree.pdf") { Headers = new HeaderDictionary(), ContentType = "application/pdf" };
Check((await service.GetProfile(user)).DiplomaUrl == null, "Legacy profile must remain readable");
await Reject(() => service.CreateCredentialRequest(user, new() { Kind = "DEGREE", Degree = "Thạc sĩ" }));
await service.CreateCredentialRequest(user, new() { Kind = "DEGREE", Degree = "Thạc sĩ", Document = File() });
var request = await db.DoctorCredentialRequests.SingleAsync(r => r.DoctorId == doctor.DoctorId);
Check(doctor.Degree == "Bác sĩ" && doctor.DiplomaUrl == null && doctor.VerificationStatus == VerificationStatus.APPROVED, "Pending request changed approved profile");
await Reject(() => service.CreateCredentialRequest(user, new() { Kind = "CERTIFICATE", Document = File() }));
await Reject(() => service.ProcessCredentialRequest(request.Id, false, " "));
await service.ProcessCredentialRequest(request.Id, false, "Bằng cấp không rõ");
Check(doctor.Degree == "Bác sĩ" && doctor.DiplomaUrl == null, "Rejection changed profile");
Check(await db.Notifications.AnyAsync(n => n.UserId == user.UserId && n.Message == "Bằng cấp không rõ"), "Rejection notification missing");
await service.CreateCredentialRequest(user, new() { Kind = "DEGREE", Degree = "Thạc sĩ", Document = File() });
request = await db.DoctorCredentialRequests.SingleAsync(r => r.DoctorId == doctor.DoctorId && r.Status == TransferStatus.PENDING);
await service.ProcessCredentialRequest(request.Id, true, null);
Check(doctor.Degree == "Thạc sĩ" && doctor.DiplomaUrl == request.NewDocumentUrl && doctor.CertificateUrl == "https://test.invalid/old.pdf", "Approval did not update only requested fields");
await Reject(() => service.ProcessCredentialRequest(request.Id, false, "Duplicate"));
await service.CreateCredentialRequest(user, new() { Kind = "CERTIFICATE", Document = File() });
request = await db.DoctorCredentialRequests.SingleAsync(r => r.DoctorId == doctor.DoctorId && r.Status == TransferStatus.PENDING);
Check(doctor.CertificateUrl == "https://test.invalid/old.pdf", "Pending certificate replaced approved certificate");
await service.ProcessCredentialRequest(request.Id, true, null);
Check(doctor.CertificateUrl == request.NewDocumentUrl && doctor.Degree == "Thạc sĩ", "Certificate approval changed degree");
await service.CreateCredentialRequest(user, new() { Kind = "DIPLOMA", Document = File() });
request = await db.DoctorCredentialRequests.SingleAsync(r => r.DoctorId == doctor.DoctorId && r.Status == TransferStatus.PENDING);
await service.ProcessCredentialRequest(request.Id, true, null);
Check(doctor.DiplomaUrl == request.NewDocumentUrl && doctor.Degree == "Thạc sĩ", "Diploma replacement changed degree");
Console.WriteLine("Credential regression passed");
sealed class Upload : IFileUploadService
{
 public Task<string?> UploadFileAsync(IFormFile? file) => Task.FromResult<string?>("https://test.invalid/" + Guid.NewGuid() + ".pdf");
 public void ValidateFile(IFormFile? file, string name, params string[] types) { if (file == null || file.Length == 0 || !types.Contains(file.ContentType)) throw new InvalidOperationException("Invalid file"); }
 public CloudinarySignatureResponse GenerateUploadSignature() => throw new NotSupportedException();
}
