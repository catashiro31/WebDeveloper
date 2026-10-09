using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using WebDeveloper.Models.Enums;

namespace WebDeveloper.Models.Entities
{
    [Table("users")]
    [Index(nameof(Email), IsUnique = true)]
    public class User
    {
        [Key]
        [Column("user_id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int UserId { get; set; }

        [Required]
        [Column("email")]
        public string Email { get; set; } = string.Empty;

        [Column("password_hash")]
        public string? PasswordHash { get; set; }

        [Column("full_name")]
        [MaxLength(100)]
        public string? FullName { get; set; }

        [Column("phone_number")]
        [MaxLength(10)]
        public string? PhoneNumber { get; set; }

        [Column("role")]
        public RoleStatus? Role { get; set; }

        [Column("is_active")]
        public bool? IsActive { get; set; }

        [Column("avatar_url")]
        public string? AvatarUrl { get; set; }

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        [Column("verification_code")]
        public string? VerificationCode { get; set; }

        [Column("refresh_token")]
        public string? RefreshToken { get; set; }

        [Column("refresh_token_expiry_time")]
        public DateTime? RefreshTokenExpiryTime { get; set; }

        [Column("code_expiry")]
        public DateTime? CodeExpiry { get; set; }

        [Column("reason_banned")]
        public string? ReasonBanned { get; set; }

        // Navigation
        [JsonIgnore]
        public ICollection<PatientProfile> PatientProfiles { get; set; } = new List<PatientProfile>();
    }
}
