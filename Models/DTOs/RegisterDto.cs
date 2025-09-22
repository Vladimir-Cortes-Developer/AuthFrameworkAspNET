using System.ComponentModel.DataAnnotations;
using System.Data;

namespace Lab05WebApiML.Models.DTOs
{
    /// <summary>
    /// DTO para registro de nuevos usuarios
    /// </summary>
    public class RegisterDto
    {
        // Autenticación
        [Required(ErrorMessage = "El email es requerido")]
        [EmailAddress(ErrorMessage = "El formato del email no es válido")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es requerida")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "La contraseña debe tener entre 8 y 100 caracteres")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "La confirmación de contraseña es requerida")]
        [Compare("Password", ErrorMessage = "Las contraseñas no coinciden")]
        public string ConfirmPassword { get; set; } = string.Empty;

        // Identificación
        [Required(ErrorMessage = "El tipo de identificación es requerido")]
        public string TipoIdentificacion { get; set; } = string.Empty;

        [Required(ErrorMessage = "El número de identificación es requerido")]
        public string NumeroIdentificacion { get; set; } = string.Empty;

        // Información personal
        [Required(ErrorMessage = "Los nombres son requeridos")]
        public string Names { get; set; } = string.Empty;

        [Required(ErrorMessage = "Los apellidos son requeridos")]
        public string Surnames { get; set; } = string.Empty;

        [Required(ErrorMessage = "La fecha de nacimiento es requerida")]
        public DateOnly FechaNacimiento { get; set; }

        [Required(ErrorMessage = "El sexo es requerido")]
        public string Sexo { get; set; } = string.Empty;

        // Ubicación
        [Required(ErrorMessage = "La ciudad es requerida")]
        public string Ciudad { get; set; } = string.Empty;

        [Required(ErrorMessage = "El país es requerido")]
        public string Pais { get; set; } = string.Empty;

        [Required(ErrorMessage = "La dirección es requerida")]
        public string Direccion { get; set; } = string.Empty;

        [Required(ErrorMessage = "El número de teléfono es requerido")]
        public string PhoneNumber { get; set; } = string.Empty;

        // Información laboral (opcional)
        public string? Department { get; set; }
        public string? EmployeeCode { get; set; }
    }
}
