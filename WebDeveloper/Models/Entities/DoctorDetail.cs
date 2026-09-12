using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using WebDeveloper.Models.Enums;

namespace WebDeveloper.Models.Entities
{
    [Table("doctor_details")]
    public class DoctorDetail
    {
        [Key]
        [Column("doctor_id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int DoctorId { get; set; }

        [Column("user_id")]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public User User { get; set; } = null!;

        [Column("specialty_id")]
        public int SpecialtyId { get; set; }

        [ForeignKey("SpecialtyId")]
        public Specialty Specialty { get; set; } = null!;

        [Column("facility_id")]
        public int FacilityId { get; set; }

        [ForeignKey("FacilityId")]
        public Facility Facility { get; set; } = null!;

        [Column("bio")]
        public string? Bio { get; set; }

        [Column("degree")]
        [MaxLength(50)]
        public string? Degree { get; set; }

        [Column("experience_years")]
        public int? ExperienceYears { get; set; }

        [Column("price")]
        public double? Price { get; set; }

        [Column("id_card_url")]
        public string? IdCardUrl { get; set; }

        [Column("certificate_url")]
        public string? CertificateUrl { get; set; }

        [Column("verification_status")]
        public VerificationStatus VerificationStatus { get; set; }

        [Column("rating_average")]
        public double? RatingAverage { get; set; }

        [Column("review_count")]
        public int? ReviewCount { get; set; }

        [Column("reason_reject")]
        public string? ReasonReject { get; set; }

        // Navigation
        [JsonIgnore]
        public ICollection<DoctorSchedule> DoctorSchedules { get; set; } = new List<DoctorSchedule>();
    }
}
