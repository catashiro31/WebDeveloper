using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
namespace WebDeveloper.Models.Entities;

// Ghi cùng transaction với nghiệp vụ; worker gửi sau và retry khi SMTP lỗi.
[Table("email_outbox")]
[Index(nameof(DeliveryKey), IsUnique = true)]
[Index(nameof(SentAt), nameof(NextAttemptAt))]
public class EmailOutbox
{
    public long Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    [MaxLength(100)] public string DeliveryKey { get; set; } = Guid.NewGuid().ToString("N");
    [MaxLength(40)] public string Kind { get; set; } = "Verification";
    public string? Code { get; set; }
    public bool? Approved { get; set; }
    [MaxLength(1000)] public string? Reason { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime NextAttemptAt { get; set; } = DateTime.UtcNow;
    public DateTime? SentAt { get; set; }
    public DateTime? DiscardedAt { get; set; }
    public int Attempts { get; set; }
    [MaxLength(100)] public string? LastError { get; set; }
}
