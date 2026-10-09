using Microsoft.EntityFrameworkCore;
using WebDeveloper.Data;
using WebDeveloper.Helpers;
using WebDeveloper.Models.Enums;

namespace WebDeveloper.BackgroundServices
{
    /// <summary>
    /// Background service dọn dẹp hệ thống định kỳ (Tương đương @Scheduled AppointmentScheduler.java)
    /// </summary>
    public class AppointmentCleanupService : BackgroundService
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<AppointmentCleanupService> _logger;

        public AppointmentCleanupService(IServiceProvider services, ILogger<AppointmentCleanupService> logger)
        {
            _services = services;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CleanupSystem();
                    await CleanupBlacklist();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi khi chạy background cleanup task");
                }

                // Chạy mỗi giờ một lần (trong thực tế Spring Boot chạy lúc 1h và 2h sáng, ở đây có thể tuỳ chỉnh)
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }

        private async Task CleanupSystem()
        {
            using var scope = _services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var now = TimeOnly.FromDateTime(DateTime.UtcNow);

            _logger.LogInformation("--- Bắt đầu tiến trình dọn dẹp hệ thống: {Time} ---", now);

            // 1. Đóng khung giờ trống của ngày cũ
            var pastSlots = await db.DoctorSchedules.Where(s => s.DateWorking < today && s.SlotStatus == SlotStatus.AVAILABLE).ToListAsync();
            foreach (var s in pastSlots) s.SlotStatus = SlotStatus.CLOSED;

            // 2. Đóng khung giờ trống ngày hôm nay đã quá giờ
            var todaySlots = await db.DoctorSchedules.Where(s => s.DateWorking == today && s.SlotStatus == SlotStatus.AVAILABLE).ToListAsync();
            foreach (var s in todaySlots)
            {
                if (TimeSlotHelper.ParseTimeSlot(s.TimeSlot) < now)
                    s.SlotStatus = SlotStatus.CLOSED;
            }

            // 3. Hủy lịch khám Pending quá hạn
            var pastDue = await db.Appointments
                .Include(a => a.Schedule)
                .Where(a => a.Schedule.DateWorking < today && a.BookingStatus == BookingStatus.PENDING)
                .ToListAsync();

            foreach (var a in pastDue)
            {
                a.BookingStatus = BookingStatus.CANCELLED;
                if (a.Schedule != null) a.Schedule.SlotStatus = SlotStatus.CLOSED;
            }

            await db.SaveChangesAsync();
            _logger.LogInformation("--- Dọn dẹp hoàn tất! ---");
        }

        private async Task CleanupBlacklist()
        {
            using var scope = _services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var expired = await db.TokenBlacklists.Where(t => t.ExpiryDate < DateTime.UtcNow).ToListAsync();
            db.TokenBlacklists.RemoveRange(expired);
            await db.SaveChangesAsync();
        }
    }
}
