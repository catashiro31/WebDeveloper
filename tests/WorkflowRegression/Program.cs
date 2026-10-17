using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using WebDeveloper.Controllers.Api;
using WebDeveloper.Data;
using WebDeveloper.Helpers;
using WebDeveloper.Models.DTOs.Auth;
using WebDeveloper.Models.DTOs.Doctor;
using WebDeveloper.Models.Entities;
using WebDeveloper.Models.Enums;
using WebDeveloper.Security;
using WebDeveloper.Services;

var connection=Environment.GetEnvironmentVariable("WORKFLOW_TEST_CONNECTION") ?? throw new Exception("Use a new isolated PostgreSQL database.");
await using var db=new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(connection).Options);
await db.Database.MigrateAsync();
void Check(bool value,string message){if(!value)throw new Exception(message);}
async Task Reject(Func<Task> action){try{await action();}catch(InvalidOperationException){return;}throw new Exception("Expected rejection");}
var key=Guid.NewGuid().ToString("N");
var config=new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>{["Jwt:SecretKey"]=new string('x',80),["Jwt:Issuer"]="test",["Jwt:Audience"]="test"}).Build();
var auth=new AuthService(db,new AccessTokenProvider(config),null!); // Any inline email call would fail the test.
var email=key+"@example.test";
await auth.SignUp(new SignUpRequest {Email=email,Password="123456",FullName="Test account",PhoneNumber="0901234567",Role=RoleStatus.PATIENT});
var patient=await db.Users.SingleAsync(u=>u.Email==email);
var queued=await db.EmailOutbox.SingleAsync(e=>e.UserId==patient.UserId);
Check(queued.SentAt==null && queued.Code==patient.VerificationCode,"Sign up did not queue email atomically");
var fake=new TestEmail(config);var processor=new EmailOutboxProcessor(db,fake,NullLogger<EmailOutboxProcessor>.Instance);
fake.Fail=true;await processor.ProcessNext(default);
Check(queued.Attempts==1 && queued.SentAt==null && queued.NextAttemptAt>DateTime.UtcNow,"SMTP failure was not retried");
queued.NextAttemptAt=DateTime.UtcNow.AddSeconds(-1);await db.SaveChangesAsync();fake.Fail=false;await processor.ProcessNext(default);
Check(queued.Attempts==2 && queued.SentAt!=null && queued.Code==null,"Retry failed or token retained after delivery");
queued.CreatedAt=DateTime.UtcNow.AddMinutes(-2);await db.SaveChangesAsync();
var resend=new VerificationEmailController(db);await resend.Resend(new(email));
Check(await db.EmailOutbox.CountAsync(e=>e.UserId==patient.UserId)==2,"Resend not persisted");
await resend.Resend(new(email));Check(await db.EmailOutbox.CountAsync(e=>e.UserId==patient.UserId)==2,"Resend cooldown failed");
patient.IsActive=true;await db.SaveChangesAsync();await processor.ProcessNext(default);
Check(await db.EmailOutbox.AnyAsync(e=>e.UserId==patient.UserId && e.DiscardedAt!=null),"Stale verification delivered");
var admin=new User{Email=key+"a@test.invalid",Role=RoleStatus.ADMIN,IsActive=true};
var doctorUser=new User{Email=key+"d@test.invalid",Role=RoleStatus.DOCTOR,IsActive=true};
var doctor=new DoctorDetail{User=doctorUser,Degree="Bác sĩ",VerificationStatus=VerificationStatus.APPROVED,
 Specialty=new Specialty{SpecialtyName=key,IsActive=true},Facility=new Facility{FacilityName=key,IsActive=true}};
db.AddRange(admin,doctor);await db.SaveChangesAsync();
var service=new DoctorService(db,null!);var today=DateOnly.FromDateTime(VietnamTime.Now);
var slots=Enum.GetNames<TimeSlot>().ToList();
await Reject(()=>service.CreateSchedule(doctorUser,new(){Date=today.AddDays(6),SlotIds=slots}));
await Reject(()=>service.CreateSchedule(doctorUser,new(){Date=today.AddDays(7),SlotIds=["SLOT_09_00"]}));
await service.CreateSchedule(doctorUser,new(){Date=today.AddDays(7),EndDate=today.AddDays(20),SlotIds=slots});
Check(await db.DoctorSchedules.CountAsync(s=>s.DoctorId==doctor.DoctorId)==98,"14 full days must include weekends");
await service.CreateSchedule(doctorUser,new(){Date=today.AddDays(7),EndDate=today.AddDays(20),SlotIds=slots});
Check(await db.DoctorSchedules.CountAsync(s=>s.DoctorId==doctor.DoctorId)==98,"Duplicate bulk request duplicated slots");
var slot=await db.DoctorSchedules.FirstAsync(s=>s.DoctorId==doctor.DoctorId);
var profile=new PatientProfile{User=patient,FullName="Test"};
var appointment=new Appointment{Patient=profile,Schedule=slot,BookingStatus=BookingStatus.CONFIRMED};db.Add(appointment);await db.SaveChangesAsync();
await Reject(()=>service.CompleteAppointment(doctorUser,appointment.Id));
await Reject(()=>service.SaveMedicalResult(doctorUser,appointment.Id,new(){Diagnosis="Must not save early"}));
Check(appointment.BookingStatus==BookingStatus.CONFIRMED && !await db.MedicalResults.AnyAsync(m=>m.AppointmentId==appointment.Id),"Early result saved");
slot.DateWorking=today.AddDays(-1);await db.SaveChangesAsync();await service.SaveMedicalResult(doctorUser,appointment.Id,new(){Diagnosis="After visit"});
Check(appointment.BookingStatus==BookingStatus.COMPLETED,"Due result rejected");
Check(!DoctorDegree.IsValid("123323") && !DoctorDegree.IsValid(" ") && DoctorDegree.IsValid("Bác sĩ") && DoctorDegree.IsValid("Khác: Bác sĩ nội trú"),"Degree validation failed");
await Reject(()=>service.RegisterDoctor(doctorUser,new(){Degree="12345",Price=1}));
patient.IsActive=false;patient.ReasonBanned="Test ban";db.Add(new AccountAppeal{User=patient,Reason="Test appeal"});await db.SaveChangesAsync();
var ctx=new DefaultHttpContext{User=new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier,admin.UserId.ToString()),new Claim(ClaimTypes.Role,"ADMIN")],"test"))};
var appeals=new AccountAppealsController(db){ControllerContext=new(){HttpContext=ctx}};
var appeal=await db.AccountAppeals.SingleAsync(a=>a.UserId==patient.UserId);
Check(await appeals.Process(appeal.Id,new(false)) is BadRequestObjectResult,"Reject without reason accepted");
Check(await appeals.Process(appeal.Id,new(true)) is OkObjectResult && patient.IsActive==true,"Approval without reason failed");
Check(await db.EmailOutbox.CountAsync(e=>e.DeliveryKey=="appeal:"+appeal.Id)==1,"Decision email missing");
Check(await appeals.Process(appeal.Id,new(true)) is ConflictObjectResult,"Repeated processing accepted");
Check(await db.EmailOutbox.CountAsync(e=>e.DeliveryKey=="appeal:"+appeal.Id)==1,"Duplicate decision email");
var read=new Notification{User=admin,Title="Read",ReadAt=DateTime.UtcNow};
var unread=new Notification{User=admin,Title="Unread"};
var legacy=new Notification{User=admin,Title="Có hồ sơ bác sĩ cần duyệt",Url="/Admin/Doctors"};
var other=new Notification{User=patient,Title="Other",ReadAt=DateTime.UtcNow};db.AddRange(read,unread,legacy,other);await db.SaveChangesAsync();
var notifications=new NotificationsController(db){ControllerContext=new(){HttpContext=ctx}};
await notifications.DeleteRead(default);
Check(!await db.Notifications.AnyAsync(n=>n.Id==read.Id) && await db.Notifications.AnyAsync(n=>n.Id==unread.Id) && await db.Notifications.AnyAsync(n=>n.Id==other.Id),"Delete-read removed unread/other-user data");
var list=(OkObjectResult)await notifications.List(null,default);
Check(JsonSerializer.Serialize(list.Value).Contains("status=PENDING"),"Legacy notification link not repaired");
Console.WriteLine("PASS: durable registration and SMTP retry/resend; 7-day lead and 14 full days; early results blocked; degree validation; approval without reason/rejection required; email idempotency; unread/ownership deletion; legacy pending links.");
sealed class TestEmail(IConfiguration config):EmailService(config,NullLogger<EmailService>.Instance)
{
 public bool Fail {get;set;}
 public override Task DeliverOutbox(EmailOutbox item,CancellationToken ct)=>Fail ? Task.FromException(new IOException("Simulated SMTP outage")) : Task.CompletedTask;
}
