using System.ComponentModel.DataAnnotations;
using WebDeveloper.Models.Enums;

namespace WebDeveloper.Models.DTOs.Admin
{
    public class FacilityRequest
    {
        [Required(ErrorMessage = "Không được để trống")]
        public string Address { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Required(ErrorMessage = "Không được để trống")]
        public string FacilityName { get; set; } = string.Empty;

        public string? MapUrl { get; set; }
        public string? Province { get; set; }

        public IFormFile? File { get; set; }
        public IFormFile? LicenseFile { get; set; }
    }

    public class SpecialtyRequest
    {
        [Required(ErrorMessage = "Tên chuyên khoa không được để trống!")]
        public string SpecialtyName { get; set; } = string.Empty;

        public string? Description { get; set; }
    }

    public class ProcessTransferRequest
    {
        public string? AdminNote { get; set; }
    }

    public class AppointmentAdminResponse
    {
        public int AppointmentId { get; set; }
        public int? PatientId { get; set; }
        public string? PatientName { get; set; }
        public string? PatientPhone { get; set; }
        public string? PatientEmail { get; set; }
        public int? DoctorId { get; set; }
        public string? DoctorName { get; set; }
        public string? SpecialtyName { get; set; }
        public string? FacilityName { get; set; }
        public DateOnly? DateWorking { get; set; }
        public string? TimeSlot { get; set; }
        public string? Reason { get; set; }
        public BookingStatus BookingStatus { get; set; }
        public DateTime? CreatedAt { get; set; }
    }

    public class AppointmentStatsResponse
    {
        public long Completed { get; set; }
        public long Pending { get; set; }
        public long Cancelled { get; set; }
    }

    public class ReviewAdminResponse
    {
        public int ReviewId { get; set; }
        public string? PatientName { get; set; }
        public string? DoctorName { get; set; }
        public int? Rating { get; set; }
        public string? Comment { get; set; }
        public DateTime? CreatedAt { get; set; }
        public bool? IsVisible { get; set; }
        public Dictionary<string, double>? AiLabels { get; set; }
    }

    public class StatResponse
    {
        public long NumberOfDoctors { get; set; }
        public long NumberOfPatients { get; set; }
        public long NumberOfSuccessAppointments { get; set; }
        public long NumberOfPendingAppointments { get; set; }
        public long NumberOfFailingAppointments { get; set; }
        public long TotalUsers { get; set; }
        public long TotalDoctors { get; set; }
        public long TotalPatients { get; set; }
        public long TotalAppointments { get; set; }
        public long PendingDoctors { get; set; }
        public long TodayAppointments { get; set; }
        public long TotalReviews { get; set; }
    }
}
