using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebDeveloper.Models.Entities
{
    [Table("token_blacklist")]
    public class TokenBlacklist
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        [Required]
        [Column("token")]
        [MaxLength(500)]
        public string Token { get; set; } = string.Empty;

        [Required]
        [Column("expiry_date")]
        public DateTime ExpiryDate { get; set; }

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }
    }
}
