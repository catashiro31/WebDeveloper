using WebDeveloper.Helpers;
using WebDeveloper.Models.DTOs.Doctor;
using WebDeveloper.Models.DTOs.Patient;
using WebDeveloper.Models.DTOs.Admin;
using WebDeveloper.Models.DTOs.Portal;
using WebDeveloper.Models.Entities;
using WebDeveloper.Models.Enums;

namespace WebDeveloper.Services.Interfaces
{
    /// <summary>Hợp đồng hồ sơ, lịch làm việc, khám bệnh và chuyển công tác. Xem Function_detail.md.</summary>
    public interface IDoctorService
    {
        Task<DoctorProfileResponse> GetProfile(User user);
        Task<string> RegisterDoctor(User user, DoctorProfileRequest request);
        Task<DoctorProfileResponse> ChangeProfile(User user, ChangeProfileRequest request);
        Task<List<DoctorScheduleResponse>> GetSchedules(User user);
        Task<string> DeclineAppointment(User user, int id, BookingStatus status, string? reason = null);
        Task<string> CreateSchedule(User user, ScheduleRequest request);
        Task<string> DeleteSchedule(User user, int scheduleId);
        Task<PagedResult<DoctorAppointmentResponse>> GetAppointments(User user, BookingStatus? status, int page, int size);
        Task<string> ConfirmAppointment(User user, int appointmentId);
        Task<string> CompleteAppointment(User user, int appointmentId);
        Task<string> SaveMedicalResult(User user, int appointmentId, MedicalResultRequest request);
        Task<PagedResult<DoctorReviewResponse>> GetReviews(User user, int page, int size);
        Task<List<DoctorAppointmentResponse>> GetOverdueConfirmedAppointments(User user);
        Task<string> CreateTransferRequest(User user, TransferRequestDto request);

        Task<PagedResult<object>> GetTransferRequests(TransferStatus status, int page, int size);
        Task ApproveTransfer(int id, string? adminNote);
        Task RejectTransfer(int id, string? adminNote);
    }
}
