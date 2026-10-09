using Microsoft.EntityFrameworkCore;
using WebDeveloper.Data;
using WebDeveloper.Helpers;
using WebDeveloper.Models.DTOs.Doctor;
using WebDeveloper.Models.DTOs.Portal;
using WebDeveloper.Models.Enums;
using WebDeveloper.Services.Interfaces;

namespace WebDeveloper.Services
{
    public class PublicService : IPublicService
    {
        private readonly ApplicationDbContext _db;

        public PublicService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<PortalStatsResponse> GetPortalStats()
        {
            var totalDoctors = await _db.DoctorDetails.CountAsync(d => d.VerificationStatus == VerificationStatus.APPROVED);
            var totalAppointments = await _db.Appointments.CountAsync();
            var avgRatingObj = await _db.DoctorDetails.Where(d => d.VerificationStatus == VerificationStatus.APPROVED).AverageAsync(d => d.RatingAverage);
            var avgRating = avgRatingObj ?? 5.0;

            return new PortalStatsResponse
            {
                TotalDoctors = totalDoctors,
                TotalAppointments = totalAppointments,
                AverageRating = Math.Round(avgRating * 10.0) / 10.0
            };
        }

        public async Task<List<FacilityResponse>> GetAllFacilities()
        {
            var items = await _db.Facilities.Where(f => f.IsActive).ToListAsync();
            return items.Select(f => new FacilityResponse
            {
                Id = f.FacilityId,
                Name = f.FacilityName,
                Address = f.Address,
                Description = f.Description,
                ImageUrl = f.ImageUrl,
                LicenseUrl = f.LicenseUrl,
                MapUrl = f.MapUrl,
                Province = f.Province,
                Verified = f.IsVerified
            }).ToList();
        }

        public async Task<List<SpecialtyResponse>> GetAllSpecialties()
        {
            var items = await _db.Specialties.Where(s => s.IsActive == true).ToListAsync();
            return items.Select(s => new SpecialtyResponse
            {
                Id = s.SpecialtyId,
                Name = s.SpecialtyName,
                Description = s.Description
            }).ToList();
        }

        public async Task<PagedResult<DoctorCardResponse>> GetDoctors(string? keyword, int? specId, int? facilityId, string? province, double? minPrice, double? maxPrice, string? sortBy, int page, int size)
        {
            var query = _db.DoctorDetails
                .Include(d => d.User)
                .Include(d => d.Specialty)
                .Include(d => d.Facility)
                .Where(d => d.VerificationStatus == VerificationStatus.APPROVED);

            if (!string.IsNullOrWhiteSpace(keyword))
                query = query.Where(d => d.User.FullName!.Contains(keyword));
            if (specId.HasValue)
                query = query.Where(d => d.SpecialtyId == specId.Value);
            if (facilityId.HasValue)
                query = query.Where(d => d.FacilityId == facilityId.Value);
            if (!string.IsNullOrWhiteSpace(province))
                query = query.Where(d => d.Facility.Province == province);
            if (minPrice.HasValue)
                query = query.Where(d => d.Price >= minPrice.Value);
            if (maxPrice.HasValue)
                query = query.Where(d => d.Price <= maxPrice.Value);

            if (sortBy == "rating") query = query.OrderByDescending(d => d.RatingAverage);
            else if (sortBy == "priceAsc") query = query.OrderBy(d => d.Price);
            else if (sortBy == "priceDesc") query = query.OrderByDescending(d => d.Price);
            else if (sortBy == "experience") query = query.OrderByDescending(d => d.ExperienceYears);
            else query = query.OrderBy(d => d.DoctorId);

            var total = await query.CountAsync();
            var items = await query.Skip(page * size).Take(size).ToListAsync();

            var dtos = items.Select(d => new DoctorCardResponse
            {
                DoctorId = d.DoctorId,
                DoctorName = d.User.FullName,
                SpecialtyName = d.Specialty?.SpecialtyName,
                DoctorEmail = d.User.Email,
                DoctorPhone = d.User.PhoneNumber,
                AvatarUrl = d.User.AvatarUrl,
                RatingAverage = d.RatingAverage,
                TotalReviews = d.ReviewCount,
                Price = d.Price,
                Experience = d.ExperienceYears
            }).ToList();

            return PagedResult<DoctorCardResponse>.Create(dtos, page, size, total);
        }

        public async Task<DoctorDetailPublicResponse> GetDoctorById(int doctorId)
        {
            var d = await _db.DoctorDetails
                .Include(d => d.User)
                .Include(d => d.Specialty)
                .Include(d => d.Facility)
                .FirstOrDefaultAsync(d => d.DoctorId == doctorId)
                ?? throw new InvalidOperationException("Bác sĩ không tồn tại");

            if (d.VerificationStatus != VerificationStatus.APPROVED)
                throw new InvalidOperationException("Thông tin chưa được công khai");

            return new DoctorDetailPublicResponse
            {
                Id = d.DoctorId,
                FullName = d.User.FullName,
                SpecialtyName = d.Specialty?.SpecialtyName,
                SpecialtyId = d.SpecialtyId,
                Degree = d.Degree,
                ExperienceYears = d.ExperienceYears,
                Price = d.Price,
                Bio = d.Bio,
                FacilityName = d.Facility?.FacilityName,
                FacilityAddress = d.Facility?.Address,
                FacilityProvince = d.Facility?.Province,
                FacilityMapUrl = d.Facility?.MapUrl,
                FacilityVerified = d.Facility?.IsVerified,
                DoctorEmail = d.User.Email,
                DoctorPhone = d.User.PhoneNumber,
                AvatarUrl = d.User.AvatarUrl,
                RatingAverage = d.RatingAverage,
                TotalReviews = d.ReviewCount
            };
        }

        public async Task<List<DoctorReviewResponse>> GetReviewsByDoctorId(int doctorId)
        {
            var reviews = await _db.Reviews
                .Include(r => r.Appointment).ThenInclude(a => a.Patient).ThenInclude(p => p.User)
                .Where(r => r.Appointment.Schedule.DoctorId == doctorId && r.IsVisible == true)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return reviews.Select(r => new DoctorReviewResponse
            {
                ReviewId = r.ReviewId,
                Rating = r.Rating,
                Comment = r.Comment,
                PatientName = r.Appointment.Patient.User?.FullName ?? r.Appointment.Patient.FullName,
                CreatedAt = r.CreatedAt
            }).ToList();
        }

        public async Task<List<DoctorSlotResponse>> GetAvailableSlots(int doctorId, DateOnly date)
        {
            var d = await _db.DoctorDetails.FindAsync(doctorId) ?? throw new InvalidOperationException("Không tồn tại");
            if (d.VerificationStatus != VerificationStatus.APPROVED) return new List<DoctorSlotResponse>();

            var schedules = await _db.DoctorSchedules
                .Where(s => s.DoctorId == doctorId && s.DateWorking == date && s.SlotStatus == SlotStatus.AVAILABLE)
                .ToListAsync();

            var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));
            var minTime = TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(8)); // 1 hour buffer

            return schedules.Where(s => 
            {
                if (date == today) return TimeSlotHelper.ParseTimeSlot(s.TimeSlot) > minTime;
                return true;
            }).Select(s => new DoctorSlotResponse
            {
                ScheduleId = s.ScheduleId,
                TimeSlot = s.TimeSlot.GetDisplayValue(),
                SlotStatus = s.SlotStatus.ToString()
            }).ToList();
        }
    }
}
