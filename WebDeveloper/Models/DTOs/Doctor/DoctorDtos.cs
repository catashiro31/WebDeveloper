using System.ComponentModel.DataAnnotations;

namespace WebDeveloper.Models.DTOs.Doctor
{
    public class DoctorProfileRequest
    {
        [Required(ErrorMessage = "Tiểu sử không được để trống")]
        public string Bio { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập bằng cấp")]
        public string Degree { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập số năm kinh nghiệm")]
        [Range(0, int.MaxValue, ErrorMessage = "Kinh nghiệm không được là số âm")]
        public int ExperienceYears { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập giá khám")]
        [Range(0, double.MaxValue, ErrorMessage = "Giá khám không được là số âm")]
        public double Price { get; set; }

        [Required(ErrorMessage = "Vui lòng tải lên ảnh CCCD")]
        public IFormFile? IdCardImage { get; set; }

        [Required(ErrorMessage = "Vui lòng đính kèm file chứng chỉ")]
        public IFormFile? CertificatePdf { get; set; }

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

    public class ChangeProfileRequest
    {
        public string? Bio { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Giá khám không được là số âm")]
        public double? Price { get; set; }

        public string? FacilityName { get; set; }
        public string? FacilityAddress { get; set; }
        public string? FacilityDescription { get; set; }
        public string? FacilityMapUrl { get; set; }
        public bool? FacilityVerified { get; set; }
    }

    public class ScheduleRequest
    {
        public DateOnly Date { get; set; }
        public List<string> SlotIds { get; set; } = new();
    }

    public class MedicalResultRequest
    {
        public string? Diagnosis { get; set; }
        public string? DoctorNotes { get; set; }
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
        public bool? HasPendingTransfer { get; set; }
        public string? LastTransferRejectionNote { get; set; }
    }

    public class DoctorAppointmentResponse
    {
        public int AppointmentId { get; set; }
        public string? PatientName { get; set; }
        public string? PatientPhoneNumber { get; set; }
        public string? PatientGender { get; set; }
        public DateOnly DateWorking { get; set; }
        public string? TimeSlot { get; set; }
        public string? Reason { get; set; }
        public string? BookingStatus { get; set; }
        public DateTime? CreatedAt { get; set; }
        public string? Diagnosis { get; set; }
        public string? DoctorNotes { get; set; }
        public string? PrescriptionUrl { get; set; }
    }

    public class DoctorReviewResponse
    {
        public int ReviewId { get; set; }
        public int? Rating { get; set; }
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
