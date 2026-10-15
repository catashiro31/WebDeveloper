namespace WebDeveloper.Helpers;

public static class VietnamTime
{
    public static DateTime Now => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
        TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh"));
    public static bool HasStarted(DateOnly date, Models.Enums.TimeSlot slot, DateTime now)
        => date.ToDateTime(TimeSlotHelper.ParseTimeSlot(slot)) <= now;
}
