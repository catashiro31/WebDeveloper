using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WebDeveloper.Models.Enums;

namespace WebDeveloper.Models.Entities
{
    [Table("doctor_transfer_requests")]
    public class DoctorTransferRequest
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Column("doctor_id")]
        public int DoctorId { get; set; }

        [ForeignKey("DoctorId")]
        public DoctorDetail Doctor { get; set; } = null!;

        [Column("target_facility_id")]
        public int TargetFacilityId { get; set; }

        [ForeignKey("TargetFacilityId")]
        public Facility TargetFacility { get; set; } = null!;

        [Column("status")]
        public TransferStatus Status { get; set; }

        [Column("reason")]
        public string? Reason { get; set; }

        [Column("admin_note")]
        public string? AdminNote { get; set; }

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        [Column("processed_at")]
        public DateTime? ProcessedAt { get; set; }
    }
}
