using WebDeveloper.Helpers;
using WebDeveloper.Models.DTOs.Doctor;
using WebDeveloper.Models.DTOs.Patient;
using WebDeveloper.Models.DTOs.Admin;
using WebDeveloper.Models.DTOs.Portal;
using WebDeveloper.Models.Entities;
using WebDeveloper.Models.Enums;

namespace WebDeveloper.Services.Interfaces
{
    /// <summary>Hợp đồng hồ sơ bệnh nhân, đặt lịch và đánh giá. Xem Function_detail.md.</summary>
    public interface IPatientService
    {
        Task<List<RelativeResponse>> GetRelatives(User user);
        Task<RelativeResponse> GetRelativeById(User user, int id);
        Task<string> AddRelative(User user, RelativeRequest request);
        Task<string> UpdateRelative(User user, int id, RelativeRequest request);
        Task<string> DeleteRelative(User user, int id);
        Task<string> BookAppointment(User user, AppointmentRequest request);
        Task<string> CancelAppointment(User user, int appointmentId, string? reason = null);
        Task<PagedResult<AppointmentResponse>> GetAppointments(User user, BookingStatus? status, DateOnly? startDate, DateOnly? endDate, int page, int size);
        Task<AppointmentDetailResponse> GetAppointmentDetail(User user, int appointmentId);
        Task<string> SubmitReview(User user, int appointmentId, ReviewRequest request);
        Task<string> UpdateReview(User user, int appointmentId, ReviewRequest request);
    }
}
