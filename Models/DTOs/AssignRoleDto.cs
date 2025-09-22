using System.ComponentModel.DataAnnotations;

namespace Lab05WebApiML.Models.DTOs
{
    public class AssignRoleDto
    {
        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        public List<string> Roles { get; set; } = new List<string>();

        public DateTime? ExpiresAt { get; set; }

        public string? AssignedBy { get; set; }
    }
}
