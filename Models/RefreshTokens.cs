using System.ComponentModel.DataAnnotations;

namespace Lab05WebApiML.Models
{
    public class RefreshTokens
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Token { get; set; } = string.Empty;

        [Required]
        public string UserId { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime ExpiresAt { get; set; }

        public bool IsRevoked { get; set; } = false;

        public DateTime? RevokedAt { get; set; }

        public string? RevokedBy { get; set; }

        public string? ReplacedByToken { get; set; }

        public string? CreatedByIp { get; set; }

        public string? RevokedByIp { get; set; }

        // Propiedad de navegación
        public virtual ApplicationUser User { get; set; } = null!;

        // Propiedad calculada
        public bool IsActive => !IsRevoked && DateTime.UtcNow < ExpiresAt;
    }
}
