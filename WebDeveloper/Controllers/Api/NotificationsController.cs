using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebDeveloper.Data;
using System.Security.Claims;

namespace WebDeveloper.Controllers.Api;

[ApiController, Authorize, Route("api/v1/notifications")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class NotificationsController(ApplicationDbContext db) : ControllerBase
{
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // Mọi truy vấn phải lọc UserId từ claim, kể cả đánh dấu đã đọc và xóa. Cursor dùng Id để tải tiếp.
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] long? before, CancellationToken ct)
    {
        var own = db.Notifications.AsNoTracking().Where(n => n.UserId == UserId);
        var unreadCount = await own.CountAsync(n => n.ReadAt == null, ct);
        var items = await own.Where(n => !before.HasValue || n.Id < before.Value).OrderByDescending(n => n.Id)
            .Select(n => new { n.Id, n.Title, n.Message, n.Url, n.CreatedAt, n.ReadAt }).Take(21).ToListAsync(ct);
        return Ok(new { unreadCount, items = items.Take(20), hasMore = items.Count > 20 });
    }

    [HttpPatch("{id:long}/read")]
    public async Task<IActionResult> Read(long id, CancellationToken ct)
    {
        var own = db.Notifications.Where(n => n.UserId == UserId && n.Id == id);
        if (!await own.AnyAsync(ct)) return NotFound();
        await own.Where(n => n.ReadAt == null).ExecuteUpdateAsync(s => s.SetProperty(n => n.ReadAt, DateTime.UtcNow), ct);
        return NoContent();
    }

    [HttpDelete("read")]
    public async Task<IActionResult> DeleteRead(CancellationToken ct)
    {
        var deleted = await db.Notifications.Where(n => n.UserId == UserId && n.ReadAt != null).ExecuteDeleteAsync(ct);
        return Ok(new { deleted });
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        var deleted = await db.Notifications.Where(n => n.UserId == UserId && n.Id == id && n.ReadAt != null).ExecuteDeleteAsync(ct);
        return deleted == 0 ? NotFound() : NoContent();
    }

    [HttpPatch("read-all")]
    public async Task<IActionResult> ReadAll(CancellationToken ct)
    {
        await db.Notifications.Where(n => n.UserId == UserId && n.ReadAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.ReadAt, DateTime.UtcNow), ct);
        return NoContent();
    }
}
