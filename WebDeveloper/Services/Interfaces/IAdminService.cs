using WebDeveloper.Helpers;
using WebDeveloper.Models.DTOs.Doctor;
using WebDeveloper.Models.DTOs.Patient;
using WebDeveloper.Models.DTOs.Admin;
using WebDeveloper.Models.DTOs.Portal;
using WebDeveloper.Models.Entities;
using WebDeveloper.Models.Enums;

namespace WebDeveloper.Services.Interfaces
{
    /// <summary>Hợp đồng quản trị tài khoản, danh mục, phê duyệt và thống kê. Xem Function_detail.md.</summary>
    public interface IAdminService
    {
        Task<StatResponse> GetStats(DateOnly? start, DateOnly? end);
        Task<PagedResult<object>> GetAllDoctors(int page, int size, string? search = null, int? specialtyId = null, int? facilityId = null);
        Task<List<object>> GetPendingDoctors(string? search = null, int? specialtyId = null, int? facilityId = null);
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
        Task<PagedResult<object>> GetAllUsers(int page, int size, string? search = null, RoleStatus? role = null, bool? isActive = null);
        Task<string> BlockUser(int id, string reason);
        Task<string> UnblockUser(int id, string reason);
        Task<PagedResult<AppointmentAdminResponse>> GetAllAppointments(DateOnly? dateFrom, DateOnly? dateTo, BookingStatus? status, int page, int size, int? doctorId = null, int? facilityId = null, DateOnly? appointmentDate = null, int? specialtyId = null, DateOnly? workDateFrom = null, DateOnly? workDateTo = null);
        Task<PagedResult<ReviewAdminResponse>> GetAllReviews(int page, int size, string? category = null, string? search = null);
        Task<string> RejectReview(int id);
        Dictionary<string, double> AnalyzeComment(string text);
    }
}
