using Microsoft.EntityFrameworkCore;
using WebDeveloper.Data;
using WebDeveloper.Models.Enums;
using WebDeveloper.Services.Interfaces;

namespace WebDeveloper.Services
{
    public class AppointmentJobService : IAppointmentJobService
    {
        private readonly ApplicationDbContext _db;
        private readonly IEmailService _emailService;

        public AppointmentJobService(ApplicationDbContext db, IEmailService emailService)
        {
            _db = db;
            _emailService = emailService;
        }

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

        public async Task CancelUnconfirmedAppointment(int appointmentId)
        {
            var appointment = await _db.Appointments
                .Include(a => a.Schedule)
                .Include(a => a.Patient)
                .ThenInclude(p => p.User)
                .FirstOrDefaultAsync(a => a.Id == appointmentId);

            if (appointment != null && appointment.BookingStatus == BookingStatus.PENDING)
            {
                appointment.BookingStatus = BookingStatus.CANCELLED;
                appointment.Schedule.SlotStatus = SlotStatus.AVAILABLE;
                
                await _db.SaveChangesAsync();
                
                var patientEmail = appointment.Patient.User.Email;
                Console.WriteLine($"[Hangfire] HỦY TỰ ĐỘNG: Gửi email tới {patientEmail} báo lịch khám đã bị hủy do bác sĩ không phản hồi.");
            }
        }
    }
}
