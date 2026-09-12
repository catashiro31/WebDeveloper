using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace WebDeveloper.Models.Entities
{
    [Table("facilities")]
    public class Facility
    {
        [Key]
        [Column("facility_id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int FacilityId { get; set; }

        [Column("facility_name")]
        public string? FacilityName { get; set; }

        [Column("address")]
        public string? Address { get; set; }

        [Column("description")]
        public string? Description { get; set; }

        [Column("image_url")]
        public string? ImageUrl { get; set; }

        [Column("license_url")]
        public string? LicenseUrl { get; set; }

        [Column("map_url")]
        public string? MapUrl { get; set; }

        [Column("province")]
        public string? Province { get; set; }

        [Column("is_verified")]
        public bool IsVerified { get; set; } = false;

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        // Navigation
        [JsonIgnore]
        public ICollection<DoctorDetail> DoctorDetails { get; set; } = new List<DoctorDetail>();
    }
}
