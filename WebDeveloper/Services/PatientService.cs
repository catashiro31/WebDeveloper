using Microsoft.EntityFrameworkCore;
using WebDeveloper.Data;
using WebDeveloper.Helpers;
using WebDeveloper.Models.DTOs.Patient;
using WebDeveloper.Models.Entities;
using WebDeveloper.Models.Enums;
using WebDeveloper.Services.Interfaces;

namespace WebDeveloper.Services
{
    public class PatientService : IPatientService
    {
        private readonly ApplicationDbContext _db;

        public PatientService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<List<RelativeResponse>> GetRelatives(User user)
        {
            var profiles = await _db.PatientProfiles
                .Where(p => p.UserId == user.UserId && p.IsActive)
                .ToListAsync();

            return profiles.Select(MapToRelativeResponse).ToList();
        }

        public async Task<RelativeResponse> GetRelativeById(User user, int id)
        {
            var profile = await _db.PatientProfiles
                .FirstOrDefaultAsync(p => p.PatientId == id && p.UserId == user.UserId && p.IsActive)
                ?? throw new InvalidOperationException("Không tìm thấy người thân hoặc bạn không có quyền xem hồ sơ này!");

            return MapToRelativeResponse(profile);
        }

        private static RelativeResponse MapToRelativeResponse(PatientProfile p)
        {
            return new RelativeResponse
            {
                PatientId = p.PatientId,
                FullName = p.FullName,
                DateOfBirth = p.DateOfBirth,
                Gender = p.Gender?.ToString(),
                PhoneNumber = p.PhoneNumber,
                Address = p.Address,
                Relationship = p.Relationship
            };
        }

        public async Task<string> AddRelative(User user, RelativeRequest req)
        {
            if (await _db.PatientProfiles.AnyAsync(p => p.FullName == req.FullName && p.PhoneNumber == req.PhoneNumber && p.UserId == user.UserId && p.IsActive))
                throw new InvalidOperationException("Hồ sơ bệnh nhân này đã tồn tại!");

            var profile = new PatientProfile
            {
                UserId = user.UserId,
                FullName = req.FullName,
                DateOfBirth = req.DateOfBirth,
                Gender = req.Gender,
                PhoneNumber = req.PhoneNumber,
                Address = req.Address,
                Relationship = req.Relationship,
                IsActive = true
            };

            _db.PatientProfiles.Add(profile);
            await _db.SaveChangesAsync();
            return "Thêm hồ sơ thành công";
        }

        public async Task<string> UpdateRelative(User user, int id, RelativeRequest req)
        {
            var profile = await _db.PatientProfiles.FindAsync(id)
                ?? throw new InvalidOperationException("Không tìm thấy hồ sơ!");

            if (profile.UserId != user.UserId)
                throw new InvalidOperationException("Bạn không có quyền sửa hồ sơ này!");

            if (await _db.PatientProfiles.AnyAsync(p => p.FullName == req.FullName && p.PhoneNumber == req.PhoneNumber && p.UserId == user.UserId && p.IsActive && p.PatientId != id))
                throw new InvalidOperationException("Hồ sơ bệnh nhân này đã tồn tại!");

            profile.FullName = req.FullName;
            profile.DateOfBirth = req.DateOfBirth;
            profile.Gender = req.Gender;
            profile.PhoneNumber = req.PhoneNumber;
            profile.Address = req.Address;
            profile.Relationship = req.Relationship;

            await _db.SaveChangesAsync();
            return "Cập nhật hồ sơ thành công";
        }

        public async Task<string> DeleteRelative(User user, int id)
        {
            var profile = await _db.PatientProfiles.FindAsync(id)
                ?? throw new InvalidOperationException("Không tìm thấy hồ sơ!");

            if (profile.UserId != user.UserId)
                throw new InvalidOperationException("Bạn không có quyền xóa hồ sơ này!");

            if (profile.Relationship == "SELF")
                throw new InvalidOperationException("Không thể xóa hồ sơ chính!");

            var activeStatuses = new[] { BookingStatus.PENDING, BookingStatus.CONFIRMED };
            var hasActiveApps = await _db.Appointments
                .AnyAsync(a => a.PatientId == id && activeStatuses.Contains(a.BookingStatus));

            if (hasActiveApps)
                throw new InvalidOperationException("Không thể xóa hồ sơ này vì đang có lịch hẹn chưa hoàn thành. Vui lòng hủy lịch hẹn trước!");

            profile.IsActive = false;
            await _db.SaveChangesAsync();
            return "Xóa hồ sơ người thân thành công";
        }

        public async Task<string> BookAppointment(User user, AppointmentRequest req)
        {
            // Giới hạn 3 lịch hẹn active (giống Java)
            var activeStatuses = new[] { BookingStatus.PENDING, BookingStatus.CONFIRMED };
            var activeCount = await _db.Appointments
                .Where(a => a.Patient.UserId == user.UserId && activeStatuses.Contains(a.BookingStatus))
                .CountAsync();

            if (activeCount >= 3)
                throw new InvalidOperationException("Bạn chỉ được đặt tối đa 3 lịch hẹn!");

            var profile = await _db.PatientProfiles.FindAsync(req.PatientId)
                ?? throw new InvalidOperationException("Hồ sơ bệnh nhân này không tồn tại hoặc bạn không có quyền sử dụng!");

            if (profile.UserId != user.UserId)
                throw new InvalidOperationException("Hồ sơ này không thuộc về bạn!");

            var schedule = await _db.DoctorSchedules
                .Include(s => s.Doctor)
                .FirstOrDefaultAsync(s => s.ScheduleId == req.ScheduleId)
                ?? throw new InvalidOperationException("Ca khám không tồn tại");

            if (schedule.Doctor.VerificationStatus != VerificationStatus.APPROVED)
                throw new InvalidOperationException("Bác sĩ chưa được xác minh, không thể đặt lịch!");

            if (schedule.SlotStatus != SlotStatus.AVAILABLE)
                throw new InvalidOperationException("Ca khám đã có người đặt hoặc đã đóng");

            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            if (schedule.DateWorking < today)
                throw new InvalidOperationException("Không thể đặt lịch cho ngày đã qua!");

            // Overlap check
            var overlapStatuses = new[] { BookingStatus.PENDING, BookingStatus.CONFIRMED };
            var isOverlapping = await _db.Appointments
                .AnyAsync(a => a.PatientId == req.PatientId && a.Schedule.DateWorking == schedule.DateWorking && a.Schedule.TimeSlot == schedule.TimeSlot && overlapStatuses.Contains(a.BookingStatus));
            
            if (isOverlapping)
                throw new InvalidOperationException("Hồ sơ bệnh nhân này đã có một lịch hẹn khác vào cùng thời gian!");

            schedule.SlotStatus = SlotStatus.BOOKED;

            var appointment = new Appointment
            {
                PatientId = req.PatientId,
                ScheduleId = req.ScheduleId,
                Reason = req.Reason,
                BookingStatus = BookingStatus.PENDING,
                CreatedAt = DateTime.UtcNow
            };

            _db.Appointments.Add(appointment);
            await _db.SaveChangesAsync();
            return "Đặt lịch khám thành công";
        }

        public async Task<string> CancelAppointment(User user, int appointmentId)
        {
            var app = await _db.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Schedule)
                .FirstOrDefaultAsync(a => a.Id == appointmentId)
                ?? throw new InvalidOperationException("Không tìm thấy lịch hẹn");

            if (app.Patient.UserId != user.UserId)
                throw new InvalidOperationException("Bạn không có quyền");

            if (app.BookingStatus != BookingStatus.PENDING && app.BookingStatus != BookingStatus.CONFIRMED)
                throw new InvalidOperationException("Không thể hủy lịch hẹn ở trạng thái này");

            app.BookingStatus = BookingStatus.CANCELLED;
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            if (app.Schedule.DateWorking >= today)
            {
                app.Schedule.SlotStatus = SlotStatus.AVAILABLE;
            }

            await _db.SaveChangesAsync();
            return "Hủy lịch hẹn thành công";
        }

        public async Task<PagedResult<AppointmentResponse>> GetAppointments(User user, BookingStatus? status, DateOnly? startDate, DateOnly? endDate, int page, int size)
        {
            var query = _db.Appointments
                .Include(a => a.Schedule).ThenInclude(s => s.Doctor).ThenInclude(d => d.Specialty)
                .Include(a => a.Schedule).ThenInclude(s => s.Doctor).ThenInclude(d => d.Facility)
                .Include(a => a.Schedule).ThenInclude(s => s.Doctor).ThenInclude(d => d.User)
                .Include(a => a.Patient)
                .AsSplitQuery()
                .Where(a => a.Patient.UserId == user.UserId);

            if (status.HasValue)
                query = query.Where(a => a.BookingStatus == status.Value);

            if (startDate.HasValue && endDate.HasValue)
                query = query.Where(a => a.Schedule.DateWorking >= startDate.Value && a.Schedule.DateWorking <= endDate.Value);

            query = query.OrderByDescending(a => a.Schedule.DateWorking);

            var total = await query.CountAsync();
            var items = await query.Skip(page * size).Take(size).ToListAsync();

            var appIds = items.Select(a => a.Id).ToList();
            var results = await _db.MedicalResults.Where(m => appIds.Contains(m.AppointmentId)).Select(m => m.AppointmentId).ToListAsync();

            var dtos = items.Select(a => new AppointmentResponse
            {
                AppointmentId = a.Id,
                PatientName = a.Patient.FullName,
                DoctorName = a.Schedule.Doctor.User.FullName,
                SpecialtyName = a.Schedule.Doctor.Specialty.SpecialtyName,
                FacilityName = a.Schedule.Doctor.Facility.FacilityName,
                Address = a.Schedule.Doctor.Facility.Address,
                DateWorking = a.Schedule.DateWorking,
                TimeSlot = a.Schedule.TimeSlot.GetDisplayValue(),
                BookingStatus = a.BookingStatus.ToString(),
                CreatedAt = a.CreatedAt,
                HasResult = results.Contains(a.Id)
            }).ToList();

            return PagedResult<AppointmentResponse>.Create(dtos, page, size, total);
        }

        public async Task<AppointmentDetailResponse> GetAppointmentDetail(User user, int appointmentId)
        {
            var a = await _db.Appointments
                .Include(a => a.Schedule).ThenInclude(s => s.Doctor).ThenInclude(d => d.Specialty)
                .Include(a => a.Schedule).ThenInclude(s => s.Doctor).ThenInclude(d => d.Facility)
                .Include(a => a.Schedule).ThenInclude(s => s.Doctor).ThenInclude(d => d.User)
                .Include(a => a.Patient)
                .FirstOrDefaultAsync(a => a.Id == appointmentId)
                ?? throw new InvalidOperationException("Không tìm thấy lịch hẹn");

            if (a.Patient.UserId != user.UserId)
                throw new InvalidOperationException("Bạn không có quyền");

            var result = await _db.MedicalResults.FirstOrDefaultAsync(m => m.AppointmentId == a.Id);
            var review = await _db.Reviews.FirstOrDefaultAsync(r => r.AppointmentId == a.Id);

            return new AppointmentDetailResponse
            {
                AppointmentId = a.Id,
                PatientName = a.Patient.FullName,
                DoctorName = a.Schedule.Doctor.User.FullName,
                SpecialtyName = a.Schedule.Doctor.Specialty.SpecialtyName,
                FacilityName = a.Schedule.Doctor.Facility.FacilityName,
                Address = a.Schedule.Doctor.Facility.Address,
                DateWorking = a.Schedule.DateWorking,
                TimeSlot = a.Schedule.TimeSlot.GetDisplayValue(),
                BookingStatus = a.BookingStatus.ToString(),
                CreatedAt = a.CreatedAt,
                HasResult = result != null,
                Diagnosis = result?.Diagnosis,
                PrescriptionUrl = result?.PrescriptionUrl,
                DoctorNotes = result?.DoctorNotes,
                Rating = review?.Rating,
                Comment = review?.Comment
            };
        }

        public async Task<string> SubmitReview(User user, int appointmentId, ReviewRequest req)
        {
            var app = await _db.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Schedule).ThenInclude(s => s.Doctor)
                .FirstOrDefaultAsync(a => a.Id == appointmentId)
                ?? throw new InvalidOperationException("Lịch hẹn không tồn tại");

            if (app.Patient.UserId != user.UserId)
                throw new InvalidOperationException("Bạn không có quyền đánh giá");

            if (app.BookingStatus != BookingStatus.COMPLETED)
                throw new InvalidOperationException("Chỉ có thể đánh giá sau khi hoàn thành khám");

            if (await _db.Reviews.AnyAsync(r => r.AppointmentId == appointmentId))
                throw new InvalidOperationException("Bạn đã đánh giá lịch hẹn này rồi");

            var review = new Review
            {
                AppointmentId = appointmentId,
                Rating = req.Rating,
                Comment = req.Comment,
                IsVisible = true,
                CreatedAt = DateTime.UtcNow
            };
            _db.Reviews.Add(review);
            await _db.SaveChangesAsync();

            // Update Doctor Stats
            var docId = app.Schedule.DoctorId;
            var doc = await _db.DoctorDetails.FindAsync(docId);
            if (doc != null)
            {
                var reviews = await _db.Reviews
                    .Include(r => r.Appointment).ThenInclude(a => a.Schedule)
                    .Where(r => r.Appointment.Schedule.DoctorId == docId && r.IsVisible == true)
                    .ToListAsync();
                
                doc.ReviewCount = reviews.Count;
                doc.RatingAverage = reviews.Any() ? Math.Round(reviews.Average(r => r.Rating ?? 0) * 10) / 10.0 : 0;
            }

            await _db.SaveChangesAsync();
            return "Gửi đánh giá thành công!";
        }

        public async Task<string> UpdateReview(User user, int appointmentId, ReviewRequest req)
        {
            var app = await _db.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Schedule).ThenInclude(s => s.Doctor)
                .FirstOrDefaultAsync(a => a.Id == appointmentId)
                ?? throw new InvalidOperationException("Lịch hẹn không tồn tại");

            if (app.Patient.UserId != user.UserId)
                throw new InvalidOperationException("Bạn không có quyền đánh giá");

            if (app.BookingStatus != BookingStatus.COMPLETED)
                throw new InvalidOperationException("Chỉ có thể đánh giá sau khi hoàn thành khám");

            var review = await _db.Reviews.FirstOrDefaultAsync(r => r.AppointmentId == appointmentId)
                ?? throw new InvalidOperationException("Chưa có đánh giá nào để cập nhật!");

            review.Rating = req.Rating;
            review.Comment = req.Comment;
            await _db.SaveChangesAsync();

            // Update Doctor Stats
            var docId = app.Schedule.DoctorId;
            var doc = await _db.DoctorDetails.FindAsync(docId);
            if (doc != null)
            {
                var reviews = await _db.Reviews
                    .Include(r => r.Appointment).ThenInclude(a => a.Schedule)
                    .Where(r => r.Appointment.Schedule.DoctorId == docId && r.IsVisible == true)
                    .ToListAsync();

                doc.ReviewCount = reviews.Count;
                doc.RatingAverage = reviews.Any() ? Math.Round(reviews.Average(r => r.Rating ?? 0) * 10) / 10.0 : 0;
            }

            await _db.SaveChangesAsync();
            return "Cập nhật đánh giá thành công!";
        }
    }
}
