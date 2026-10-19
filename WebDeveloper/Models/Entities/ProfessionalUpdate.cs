using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebDeveloper.Models.Entities;

[Table("professional_updates")]
public class ProfessionalUpdate
{
    public long Id { get; set; }
    public int DoctorId { get; set; }
    public DoctorDetail Doctor { get; set; } = null!;
    public int SubmittedByUserId { get; set; }
    public User SubmittedByUser { get; set; } = null!;
    public int? ReviewedByUserId { get; set; }
    public User? ReviewedByUser { get; set; }
    [MaxLength(20)] public string Status { get; set; } = "PENDING";
    public bool AppliedDirectly { get; set; }
    public int BaseVersion { get; set; }
    public string BeforeJson { get; set; } = "";
    public string ProposedJson { get; set; } = "";
    public string? AppliedBeforeJson { get; set; }
    [MaxLength(1000)] public string Reason { get; set; } = "";
    [MaxLength(1000)] public string? DecisionReason { get; set; }
    public string? EvidenceUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedAt { get; set; }
}
