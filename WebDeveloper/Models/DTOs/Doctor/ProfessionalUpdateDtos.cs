using System.ComponentModel.DataAnnotations;
namespace WebDeveloper.Models.DTOs.Doctor;

public class ProfessionalUpdateRequest
{
    [StringLength(100)] public string? Degree { get; set; }
    public int? PracticeStartYear { get; set; }
    public int? SpecialtyId { get; set; }
    public IFormFile? EvidenceFile { get; set; }
    public IFormFile? CertificateFile { get; set; }
    public bool UseExistingCertificate { get; set; }
    [Required, StringLength(1000, MinimumLength = 10)] public string Reason { get; set; } = "";
    // Admin may correct immediate/public fields in the same audited action.
    [Range(1, 1_000_000_000)] public double? Price { get; set; }
    [StringLength(10000)] public string? Bio { get; set; }
    public int? ExpectedVersion { get; set; }
}
public record ProfessionalDecision(bool Approve, [StringLength(1000)] string? Reason);
public record ProfessionalSnapshot(string? Degree, int? PracticeStartYear, int? LegacyExperienceYears,
    int SpecialtyId, string SpecialtyName, string? CertificateUrl, double? Price, string? Bio);
