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
                .Where(t => t.DoctorId == doctor.DoctorId && t.Status == TransferStatus.REJECTED)
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
                LastTransferRejectionNote = lastRejectedTransfer?.AdminNote
            };
        }

        public async Task<string> RegisterDoctor(User user, DoctorProfileRequest req)
        {
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
                doctor.Price = req.Price;

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

        public async Task<List<DoctorScheduleResponse>> GetSchedules(User user)
        {
            var doctor = await _db.DoctorDetails.FirstOrDefaultAsync(d => d.UserId == user.UserId)
                ?? throw new InvalidOperationException("Bác sĩ không tồn tại");

            var schedules = await _db.DoctorSchedules
                .Where(s => s.DoctorId == doctor.DoctorId)
                .OrderByDescending(s => s.DateWorking)
                .ToListAsync();

            return schedules.Select(s => new DoctorScheduleResponse
            {
                ScheduleId = s.ScheduleId,
                DateWorking = s.DateWorking,
                TimeSlot = s.TimeSlot.GetDisplayValue(),
                SlotStatus = s.SlotStatus.ToString()
            }).ToList();
        }

        public async Task<string> CreateSchedule(User user, ScheduleRequest req)
        {
            var doctor = await _db.DoctorDetails.FirstOrDefaultAsync(d => d.UserId == user.UserId)
                ?? throw new InvalidOperationException("Bác sĩ không tồn tại");

            if (doctor.VerificationStatus != VerificationStatus.APPROVED)
                throw new InvalidOperationException("Tài khoản chưa được duyệt, không thể tạo lịch!");
            
            if (req.Date < DateOnly.FromDateTime(DateTime.UtcNow)) // Vietnam time roughly
                throw new InvalidOperationException("Không thể đăng ký lịch cho ngày trong quá khứ hoặc hôm nay!");

            var existingSchedules = await _db.DoctorSchedules
                .Where(s => s.DoctorId == doctor.DoctorId && s.DateWorking == req.Date)
                .ToListAsync();

            int addedCount = 0;
            foreach (var slotStr in req.SlotIds)
            {
                if (!Enum.TryParse<TimeSlot>(slotStr, out var slot))
                    continue;

                if (!existingSchedules.Any(s => s.TimeSlot == slot))
                {
                    _db.DoctorSchedules.Add(new DoctorSchedule
                    {
                        DoctorId = doctor.DoctorId,
                        FacilityId = doctor.FacilityId,
                        DateWorking = req.Date,
                        TimeSlot = slot,
                        SlotStatus = SlotStatus.AVAILABLE,
                        Version = 0
                    });
                    addedCount++;
                }
            }

            await _db.SaveChangesAsync();
            return $"Đã thêm thành công {addedCount} ca làm việc.";
        }

        public async Task<string> DeleteSchedule(User user, int scheduleId)
        {
            var doctor = await _db.DoctorDetails.FirstOrDefaultAsync(d => d.UserId == user.UserId)
                ?? throw new InvalidOperationException("Bác sĩ không tồn tại");

            var schedule = await _db.DoctorSchedules.FindAsync(scheduleId)
                ?? throw new InvalidOperationException("Ca làm việc không tồn tại");

            if (schedule.DoctorId != doctor.DoctorId)
                throw new InvalidOperationException("Bạn không có quyền thao tác trên ca làm việc này");

            if (schedule.SlotStatus == SlotStatus.BOOKED)
                throw new InvalidOperationException("Ca làm việc đã có người đặt, không thể xóa");

            schedule.SlotStatus = SlotStatus.CLOSED;
            await _db.SaveChangesAsync();
            return "Đã đóng ca làm việc";
        }

        public async Task<PagedResult<DoctorAppointmentResponse>> GetAppointments(User user, int page, int size)
        {
            var appointmentsQuery = _db.Appointments
                .Include(a => a.Schedule)
                .Include(a => a.Patient).ThenInclude(p => p.User)
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
                    PatientName = a.Patient.User?.FullName ?? a.Patient.FullName,
                    PatientPhoneNumber = a.Patient.PhoneNumber,
                    PatientGender = a.Patient.Gender?.ToString(),
                    DateWorking = a.Schedule.DateWorking,
                    TimeSlot = a.Schedule.TimeSlot.GetDisplayValue(),
                    Reason = a.Reason,
                    BookingStatus = a.BookingStatus.ToString(),
                    CreatedAt = a.CreatedAt,
                    Diagnosis = result?.Diagnosis,
                    DoctorNotes = result?.DoctorNotes,
                    PrescriptionUrl = result?.PrescriptionUrl
                };
            }).ToList();

            return PagedResult<DoctorAppointmentResponse>.Create(dtos, page, size, total);
        }

        public async Task<string> ConfirmAppointment(User user, int appointmentId)
        {
            var app = await _db.Appointments
                .Include(a => a.Schedule)
                .ThenInclude(s => s.Doctor)
                .FirstOrDefaultAsync(a => a.Id == appointmentId)
                ?? throw new InvalidOperationException("Không tìm thấy lịch hẹn");

            if (app.Schedule.Doctor.UserId != user.UserId)
                throw new InvalidOperationException("Bạn không có quyền xác nhận lịch hẹn này");

            if (app.BookingStatus != BookingStatus.PENDING)
                throw new InvalidOperationException("Chỉ có thể xác nhận lịch hẹn đang chờ duyệt");

            app.BookingStatus = BookingStatus.CONFIRMED;
            await _db.SaveChangesAsync();
            return "Xác nhận lịch hẹn thành công";
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

            app.BookingStatus = BookingStatus.COMPLETED;
            await _db.SaveChangesAsync();
            return "Đã hoàn thành ca khám";
        }

        public async Task<string> SaveMedicalResult(User user, int appointmentId, MedicalResultRequest req)
        {
            var app = await _db.Appointments
                .Include(a => a.Schedule)
                .ThenInclude(s => s.Doctor)
                .FirstOrDefaultAsync(a => a.Id == appointmentId)
                ?? throw new InvalidOperationException("Không tìm thấy lịch hẹn");

            if (app.Schedule.Doctor.UserId != user.UserId)
                throw new InvalidOperationException("Bạn không có quyền");

            if (app.BookingStatus != BookingStatus.COMPLETED)
                throw new InvalidOperationException("Chỉ có thể lưu kết quả khi lịch khám đã hoàn thành");

            var result = await _db.MedicalResults.FirstOrDefaultAsync(m => m.AppointmentId == appointmentId);
            if (result == null)
            {
                result = new MedicalResult
                {
                    AppointmentId = appointmentId,
                    Diagnosis = req.Diagnosis,
                    DoctorNotes = req.DoctorNotes,
                    CreatedAt = DateTime.UtcNow
                };
                if (req.PrescriptionFile != null)
                {
                    _fileUpload.ValidateFile(req.PrescriptionFile, "Đơn thuốc", "application/pdf", "image/jpeg", "image/png");
                    result.PrescriptionUrl = await _fileUpload.UploadFileAsync(req.PrescriptionFile);
                }
                _db.MedicalResults.Add(result);
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(req.Diagnosis)) result.Diagnosis = req.Diagnosis;
                if (!string.IsNullOrWhiteSpace(req.DoctorNotes)) result.DoctorNotes = req.DoctorNotes;
                if (req.PrescriptionFile != null)
                {
                    _fileUpload.ValidateFile(req.PrescriptionFile, "Đơn thuốc", "application/pdf", "image/jpeg", "image/png");
                    result.PrescriptionUrl = await _fileUpload.UploadFileAsync(req.PrescriptionFile);
                }
            }

            await _db.SaveChangesAsync();
            return "Lưu kết quả khám thành công";
        }

        public async Task<PagedResult<DoctorReviewResponse>> GetReviews(User user, int page, int size)
        {
            var query = _db.Reviews
                .Include(r => r.Appointment).ThenInclude(a => a.Patient).ThenInclude(p => p.User)
                .Include(r => r.Appointment).ThenInclude(a => a.Schedule).ThenInclude(s => s.Doctor)
                .Where(r => r.Appointment.Schedule.Doctor.UserId == user.UserId && r.IsVisible == true)
                .OrderByDescending(r => r.CreatedAt);

            var total = await query.CountAsync();
            var items = await query.Skip(page * size).Take(size).ToListAsync();

            var dtos = items.Select(r => new DoctorReviewResponse
            {
                ReviewId = r.ReviewId,
                Rating = r.Rating,
                Comment = r.Comment,
                PatientName = r.Appointment.Patient.User?.FullName ?? r.Appointment.Patient.FullName,
                CreatedAt = r.CreatedAt
            }).ToList();

            return PagedResult<DoctorReviewResponse>.Create(dtos, page, size, total);
        }

        public async Task<List<DoctorAppointmentResponse>> GetOverdueConfirmedAppointments(User user)
        {
            // Vietnam time logic: UTC + 7
            var now = DateTime.UtcNow;
            var today = DateOnly.FromDateTime(now);
            
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
                PatientName = a.Patient.User?.FullName ?? a.Patient.FullName,
                PatientPhoneNumber = a.Patient.PhoneNumber,
                PatientGender = a.Patient.Gender?.ToString(),
                DateWorking = a.Schedule.DateWorking,
                TimeSlot = a.Schedule.TimeSlot.GetDisplayValue(),
                Reason = a.Reason,
                BookingStatus = a.BookingStatus.ToString(),
                CreatedAt = a.CreatedAt
            }).ToList();
        }

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
                currentFacilityName = t.Doctor.Facility?.FacilityName,
                targetFacilityName = t.TargetFacility?.FacilityName,
                reason = t.Reason,
                status = t.Status.ToString(),
                createdAt = t.CreatedAt,
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
