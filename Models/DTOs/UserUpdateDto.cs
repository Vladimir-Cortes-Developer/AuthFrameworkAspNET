namespace Lab05WebApiML.Models.DTOs
{
    public class UserUpdateDto
    {
        // Información personal
        public string Names { get; set; } = string.Empty;
        public string Surnames { get; set; } = string.Empty;
        public DateOnly FechaNacimiento { get; set; }
        public string Sexo { get; set; } = string.Empty;

        // Ubicación
        public string Ciudad { get; set; } = string.Empty;
        public string Pais { get; set; } = string.Empty;
        public string Direccion { get; set; } = string.Empty;

        // Información laboral
        public string? Department { get; set; }
        public string? EmployeeCode { get; set; }
    }
}
