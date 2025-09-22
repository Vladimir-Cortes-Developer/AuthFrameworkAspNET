namespace Lab05WebApiML.Models.DTOs
{
    /// <summary>
    /// DTO para respuesta de autenticación
    /// </summary>
    public class AuthResponseDto
    {
        public bool IsAuthenticated { get; set; }
        public string Token { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime TokenExpiration { get; set; }
        public string Message { get; set; } = string.Empty;
        public UserInfoDto? User { get; set; }
        public List<string> Errors { get; set; } = new List<string>();

    }
}
