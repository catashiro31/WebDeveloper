using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using WebDeveloper.Models.Enums;

namespace WebDeveloper.Models.Entities
{
    [Table("appointments")]
    public class Appointment
    {
        [Key]
        [Column("appointment_id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Column("patient_id")]
        public int PatientId { get; set; }

        [JsonIgnore]
        [ForeignKey("PatientId")]
        public PatientProfile Patient { get; set; } = null!;

        [Column("schedule_id")]
        public int ScheduleId { get; set; }

        [JsonIgnore]
        [ForeignKey("ScheduleId")]
        public DoctorSchedule Schedule { get; set; } = null!;

        public double? BookedPrice { get; set; }
        public int? BookedSpecialtyId { get; set; }
        public string? BookedSpecialtyName { get; set; }
        public int? BookedFacilityId { get; set; }
        public string? BookedFacilityName { get; set; }
        public string? BookedFacilityAddress { get; set; }
        public bool IsLegacySnapshot { get; set; }

        [Column("reason")]
        public string? Reason { get; set; }

        [Column("booking_status")]
        public BookingStatus BookingStatus { get; set; }

        [Column("cancellation_reason")]
        public string? CancellationReason { get; set; }

        [Column("cancelled_by")]
        public string? CancelledBy { get; set; }

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }
    }
}
