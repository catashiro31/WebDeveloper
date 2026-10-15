using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using WebDeveloper.Controllers.Api;
using WebDeveloper.Data;
using WebDeveloper.Models.DTOs.Auth;
using WebDeveloper.Models.Entities;
using WebDeveloper.Models.Enums;
using WebDeveloper.Security;
using WebDeveloper.Services;
using WebDeveloper.Services.Interfaces;

var connection = Environment.GetEnvironmentVariable("ACCOUNT_RECOVERY_TEST_CONNECTION")
    ?? throw new Exception("Set ACCOUNT_RECOVERY_TEST_CONNECTION to a new isolated PostgreSQL database.");
await using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(connection).Options);
await db.Database.EnsureCreatedAsync();
void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
async Task Reject(Func<Task> call, string expected) {
    try { await call(); } catch (InvalidOperationException ex) { Check(ex.Message.Contains(expected),ex.Message); return; }
    throw new Exception("Expected rejection: " + expected);
}
var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?> {
    ["Jwt:SecretKey"] = new string('x',80), ["Jwt:Issuer"]="test", ["Jwt:Audience"]="test"
}).Build();
var email = new TestEmail();
var auth = new AuthService(db,new AccessTokenProvider(config),email);
var adminService = new AdminService(db,null!,email);
var key = Guid.NewGuid().ToString("N");
var admin = new User { Email=key+"admin@example.test",Role=RoleStatus.ADMIN,IsActive=true };
db.Users.Add(admin);await db.SaveChangesAsync();
var controller = new AccountAppealsController(db) { ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext {
    User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier,admin.UserId.ToString()),new Claim(ClaimTypes.Role,"ADMIN")],"test")) } } };
foreach(var role in new[]{RoleStatus.PATIENT,RoleStatus.DOCTOR}) {
    var user = new User { Email=key+role+"@example.test",Role=role,IsActive=true,
        PasswordHash=BCrypt.Net.BCrypt.HashPassword("test-password"),VerificationCode="reset-before-ban",CodeExpiry=DateTime.UtcNow.AddHours(1) };
    db.Users.Add(user);await db.SaveChangesAsync();
    await adminService.BlockUser(user.UserId,"Admin ban regression");
    Check(user.VerificationCode==null && user.CodeExpiry==null,"New ban did not invalidate reset code");
    // Reproduce the old database state shown in the report.
    user.VerificationCode="legacy-reset";user.CodeExpiry=DateTime.UtcNow.AddHours(1);await db.SaveChangesAsync();
    await Reject(()=>auth.SignIn(new SignInRequest {Email=user.Email,Password="test-password"}),"đã bị khóa");
    await Reject(()=>auth.VerifyAccount(user.Email,"legacy-reset"),"đã bị khóa");
    Check(user.IsActive==false,"Verification bypassed the ban");
    Check(await controller.Submit(new(user.Email,"test-password","Xin xem xét mở khóa tài khoản")) is OkObjectResult,"Blocked legacy-code account cannot appeal");
    var appeal=await db.AccountAppeals.SingleAsync(a=>a.UserId==user.UserId);
    if(role==RoleStatus.PATIENT) await adminService.UnblockUser(user.UserId,"Đã xác minh tài khoản");
    else Check(await controller.Process(appeal.Id,new(true,"Đã xác minh tài khoản")) is OkObjectResult,"Appeal approval failed");
    Check(user.IsActive==true && user.VerificationCode==null && user.CodeExpiry==null && appeal.Status=="APPROVED","Unlock failed to clear stale code or resolve appeal");
    await auth.SignIn(new SignInRequest {Email=user.Email,Password="test-password"});
}
var pending = new User {Email=key+"pending@example.test",Role=RoleStatus.PATIENT,IsActive=false,VerificationCode="signup",CodeExpiry=DateTime.UtcNow.AddHours(1),PasswordHash=BCrypt.Net.BCrypt.HashPassword("test-password")};
db.Users.Add(pending);await db.SaveChangesAsync();
Check(await controller.Submit(new(pending.Email,"test-password","Xin mở khóa tài khoản")) is BadRequestObjectResult,"Unverified registration accepted as appeal");
await Reject(()=>adminService.UnblockUser(pending.UserId,"Open registration"),"xác thực email");
await auth.VerifyAccount(pending.Email,"signup");
Check(pending.IsActive==true,"Real signup verification broken");
pending.VerificationCode="reset";pending.CodeExpiry=DateTime.UtcNow.AddHours(1);await db.SaveChangesAsync();
await Reject(()=>auth.VerifyAccount(pending.Email,"reset"),"đã được xác thực");
Console.WriteLine("PASS: patient/doctor stale reset code appeals, direct/admin appeal unlock, login after unlock, code invalidation, ban verification bypass blocked, real signup verification preserved.");
sealed class TestEmail : IEmailService {
 public Task SendDoctorApprovedEmail(string email,string fullName)=>Task.CompletedTask;
 public Task SendDoctorRejectedEmail(string email,string fullName,string reason)=>Task.CompletedTask;
 public Task SendPermanentBanEmail(string email,string fullName,string reason)=>Task.CompletedTask;
 public Task SendSignUpConfirmationAsync(string email,string fullName,string code)=>Task.CompletedTask;
 public Task SendPasswordResetLinkAsync(string email,string fullName,string code)=>Task.CompletedTask;
}
