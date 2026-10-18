using Microsoft.EntityFrameworkCore;
using WebDeveloper.Data;
using WebDeveloper.Helpers;
using WebDeveloper.Models.DTOs.Doctor;
using WebDeveloper.Models.Entities;
using WebDeveloper.Models.Enums;
using WebDeveloper.Services.Interfaces;

namespace WebDeveloper.Services
{
    public class DoctorService : IDoctorService
    {
        private readonly ApplicationDbContext _db;
        private readonly IFileUploadService _fileUpload;

        public DoctorService(ApplicationDbContext db, IFileUploadService fileUpload)
        {
            _db = db;
            _fileUpload = fileUpload;
        }

        // === Hồ sơ chuyên môn: trạng thái duyệt tách biệt với role và trạng thái tài khoản. ===
        public async Task<DoctorProfileResponse> GetProfile(User user)
        {
            var doctor = await _db.DoctorDetails
                .Include(d => d.User)
                .Include(d => d.Specialty)
                .Include(d => d.Facility)
                .FirstOrDefaultAsync(d => d.UserId == user.UserId);

            if (doctor == null)
            {
                // Bác sĩ mới đăng ký chưa nộp hồ sơ — không phải lỗi 400; FE cần 200 để hiển thị form / dashboard
                return new DoctorProfileResponse
                {
                    FullName = user.FullName,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber,
                    AvatarUrl = user.AvatarUrl,
                    VerificationStatus = null
                };
            }

            var pendingTransfer = await _db.DoctorTransferRequests
                .Where(t => t.DoctorId == doctor.DoctorId && t.Status == TransferStatus.PENDING)
                .OrderByDescending(t => t.CreatedAt)
                .FirstOrDefaultAsync();

            var lastRejectedTransfer = await _db.DoctorTransferRequests
                .Where(t => t.DoctorId == doctor.DoctorId && t.Status != TransferStatus.PENDING)
                .OrderByDescending(t => t.CreatedAt)
                .FirstOrDefaultAsync();

            return new DoctorProfileResponse
            {
                FullName = doctor.User.FullName,
                Email = doctor.User.Email,
                PhoneNumber = doctor.User.PhoneNumber,
                AvatarUrl = doctor.User.AvatarUrl,
                Bio = doctor.Bio,
                Degree = doctor.Degree,
                ExperienceYears = doctor.ExperienceYears,
                Price = doctor.Price,
                SpecialtyId = doctor.SpecialtyId,
                SpecialtyName = doctor.Specialty?.SpecialtyName,
                FacilityId = doctor.FacilityId,
                FacilityName = doctor.Facility?.FacilityName,
                FacilityAddress = doctor.Facility?.Address,
                FacilityDescription = doctor.Facility?.Description,
                FacilityMapUrl = doctor.Facility?.MapUrl,
                FacilityProvince = doctor.Facility?.Province,
                FacilityVerified = doctor.Facility?.IsVerified,
                RatingAverage = doctor.RatingAverage,
                ReviewCount = doctor.ReviewCount,
                VerificationStatus = doctor.VerificationStatus.ToString(),
                HasPendingTransfer = pendingTransfer != null,
                LastTransferRejectionNote = lastRejectedTransfer?.Status == TransferStatus.REJECTED ? lastRejectedTransfer.AdminNote : null,
                LastTransferStatus = lastRejectedTransfer?.Status.ToString(),
                LastTransferNote = lastRejectedTransfer?.AdminNote
            };
        }

        public async Task<string> RegisterDoctor(User user, DoctorProfileRequest req)
        {
            if (!DoctorDegree.IsValid(req.Degree)) throw new InvalidOperationException("Học vị/bằng cấp không hợp lệ. Vui lòng chọn lại hoặc mô tả mục Khác.");
            req.Degree = req.Degree.Trim();
            if (!double.IsFinite(req.Price) || req.Price <= 0) throw new InvalidOperationException("Giá khám phải lớn hơn 0 đồng");
            var existingDoctor = await _db.DoctorDetails.FirstOrDefaultAsync(d => d.UserId == user.UserId);

            if (existingDoctor != null)
            {
                if (existingDoctor.VerificationStatus == VerificationStatus.PENDING)
                    throw new InvalidOperationException("Hồ sơ của bạn đang trong quá trình xét duyệt. Vui lòng chờ phản hồi từ Admin!");
                if (existingDoctor.VerificationStatus == VerificationStatus.APPROVED)
                    throw new InvalidOperationException("Hồ sơ bác sĩ của bạn đã được duyệt và đang hoạt động. Bạn không cần nộp lại hồ sơ xác minh!");
                
                // If REJECTED, allow resubmission
            }

            if (existingDoctor == null)
            {
                if (req.IdCardImage == null || req.IdCardImage.Length == 0)
                    throw new InvalidOperationException("Vui lòng tải lên ảnh CCCD!");
                if (req.CertificatePdf == null || req.CertificatePdf.Length == 0)
                    throw new InvalidOperationException("Vui lòng đính kèm chứng chỉ hành nghề!");
                _fileUpload.ValidateFile(req.IdCardImage, "Ảnh CCCD", "image/jpeg", "image/png");
                _fileUpload.ValidateFile(req.CertificatePdf, "Chứng chỉ", "application/pdf");
            }
            else
            {
                if (req.IdCardImage != null) _fileUpload.ValidateFile(req.IdCardImage, "Ảnh CCCD", "image/jpeg", "image/png");
                if (req.CertificatePdf != null) _fileUpload.ValidateFile(req.CertificatePdf, "Chứng chỉ", "application/pdf");
            }

            if (req.FacilityId == null && string.IsNullOrWhiteSpace(req.NewFacilityName))
                throw new InvalidOperationException("Vui lòng chọn cơ sở y tế có sẵn hoặc tạo mới!");

            Facility facility;
            if (req.FacilityId != null)
            {
                facility = await _db.Facilities.FindAsync(req.FacilityId)
                    ?? throw new InvalidOperationException("Cơ sở y tế không tồn tại!");
                
                if (!facility.IsActive)
                    throw new InvalidOperationException("Cơ sở y tế này đang ngừng hoạt động!");
            }
            else
            {
                _fileUpload.ValidateFile(req.FacilityLicensePdf, "Giấy phép hoạt động", "application/pdf");
                
                if (await _db.Facilities.AnyAsync(f => f.FacilityName!.ToLower() == req.NewFacilityName!.ToLower()))
                    throw new InvalidOperationException("Tên cơ sở y tế này đã tồn tại!");

                facility = new Facility
                {
                    FacilityName = req.NewFacilityName,
                    Address = req.FacilityAddress,
                    Description = req.FacilityDescription,
                    MapUrl = req.FacilityMapUrl,
                    Province = req.NewFacilityProvince,
                    LicenseUrl = await _fileUpload.UploadFileAsync(req.FacilityLicensePdf),
                    IsVerified = false,
                    IsActive = true
                };
                _db.Facilities.Add(facility);
                await _db.SaveChangesAsync();
            }

            if (existingDoctor != null)
            {
                existingDoctor.SpecialtyId = req.SpecialtyId;
                existingDoctor.FacilityId = facility.FacilityId;
                existingDoctor.Bio = req.Bio;
                existingDoctor.Degree = req.Degree;
                existingDoctor.ExperienceYears = req.ExperienceYears;
                existingDoctor.Price = req.Price;
                existingDoctor.VerificationStatus = VerificationStatus.PENDING;
                
                if (req.IdCardImage != null)
                    existingDoctor.IdCardUrl = await _fileUpload.UploadFileAsync(req.IdCardImage);
                if (req.CertificatePdf != null)
                    existingDoctor.CertificateUrl = await _fileUpload.UploadFileAsync(req.CertificatePdf);

                await _db.SaveChangesAsync();
                return "Đã gửi lại hồ sơ xác minh bác sĩ. Vui lòng chờ Admin duyệt!";
            }
            else
            {
                var doctor = new DoctorDetail
                {
                    UserId = user.UserId,
                    SpecialtyId = req.SpecialtyId,
                    FacilityId = facility.FacilityId,
                    Bio = req.Bio,
                    Degree = req.Degree,
                    ExperienceYears = req.ExperienceYears,
                    Price = req.Price,
                    VerificationStatus = VerificationStatus.PENDING,
                    IdCardUrl = await _fileUpload.UploadFileAsync(req.IdCardImage),
                    CertificateUrl = await _fileUpload.UploadFileAsync(req.CertificatePdf)
                };

                _db.DoctorDetails.Add(doctor);
                
                // Change user role
                var dbUser = await _db.Users.FindAsync(user.UserId);
                if (dbUser != null)
                {
                    dbUser.Role = RoleStatus.DOCTOR;
                }

                await _db.SaveChangesAsync();
                return "Hồ sơ của bạn đã được gửi và đang chờ Admin duyệt!";
            }
        }

        public async Task<DoctorProfileResponse> ChangeProfile(User user, ChangeProfileRequest req)
        {
            var doctor = await _db.DoctorDetails
                .Include(d => d.Facility)
                .Include(d => d.User)
                .Include(d => d.Specialty)
                .FirstOrDefaultAsync(d => d.UserId == user.UserId)
                ?? throw new InvalidOperationException("Bạn chưa đăng ký hồ sơ bác sĩ!");

            if (!string.IsNullOrWhiteSpace(req.Bio))
                doctor.Bio = req.Bio;
            if (req.Price != null)
            {
                if (!double.IsFinite(req.Price.Value) || req.Price <= 0) throw new InvalidOperationException("Giá khám phải lớn hơn 0 đồng");
                doctor.Price = req.Price;
            }

            var facility = doctor.Facility;
            if (facility != null)
            {
                if (!string.IsNullOrWhiteSpace(req.FacilityName))
                    facility.FacilityName = req.FacilityName;
                if (!string.IsNullOrWhiteSpace(req.FacilityAddress))
                    facility.Address = req.FacilityAddress;
                if (!string.IsNullOrWhiteSpace(req.FacilityDescription))
                    facility.Description = req.FacilityDescription;
                if (!string.IsNullOrWhiteSpace(req.FacilityMapUrl))
                    facility.MapUrl = req.FacilityMapUrl;
                if (req.FacilityVerified != null)
                    facility.IsVerified = req.FacilityVerified.Value;
            }

            await _db.SaveChangesAsync();
            return await GetProfile(user);
        }

        // === Lịch làm việc: lọc ca đóng/quá giờ theo Việt Nam; tạo, đóng và mở lại ca dùng chung quy tắc. ===
        public async Task<List<DoctorScheduleResponse>> GetSchedules(User user)
        {
            var doctor = await _db.DoctorDetails.FirstOrDefaultAsync(d => d.UserId == user.UserId)
                ?? throw new InvalidOperationException("Bác sĩ không tồn tại");

            var now = VietnamTime.Now;
            var today = DateOnly.FromDateTime(now);

            // Kiểm tra và gửi thông báo chuông nếu 14 ngày tới chưa có lịch làm việc
            await CheckAndNotifyMissingSchedule(user, doctor.DoctorId, today);

            var schedules = await _db.DoctorSchedules
                .Where(s => s.DoctorId == doctor.DoctorId && s.DateWorking >= today)
                .OrderBy(s => s.DateWorking).ThenBy(s => s.TimeSlot)
                .ToListAsync();

            return schedules.Where(s => s.SlotStatus != SlotStatus.CLOSED && !VietnamTime.HasStarted(s.DateWorking, s.TimeSlot, now)).Select(s => new DoctorScheduleResponse
            {
                ScheduleId = s.ScheduleId,
                DateWorking = s.DateWorking,
                TimeSlot = s.TimeSlot.GetDisplayValue(),
                SlotStatus = s.SlotStatus.ToString()
            }).ToList();
        }

        private async Task CheckAndNotifyMissingSchedule(User user, int doctorId, DateOnly today)
        {
            try
            {
                var next14Days = today.AddDays(14);
                var hasSlots = await _db.DoctorSchedules.AnyAsync(s =>
                    s.DoctorId == doctorId &&
                    s.DateWorking >= today &&
                    s.DateWorking <= next14Days &&
                    s.SlotStatus != SlotStatus.CLOSED);

                if (!hasSlots)
                {
                    var threeDaysAgo = DateTime.UtcNow.AddDays(-3);
                    var existingReminder = await _db.Notifications.AnyAsync(n =>
                        n.UserId == user.UserId &&
                        n.Title.Contains("Nhắc nhở: Lên lịch làm việc") &&
                        (n.ReadAt == null || n.CreatedAt >= threeDaysAgo));

                    if (!existingReminder)
                    {
                        _db.Notifications.Add(new Notification
                        {
                            UserId = user.UserId,
                            Title = "Nhắc nhở: Lên lịch làm việc 2 tuần tới",
                            Message = "Bạn chưa có lịch khám nào trong 2 tuần tới. Vui lòng thiết lập lịch làm việc để bệnh nhân có thể đặt hẹn trước.",
                            Url = "/Doctor/Schedules",
                            CreatedAt = DateTime.UtcNow
                        });
                        await _db.SaveChangesAsync();
                    }
                }
            }
            catch
            {
                // Tránh lỗi thông báo làm gián đoạn luồng lấy lịch
            }
        }

        public async Task<string> CreateSchedule(User user, ScheduleRequest req)
        {
            var doctor = await _db.DoctorDetails.FirstOrDefaultAsync(d => d.UserId == user.UserId)
                ?? throw new InvalidOperationException("Bác sĩ không tồn tại");

            if (doctor.VerificationStatus != VerificationStatus.APPROVED)
                throw new InvalidOperationException("Tài khoản chưa được duyệt, không thể tạo lịch!");

            var now = VietnamTime.Now;
            var today = DateOnly.FromDateTime(now);
            var maxFutureDate = today.AddDays(BookingWindow.MaxAdvanceDays);

            var items = new List<DayScheduleItem>();
            if (req.Items != null && req.Items.Count > 0)
            {
                items = req.Items;
            }
            else if (req.Date.HasValue)
            {
                var end = req.EndDate ?? req.Date.Value;
                var defaultSlots = req.SlotIds ?? Enum.GetValues<TimeSlot>().Select(s => s.ToString()).ToList();
                for (var d = req.Date.Value; d <= end; d = d.AddDays(1))
                {
                    items.Add(new DayScheduleItem { Date = d, SlotIds = defaultSlots });
                }
            }

            if (items.Count == 0)
                throw new InvalidOperationException("Vui lòng chọn ít nhất 1 ngày làm việc.");

            var normalized = new List<(DateOnly Date, List<TimeSlot> Slots)>();
            foreach (var item in items)
            {
                if (item.Date < today)
                    throw new InvalidOperationException($"Không thể tạo lịch cho ngày đã qua ({item.Date:dd/MM/yyyy}).");
                if (item.Date > maxFutureDate)
                    throw new InvalidOperationException($"Chỉ được tạo lịch trong vòng 30 ngày tới (tối đa {maxFutureDate:dd/MM/yyyy}).");

                if (item.SlotIds == null || item.SlotIds.Count == 0)
                    throw new InvalidOperationException($"Ngày {item.Date:dd/MM/yyyy} chưa chọn khung giờ nào.");

                var validSlots = new List<TimeSlot>();
                foreach (var slotStr in item.SlotIds)
                {
                    if (Enum.TryParse<TimeSlot>(slotStr, out var parsedSlot))
                    {
                        if (item.Date == today && VietnamTime.HasStarted(item.Date, parsedSlot, now))
                        {
                            continue; // Bỏ qua khung giờ đã bắt đầu hôm nay
                        }
                        if (!validSlots.Contains(parsedSlot))
                            validSlots.Add(parsedSlot);
                    }
                }

                if (validSlots.Count == 0)
                    throw new InvalidOperationException($"Ngày {item.Date:dd/MM/yyyy} không có khung giờ hợp lệ hoặc các giờ chọn đã trôi qua.");

                normalized.Add((item.Date, validSlots));
            }

            await using var transaction = await _db.Database.BeginTransactionAsync();
            await _db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({doctor.DoctorId})");

            var targetDates = normalized.Select(n => n.Date).Distinct().ToList();
            var existing = await _db.DoctorSchedules
                .Where(s => s.DoctorId == doctor.DoctorId && targetDates.Contains(s.DateWorking))
                .ToListAsync();

            var closedIds = existing.Where(s => s.SlotStatus == SlotStatus.CLOSED).Select(s => s.ScheduleId).ToList();
            if (await _db.Appointments.AnyAsync(a => closedIds.Contains(a.ScheduleId) && a.BookingStatus != BookingStatus.CANCELLED))
                throw new InvalidOperationException("Có ca đã đóng còn lịch khám chưa hủy. Không thể tự mở lại; vui lòng kiểm tra lịch.");

            int addedCount = 0;
            foreach (var (date, slots) in normalized)
            {
                foreach (var slot in slots)
                {
                    var current = existing.SingleOrDefault(s => s.DateWorking == date && s.TimeSlot == slot);
                    if (current?.SlotStatus is SlotStatus.AVAILABLE or SlotStatus.BOOKED) continue;
                    if (current != null)
                    {
                        current.SlotStatus = SlotStatus.AVAILABLE;
                        current.FacilityId = doctor.FacilityId;
                        current.Version = (current.Version ?? 0) + 1;
                    }
                    else
                    {
                        _db.DoctorSchedules.Add(new DoctorSchedule
                        {
                            DoctorId = doctor.DoctorId,
                            FacilityId = doctor.FacilityId,
                            DateWorking = date,
                            TimeSlot = slot,
                            SlotStatus = SlotStatus.AVAILABLE,
                            Version = 0
                        });
                    }
                    addedCount++;
                }
            }

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();
            return $"Đã thêm thành công {addedCount} ca làm việc cho {targetDates.Count} ngày.";
        }

        public async Task<string> DeleteSchedule(User user, int scheduleId)
        {
            var doctor = await _db.DoctorDetails.FirstOrDefaultAsync(d => d.UserId == user.UserId)
                ?? throw new InvalidOperationException("Bác sĩ không tồn tại");

            await using var transaction = await _db.Database.BeginTransactionAsync();
            await _db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({doctor.DoctorId})");
            var schedule = await _db.DoctorSchedules.FindAsync(scheduleId)
                ?? throw new InvalidOperationException("Ca làm việc không tồn tại");

            if (schedule.DoctorId != doctor.DoctorId)
                throw new InvalidOperationException("Bạn không có quyền thao tác trên ca làm việc này");

            if (schedule.SlotStatus == SlotStatus.BOOKED)
                throw new InvalidOperationException("Ca làm việc đã có người đặt, không thể xóa");

            if (VietnamTime.HasStarted(schedule.DateWorking, schedule.TimeSlot, VietnamTime.Now))
                throw new InvalidOperationException("Ca làm việc đã đến giờ hoặc đã qua, không thể hủy.");
            if (await _db.Appointments.AnyAsync(a => a.ScheduleId == scheduleId && a.BookingStatus != BookingStatus.CANCELLED))
                throw new InvalidOperationException("Ca làm việc đã có lịch khám, không thể hủy.");
            schedule.SlotStatus = SlotStatus.CLOSED;
            schedule.Version = (schedule.Version ?? 0) + 1;
            await _db.SaveChangesAsync();
            await transaction.CommitAsync();
            return "Đã đóng ca làm việc";
        }

        // === Lịch hẹn của bác sĩ: quyền sở hữu được kiểm tra trước mỗi thay đổi trạng thái. ===
        public async Task<PagedResult<DoctorAppointmentResponse>> GetAppointments(User user, int page, int size)
        {
            var appointmentsQuery = _db.Appointments
                .Include(a => a.Schedule).ThenInclude(s => s.Doctor).ThenInclude(d => d.Specialty)
                .Include(a => a.Patient).ThenInclude(p => p.User)
                .AsSplitQuery()
                .Where(a => a.Schedule.Doctor.UserId == user.UserId)
                .OrderByDescending(a => a.Schedule.DateWorking)
                .ThenBy(a => a.Schedule.TimeSlot);

            var total = await appointmentsQuery.CountAsync();
            var items = await appointmentsQuery
                .Skip(page * size)
                .Take(size)
                .ToListAsync();

            // Load medical results separately to avoid cartesian explosion or if complex
            var appIds = items.Select(a => a.Id).ToList();
            var results = await _db.MedicalResults
                .Where(m => appIds.Contains(m.AppointmentId))
                .ToDictionaryAsync(m => m.AppointmentId);

            var dtos = items.Select(a => 
            {
                results.TryGetValue(a.Id, out var result);
                return new DoctorAppointmentResponse
                {
                    AppointmentId = a.Id,
                    PatientName = a.Patient.FullName ?? a.Patient.User?.FullName,
                    PatientPhoneNumber = a.Patient.PhoneNumber,
                    PatientGender = a.Patient.Gender?.ToString(),
                    DateWorking = a.Schedule.DateWorking,
                    TimeSlot = a.Schedule.TimeSlot.GetDisplayValue(),
                    Reason = a.Reason,
                    BookingStatus = a.BookingStatus.ToString(),
                    CreatedAt = a.CreatedAt,
                    SpecialtyName = a.Schedule.Doctor.Specialty.SpecialtyName,
                    FollowUpDate = result?.FollowUpDate,
                    Diagnosis = result?.Diagnosis,
                    DoctorNotes = result?.DoctorNotes,
                    PrescriptionUrl = result?.PrescriptionUrl,
                    CancellationReason = a.CancellationReason,
                    CancelledBy = a.CancelledBy
                };
            }).ToList();

            return PagedResult<DoctorAppointmentResponse>.Create(dtos, page, size, total);
        }

        public async Task<string> ConfirmAppointment(User user, int appointmentId)
        {
            await using var transaction = await _db.Database.BeginTransactionAsync();
            await _db.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM appointments WHERE appointment_id = {appointmentId} FOR UPDATE");
            var app = await _db.Appointments
                .Include(a => a.Schedule)
                .ThenInclude(s => s.Doctor)
                .FirstOrDefaultAsync(a => a.Id == appointmentId)
                ?? throw new InvalidOperationException("Không tìm thấy lịch hẹn");

            if (app.Schedule.Doctor.UserId != user.UserId)
                throw new InvalidOperationException("Bạn không có quyền xác nhận lịch hẹn này");

            if (app.BookingStatus != BookingStatus.PENDING)
                throw new InvalidOperationException("Chỉ có thể xác nhận lịch hẹn đang chờ duyệt");

            await _db.Entry(app).ReloadAsync();
            if (app.BookingStatus != BookingStatus.PENDING || app.CreatedAt <= DateTime.UtcNow.AddMinutes(-30) ||
                VietnamTime.HasStarted(app.Schedule.DateWorking, app.Schedule.TimeSlot, VietnamTime.Now))
                throw new InvalidOperationException("Lịch hẹn đã quá thời hạn xác nhận.");
            if (app.Schedule.Doctor.VerificationStatus != VerificationStatus.APPROVED)
                throw new InvalidOperationException("Hồ sơ bác sĩ chưa được duyệt, không thể nhận lịch.");
            app.BookingStatus = BookingStatus.CONFIRMED;
            await _db.SaveChangesAsync();
            await transaction.CommitAsync();
            return "Xác nhận lịch hẹn thành công";
        }

        // === Từ chối/vắng mặt: khóa theo tài khoản đặt lịch, đếm vắng 5 lần/30 ngày để báo admin. ===
        public async Task<string> DeclineAppointment(User user, int id, BookingStatus status, string? reason = null)
        {
            await using var transaction = await _db.Database.BeginTransactionAsync();
            var app = await _db.Appointments.Include(a => a.Schedule).ThenInclude(s => s.Doctor)
                .FirstOrDefaultAsync(a => a.Id == id) ?? throw new InvalidOperationException("Không tìm thấy lịch hẹn");
            if (app.Schedule.Doctor.UserId != user.UserId) throw new InvalidOperationException("Bạn không có quyền");
            var ownerId = await _db.PatientProfiles.Where(p => p.PatientId == app.PatientId).Select(p => p.UserId).SingleAsync();
            await _db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({-ownerId})");
            await _db.Entry(app).ReloadAsync();
            if (status == BookingStatus.CANCELLED && (app.BookingStatus == BookingStatus.PENDING || app.BookingStatus == BookingStatus.CONFIRMED))
            {
                app.BookingStatus = status;
                app.CancelledBy = "DOCTOR";
                app.CancellationReason = string.IsNullOrWhiteSpace(reason) ? "Bác sĩ từ chối lịch hẹn" : reason.Trim();
                app.Schedule.SlotStatus = SlotStatus.AVAILABLE;
            }
            else if (status == BookingStatus.NO_SHOW && app.BookingStatus == BookingStatus.CONFIRMED)
            {
                if (app.Schedule.DateWorking.ToDateTime(TimeOnly.Parse(app.Schedule.TimeSlot.GetDisplayValue())) > VietnamTime.Now)
                    throw new InvalidOperationException("Chưa đến giờ khám, không thể đánh dấu vắng mặt");
                var since = DateOnly.FromDateTime(VietnamTime.Now).AddDays(-29);
                var today = DateOnly.FromDateTime(VietnamTime.Now);
                var missed = await _db.Appointments.CountAsync(a => a.Patient.UserId == ownerId &&
                    a.BookingStatus == BookingStatus.NO_SHOW && a.Schedule.DateWorking >= since && a.Schedule.DateWorking <= today);
                app.BookingStatus = status;
                if (app.Schedule.DateWorking >= since && missed == 4)
                {
                    var patient = await _db.Users.FindAsync(ownerId);
                    foreach (var adminId in await _db.Users.Where(u => u.Role == RoleStatus.ADMIN && u.IsActive == true).Select(u => u.UserId).ToListAsync())
                        _db.Notifications.Add(new Notification { UserId = adminId, Title = "Cảnh báo bệnh nhân vắng khám",
                            Message = $"{patient?.FullName} (mã tài khoản {ownerId}) đã vắng khám 5 lần trong 30 ngày. Vui lòng xem xét trước khi khóa tài khoản.", Url = "/Admin/Users" });
                }
            }
            else throw new InvalidOperationException("Trạng thái lịch hẹn không cho phép thao tác này");
            await _db.SaveChangesAsync();
            await transaction.CommitAsync();
            return "Đã cập nhật lịch hẹn";
        }

        public async Task<string> CompleteAppointment(User user, int appointmentId)
        {
            var app = await _db.Appointments
                .Include(a => a.Schedule)
                .ThenInclude(s => s.Doctor)
                .FirstOrDefaultAsync(a => a.Id == appointmentId)
                ?? throw new InvalidOperationException("Không tìm thấy lịch hẹn");

            if (app.Schedule.Doctor.UserId != user.UserId)
                throw new InvalidOperationException("Bạn không có quyền hoàn thành lịch hẹn này");

            if (app.BookingStatus != BookingStatus.CONFIRMED)
                throw new InvalidOperationException("Chỉ có thể hoàn thành lịch hẹn đã được xác nhận");

            if (!VietnamTime.HasStarted(app.Schedule.DateWorking, app.Schedule.TimeSlot, VietnamTime.Now))
                throw new InvalidOperationException("Chưa đến giờ khám, chưa thể hoàn thành hoặc trả kết quả.");
            app.BookingStatus = BookingStatus.COMPLETED;
            await _db.SaveChangesAsync();
            return "Đã hoàn thành ca khám";
        }

        // === Kết quả khám: kiểm tra chủ ca, chẩn đoán, ngày tái khám và tệp/link trước khi hoàn tất. ===
        public async Task<string> SaveMedicalResult(User user, int appointmentId, MedicalResultRequest req)
        {
            var app = await _db.Appointments
                .Include(a => a.Schedule)
                .ThenInclude(s => s.Doctor)
                .FirstOrDefaultAsync(a => a.Id == appointmentId)
                ?? throw new InvalidOperationException("Không tìm thấy lịch hẹn");

            if (app.Schedule.Doctor.UserId != user.UserId)
                throw new InvalidOperationException("Bạn không có quyền");

            if (!VietnamTime.HasStarted(app.Schedule.DateWorking, app.Schedule.TimeSlot, VietnamTime.Now))
                throw new InvalidOperationException("Chưa đến giờ khám, chưa thể hoàn thành hoặc trả kết quả.");
            if (app.BookingStatus != BookingStatus.CONFIRMED && app.BookingStatus != BookingStatus.COMPLETED)
                throw new InvalidOperationException("Chỉ có thể lưu kết quả cho lịch khám đã xác nhận hoặc hoàn thành");
            if (string.IsNullOrWhiteSpace(req.Diagnosis))
                throw new InvalidOperationException("Vui lòng nhập chẩn đoán");
            var vnToday = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));
            if (req.FollowUpDate.HasValue && (req.FollowUpDate.Value <= app.Schedule.DateWorking || req.FollowUpDate.Value <= vnToday))
                throw new InvalidOperationException("Ngày tái khám phải sau ngày khám và ít nhất là từ ngày mai");
            var link = req.PrescriptionLink?.Trim();
            if (!string.IsNullOrEmpty(link) && (!Uri.TryCreate(link, UriKind.Absolute, out var uri)
                || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp)))
                throw new InvalidOperationException("Link kết quả phải là địa chỉ http:// hoặc https:// hợp lệ");
            if (req.PrescriptionFile != null && !string.IsNullOrEmpty(link))
                throw new InvalidOperationException("Chỉ chọn tệp hoặc link kết quả");

            string? uploadedUrl = null;
            if (req.PrescriptionFile != null)
            {
                _fileUpload.ValidateFile(req.PrescriptionFile, "Kết quả khám", "application/pdf", "image/jpeg", "image/png",
                    "application/msword", "application/vnd.openxmlformats-officedocument.wordprocessingml.document");
                uploadedUrl = await _fileUpload.UploadFileAsync(req.PrescriptionFile);
                if (string.IsNullOrWhiteSpace(uploadedUrl))
                    throw new InvalidOperationException("Không thể tải tệp kết quả. Vui lòng thử lại");
            }
            var result = await _db.MedicalResults.FirstOrDefaultAsync(m => m.AppointmentId == appointmentId);
            if (result == null)
            {
                result = new MedicalResult { AppointmentId = appointmentId, CreatedAt = DateTime.UtcNow };
                _db.MedicalResults.Add(result);
            }
            result.Diagnosis = req.Diagnosis.Trim();
            result.DoctorNotes = req.DoctorNotes?.Trim();
            result.FollowUpDate = req.FollowUpDate;
            // An omitted attachment preserves the existing file/link when editing.
            if (uploadedUrl != null) result.PrescriptionUrl = uploadedUrl;
            else if (!string.IsNullOrEmpty(link)) result.PrescriptionUrl = link;
            app.BookingStatus = BookingStatus.COMPLETED;

            await _db.SaveChangesAsync();
            return "Lưu kết quả khám thành công";
        }

        // === Đánh giá và ca quá hạn: các truy vấn chỉ giới hạn vào bác sĩ hiện tại. ===
        public async Task<PagedResult<DoctorReviewResponse>> GetReviews(User user, int page, int size)
        {
            var query = _db.Reviews
                .Include(r => r.Appointment).ThenInclude(a => a.Patient).ThenInclude(p => p.User)
                .Include(r => r.Appointment).ThenInclude(a => a.Schedule).ThenInclude(s => s.Doctor)
                .Where(r => r.Appointment.Schedule.Doctor.UserId == user.UserId)
                .OrderByDescending(r => r.CreatedAt);

            var total = await query.CountAsync();
            var items = await query.Skip(page * size).Take(size).ToListAsync();

            var dtos = items.Select(r => new DoctorReviewResponse
            {
                ReviewId = r.ReviewId,
                Rating = r.Rating,
                    IsVisible = r.IsVisible != false,
                Comment = r.Comment,
                PatientName = r.Appointment.Patient.User?.FullName ?? r.Appointment.Patient.FullName,
                CreatedAt = r.CreatedAt
            }).ToList();

            return PagedResult<DoctorReviewResponse>.Create(dtos, page, size, total);
        }

        public async Task<List<DoctorAppointmentResponse>> GetOverdueConfirmedAppointments(User user)
        {
            // Roll over at midnight in Vietnam, not at midnight UTC (07:00 in Vietnam).
            var today = DateOnly.FromDateTime(VietnamTime.Now);
            
            // Get appointments that are CONFIRMED and the date has passed
            var query = _db.Appointments
                .Include(a => a.Schedule).ThenInclude(s => s.Doctor)
                .Include(a => a.Patient).ThenInclude(p => p.User)
                .Where(a => a.Schedule.Doctor.UserId == user.UserId
                            && a.BookingStatus == BookingStatus.CONFIRMED
                            && a.Schedule.DateWorking < today)
                .OrderByDescending(a => a.Schedule.DateWorking);

            var items = await query.ToListAsync();
            
            return items.Select(a => new DoctorAppointmentResponse
            {
                AppointmentId = a.Id,
                PatientName = a.Patient.FullName ?? a.Patient.User?.FullName,
                PatientPhoneNumber = a.Patient.PhoneNumber,
                PatientGender = a.Patient.Gender?.ToString(),
                DateWorking = a.Schedule.DateWorking,
                TimeSlot = a.Schedule.TimeSlot.GetDisplayValue(),
                Reason = a.Reason,
                BookingStatus = a.BookingStatus.ToString(),
                CreatedAt = a.CreatedAt
            }).ToList();
        }

        // === Chuyển công tác: chụp tên cơ sở cũ khi gửi, admin xử lý và lưu thời điểm quyết định. ===
        public async Task<string> CreateTransferRequest(User user, TransferRequestDto req)
        {
            var doctor = await _db.DoctorDetails.FirstOrDefaultAsync(d => d.UserId == user.UserId)
                ?? throw new InvalidOperationException("Không tìm thấy bác sĩ");

            if (doctor.FacilityId == req.TargetFacilityId)
                throw new InvalidOperationException("Bạn đã ở cơ sở này rồi!");

            var hasPending = await _db.DoctorTransferRequests
                .AnyAsync(t => t.DoctorId == doctor.DoctorId && t.Status == TransferStatus.PENDING);
            
            if (hasPending)
                throw new InvalidOperationException("Bạn đang có yêu cầu chuyển công tác chờ xử lý");

            var request = new DoctorTransferRequest
            {
                DoctorId = doctor.DoctorId,
                OldFacilityName = await _db.Facilities.Where(f => f.FacilityId == doctor.FacilityId).Select(f => f.FacilityName).FirstOrDefaultAsync(),
                TargetFacilityId = req.TargetFacilityId,
                Reason = req.Reason,
                Status = TransferStatus.PENDING,
                CreatedAt = DateTime.UtcNow
            };

            _db.DoctorTransferRequests.Add(request);
            await _db.SaveChangesAsync();
            return "Đã gửi yêu cầu chuyển công tác thành công!";
        }



        public async Task<PagedResult<object>> GetTransferRequests(TransferStatus status, int page, int size)
        {
            var query = _db.DoctorTransferRequests
                .Include(t => t.Doctor).ThenInclude(d => d.User)
                .Include(t => t.Doctor).ThenInclude(d => d.Facility)
                .Include(t => t.TargetFacility)
                .Where(t => t.Status == status)
                .OrderByDescending(t => t.CreatedAt);

            var total = await query.CountAsync();
            var items = await query.Skip(page * size).Take(size).ToListAsync();

            var dtos = items.Select(t => new
            {
                id = t.Id,
                doctorId = t.DoctorId,
                doctorName = t.Doctor.User.FullName,
                oldFacilityName = t.OldFacilityName ?? (t.Status == TransferStatus.PENDING ? t.Doctor.Facility?.FacilityName : null),
                targetFacilityName = t.TargetFacility?.FacilityName,
                reason = t.Reason,
                status = t.Status.ToString(),
                createdAt = t.CreatedAt,
                processedAt = t.ProcessedAt,
                adminNote = t.AdminNote
            }).Cast<object>().ToList();

            return PagedResult<object>.Create(dtos, page, size, total);
        }

        public async Task ApproveTransfer(int id, string? adminNote)
        {
            var request = await _db.DoctorTransferRequests
                .Include(t => t.Doctor)
                .FirstOrDefaultAsync(t => t.Id == id)
                ?? throw new InvalidOperationException("Không tìm thấy yêu cầu");

            if (request.Status != TransferStatus.PENDING)
                throw new InvalidOperationException("Chỉ duyệt yêu cầu đang chờ");

            request.OldFacilityName ??= await _db.Facilities.Where(f => f.FacilityId == request.Doctor.FacilityId).Select(f => f.FacilityName).FirstOrDefaultAsync();
            request.Status = TransferStatus.APPROVED;
            request.AdminNote = adminNote;
            request.ProcessedAt = DateTime.UtcNow;

            var doctor = request.Doctor;
            doctor.FacilityId = request.TargetFacilityId;

            // Đóng các slot AVAILABLE trong tương lai (tại cơ sở cũ)
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var futureSlots = await _db.DoctorSchedules
                .Where(s => s.DoctorId == doctor.DoctorId && s.DateWorking >= today && s.SlotStatus == SlotStatus.AVAILABLE)
                .ToListAsync();
            
            foreach(var slot in futureSlots)
            {
                slot.SlotStatus = SlotStatus.CLOSED;
            }

            await _db.SaveChangesAsync();
        }

        public async Task RejectTransfer(int id, string? adminNote)
        {
            var request = await _db.DoctorTransferRequests.FindAsync(id)
                ?? throw new InvalidOperationException("Không tìm thấy yêu cầu");

            if (request.Status != TransferStatus.PENDING)
                throw new InvalidOperationException("Chỉ từ chối yêu cầu đang chờ");

            request.Status = TransferStatus.REJECTED;
            request.AdminNote = adminNote;
            request.ProcessedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
        }
    }
}
