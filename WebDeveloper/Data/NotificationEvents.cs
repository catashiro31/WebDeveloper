using Microsoft.EntityFrameworkCore;
using WebDeveloper.Models.Entities;
using WebDeveloper.Models.Enums;
using WebDeveloper.Moderation;

namespace WebDeveloper.Data;

// Prepare notifications before the business SaveChanges: EF commits both atomically.
internal static class NotificationEvents
{
    public static async Task<List<Notification>> Collect(ApplicationDbContext db, CancellationToken ct)
    {
        var notifications = new List<Notification>();
        void Send(int id, string title, string url, string message = "")
        {
            if (id > 0) notifications.Add(new Notification { UserId = id, Title = title, Message = message, Url = url });
        }
        async Task Admins(string title, string url)
        {
            foreach (var id in await db.Users.Where(u => u.Role == RoleStatus.ADMIN && u.IsActive == true).Select(u => u.UserId).ToListAsync(ct))
                Send(id, title, url);
        }
        // Chỉ xét entity vừa thêm hoặc thuộc tính thực sự đổi; SaveChanges lặp không tạo thông báo lặp.
        var entries = db.ChangeTracker.Entries().Where(e => e.State is EntityState.Added or EntityState.Modified).ToArray();
        foreach (var entry in entries)
        {
            bool changed(string name) => entry.State == EntityState.Modified && !Equals(entry.Property(name).OriginalValue, entry.Property(name).CurrentValue);
            if (entry.Entity is Appointment appointment && (entry.State == EntityState.Added || changed(nameof(Appointment.BookingStatus))))
            {
                var patientId = appointment.Patient?.UserId ?? await db.PatientProfiles.Where(p => p.PatientId == appointment.PatientId).Select(p => p.UserId).SingleAsync(ct);
                var doctorId = appointment.Schedule?.Doctor?.UserId ?? await db.DoctorSchedules.Where(s => s.ScheduleId == appointment.ScheduleId).Select(s => s.Doctor.UserId).SingleAsync(ct);
                var schedule = appointment.Schedule ?? await db.DoctorSchedules.SingleAsync(s => s.ScheduleId == appointment.ScheduleId, ct);
                var detail = $"Ngày khám: {schedule.DateWorking:dd/MM/yyyy}, {schedule.TimeSlot.GetDisplayValue()}";
                var title = appointment.BookingStatus switch {
                    BookingStatus.PENDING => "Đặt lịch thành công, đang chờ bác sĩ xác nhận",
                    BookingStatus.CONFIRMED => "Lịch hẹn đã được xác nhận",
                    BookingStatus.CANCELLED => "Lịch hẹn đã bị hủy hoặc từ chối",
                    BookingStatus.COMPLETED => "Ca khám đã hoàn thành",
                    BookingStatus.NO_SHOW => "Lịch hẹn được đánh dấu vắng mặt",
                    _ => "Lịch hẹn đã được cập nhật"
                };
                Send(patientId, title, "/Patient/Appointments", detail);
                Send(doctorId, entry.State == EntityState.Added ? "Có yêu cầu đặt lịch mới" : title, "/Doctor/Appointments", detail);
            }
            if (entry.Entity is MedicalResult result && (entry.State == EntityState.Added || entry.Properties.Any(p => p.IsModified)))
            {
                var id = await db.Appointments.Where(a => a.Id == result.AppointmentId).Select(a => a.Patient.UserId).SingleAsync(ct);
                Send(id, "Kết quả khám đã được cập nhật", "/Patient/History");
            }
            if (entry.Entity is DoctorTransferRequest transfer && (entry.State == EntityState.Added || changed(nameof(DoctorTransferRequest.Status))))
            {
                var id = transfer.Doctor?.UserId ?? await db.DoctorDetails.Where(d => d.DoctorId == transfer.DoctorId).Select(d => d.UserId).SingleAsync(ct);
                if (transfer.Status == TransferStatus.PENDING)
                {
                    Send(id, "Đã gửi yêu cầu chuyển công tác", "/Doctor/Profile");
                    await Admins("Có yêu cầu chuyển công tác cần duyệt", "/Admin/Transfers");
                }
                else Send(id, transfer.Status == TransferStatus.APPROVED ? "Chuyển công tác đã được duyệt" : "Chuyển công tác bị từ chối",
                    "/Doctor/Profile", (transfer.AdminNote ?? "")[..Math.Min(1000, (transfer.AdminNote ?? "").Length)]);
            }
            if (entry.Entity is DoctorDetail doctor && (entry.State == EntityState.Added || changed(nameof(DoctorDetail.VerificationStatus))))
            {
                if (doctor.VerificationStatus == VerificationStatus.PENDING)
                    await Admins("Có hồ sơ bác sĩ cần duyệt", "/Admin/Doctors?status=PENDING" + (doctor.DoctorId > 0 ? $"&doctorId={doctor.DoctorId}" : ""));
                else Send(doctor.UserId, doctor.VerificationStatus == VerificationStatus.APPROVED ? "Hồ sơ bác sĩ đã được duyệt" : "Hồ sơ bác sĩ bị từ chối", "/Doctor/Profile");
            }
            if (entry.Entity is Review review && (entry.State == EntityState.Added || changed(nameof(Review.Comment))))
            {
                if (ReviewClassifier.Suggestion(ReviewClassifier.Instance.Classify(review.Comment)) != null)
                    await Admins("Có đánh giá nghi vấn cần xem xét", "/Admin/Reviews");
            }
        }
        return notifications;
    }
}
