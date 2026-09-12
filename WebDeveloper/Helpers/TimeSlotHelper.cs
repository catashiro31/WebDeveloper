using WebDeveloper.Models.Enums;

namespace WebDeveloper.Helpers
{
    /// <summary>
    /// Chuyển đổi Enum TimeSlot (SLOT_09_00) thành TimeOnly (09:00)
    /// Tương đương docbooking.utils.Time trong Java
    /// </summary>
    public static class TimeSlotHelper
    {
        public static TimeOnly ParseTimeSlot(TimeSlot timeSlot)
        {
            try
            {
                // SLOT_09_00 -> 09:00
                var timeStr = timeSlot.ToString().Replace("SLOT_", "").Replace("_", ":");
                return TimeOnly.Parse(timeStr);
            }
            catch
            {
                return TimeOnly.MinValue;
            }
        }
    }
}
