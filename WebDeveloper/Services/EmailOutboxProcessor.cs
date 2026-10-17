using Microsoft.EntityFrameworkCore;
using WebDeveloper.Data;
namespace WebDeveloper.Services;

public class EmailOutboxProcessor(ApplicationDbContext db, EmailService email, ILogger<EmailOutboxProcessor> logger)
{
    // SKIP LOCKED cho phép nhiều instance xử lý các email khác nhau, không gửi đồng thời cùng hàng.
    public async Task<bool> ProcessNext(CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var rows = await db.EmailOutbox.FromSqlRaw("""
            SELECT * FROM email_outbox
            WHERE "SentAt" IS NULL AND "DiscardedAt" IS NULL AND "Attempts" < 5 AND "NextAttemptAt" <= now()
            ORDER BY "Id" LIMIT 1 FOR UPDATE SKIP LOCKED
            """).ToListAsync(ct);
        var item = rows.SingleOrDefault();
        if (item == null) return false;
        await db.Entry(item).Reference(x => x.User).LoadAsync(ct);
        if (item.Kind == "Verification" && (item.User.IsActive == true || item.User.VerificationCode != item.Code
            || item.User.CodeExpiry <= DateTime.UtcNow || !string.IsNullOrWhiteSpace(item.User.ReasonBanned)))
        {
            item.DiscardedAt = DateTime.UtcNow;
            item.Code = null;
        }
        else
        {
            item.Attempts++;
            try { await email.DeliverOutbox(item, ct); item.SentAt = DateTime.UtcNow; item.Code = null; item.LastError = null; }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch (Exception ex)
            {
                item.LastError = ex.GetType().Name; // Không ghi token/mật khẩu SMTP vào database/log.
                item.NextAttemptAt = DateTime.UtcNow.AddSeconds(Math.Min(3600, 30 * Math.Pow(2, item.Attempts)));
                logger.LogWarning("Email outbox {Id} failed, attempt {Attempt}: {Type}", item.Id, item.Attempts, item.LastError);
            }
        }
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return true;
    }
}
