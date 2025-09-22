namespace Lab05WebApiML.Models.DTOs
{
    /// <summary>
    /// DTO con información del usuario autenticado
    /// </summary>
    public class UserInfoDto
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;

        // Propiedades de identificación
        public string TipoIdentificacion { get; set; } = string.Empty;
        public string NumeroIdentificacion { get; set; } = string.Empty;

        // Información personal
        public string Names { get; set; } = string.Empty;
        public string Surnames { get; set; } = string.Empty;
        public string? FullName { get; set; } = string.Empty;
        public DateOnly FechaNacimiento { get; set; }
        public string Sexo { get; set; } = string.Empty;

        // Información de ubicación
        public string Ciudad { get; set; } = string.Empty;
        public string Pais { get; set; } = string.Empty;
        public string Direccion { get; set; } = string.Empty;

        public string PhoneNumber { get; set; } = string.Empty;

        // Información laboral
        public string? Department { get; set; }
        public string? EmployeeCode { get; set; }

        // Información del sistema
        public List<string> Roles { get; set; } = new List<string>();
        public DateTime? LastLoginAt { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
