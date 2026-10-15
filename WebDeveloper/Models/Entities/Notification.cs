using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace WebDeveloper.Models.Entities;

[Table("notifications")]
[Index(nameof(UserId), nameof(Id))]
[Index(nameof(UserId), nameof(ReadAt))]
public class Notification
{
    public long Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    [MaxLength(200)] public string Title { get; set; } = "";
    [MaxLength(1000)] public string Message { get; set; } = "";
    [MaxLength(250)] public string Url { get; set; } = "/";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReadAt { get; set; }
}
