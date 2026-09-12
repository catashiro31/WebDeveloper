using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace WebDeveloper.Models.Entities
{
    [Table("medical_results")]
    public class MedicalResult
    {
        [Key]
        [Column("result_id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ResultId { get; set; }

        [Column("appointment_id")]
        public int AppointmentId { get; set; }

        [JsonIgnore]
        [ForeignKey("AppointmentId")]
        public Appointment Appointment { get; set; } = null!;

        [Column("diagnosis")]
        public string? Diagnosis { get; set; }

        [Column("prescription_url")]
        public string? PrescriptionUrl { get; set; }

        [Column("doctor_notes")]
        public string? DoctorNotes { get; set; }

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }
    }
}
