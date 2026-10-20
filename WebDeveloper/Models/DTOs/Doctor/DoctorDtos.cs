using System.ComponentModel.DataAnnotations;

namespace WebDeveloper.Models.DTOs.Doctor
{
    public class DoctorProfileRequest
    {
        [Required(ErrorMessage = "Tiểu sử không được để trống")]
        public string Bio { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập bằng cấp")]
        [WebDeveloper.Helpers.DoctorDegree]
        public string Degree { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập số năm kinh nghiệm")]
        [Range(0, int.MaxValue, ErrorMessage = "Kinh nghiệm không được là số âm")]
        public int ExperienceYears { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập giá khám")]
        [Range(1, double.MaxValue, ErrorMessage = "Giá khám phải lớn hơn 0 đồng")]
        public double Price { get; set; }

        public IFormFile? IdCardImage { get; set; }

        public IFormFile? CertificatePdf { get; set; }
        public IFormFile? DiplomaPdf { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn chuyên khoa")]
        public int SpecialtyId { get; set; }

        public int? FacilityId { get; set; }

        // Các trường dùng khi tạo cơ sở y tế mới
        public string? NewFacilityName { get; set; }
        public string? FacilityAddress { get; set; }
        public string? FacilityDescription { get; set; }
        public string? FacilityMapUrl { get; set; }
        public string? NewFacilityProvince { get; set; }
        public IFormFile? FacilityLicensePdf { get; set; }
    }

    public class CredentialChangeRequest
    {
        [Required] public string Kind { get; set; } = "";
        public string? Degree { get; set; }
        [Required] public IFormFile? Document { get; set; }
    }

    public class ChangeProfileRequest
    {
        public string? Bio { get; set; }

        [Range(1, double.MaxValue, ErrorMessage = "Giá khám phải lớn hơn 0 đồng")]
        public double? Price { get; set; }

        public string? FacilityName { get; set; }
        public string? FacilityAddress { get; set; }
        public string? FacilityDescription { get; set; }
        public string? FacilityMapUrl { get; set; }
        public bool? FacilityVerified { get; set; }
    }

    public class DayScheduleItem
    {
        public DateOnly Date { get; set; }
        public List<string> SlotIds { get; set; } = new();
    }

    public class ScheduleRequest
    {
        public DateOnly? Date { get; set; }
        public DateOnly? EndDate { get; set; }
        public List<string>? SlotIds { get; set; }
        public List<DayScheduleItem>? Items { get; set; }
    }

    public class MedicalResultRequest
    {
        public DateOnly? FollowUpDate { get; set; }
        public string? Diagnosis { get; set; }
        public string? DoctorNotes { get; set; }
        public string? PrescriptionLink { get; set; }
        public IFormFile? PrescriptionFile { get; set; }
    }

    public class TransferRequestDto
    {
        public int TargetFacilityId { get; set; }
        public string? Reason { get; set; }
    }

    public class DoctorProfileResponse
    {
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? AvatarUrl { get; set; }
        public string? Bio { get; set; }
        public string? Degree { get; set; }
        public int? ExperienceYears { get; set; }
        public double? Price { get; set; }
        public int? SpecialtyId { get; set; }
        public string? SpecialtyName { get; set; }
        public int? FacilityId { get; set; }
        public string? FacilityName { get; set; }
        public string? FacilityAddress { get; set; }
        public string? FacilityDescription { get; set; }
        public string? FacilityMapUrl { get; set; }
        public string? FacilityProvince { get; set; }
        public bool? FacilityVerified { get; set; }
        public double? RatingAverage { get; set; }
        public int? ReviewCount { get; set; }
        public string? VerificationStatus { get; set; }
        public string? CertificateUrl { get; set; }
        public string? DiplomaUrl { get; set; }
        public bool HasPendingCredentialRequest { get; set; }
        public string? LastCredentialStatus { get; set; }
        public string? LastCredentialNote { get; set; }
        public bool? HasPendingTransfer { get; set; }
        public string? LastTransferStatus { get; set; }
        public string? LastTransferNote { get; set; }
        public string? LastTransferRejectionNote { get; set; }
    }

    public class DoctorAppointmentResponse
    {
        public string? SpecialtyName { get; set; }
        public int AppointmentId { get; set; }
        public string? PatientName { get; set; }
        public string? PatientPhoneNumber { get; set; }
        public string? PatientGender { get; set; }
        public DateOnly DateWorking { get; set; }
        public string? TimeSlot { get; set; }
        public string? Reason { get; set; }
        public string? BookingStatus { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateOnly? FollowUpDate { get; set; }
        public string? Diagnosis { get; set; }
        public string? DoctorNotes { get; set; }
        public string? PrescriptionUrl { get; set; }
        public string? CancellationReason { get; set; }
        public string? CancelledBy { get; set; }
    }

    public class DoctorReviewResponse
    {
        public int ReviewId { get; set; }
        public int? Rating { get; set; }
        public bool IsVisible { get; set; }
        public string? Comment { get; set; }
        public string? PatientName { get; set; }
        public DateTime? CreatedAt { get; set; }
    }

    public class DoctorScheduleResponse
    {
        public int ScheduleId { get; set; }
        public DateOnly DateWorking { get; set; }
        public string? TimeSlot { get; set; }
        public string? SlotStatus { get; set; }
    }
}
