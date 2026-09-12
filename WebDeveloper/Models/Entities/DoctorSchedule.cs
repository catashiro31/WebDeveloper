using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using WebDeveloper.Models.Enums;

namespace WebDeveloper.Models.Entities
{
    [Table("doctor_schedules")]
    public class DoctorSchedule
    {
        [Key]
        [Column("schedule_id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ScheduleId { get; set; }

        [Column("doctor_id")]
        public int DoctorId { get; set; }

        [JsonIgnore]
        [ForeignKey("DoctorId")]
        public DoctorDetail Doctor { get; set; } = null!;

        [Column("facility_id")]
        public int? FacilityId { get; set; }

        [ForeignKey("FacilityId")]
        public Facility? Facility { get; set; }

        [Column("date_working")]
        public DateOnly DateWorking { get; set; }

        [Column("time_slot")]
        public TimeSlot TimeSlot { get; set; }

        [Column("slot_status")]
        public SlotStatus SlotStatus { get; set; }

        [ConcurrencyCheck]
        [Column("version")]
        public int? Version { get; set; }

        // Navigation
        [JsonIgnore]
        public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    }
}
