using Microsoft.EntityFrameworkCore;
using WebDeveloper.Data;
using WebDeveloper.Helpers;
using WebDeveloper.Models.DTOs.Admin;
using WebDeveloper.Models.Entities;
using WebDeveloper.Models.Enums;
using WebDeveloper.Services.Interfaces;

namespace WebDeveloper.Services
{
    public class AdminService : IAdminService
    {
        private readonly ApplicationDbContext _db;
        private readonly IFileUploadService _fileUpload;
        private readonly IEmailService _emailService;

        public AdminService(ApplicationDbContext db, IFileUploadService fileUpload, IEmailService emailService)
        {
            _db = db;
            _fileUpload = fileUpload;
            _emailService = emailService;
        }

        public async Task<StatResponse> GetStats(DateOnly? start, DateOnly? end)
        {
            var actualStart = start ?? DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30));
            var actualEnd = end ?? DateOnly.FromDateTime(DateTime.UtcNow);

            var apps = await _db.Appointments
                .Include(a => a.Schedule)
                .Where(a => a.Schedule.DateWorking >= actualStart && a.Schedule.DateWorking <= actualEnd)
                .ToListAsync();

            long completed = apps.Count(a => a.BookingStatus == BookingStatus.COMPLETED);
            long pending = apps.Count(a => a.BookingStatus == BookingStatus.PENDING || a.BookingStatus == BookingStatus.CONFIRMED);
            long cancelled = apps.Count(a => a.BookingStatus == BookingStatus.CANCELLED);

            var startDateTime = actualStart.ToDateTime(TimeOnly.MinValue);
            var endDateTime = actualEnd.AddDays(1).ToDateTime(TimeOnly.MinValue);

            long doctorsInPeriod = await _db.Users.CountAsync(u => u.Role == RoleStatus.DOCTOR && u.IsActive == true && u.CreatedAt >= startDateTime && u.CreatedAt < endDateTime);
            long patientsInPeriod = await _db.Users.CountAsync(u => u.Role == RoleStatus.PATIENT && u.IsActive == true && u.CreatedAt >= startDateTime && u.CreatedAt < endDateTime);

            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            long todayApps = await _db.Appointments.CountAsync(a => a.Schedule.DateWorking == today);

            return new StatResponse
            {
                NumberOfDoctors = doctorsInPeriod,
                NumberOfPatients = patientsInPeriod,
                NumberOfSuccessAppointments = completed,
                NumberOfPendingAppointments = pending,
                NumberOfFailingAppointments = cancelled,
                TotalUsers = await _db.Users.CountAsync(),
                TotalDoctors = await _db.DoctorDetails.CountAsync(d => d.VerificationStatus == VerificationStatus.APPROVED && d.User.IsActive == true),
                TotalPatients = await _db.Users.CountAsync(u => u.Role == RoleStatus.PATIENT && u.IsActive == true),
                TotalAppointments = await _db.Appointments.CountAsync(),
                PendingDoctors = await _db.DoctorDetails.CountAsync(d => d.VerificationStatus == VerificationStatus.PENDING),
                TodayAppointments = todayApps,
                TotalReviews = await _db.Reviews.CountAsync()
            };
        }

        public async Task<PagedResult<object>> GetAllDoctors(int page, int size)
        {
            var query = _db.DoctorDetails
                .Include(d => d.User)
                .Include(d => d.Specialty)
                .Include(d => d.Facility)
                .Where(d => d.VerificationStatus == VerificationStatus.APPROVED)
                .OrderByDescending(d => d.DoctorId);

            var total = await query.CountAsync();
            var items = await query.Skip(page * size).Take(size).ToListAsync();

            // Return generic objects (as Java does returning entities, but DTO is better)
            var dtos = items.Select(d => new
            {
                d.DoctorId,
                d.User.FullName,
                d.User.Email,
                d.User.AvatarUrl,
                VerificationStatus = d.VerificationStatus.ToString(),
                SpecialtyName = d.Specialty?.SpecialtyName,
                FacilityName = d.Facility?.FacilityName,
                d.RatingAverage,
                d.ReviewCount
            }).Cast<object>().ToList();

            return PagedResult<object>.Create(dtos, page, size, total);
        }

        public async Task<List<object>> GetPendingDoctors()
        {
            var items = await _db.DoctorDetails
                .Include(d => d.User)
                .Include(d => d.Specialty)
                .Include(d => d.Facility)
                .Where(d => d.VerificationStatus == VerificationStatus.PENDING)
                .ToListAsync();

            return items.Select(d => new
            {
                d.DoctorId,
                d.User.FullName,
                d.User.Email,
                d.User.AvatarUrl,
                VerificationStatus = d.VerificationStatus.ToString(),
                SpecialtyName = d.Specialty?.SpecialtyName,
                FacilityName = d.Facility?.FacilityName,
                d.Degree,
                d.ExperienceYears
            }).Cast<object>().ToList();
        }

        public async Task<object> GetDoctorDetail(int id)
        {
            var d = await _db.DoctorDetails
                .Include(d => d.User)
                .Include(d => d.Specialty)
                .Include(d => d.Facility)
                .FirstOrDefaultAsync(d => d.DoctorId == id)
                ?? throw new InvalidOperationException("Không tìm thấy thông tin bác sĩ");

            return new
            {
                d.DoctorId,
                FullName = d.User.FullName,
                Email = d.User.Email,
                PhoneNumber = d.User.PhoneNumber,
                AvatarUrl = d.User.AvatarUrl,
                SpecialtyName = d.Specialty?.SpecialtyName,
                FacilityName = d.Facility?.FacilityName,
                FacilityAddress = d.Facility?.Address,
                FacilityProvince = d.Facility?.Province,
                FacilityLicenseUrl = d.Facility?.LicenseUrl,
                FacilityVerified = d.Facility?.IsVerified,
                d.Price,
                d.Degree,
                d.ExperienceYears,
                d.Bio,
                d.IdCardUrl,
                d.CertificateUrl,
                VerificationStatus = d.VerificationStatus.ToString(),
                d.RatingAverage,
                d.ReviewCount
            };
        }

        public async Task<object> ApproveDoctor(int doctorId)
        {
            var doctor = await _db.DoctorDetails
                .Include(d => d.User)
                .Include(d => d.Facility)
                .FirstOrDefaultAsync(d => d.DoctorId == doctorId)
                ?? throw new InvalidOperationException("Không tìm thấy thông tin bác sĩ");

            if (doctor.VerificationStatus != VerificationStatus.PENDING)
                throw new InvalidOperationException("Chỉ có thể duyệt bác sĩ đang chờ");

            doctor.VerificationStatus = VerificationStatus.APPROVED;

            if (doctor.Facility != null && !doctor.Facility.IsVerified)
            {
                doctor.Facility.IsVerified = true;
            }

            await _db.SaveChangesAsync();
            await _emailService.SendDoctorApprovedEmail(doctor.User.Email, doctor.User.FullName ?? "");
            
            return new { Message = "Duyệt thành công" };
        }

        public async Task<object> RejectDoctor(int doctorId, string reason)
        {
            var doctor = await _db.DoctorDetails
                .Include(d => d.User)
                .FirstOrDefaultAsync(d => d.DoctorId == doctorId)
                ?? throw new InvalidOperationException("Không tìm thấy thông tin bác sĩ");

            if (doctor.VerificationStatus != VerificationStatus.PENDING)
                throw new InvalidOperationException("Chỉ có thể từ chối bác sĩ đang chờ");

            doctor.VerificationStatus = VerificationStatus.REJECTED;
            doctor.ReasonReject = reason;

            await _db.SaveChangesAsync();
            await _emailService.SendDoctorRejectedEmail(doctor.User.Email, doctor.User.FullName ?? "", reason);

            return new { Message = "Từ chối thành công" };
        }

        public async Task<string> AddSpecialty(SpecialtyRequest req)
        {
            if (await _db.Specialties.AnyAsync(s => s.SpecialtyName.ToLower() == req.SpecialtyName.ToLower()))
                throw new InvalidOperationException("Chuyên khoa đã tồn tại!");

            _db.Specialties.Add(new Specialty
            {
                SpecialtyName = req.SpecialtyName.Trim(),
                Description = req.Description,
                IsActive = true
            });
            await _db.SaveChangesAsync();
            return "Đã thêm chuyên khoa";
        }

        public async Task<string> UpdateSpecialty(int id, SpecialtyRequest req)
        {
            var s = await _db.Specialties.FindAsync(id)
                ?? throw new InvalidOperationException("Không tìm thấy chuyên khoa");

            if (await _db.Specialties.AnyAsync(sp => sp.SpecialtyName.ToLower() == req.SpecialtyName.ToLower() && sp.SpecialtyId != id))
                throw new InvalidOperationException("Tên chuyên khoa đã được sử dụng");

            s.SpecialtyName = req.SpecialtyName.Trim();
            s.Description = req.Description;
            await _db.SaveChangesAsync();
            return "Đã sửa thành công";
        }

        public async Task<string> DeleteSpecialty(int id)
        {
            var s = await _db.Specialties.FindAsync(id)
                ?? throw new InvalidOperationException("Không tìm thấy chuyên khoa");

            if (await _db.DoctorDetails.AnyAsync(d => d.SpecialtyId == id))
                throw new InvalidOperationException("Không thể xóa chuyên khoa đang có bác sĩ");

            s.IsActive = false;
            await _db.SaveChangesAsync();
            return "Đã xóa chuyên ngành";
        }

        public async Task<string> AddFacility(FacilityRequest req)
        {
            if (await _db.Facilities.AnyAsync(f => f.FacilityName!.ToLower() == req.FacilityName.ToLower()))
                throw new InvalidOperationException("Cơ sở y tế đã tồn tại!");

            if (req.LicenseFile == null || req.LicenseFile.Length == 0)
                throw new InvalidOperationException("Vui lòng tải lên giấy phép hoạt động!");

            _fileUpload.ValidateFile(req.LicenseFile, "Giấy phép hoạt động", "application/pdf", "image/jpeg", "image/png");

            var facility = new Facility
            {
                FacilityName = req.FacilityName.Trim(),
                Address = req.Address,
                Description = req.Description,
                MapUrl = req.MapUrl,
                Province = req.Province,
                IsActive = true,
                IsVerified = true, // Admin tạo thì mặc định verified
                LicenseUrl = await _fileUpload.UploadFileAsync(req.LicenseFile)
            };

            if (req.File != null)
            {
                _fileUpload.ValidateFile(req.File, "Ảnh", "image/jpeg", "image/png");
                facility.ImageUrl = await _fileUpload.UploadFileAsync(req.File);
            }

            _db.Facilities.Add(facility);
            await _db.SaveChangesAsync();
            return "Đã thêm thành công";
        }

        public async Task<string> UpdateFacility(int id, FacilityRequest req)
        {
            var f = await _db.Facilities.FindAsync(id)
                ?? throw new InvalidOperationException("Không tìm thấy cơ sở y tế");

            if (await _db.Facilities.AnyAsync(fac => fac.FacilityName!.ToLower() == req.FacilityName.ToLower() && fac.FacilityId != id))
                throw new InvalidOperationException("Tên cơ sở đã được sử dụng");

            f.FacilityName = req.FacilityName.Trim();
            f.Address = req.Address;
            f.Description = req.Description;
            f.MapUrl = req.MapUrl;
            f.Province = req.Province;

            if (req.File != null)
            {
                _fileUpload.ValidateFile(req.File, "Ảnh", "image/jpeg", "image/png");
                f.ImageUrl = await _fileUpload.UploadFileAsync(req.File);
            }
            if (req.LicenseFile != null)
            {
                _fileUpload.ValidateFile(req.LicenseFile, "Giấy phép", "application/pdf", "image/jpeg", "image/png");
                f.LicenseUrl = await _fileUpload.UploadFileAsync(req.LicenseFile);
            }

            await _db.SaveChangesAsync();
            return "Đã cập nhật thành công";
        }

        public async Task<string> VerifyFacility(int id)
        {
            var f = await _db.Facilities.FindAsync(id)
                ?? throw new InvalidOperationException("Không tìm thấy");
            if (!f.IsActive) throw new InvalidOperationException("Cơ sở ngừng hoạt động");
            f.IsVerified = true;
            await _db.SaveChangesAsync();
            return "Đã xác minh";
        }

        public async Task<string> DeleteFacility(int id)
        {
            var f = await _db.Facilities.FindAsync(id)
                ?? throw new InvalidOperationException("Không tìm thấy");
            
            if (await _db.DoctorDetails.AnyAsync(d => d.FacilityId == id))
                throw new InvalidOperationException("Không thể xóa cơ sở đang có bác sĩ");
            
            f.IsActive = false;
            await _db.SaveChangesAsync();
            return "Đã xóa";
        }

        public async Task<PagedResult<object>> GetAllUsers(int page, int size)
        {
            var query = _db.Users.OrderByDescending(u => u.CreatedAt);
            var total = await query.CountAsync();
            var items = await query.Skip(page * size).Take(size).Select(u => new
            {
                u.UserId,
                u.Email,
                u.FullName,
                u.Role,
                u.IsActive,
                u.ReasonBanned,
                u.CreatedAt
            }).Cast<object>().ToListAsync();

            return PagedResult<object>.Create(items, page, size, total);
        }

        public async Task<string> BlockUser(int id, string reason)
        {
            var user = await _db.Users.FindAsync(id)
                ?? throw new InvalidOperationException("Không tìm thấy người dùng");
            
            user.IsActive = false;
            user.ReasonBanned = reason;

            // Hủy lịch nếu là bệnh nhân
            var activeStatuses = new[] { BookingStatus.PENDING, BookingStatus.CONFIRMED };
            var pApps = await _db.Appointments.Include(a => a.Schedule).Where(a => a.Patient.UserId == id && activeStatuses.Contains(a.BookingStatus)).ToListAsync();
            foreach(var a in pApps)
            {
                a.BookingStatus = BookingStatus.CANCELLED;
                if (a.Schedule.DateWorking >= DateOnly.FromDateTime(DateTime.UtcNow))
                    a.Schedule.SlotStatus = SlotStatus.AVAILABLE;
            }

            if (user.Role == RoleStatus.DOCTOR)
            {
                var dApps = await _db.Appointments.Where(a => a.Schedule.Doctor.UserId == id && activeStatuses.Contains(a.BookingStatus)).ToListAsync();
                foreach(var a in dApps) a.BookingStatus = BookingStatus.CANCELLED;

                var dSlots = await _db.DoctorSchedules.Where(s => s.Doctor.UserId == id && s.SlotStatus == SlotStatus.AVAILABLE).ToListAsync();
                foreach(var s in dSlots) s.SlotStatus = SlotStatus.CLOSED;
            }

            await _db.SaveChangesAsync();
            await _emailService.SendPermanentBanEmail(user.Email, user.FullName ?? "", reason);
            return $"Đã khóa tài khoản {id}";
        }

        public async Task<string> UnblockUser(int id)
        {
            var user = await _db.Users.FindAsync(id)
                ?? throw new InvalidOperationException("Không tìm thấy người dùng");
            user.IsActive = true;
            user.ReasonBanned = null;
            await _db.SaveChangesAsync();
            return $"Đã mở khóa tài khoản {id}";
        }

        public async Task<PagedResult<AppointmentAdminResponse>> GetAllAppointments(DateOnly? dateFrom, DateOnly? dateTo, BookingStatus? status, int page, int size)
        {
            var query = _db.Appointments.AsNoTracking().AsQueryable();

            if (dateFrom.HasValue) query = query.Where(a => a.CreatedAt >= dateFrom.Value.ToDateTime(TimeOnly.MinValue));
            if (dateTo.HasValue) query = query.Where(a => a.CreatedAt < dateTo.Value.AddDays(1).ToDateTime(TimeOnly.MinValue));
            if (status.HasValue) query = query.Where(a => a.BookingStatus == status.Value);

            query = query.OrderByDescending(a => a.CreatedAt);
            var total = await query.CountAsync();
            var dtos = await query.Skip(page * size).Take(size).Select(a => new AppointmentAdminResponse
            {
                AppointmentId = a.Id,
                PatientId = a.PatientId,
                PatientName = a.Patient.FullName,
                PatientPhone = a.Patient.PhoneNumber,
                PatientEmail = a.Patient.User.Email,
                DoctorId = a.Schedule.DoctorId,
                DoctorName = a.Schedule.Doctor.User.FullName,
                SpecialtyName = a.Schedule.Doctor.Specialty.SpecialtyName,
                FacilityName = a.Schedule.Doctor.Facility.FacilityName,
                DateWorking = a.Schedule.DateWorking,
                TimeSlot = a.Schedule.TimeSlot.GetDisplayValue(),
                Reason = a.Reason,
                BookingStatus = a.BookingStatus,
                CreatedAt = a.CreatedAt
            }).ToListAsync();

            return PagedResult<AppointmentAdminResponse>.Create(dtos, page, size, total);
        }

        public async Task<PagedResult<ReviewAdminResponse>> GetAllReviews(int page, int size)
        {
            var query = _db.Reviews
                .Include(r => r.Appointment).ThenInclude(a => a.Patient).ThenInclude(p => p.User)
                .Include(r => r.Appointment).ThenInclude(a => a.Schedule).ThenInclude(s => s.Doctor).ThenInclude(d => d.User)
                .OrderByDescending(r => r.CreatedAt);

            var total = await query.CountAsync();
            var items = await query.Skip(page * size).Take(size).ToListAsync();

            var dtos = items.Select(r => new ReviewAdminResponse
            {
                ReviewId = r.ReviewId,
                PatientName = r.Appointment.Patient.User?.FullName ?? r.Appointment.Patient.FullName,
                DoctorName = r.Appointment.Schedule.Doctor.User.FullName,
                Rating = r.Rating,
                Comment = r.Comment,
                CreatedAt = r.CreatedAt,
                IsVisible = r.IsVisible,
                AiLabels = AnalyzeComment(r.Comment ?? "")
            }).ToList();

            return PagedResult<ReviewAdminResponse>.Create(dtos, page, size, total);
        }

        public async Task<string> RejectReview(int id)
        {
            var review = await _db.Reviews.Include(r => r.Appointment).ThenInclude(a => a.Schedule).FirstOrDefaultAsync(r => r.ReviewId == id)
                ?? throw new InvalidOperationException("Không tìm thấy");
            
            // Chuyển đổi trạng thái ẩn/hiện
            bool willBeVisible = (review.IsVisible == false);
            review.IsVisible = willBeVisible;
            await _db.SaveChangesAsync();

            // Cập nhật lại RatingAverage và ReviewCount cho bác sĩ ngay lập tức
            if (review.Appointment?.Schedule != null)
            {
                var docId = review.Appointment.Schedule.DoctorId;
                var stats = await _db.Reviews
                    .Where(r => r.Appointment.Schedule.DoctorId == docId)
                    .GroupBy(r => r.Appointment.Schedule.DoctorId)
                    .Select(g => new { Count = g.Count(), Avg = g.Average(x => (double?)(x.Rating ?? 0)) })
                    .FirstOrDefaultAsync();

                var doctor = await _db.DoctorDetails.FindAsync(docId);
                if (doctor != null)
                {
                    doctor.ReviewCount = stats?.Count ?? 0;
                    doctor.RatingAverage = stats != null && stats.Count > 0 ? Math.Round((stats.Avg ?? 0) * 10) / 10.0 : 0;
                    await _db.SaveChangesAsync();
                }
            }

            return willBeVisible ? "Đã hiển thị lại bài đánh giá" : "Đã ẩn bài đánh giá";
        }

        // Tự động phân tích và thanh lọc bình luận tiếng Việt (phát hiện từ ngữ thô tục / phản cảm)
        public Dictionary<string, double> AnalyzeComment(string text)
        {
            var results = new Dictionary<string, double>();
            if (string.IsNullOrWhiteSpace(text)) return results;

            var lower = text.ToLowerInvariant();
            
            // Danh sách các từ khóa thô tục, phản cảm thường gặp trong tiếng Việt
            var vulgarKeywords = new[]
            {
                "đm", "dm", "đcm", "dcm", "vcl", "vcll", "vl", "cl", "đụ", "cặc", "lồn",
                "địt", "ngu", "chó", "khốn nạn", "mẹ kiếp", "lừa đảo", "súc vật", "con đĩ",
                "đĩ", "mất dạy", "bố láo", "khốn khiếp", "đếch", "mẹ mày", "con mẹ",
                "thằng điên", "con điên", "đồ chó", "đéo", "đeo", "dmm", "đmm"
            };

            bool isVulgar = false;
            // Tách từ để kiểm tra chính xác hoặc kiểm tra từ ghép
            var words = lower.Split(new[] { ' ', ',', '.', '!', '?', ';', ':', '-', '_', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var kw in vulgarKeywords)
            {
                if (kw.Contains(' '))
                {
                    if (lower.Contains(kw)) { isVulgar = true; break; }
                }
                else
                {
                    if (words.Contains(kw)) { isVulgar = true; break; }
                }
            }

            if (isVulgar)
            {
                results["Thô tục"] = 95.0;
                results["Cần xem xét"] = 100.0;
                results["Tiêu cực"] = 80.0;
                results["Tích cực"] = 5.0;
            }
            else
            {
                // Kiểm tra tiêu cực thông thường
                var negativeKeywords = new[] { "tệ", "kém", "chán", "dở", "thất vọng", "bực", "chờ lâu", "chậm", "khó chịu" };
                bool isNegative = negativeKeywords.Any(n => lower.Contains(n));

                if (isNegative)
                {
                    results["Tiêu cực"] = 75.0;
                    results["Tích cực"] = 20.0;
                    results["Thô tục"] = 0.0;
                }
                else
                {
                    results["Tích cực"] = 85.0;
                    results["Tiêu cực"] = 10.0;
                    results["Thô tục"] = 0.0;
                }
            }

            return results;
        }
    }
}
