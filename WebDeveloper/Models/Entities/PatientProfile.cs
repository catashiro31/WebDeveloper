using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using WebDeveloper.Models.Enums;

namespace WebDeveloper.Models.Entities
{
    [Table("patient_profiles")]
    public class PatientProfile
    {
        [Key]
        [Column("patient_id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int PatientId { get; set; }

        [Column("user_id")]
        public int UserId { get; set; }

        [JsonIgnore]
        [ForeignKey("UserId")]
        public User User { get; set; } = null!;

        [Column("full_name")]
        public string? FullName { get; set; }

        [Column("date_of_birth")]
        public DateOnly? DateOfBirth { get; set; }

        [Column("gender")]
        public GenderStatus? Gender { get; set; }

        [Column("phone_number")]
        [MaxLength(10)]
        public string? PhoneNumber { get; set; }

        [Column("address")]
        public string? Address { get; set; }

        [Column("relationship")]
        [MaxLength(50)]
        public string? Relationship { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        // Navigation
        [JsonIgnore]
        public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    }
}
