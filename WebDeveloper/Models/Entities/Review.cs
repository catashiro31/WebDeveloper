using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace WebDeveloper.Models.Entities
{
    [Table("review")]
    public class Review
    {
        [Key]
        [Column("review_id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ReviewId { get; set; }

        [Column("appointment_id")]
        public int AppointmentId { get; set; }

        [JsonIgnore]
        [ForeignKey("AppointmentId")]
        public Appointment Appointment { get; set; } = null!;

        [Column("rating")]
        public int? Rating { get; set; }

        [Column("comment")]
        public string? Comment { get; set; }

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        [Column("is_visible")]
        public bool? IsVisible { get; set; }
    }
}
