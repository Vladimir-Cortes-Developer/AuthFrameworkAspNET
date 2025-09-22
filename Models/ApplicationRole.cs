using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lab05WebApiML.Models
{
    /// <summary>
    /// Modelo de rol extendido de IdentityRole
    /// </summary>
    [Table("ApplicationRole")]
    public class ApplicationRole : IdentityRole
    {
        [StringLength(500)]
        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsActive { get; set; } = true;

        // Nivel de prioridad del rol (para jerarquía)
        public int Priority { get; set; }

        // Propiedad de navegación
    public virtual ICollection<ApplicationUserRole> UserRoles { get; set; } = new List<ApplicationUserRole>();

        
    }
}
