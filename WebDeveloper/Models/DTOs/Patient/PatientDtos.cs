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
        public string Reason { get; set; } = string.Empty;
    }

    public class RelativeRequest
    {
        [Required(ErrorMessage = "Họ tên người thân không được để trống")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ngày sinh không được để trống")]
        public DateOnly DateOfBirth { get; set; }

        [Required(ErrorMessage = "Giới tính không được để trống")]
        public GenderStatus Gender { get; set; }

        [Required(ErrorMessage = "Số điện thoại không được để trống")]
        [RegularExpression(@"^(0|\+84)(\s|\.)?((3[2-9])|(5[689])|(7[06-9])|(8[1-689])|(9[0-46-9]))(\d)(\s|\.)?(\d{3})(\s|\.)?(\d{3})$",
            ErrorMessage = "Số điện thoại không đúng định dạng Việt Nam")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Địa chỉ không được để trống")]
        public string Address { get; set; } = string.Empty;

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
        public DateTime? CreatedAt { get; set; }
        public bool HasResult { get; set; }
    }

    public class AppointmentDetailResponse : AppointmentResponse
    {
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
