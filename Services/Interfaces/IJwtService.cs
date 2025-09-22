using Lab05WebApiML.Models;
using Lab05WebApiML.Models.DTOs;
using System.Security.Claims;

namespace Lab05WebApiML.Services.Interfaces
{
    public interface IJwtService
    {
        Task<AuthResponseDto> GenerateJwtToken(ApplicationUser user);
        Task<AuthResponseDto> RefreshToken(string token, string refreshToken);
        Task<bool> RevokeToken(string token);
        ClaimsPrincipal? ValidateToken(string token, bool validateLifetime);
        string GenerateRefreshToken();
    }
}
