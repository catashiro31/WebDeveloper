using System.ComponentModel.DataAnnotations;
using WebDeveloper.Models.Enums;

namespace WebDeveloper.Models.DTOs.Patient
{
    public class AppointmentRequest
    {
        [Required(ErrorMessage = "Không được để trống")]
        public int PatientId { get; set; }

        [Required(ErrorMessage = "Không được để trống")]
        public int ScheduleId { get; set; }

        [Required(ErrorMessage = "Không được để trống lý do đặt")]
        [StringLength(500, MinimumLength = 10, ErrorMessage = "Lý do khám phải từ 10 đến 500 ký tự")]
        public string Reason { get; set; } = string.Empty;
    }

    public class RelativeRequest
    {
        [Required(ErrorMessage = "Họ tên người thân không được để trống")]
        [StringLength(60, MinimumLength = 2, ErrorMessage = "Họ và tên người thân phải từ 2 đến 60 ký tự")]
        [RegularExpression(@"^[\p{L}\s]{2,60}$", ErrorMessage = "Họ và tên chỉ được chứa chữ cái tiếng Việt và khoảng trắng, không bao gồm số hay ký tự đặc biệt")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ngày sinh không được để trống")]
        public DateOnly DateOfBirth { get; set; }

        [Required(ErrorMessage = "Giới tính không được để trống")]
        public GenderStatus Gender { get; set; }

        [Required(ErrorMessage = "Số điện thoại không được để trống")]
        [RegularExpression(@"^(03|05|07|08|09)\d{8}$", ErrorMessage = "Số điện thoại phải đúng đầu số di động Việt Nam (03, 05, 07, 08, 09) và gồm 10 chữ số")]
        public string PhoneNumber { get; set; } = string.Empty;

        public string? Address { get; set; }

        [Required(ErrorMessage = "Mối quan hệ không được để trống")]
        public string Relationship { get; set; } = string.Empty;
    }

    public class ReviewRequest
    {
        [Required(ErrorMessage = "Số sao không được để trống")]
        [Range(1, 5, ErrorMessage = "Đánh giá từ 1 đến 5 sao")]
        public int Rating { get; set; }

        [Required(ErrorMessage = "Nội dung đánh giá không được để trống")]
        public string Comment { get; set; } = string.Empty;
    }

    public class AppointmentResponse
    {
        public int AppointmentId { get; set; }
        public string? PatientName { get; set; }
        public string? DoctorName { get; set; }
        public string? SpecialtyName { get; set; }
        public string? FacilityName { get; set; }
        public string? Address { get; set; }
        public DateOnly DateWorking { get; set; }
        public string? TimeSlot { get; set; }
        public string? BookingStatus { get; set; }
        public string? CancellationReason { get; set; }
        public string? CancelledBy { get; set; }
        public DateTime? CreatedAt { get; set; }
        public bool HasResult { get; set; }
    }

    public class AppointmentDetailResponse : AppointmentResponse
    {
        public DateOnly? FollowUpDate { get; set; }
        public string? Diagnosis { get; set; }
        public string? PrescriptionUrl { get; set; }
        public string? DoctorNotes { get; set; }
        public int? Rating { get; set; }
        public string? Comment { get; set; }
    }

    public class RelativeResponse
    {
        public int PatientId { get; set; }
        public string? FullName { get; set; }
        public DateOnly? DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }
        public string? Relationship { get; set; }
    }
}
