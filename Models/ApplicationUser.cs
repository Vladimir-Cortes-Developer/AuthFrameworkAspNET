using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lab05WebApiML.Models
{
    /// <summary>  //Metadata para la documentación
    /// Modelo de usuario extendido de IdentityUser para soportar campos adicionales
    /// </summary>
    [Table("ApplicationUser")]
    public class ApplicationUser : IdentityUser
    {
       
        public string TipoIdentificacion { get; set; } = string.Empty;
        public string NumeroIdentificacion { get; set; } = string.Empty;
        public string Names { get; set; } = string.Empty;
        public string Surnames { get; set; } = string.Empty;
        public DateOnly FechaNacimiento { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddYears(-18));
        public string Sexo { get; set; } = string.Empty;
        public string Ciudad { get; set; } = string.Empty;
        public string Pais { get; set; } = string.Empty;
        public string Direccion { get; set; } = string.Empty;
        
        // Opcional
        [StringLength(100)]
        public string? Department { get; set; }
        
        [StringLength(50)]
        public string? EmployeeCode { get; set; }
        // Fin Opcional

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? LastLoginAt { get; set; }

        public bool IsActive { get; set; } = true;

        public int FailedLoginAttempts { get; set; } = 0;

        public DateTime? LockoutEndTime { get; set; }


        // Propiedad de navegación para roles
        public virtual ICollection<ApplicationUserRole> UserRoles { get; set; } = new List<ApplicationUserRole>();

        // Propiedad de navegación para refresh tokens
        public virtual ICollection<RefreshTokens> RefreshTokens { get; set; } = new List<RefreshTokens>();

        // Propiedad calculada para nombre completo
        public string FullName => $"{Names} {Surnames}";
    }
}
