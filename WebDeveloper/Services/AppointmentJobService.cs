using Microsoft.EntityFrameworkCore;
using WebDeveloper.Data;
using WebDeveloper.Helpers;
using WebDeveloper.Models.Enums;
using WebDeveloper.Services.Interfaces;

namespace WebDeveloper.Services
{
    public class AppointmentJobService : IAppointmentJobService
    {
        private readonly ApplicationDbContext _db;

        public AppointmentJobService(ApplicationDbContext db)
        {
            _db = db;
        }

        // === Tác vụ trì hoãn: đọc lại trạng thái PENDING trước khi nhắc/hủy để bỏ qua ca đã xử lý. ===
        public async Task SendWarningToDoctor(int appointmentId)
        {
            var appointment = await _db.Appointments
                .Include(a => a.Schedule)
                .ThenInclude(s => s.Doctor)
                .ThenInclude(d => d.User)
                .Include(a => a.Patient)
                .FirstOrDefaultAsync(a => a.Id == appointmentId);

            if (appointment != null && appointment.BookingStatus == BookingStatus.PENDING)
            {
                var doctorEmail = appointment.Schedule.Doctor.User.Email;
                var doctorName = appointment.Schedule.Doctor.User.FullName;
                
                // Gửi email cảnh báo (tạm thời log ra nếu chưa có hàm gửi)
                Console.WriteLine($"[Hangfire] CẢNH BÁO: Gửi email tới {doctorEmail} - Bác sĩ {doctorName}, bạn còn 5 phút để xác nhận lịch hẹn của {appointment.Patient.FullName}!");
            }
        }

        public async Task CancelOverdueAppointments()
        {
            var cutoff = DateTime.UtcNow.AddMinutes(-30);
            var today = DateOnly.FromDateTime(VietnamTime.Now);
            var ids = await _db.Appointments.AsNoTracking()
                .Where(a => a.BookingStatus == BookingStatus.PENDING &&
                    (a.CreatedAt <= cutoff || a.Schedule.DateWorking <= today))
                .Select(a => a.Id).ToListAsync();
            foreach (var id in ids) await CancelUnconfirmedAppointment(id);
        }

        public async Task CancelUnconfirmedAppointment(int appointmentId)
        {
            await using var transaction = await _db.Database.BeginTransactionAsync();
            await _db.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM appointments WHERE appointment_id = {appointmentId} FOR UPDATE");
            var appointment = await _db.Appointments
                .Include(a => a.Schedule)
                .Include(a => a.Patient)
                .ThenInclude(p => p.User)
                .FirstOrDefaultAsync(a => a.Id == appointmentId);

            if (appointment == null) return;
            await _db.Entry(appointment).ReloadAsync();
            await _db.Entry(appointment.Schedule).ReloadAsync();
            var started = VietnamTime.HasStarted(appointment.Schedule.DateWorking, appointment.Schedule.TimeSlot, VietnamTime.Now);
            if (appointment.BookingStatus == BookingStatus.PENDING &&
                (started || appointment.CreatedAt <= DateTime.UtcNow.AddMinutes(-30)))
            {
                appointment.BookingStatus = BookingStatus.CANCELLED;
                appointment.CancelledBy = "SYSTEM";
                appointment.CancellationReason = "Quá thời hạn xác nhận lịch hẹn (30 phút hoặc đã đến giờ khám).";
                appointment.Schedule.SlotStatus = started ? SlotStatus.CLOSED : SlotStatus.AVAILABLE;
                
                await _db.SaveChangesAsync();
                await transaction.CommitAsync();

                var patientEmail = appointment.Patient.User.Email;
                Console.WriteLine($"[Hangfire] HỦY TỰ ĐỘNG: Gửi email tới {patientEmail} báo lịch khám đã bị hủy do bác sĩ không phản hồi.");
            }
        }
    }
}
