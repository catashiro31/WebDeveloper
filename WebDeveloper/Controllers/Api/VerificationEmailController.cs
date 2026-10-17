using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using WebDeveloper.Data;
using WebDeveloper.Helpers;
using WebDeveloper.Models.Entities;
namespace WebDeveloper.Controllers.Api;

[ApiController, AllowAnonymous, Route("api/v1/auth/resend-verification"), EnableRateLimiting("appeals")]
public class VerificationEmailController(ApplicationDbContext db) : ControllerBase
{
    public record ResendRequest([Required, EmailAddress] string Email);
    [HttpPost]
    public async Task<IActionResult> Resend(ResendRequest req)
    {
        const string message = "Nếu tài khoản đang chờ xác thực, email sẽ được gửi. Vui lòng kiểm tra hộp thư và thư rác.";
        var user = await db.Users.SingleOrDefaultAsync(u => u.Email == req.Email.Trim());
        if (user == null) return Ok(new { message });
        await using var tx = await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({-user.UserId})");
        await db.Entry(user).ReloadAsync();
        if (!AccountState.AwaitingEmailVerification(user)) return Ok(new { message });
        var cutoff = DateTime.UtcNow.AddMinutes(-1);
        if (await db.EmailOutbox.AnyAsync(x => x.UserId == user.UserId && x.Kind == "Verification" && x.CreatedAt > cutoff))
            return Ok(new { message });
        user.VerificationCode = Guid.NewGuid().ToString(); user.CodeExpiry = DateTime.UtcNow.AddHours(24);
        db.EmailOutbox.Add(new EmailOutbox { UserId = user.UserId, Code = user.VerificationCode });
        await db.SaveChangesAsync(); await tx.CommitAsync();
        return Ok(new { message });
    }
}
