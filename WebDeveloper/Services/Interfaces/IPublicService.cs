using WebDeveloper.Helpers;
using WebDeveloper.Models.DTOs.Doctor;
using WebDeveloper.Models.DTOs.Patient;
using WebDeveloper.Models.DTOs.Admin;
using WebDeveloper.Models.DTOs.Portal;
using WebDeveloper.Models.Entities;
using WebDeveloper.Models.Enums;

namespace WebDeveloper.Services.Interfaces
{
    /// <summary>Hợp đồng tra cứu bác sĩ và danh mục công khai. Xem Function_detail.md.</summary>
    public interface IPublicService
    {
        Task<double> GetMaxDoctorPrice();
        Task<PortalStatsResponse> GetPortalStats();
        Task<PagedResult<DoctorCardResponse>> GetDoctors(string? keyword, int? specId, int? facilityId, string? province, double? minPrice, double? maxPrice, string? sortBy, int page, int size);
        Task<DoctorDetailPublicResponse> GetDoctorById(int doctorId);
        Task<List<DoctorReviewResponse>> GetReviewsByDoctorId(int doctorId);
        Task<List<DoctorSlotResponse>> GetAvailableSlots(int doctorId, DateOnly date);
        Task<List<FacilityResponse>> GetAllFacilities();
        Task<List<SpecialtyResponse>> GetAllSpecialties();
    }
}
