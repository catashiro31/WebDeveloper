using System.ComponentModel.DataAnnotations;
namespace WebDeveloper.Models.DTOs.Doctor;

public class DiplomaItemDto
{
    public string Title { get; set; } = "";
    public string? Issuer { get; set; }
    public int? Year { get; set; }
    public string? FileUrl { get; set; }
}

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

    // Giấy phép / Chứng chỉ hành nghề chi tiết
    public string? LicenseNumber { get; set; }
    public string? LicenseIssuer { get; set; }
    public string? LicenseIssueDate { get; set; }
    public string? LicenseScope { get; set; }

    // Giấy tờ đã lưu được chọn làm căn cứ đối chiếu
    public string? SelectedSavedDocumentUrl { get; set; }
    public string? SelectedSavedDocumentLabel { get; set; }

    // Bằng cấp chuyên môn (JSON array)
    public string? DiplomasJson { get; set; }
}

public record ProfessionalDecision(bool Approve, [StringLength(1000)] string? Reason);

public record ProfessionalSnapshot(
    string? Degree,
    int? PracticeStartYear,
    int? LegacyExperienceYears,
    int SpecialtyId,
    string SpecialtyName,
    string? CertificateUrl,
    double? Price,
    string? Bio,
    string? LicenseNumber = null,
    string? LicenseIssuer = null,
    string? LicenseIssueDate = null,
    string? LicenseScope = null,
    List<DiplomaItemDto>? Diplomas = null,
    string? ReferencedDocumentLabel = null,
    string? ReferencedDocumentUrl = null
);
