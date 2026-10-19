using Microsoft.EntityFrameworkCore;
using WebDeveloper.Data;
using WebDeveloper.Helpers;
using WebDeveloper.Models.DTOs.Doctor;
using WebDeveloper.Models.DTOs.Portal;
using WebDeveloper.Models.Enums;
using WebDeveloper.Services.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace WebDeveloper.Services
{
    public class PublicService : IPublicService
    {
        private readonly ApplicationDbContext _db;
        private readonly IMemoryCache _cache;

        public PublicService(ApplicationDbContext db, IMemoryCache cache)
        {
            _db = db;
            _cache = cache;
        }

        // === Trang công khai: thống kê tổng quan và dữ liệu danh mục. ===
        public async Task<PortalStatsResponse> GetPortalStats()
        {
            return await _cache.GetOrCreateAsync("PORTAL_STATS", async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30);

                var totalDoctors = await _db.DoctorDetails.CountAsync(d => d.VerificationStatus == VerificationStatus.APPROVED);
                var totalAppointments = await _db.Appointments.CountAsync();
                
                // Tránh quá tải do tính toán thống kê (Rating) Real-Time liên tục
                var avgRatingObj = await _db.DoctorDetails.Where(d => d.VerificationStatus == VerificationStatus.APPROVED).AverageAsync(d => d.RatingAverage);
                var avgRating = avgRatingObj ?? 5.0;

                return new PortalStatsResponse
                {
                    TotalDoctors = totalDoctors,
                    TotalAppointments = totalAppointments,
                    AverageRating = Math.Round(avgRating * 10.0) / 10.0
                };
            });
        }

        public async Task<List<FacilityResponse>> GetAllFacilities()
        {
            // These lists also back admin screens. Read current records so a
            // successful create/update/delete is visible on the next request.
            return await _db.ActiveFacilities().OrderBy(f => f.FacilityId)
                .ToFacilityResponses().ToListAsync();
        }

        public async Task<List<SpecialtyResponse>> GetAllSpecialties()
        {
            return await _db.ActiveSpecialties()
                .OrderBy(s => s.SpecialtyId)
                .Select(s => new SpecialtyResponse
                {
                    Id = s.SpecialtyId,
                    Name = s.SpecialtyName,
                    Description = s.Description
                }).ToListAsync();
        }

        public async Task<double> GetMaxDoctorPrice() => await _db.DoctorDetails
            .Where(d => d.VerificationStatus == VerificationStatus.APPROVED)
            .MaxAsync(d => (double?)d.Price) ?? 0;

        // === Tìm bác sĩ: ghép bộ lọc, tìm không dấu bằng unaccent và phân trang tại database. ===
        public async Task<PagedResult<DoctorCardResponse>> GetDoctors(string? keyword, int? specId, int? facilityId, string? province, double? minPrice, double? maxPrice, string? sortBy, int page, int size)
        {
            var query = _db.DoctorDetails
                .Include(d => d.User)
                .Include(d => d.Specialty)
                .Include(d => d.Facility)
                .Where(d => d.VerificationStatus == VerificationStatus.APPROVED);

            // Tìm kiếm CHÍNH XÁC hơn: chỉ tìm theo tên bác sĩ và tên chuyên khoa
            // Không tìm trong Bio hay FacilityName để tránh kết quả sai
            foreach (var word in (keyword ?? "").Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
            {
                var pattern = "%" + word.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%";
                query = query.Where(d => EF.Functions.ILike(
                    EF.Functions.Unaccent((d.User.FullName ?? "") + " " + d.Specialty.SpecialtyName),
                    EF.Functions.Unaccent(pattern), "\\"));
            }
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

            if (sortBy == "rating") query = query
                .OrderByDescending(d => d.ReviewCount > 0 ? (d.RatingAverage ?? 0) : 0)
                .ThenByDescending(d => d.ReviewCount)
                .ThenBy(d => d.DoctorId);
            else if (sortBy == "priceAsc") query = query.OrderBy(d => d.Price);
            else if (sortBy == "priceDesc") query = query.OrderByDescending(d => d.Price);
            else if (sortBy == "experience") query = query.OrderByDescending(d => d.PracticeStartYear.HasValue ? VietnamTime.Now.Year - d.PracticeStartYear.Value : d.ExperienceYears);
            else query = query.OrderBy(d => d.DoctorId);

            var total = await query.CountAsync();
            var items = await query.Skip(page * size).Take(size).ToListAsync();

            var dtos = items.Select(d => new DoctorCardResponse
            {
                DoctorId = d.DoctorId,
                DoctorName = d.User.FullName,
                SpecialtyName = d.Specialty?.SpecialtyName,
                FacilityName = d.Facility?.FacilityName,
                Province = d.Facility?.Province,
                DoctorEmail = d.User.Email,
                DoctorPhone = d.User.PhoneNumber,
                AvatarUrl = d.User.AvatarUrl,
                RatingAverage = d.ReviewCount > 0 ? d.RatingAverage : null,
                TotalReviews = d.ReviewCount,
                Price = d.Price,
                Experience = d.PracticeStartYear.HasValue ? VietnamTime.Now.Year - d.PracticeStartYear.Value : d.ExperienceYears
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
                ExperienceYears = d.PracticeStartYear.HasValue ? VietnamTime.Now.Year - d.PracticeStartYear.Value : d.ExperienceYears,
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
                RatingAverage = d.ReviewCount > 0 ? d.RatingAverage : null,
                TotalReviews = d.ReviewCount
            };
        }

        // === Chi tiết công khai: bảo vệ nội dung đánh giá đã ẩn, lọc trạng thái ca và giờ trong ngày. ===
        public async Task<List<DoctorReviewResponse>> GetReviewsByDoctorId(int doctorId)
        {
            var reviews = await _db.Reviews
                .Include(r => r.Appointment).ThenInclude(a => a.Patient).ThenInclude(p => p.User)
                .Where(r => r.Appointment.Schedule.DoctorId == doctorId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return reviews.Select(r => new DoctorReviewResponse
            {
                ReviewId = r.ReviewId,
                Rating = r.Rating,
                IsVisible = r.IsVisible != false,
                Comment = r.Comment,
                PatientName = r.Appointment.Patient.User?.FullName ?? r.Appointment.Patient.FullName,
                CreatedAt = r.CreatedAt
            }).ToList();
        }

        public async Task<List<DoctorSlotResponse>> GetAvailableSlots(int doctorId, DateOnly date)
        {
            var bookingToday = DateOnly.FromDateTime(VietnamTime.Now);
            if (date < bookingToday || date > bookingToday.AddDays(BookingWindow.MaxAdvanceDays)) return [];
            var d = await _db.DoctorDetails.FindAsync(doctorId) ?? throw new InvalidOperationException("Không tồn tại");
            if (d.VerificationStatus != VerificationStatus.APPROVED) return new List<DoctorSlotResponse>();

            var schedules = await _db.DoctorSchedules
                .Where(s => s.DoctorId == doctorId && s.DateWorking == date && s.SlotStatus == SlotStatus.AVAILABLE)
                .ToListAsync();

            var vnNow = DateTime.UtcNow.AddHours(7); // Giờ Việt Nam (UTC+7)
            var today = DateOnly.FromDateTime(vnNow);
            var vnTime = TimeOnly.FromDateTime(vnNow);

            return schedules.Where(s =>
            {
                // Nếu là ngày hôm nay, chỉ hiện slot chưa qua giờ hiện tại
                if (date == today) return TimeSlotHelper.ParseTimeSlot(s.TimeSlot) > vnTime;
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
