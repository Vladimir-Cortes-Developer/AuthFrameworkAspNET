using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lab05WebApiML.Models
{
    /// <summary>
    /// Tabla intermedia personalizada para la relación muchos a muchos entre usuarios y roles
    /// </summary>
    [Table("ApplicationUserRole")]
    public class ApplicationUserRole : IdentityUserRole<string>
    {
        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

        public string? AssignedBy { get; set; }

        public DateTime? ExpiresAt { get; set; }

        // Propiedades de navegación
        public virtual ApplicationUser User { get; set; } = null!;
        public virtual ApplicationRole Role { get; set; } = null!;

        public bool IsActive { get; set; } = true;

    }
}
