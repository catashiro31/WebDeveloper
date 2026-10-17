using WebDeveloper.Helpers;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using WebDeveloper.Data;
using WebDeveloper.Models.Entities;
using WebDeveloper.Models.Enums;

namespace WebDeveloper.Controllers.Api;

[ApiController]
[Route("api/v1")]
public class AccountAppealsController(ApplicationDbContext db) : ControllerBase
{
    public record Credentials([Required, EmailAddress] string Email, [Required] string Password);
    public record SubmitRequest([Required, EmailAddress] string Email, [Required] string Password,
        [Required, StringLength(1000, MinimumLength = 10)] string Reason);
    public record Decision(bool Approve, [StringLength(1000)] string? Reason = null);

    // API công khai xác minh bằng mật khẩu nhưng không tạo phiên đăng nhập cho tài khoản bị khóa.
    private async Task<User?> Verify(string email, string password)
    {
        var user = await db.Users.SingleOrDefaultAsync(u => u.Email == email.Trim());
        return user?.PasswordHash != null && BCrypt.Net.BCrypt.Verify(password, user.PasswordHash) ? user : null;
    }

    [AllowAnonymous, EnableRateLimiting("appeals"), HttpPost("auth/appeals/status")]
    public async Task<IActionResult> Status(Credentials req)
    {
        var user = await Verify(req.Email, req.Password);
        if (user == null) return Unauthorized(new { message = "Email hoặc mật khẩu không chính xác." });
        var appeal = await db.AccountAppeals.AsNoTracking().Where(a => a.UserId == user.UserId)
            .OrderByDescending(a => a.Id).Select(a => new { a.Status, a.AdminReason, a.CreatedAt, a.ProcessedAt }).FirstOrDefaultAsync();
        return Ok(new { isActive = user.IsActive, reasonBanned = user.ReasonBanned, appeal });
    }

    [AllowAnonymous, EnableRateLimiting("appeals"), HttpPost("auth/appeals")]
    public async Task<IActionResult> Submit(SubmitRequest req)
    {
        var user = await Verify(req.Email, req.Password);
        if (user == null) return Unauthorized(new { message = "Email hoặc mật khẩu không chính xác." });
        if (user.IsActive == true || AccountState.AwaitingEmailVerification(user) || user.Role == RoleStatus.ADMIN)
            return BadRequest(new { message = "Tài khoản này không thuộc diện yêu cầu mở khóa." });
        if (req.Reason.Trim().Length < 10) return BadRequest(new { message = "Vui lòng trình bày lý do ít nhất 10 ký tự." });
        await using var tx = await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({-user.UserId})");
        await db.Entry(user).ReloadAsync();
        if (user.IsActive == true) return Conflict(new { message = "Tài khoản đã được mở khóa." });
        var existingPending = await db.AccountAppeals.FirstOrDefaultAsync(a => a.UserId == user.UserId && a.Status == "PENDING");
        if (existingPending != null)
        {
            existingPending.Reason = req.Reason.Trim();
            existingPending.CreatedAt = DateTime.UtcNow;
        }
        else
        {
            db.AccountAppeals.Add(new AccountAppeal { UserId = user.UserId, Reason = req.Reason.Trim(), CreatedAt = DateTime.UtcNow });
        }
        var senderName = string.IsNullOrWhiteSpace(user.FullName) ? user.Email : user.FullName;
        foreach (var adminId in await db.Users.Where(u => u.Role == RoleStatus.ADMIN && u.IsActive == true).Select(u => u.UserId).ToListAsync())
            db.Notifications.Add(new Notification { UserId = adminId, Title = "Yêu cầu mở khóa tài khoản", Message = $"{senderName} đã gửi phiếu xin mở khóa.", Url = "/Admin/Appeals" });
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return Ok(new { message = existingPending != null 
            ? "Đã cập nhật lại nội dung phiếu xin mở khóa của bạn thành công." 
            : "Đã gửi phiếu xin mở khóa thành công. Bạn có thể kiểm tra kết quả tại đây." });
    }

    [Authorize(Roles = "ADMIN"), HttpGet("admin/appeals/count")]
    public async Task<IActionResult> Counts()
    {
        var pending = await db.AccountAppeals.CountAsync(a => a.Status == "PENDING");
        var approved = await db.AccountAppeals.CountAsync(a => a.Status == "APPROVED");
        var rejected = await db.AccountAppeals.CountAsync(a => a.Status == "REJECTED");
        return Ok(new { pending, approved, rejected });
    }

    [Authorize(Roles = "ADMIN"), HttpGet("admin/appeals")]
    public async Task<IActionResult> List(string status = "PENDING", int page = 0)
        => Ok(await db.AccountAppeals.AsNoTracking().Where(a => a.Status == status)
            .OrderByDescending(a => a.Id).Skip(Math.Max(0, page) * 20).Take(20)
            .Select(a => new { a.Id, a.UserId, a.User.FullName, a.User.Email, a.User.ReasonBanned, a.Reason, a.Status, a.AdminReason, a.CreatedAt, a.ProcessedAt }).ToListAsync());

    [Authorize(Roles = "ADMIN"), HttpPut("admin/appeals/{id:int}")]
    public async Task<IActionResult> Process(int id, Decision req)
    {
        if (!req.Approve && (req.Reason?.Trim().Length ?? 0) < 3) return BadRequest(new { message = "Vui lòng nhập lý do từ chối (3–1000 ký tự)." });
        var decisionReason = req.Approve ? "Yêu cầu mở khóa đã được duyệt." : req.Reason!.Trim();
        await using var tx = await db.Database.BeginTransactionAsync();
        var userId = await db.AccountAppeals.Where(a => a.Id == id).Select(a => (int?)a.UserId).SingleOrDefaultAsync();
        if (userId == null) return NotFound();
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({-userId.Value})");
        var appeal = await db.AccountAppeals.Include(a => a.User).SingleAsync(a => a.Id == id);
        if (appeal.Status != "PENDING") return Conflict(new { message = "Phiếu đã được xử lý." });
        appeal.Status = req.Approve ? "APPROVED" : "REJECTED";
        appeal.AdminReason = decisionReason;
        appeal.ProcessedAt = DateTime.UtcNow;
        appeal.ProcessedBy = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        if (req.Approve)
        {
            if (AccountState.AwaitingEmailVerification(appeal.User))
                return Conflict(new { message = "Tài khoản cần xác thực email trước." });
            AccountState.ClearOneTimeCode(appeal.User);
            appeal.User.IsActive = true;
            appeal.User.ReasonBanned = null;
        }
        db.Notifications.Add(new Notification { UserId = appeal.UserId, Title = req.Approve ? "Tài khoản đã được mở khóa" : "Yêu cầu mở khóa bị từ chối", Message = decisionReason, Url = "/" });
        db.EmailOutbox.Add(new EmailOutbox { UserId = appeal.UserId, Kind = "AccountDecision", DeliveryKey = $"appeal:{appeal.Id}", Approved = req.Approve, Reason = decisionReason });
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return Ok(new { message = "Đã xử lý phiếu; email kết quả đang được gửi." });
    }
}
