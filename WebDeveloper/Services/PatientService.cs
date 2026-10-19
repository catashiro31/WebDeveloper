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
        private readonly StackExchange.Redis.IConnectionMultiplexer _redis;
        private readonly IConfiguration _configuration;

        public PatientService(ApplicationDbContext db, StackExchange.Redis.IConnectionMultiplexer redis, IConfiguration configuration)
        {
            _db = db;
            _redis = redis;
            _configuration = configuration;
        }

        // === Hồ sơ người khám: một tài khoản quản lý nhiều hồ sơ; PatientId không phải UserId. ===
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
            req.Relationship = PatientRelationship.Validate(req.Relationship);
            var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));
            if (req.DateOfBirth > today)
                throw new InvalidOperationException("Ngày sinh không được ở tương lai!");
            if (today.Year - req.DateOfBirth.Year > 100)
                throw new InvalidOperationException("Độ tuổi không được vượt quá 100 tuổi!");

            if (req.PhoneNumber.Distinct().Count() <= 2 || System.Text.RegularExpressions.Regex.IsMatch(req.PhoneNumber, @"^0[35789](\d)\1{7}$"))
                throw new InvalidOperationException("Số điện thoại không hợp lệ (không được là chuỗi số lặp)!");

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

            if (!profile.IsActive)
                throw new InvalidOperationException("Hồ sơ đã bị xóa. Vui lòng tải lại danh sách.");

            req.Relationship = PatientRelationship.Validate(req.Relationship, profile.Relationship);

            var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));
            if (req.DateOfBirth > today)
                throw new InvalidOperationException("Ngày sinh không được ở tương lai!");
            if (today.Year - req.DateOfBirth.Year > 100)
                throw new InvalidOperationException("Độ tuổi không được vượt quá 100 tuổi!");

            if (req.PhoneNumber.Distinct().Count() <= 2 || System.Text.RegularExpressions.Regex.IsMatch(req.PhoneNumber, @"^0[35789](\d)\1{7}$"))
                throw new InvalidOperationException("Số điện thoại không hợp lệ (không được là chuỗi số lặp)!");

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

        // === Đặt lịch: khóa Redis theo ca, kiểm tra chủ hồ sơ, hạn mức và trùng giờ trước khi lưu. ===
        public async Task<string> BookAppointment(User user, AppointmentRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Reason) || req.Reason.Trim().Length < 10 || req.Reason.Trim().Length > 500)
                throw new InvalidOperationException("Lý do khám phải từ 10 đến 500 ký tự!");

            // Lock phân tán bằng Redis để chống Double Booking
            var redisDb = _redis.GetDatabase();
            var lockKey = $"lock:schedule:{req.ScheduleId}";
            
            // Khóa có hạn 30 giây; chỉ chủ sở hữu được giải phóng khóa.
            var lockToken = Guid.NewGuid().ToString("N");
            bool isLocked = await redisDb.StringSetAsync(lockKey, lockToken, TimeSpan.FromSeconds(30), StackExchange.Redis.When.NotExists);

            if (!isLocked)
            {
                throw new InvalidOperationException("Ca khám này đang có người khác thao tác đặt lịch. Vui lòng thử lại sau ít giây!");
            }

            try
            {
                await using var transaction = await _db.Database.BeginTransactionAsync();
                // Serialize bookings across different slots for the same account.
                await _db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({-user.UserId})");
                if (!await _db.Users.AnyAsync(u => u.UserId == user.UserId && u.IsActive == true))
                    throw new InvalidOperationException("Tài khoản không hoạt động, không thể đặt lịch.");
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
                if (!profile.IsActive)
                    throw new InvalidOperationException("Hồ sơ bệnh nhân đã ngừng sử dụng.");

                var schedule = await _db.DoctorSchedules
                    .Include(s => s.Doctor).ThenInclude(d => d.User)
                    .FirstOrDefaultAsync(s => s.ScheduleId == req.ScheduleId)
                    ?? throw new InvalidOperationException("Ca khám không tồn tại");

                // Coordinate with admin blocking this doctor before accepting a new booking.
                await _db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({-schedule.Doctor.UserId})");
                await _db.Entry(schedule).ReloadAsync();
                await _db.Entry(schedule.Doctor).ReloadAsync();
                await _db.Entry(schedule.Doctor.User).ReloadAsync();

                if (schedule.Doctor.VerificationStatus != VerificationStatus.APPROVED)
                    throw new InvalidOperationException("Bác sĩ chưa được xác minh, không thể đặt lịch!");
                if (schedule.Doctor.User.IsActive != true)
                    throw new InvalidOperationException("Tài khoản bác sĩ đã bị khóa, không thể đặt lịch.");

                if (schedule.SlotStatus != SlotStatus.AVAILABLE)
                    throw new InvalidOperationException("Ca khám đã có người đặt hoặc đã đóng");

                // Dùng giờ Việt Nam (UTC+7) để kiểm tra
                var vnNow = DateTime.UtcNow.AddHours(7);
                var today = DateOnly.FromDateTime(vnNow);
                if (schedule.DateWorking > today.AddDays(BookingWindow.MaxAdvanceDays))
                    throw new InvalidOperationException("Chỉ có thể đặt lịch trong 30 ngày tới.");
                if (schedule.DateWorking < today)
                    throw new InvalidOperationException("Không thể đặt lịch cho ngày đã qua!");

                // Nếu đặt lịch ngày hôm nay, kiểm tra giờ chưa qua
                if (schedule.DateWorking == today)
                {
                    var timeStr2 = schedule.TimeSlot.GetDisplayValue();
                    var timeParts2 = timeStr2.Split(':');
                    int slotHour = int.Parse(timeParts2[0]);
                    int slotMin = int.Parse(timeParts2[1]);
                    var slotTime = new TimeOnly(slotHour, slotMin);
                    var vnTime = TimeOnly.FromDateTime(vnNow);
                    if (slotTime <= vnTime)
                        throw new InvalidOperationException("Không thể đặt lịch cho khung giờ đã qua!");
                }

                // Overlap check
                var overlapStatuses = new[] { BookingStatus.PENDING, BookingStatus.CONFIRMED };
                var isOverlapping = await _db.Appointments
                    .AnyAsync(a => a.PatientId == req.PatientId && a.Schedule.DateWorking == schedule.DateWorking && a.Schedule.TimeSlot == schedule.TimeSlot && overlapStatuses.Contains(a.BookingStatus));
                
                if (isOverlapping)
                    throw new InvalidOperationException("Hồ sơ bệnh nhân này đã có một lịch hẹn khác vào cùng thời gian!");

                if (await _db.Appointments.AnyAsync(a => a.PatientId == req.PatientId &&
                    a.Schedule.DateWorking == schedule.DateWorking && a.Schedule.DoctorId == schedule.DoctorId &&
                    overlapStatuses.Contains(a.BookingStatus)))
                    throw new InvalidOperationException("Hồ sơ bệnh nhân này đã có lịch với bác sĩ trong ngày đã chọn!");

                schedule.SlotStatus = SlotStatus.BOOKED;
                schedule.Version = (schedule.Version ?? 0) + 1;

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
                await transaction.CommitAsync();

                // Lên lịch Hangfire (Gửi cảnh báo cho Bác sĩ sau 25 phút)
                Hangfire.BackgroundJob.Schedule<WebDeveloper.Services.Interfaces.IAppointmentJobService>(
                    job => job.SendWarningToDoctor(appointment.Id), 
                    TimeSpan.FromMinutes(25));
                
                // Lên lịch Hangfire (Tự động Hủy sau 30 phút)
                Hangfire.BackgroundJob.Schedule<WebDeveloper.Services.Interfaces.IAppointmentJobService>(
                    job => job.CancelUnconfirmedAppointment(appointment.Id), 
                    TimeSpan.FromMinutes(30));

                return "Đặt lịch khám thành công";
            }
            finally
            {
                // Mở khóa sau khi hoàn tất (hoặc có lỗi văng ra)
                await redisDb.ScriptEvaluateAsync(
                    "if redis.call('get', KEYS[1]) == ARGV[1] then return redis.call('del', KEYS[1]) else return 0 end",
                    new StackExchange.Redis.RedisKey[] { lockKey }, new StackExchange.Redis.RedisValue[] { lockToken });
            }
        }

        // === Hủy lịch: kiểm tra chủ tài khoản và thời hạn; lưu người hủy/lý do, trả ca về trạng thái trống. ===
        public async Task<string> CancelAppointment(User user, int appointmentId, string? reason = null)
        {
            var app = await _db.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Schedule)
                .FirstOrDefaultAsync(a => a.Id == appointmentId)
                ?? throw new WebDeveloper.Exceptions.DomainException("Không tìm thấy lịch hẹn");

            if (app.Patient.UserId != user.UserId)
                throw new WebDeveloper.Exceptions.DomainException("Bạn không có quyền", "FORBIDDEN", 403);

            if (app.BookingStatus != BookingStatus.PENDING && app.BookingStatus != BookingStatus.CONFIRMED)
                throw new WebDeveloper.Exceptions.DomainException("Không thể hủy lịch hẹn ở trạng thái này");

            // 11. Xử lý hủy lịch muộn (Late Cancellation)
            var timeStr = app.Schedule.TimeSlot.GetDisplayValue();
            var timeParts = timeStr.Split(':');
            int hours = int.Parse(timeParts[0]);
            int minutes = int.Parse(timeParts[1]);
            
            var appointmentDate = app.Schedule.DateWorking.ToDateTime(new TimeOnly(hours, minutes));
            var cancellationWindowHours = _configuration.GetValue<int>("App:CancellationWindow", 12);
            
            if (DateTime.UtcNow.AddHours(cancellationWindowHours) > appointmentDate)
            {
                throw new WebDeveloper.Exceptions.DomainException(
                    $"Bạn chỉ được phép hủy lịch trước giờ khám ít nhất {cancellationWindowHours} tiếng. Xin vui lòng liên hệ trực tiếp với cơ sở y tế để được hỗ trợ.", 
                    "LATE_CANCELLATION", 400);
            }

            app.BookingStatus = BookingStatus.CANCELLED;
            app.CancelledBy = "PATIENT";
            app.CancellationReason = string.IsNullOrWhiteSpace(reason) ? "Bệnh nhân yêu cầu hủy" : reason.Trim();

            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            if (app.Schedule.DateWorking >= today)
            {
                app.Schedule.SlotStatus = SlotStatus.AVAILABLE;
            }

            await _db.SaveChangesAsync();
            return "Hủy lịch hẹn thành công";
        }

        // === Tra cứu: phân trang và chi tiết chỉ trả lịch thuộc tài khoản hiện tại. ===
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
                CancellationReason = a.CancellationReason,
                CancelledBy = a.CancelledBy,
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
                CancellationReason = a.CancellationReason,
                CancelledBy = a.CancelledBy,
                CreatedAt = a.CreatedAt,
                HasResult = result != null,
                FollowUpDate = result?.FollowUpDate,
                Diagnosis = result?.Diagnosis,
                PrescriptionUrl = result?.PrescriptionUrl,
                DoctorNotes = result?.DoctorNotes,
                Rating = review?.Rating,
                Comment = review?.Comment
            };
        }

        // === Đánh giá: gắn với ca khám hoàn thành; nội dung bị ẩn vẫn đóng góp vào điểm trung bình. ===
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

            // Cập nhật RatingAverage và ReviewCount ngay lập tức
            await UpdateDoctorRating(app.Schedule.DoctorId);

            return "Gửi đánh giá thành công!";
        }

        private async Task UpdateDoctorRating(int doctorId)
        {
            var stats = await _db.Reviews
                .Where(r => r.Appointment.Schedule.DoctorId == doctorId)
                .GroupBy(r => r.Appointment.Schedule.DoctorId)
                .Select(g => new
                {
                    Count = g.Count(),
                    Avg = g.Average(x => (double?)(x.Rating ?? 0))
                })
                .FirstOrDefaultAsync();

            var doctor = await _db.DoctorDetails.FindAsync(doctorId);
            if (doctor != null)
            {
                doctor.ReviewCount = stats?.Count ?? 0;
                doctor.RatingAverage = stats != null ? Math.Round((stats.Avg ?? 0) * 10) / 10.0 : 0;
                await _db.SaveChangesAsync();
            }
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

            // Cập nhật RatingAverage và ReviewCount ngay lập tức
            await UpdateDoctorRating(app.Schedule.DoctorId);

            return "Cập nhật đánh giá thành công!";
        }
    }
}
