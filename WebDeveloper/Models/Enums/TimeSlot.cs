namespace WebDeveloper.Models.Enums
{
    public enum TimeSlot
    {
        SLOT_09_00,
        SLOT_10_00,
        SLOT_11_00,
        SLOT_14_00,
        SLOT_15_00,
        SLOT_16_00,
        SLOT_17_00
    }

    public static class TimeSlotExtensions
    {
        public static string GetDisplayValue(this TimeSlot slot)
        {
            return slot switch
            {
                TimeSlot.SLOT_09_00 => "09:00",
                TimeSlot.SLOT_10_00 => "10:00",
                TimeSlot.SLOT_11_00 => "11:00",
                TimeSlot.SLOT_14_00 => "14:00",
                TimeSlot.SLOT_15_00 => "15:00",
                TimeSlot.SLOT_16_00 => "16:00",
                TimeSlot.SLOT_17_00 => "17:00",
                _ => "00:00"
            };
        }
    }
}
