using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WebDeveloper.Models.Enums;
namespace WebDeveloper.Models.Entities;
[Table("doctor_credential_requests")]
public class DoctorCredentialRequest
{
    public int Id { get; set; }
    public int DoctorId { get; set; }
    public DoctorDetail Doctor { get; set; } = null!;
    [MaxLength(20)] public string Kind { get; set; } = "";
    [MaxLength(100)] public string? OldDegree { get; set; }
    [MaxLength(100)] public string? NewDegree { get; set; }
    public string? OldDocumentUrl { get; set; }
    public string NewDocumentUrl { get; set; } = "";
    [ConcurrencyCheck] public TransferStatus Status { get; set; }
    public string? AdminNote { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
}
