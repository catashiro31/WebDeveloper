using WebDeveloper.Helpers;
using WebDeveloper.Models.DTOs.Doctor;
using WebDeveloper.Models.DTOs.Patient;
using WebDeveloper.Models.DTOs.Admin;
using WebDeveloper.Models.DTOs.Portal;
using WebDeveloper.Models.Entities;
using WebDeveloper.Models.Enums;

namespace WebDeveloper.Services.Interfaces
{
    public interface IDoctorService
    {
        Task<DoctorProfileResponse> GetProfile(User user);
        Task<string> RegisterDoctor(User user, DoctorProfileRequest request);
        Task<DoctorProfileResponse> ChangeProfile(User user, ChangeProfileRequest request);
        Task<List<DoctorScheduleResponse>> GetSchedules(User user);
        Task<string> CreateSchedule(User user, ScheduleRequest request);
        Task<string> DeleteSchedule(User user, int scheduleId);
        Task<PagedResult<DoctorAppointmentResponse>> GetAppointments(User user, int page, int size);
        Task<string> ConfirmAppointment(User user, int appointmentId);
        Task<string> CompleteAppointment(User user, int appointmentId);
        Task<string> SaveMedicalResult(User user, int appointmentId, MedicalResultRequest request);
        Task<PagedResult<DoctorReviewResponse>> GetReviews(User user, int page, int size);
        Task<string> CreateTransferRequest(User user, TransferRequestDto request);
        void UpdateHistoricalSchedules();
        Task<PagedResult<object>> GetTransferRequests(TransferStatus status, int page, int size);
        Task ApproveTransfer(int id, string? adminNote);
        Task RejectTransfer(int id, string? adminNote);
    }

    public interface IPatientService
    {
        Task<List<RelativeResponse>> GetRelatives(User user);
        Task<string> AddRelative(User user, RelativeRequest request);
        Task<string> UpdateRelative(User user, int id, RelativeRequest request);
        Task<string> DeleteRelative(User user, int id);
        Task<string> BookAppointment(User user, AppointmentRequest request);
        Task<string> CancelAppointment(User user, int appointmentId);
        Task<PagedResult<AppointmentResponse>> GetAppointments(User user, BookingStatus? status, DateOnly? startDate, DateOnly? endDate, int page, int size);
        Task<AppointmentDetailResponse> GetAppointmentDetail(User user, int appointmentId);
        Task<string> SubmitReview(User user, int appointmentId, ReviewRequest request);
    }

    public interface IAdminService
    {
        Task<StatResponse> GetStats(DateOnly start, DateOnly end);
        Task<PagedResult<object>> GetAllDoctors(int page, int size);
        Task<List<object>> GetPendingDoctors();
        Task<object> GetDoctorDetail(int id);
        Task<object> ApproveDoctor(int doctorId);
        Task<object> RejectDoctor(int doctorId, string reason);
        Task<string> AddSpecialty(SpecialtyRequest request);
        Task<string> UpdateSpecialty(int id, SpecialtyRequest request);
        Task<string> DeleteSpecialty(int id);
        Task<string> AddFacility(FacilityRequest request);
        Task<string> UpdateFacility(int id, FacilityRequest request);
        Task<string> VerifyFacility(int id);
        Task<string> DeleteFacility(int id);
        Task<PagedResult<object>> GetAllUsers(int page, int size);
        Task<string> BlockUser(int id, string reason);
        Task<string> UnblockUser(int id);
        Task<PagedResult<AppointmentAdminResponse>> GetAllAppointments(DateOnly? dateFrom, DateOnly? dateTo, BookingStatus? status, int page, int size);
        Task<PagedResult<ReviewAdminResponse>> GetAllReviews(int page, int size);
        Task<string> RejectReview(int id);
        Dictionary<string, double> AnalyzeComment(string text);
    }

    public interface IPublicService
    {
        Task<PortalStatsResponse> GetPortalStats();
        Task<PagedResult<DoctorCardResponse>> GetDoctors(string? keyword, int? specId, int? facilityId, string? province, double? minPrice, double? maxPrice, string? sortBy, int page, int size);
        Task<DoctorDetailPublicResponse> GetDoctorById(int doctorId);
        Task<List<DoctorReviewResponse>> GetReviewsByDoctorId(int doctorId);
        Task<List<DoctorSlotResponse>> GetAvailableSlots(int doctorId, DateOnly date);
        Task<List<FacilityResponse>> GetAllFacilities();
        Task<List<SpecialtyResponse>> GetAllSpecialties();
    }
}
