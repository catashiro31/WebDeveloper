using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace WebDeveloper.Models.Entities
{
    [Table("specialties")]
    public class Specialty
    {
        [Key]
        [Column("specialty_id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int SpecialtyId { get; set; }

        [Required]
        [Column("specialty_name")]
        [MaxLength(100)]
        public string SpecialtyName { get; set; } = string.Empty;

        [Column("description")]
        public string? Description { get; set; }

        [Column("is_active")]
        public bool? IsActive { get; set; }

        // Navigation
        [JsonIgnore]
        public ICollection<DoctorDetail> DoctorDetails { get; set; } = new List<DoctorDetail>();
    }
}
