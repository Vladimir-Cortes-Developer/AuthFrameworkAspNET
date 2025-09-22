using System.ComponentModel.DataAnnotations;

namespace Lab05WebApiML.Models.DTOs
{
    /// <summary>
    /// DTO para renovar el token usando refresh token
    /// </summary>
    public class RefreshTokenDto
    {
        public string Token { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
    }
}
