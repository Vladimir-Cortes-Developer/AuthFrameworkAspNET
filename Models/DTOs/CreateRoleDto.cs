using System.ComponentModel.DataAnnotations;

namespace Lab05WebApiML.Models.DTOs
{
    public class CreateRoleDto
    {
        [Required]
        [StringLength(50, MinimumLength = 2)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [Range(0, 100)]
        public int Priority { get; set; } = 0;
    }
}
