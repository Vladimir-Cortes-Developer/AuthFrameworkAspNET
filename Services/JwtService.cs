using Lab05WebApiML.Datos;
using Lab05WebApiML.Models;
using Lab05WebApiML.Models.DTOs;
using Lab05WebApiML.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using NLog;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Lab05WebApiML.Services
{
    public class JwtService : IJwtService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor; // ✅ AGREGADO
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        // Configuración JWT. Campos
        private readonly string _jwtSecret;
        private readonly string _jwtIssuer;
        private readonly string _jwtAudience;
        private readonly int _jwtExpirationMinutes;
        private readonly int _refreshTokenExpirationDays;

        // ✅ CONSTRUCTOR CORREGIDO - Inyectar IHttpContextAccessor
        public JwtService(
            UserManager<ApplicationUser> userManager,
            IConfiguration configuration,
            ApplicationDbContext context,
            IHttpContextAccessor httpContextAccessor)
        {
            _userManager = userManager;
            _configuration = configuration;
            _context = context;
            _httpContextAccessor = httpContextAccessor; // ✅ AGREGADO

            // Cargar configuración JWT
            _jwtSecret = configuration["JWT:Secret"] ?? throw new InvalidOperationException("JWT Secret no configurado");
            _jwtIssuer = configuration["JWT:Issuer"] ?? "Lab05WebApiML";
            _jwtAudience = configuration["JWT:Audience"] ?? "Lab05WebApiML-Users";
            _jwtExpirationMinutes = int.Parse(configuration["JWT:ExpirationMinutes"] ?? "60");
            _refreshTokenExpirationDays = int.Parse(configuration["JWT:RefreshTokenExpirationDays"] ?? "7");
        }

        public async Task<AuthResponseDto> GenerateJwtToken(ApplicationUser user)
        {
            try
            {
                _logger.Info($"Generando token JWT para usuario: {user.Email}");

                // Obtener roles del usuario
                var userRoles = await _userManager.GetRolesAsync(user);

                // Crear claims
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
                    new Claim(ClaimTypes.Name, user.UserName ?? string.Empty),
                    new Claim("FirstName", user.Names),
                    new Claim("LastName", user.Surnames),
                    new Claim("FullName", user.FullName),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                    new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
                };

                // Agregar claims de roles
                foreach (var role in userRoles)
                {
                    claims.Add(new Claim(ClaimTypes.Role, role));
                }

                // Agregar claims personalizados
                if (!string.IsNullOrEmpty(user.Department))
                    claims.Add(new Claim("Department", user.Department));

                if (!string.IsNullOrEmpty(user.EmployeeCode))
                    claims.Add(new Claim("EmployeeCode", user.EmployeeCode));

                // Generar token
                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSecret));
                var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
                var expiration = DateTime.UtcNow.AddMinutes(_jwtExpirationMinutes);

                var token = new JwtSecurityToken(
                    issuer: _jwtIssuer,
                    audience: _jwtAudience,
                    claims: claims,
                    expires: expiration,
                    signingCredentials: credentials
                );
                var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

                // Generar refresh token
                var refreshToken = GenerateRefreshToken();

                // Guardar refresh token en base de datos
                var refreshTokenEntity = new RefreshTokens
                {
                    Token = refreshToken,
                    UserId = user.Id.ToString(),
                    ExpiresAt = DateTime.UtcNow.AddDays(_refreshTokenExpirationDays),
                    CreatedByIp = GetClientIp()
                };

                _context.RefreshTokens.Add(refreshTokenEntity);

                // Actualizar último login
                user.LastLoginAt = DateTime.UtcNow;
                user.FailedLoginAttempts = 0;

                await _context.SaveChangesAsync();

                _logger.Info($"Token JWT generado exitosamente para usuario: {user.Email}");

                return new AuthResponseDto
                {
                    IsAuthenticated = true,
                    Token = tokenString,
                    RefreshToken = refreshToken,
                    TokenExpiration = expiration,
                    Message = "Autenticación exitosa",
                    User = new UserInfoDto
                    {
                        Id = user.Id.ToString(),
                        Email = user.Email ?? string.Empty,
                        UserName = user.UserName ?? string.Empty,

                        // Información de identificación
                        TipoIdentificacion = user.TipoIdentificacion,
                        NumeroIdentificacion = user.NumeroIdentificacion,

                        // Información personal
                        Names = user.Names,
                        Surnames = user.Surnames,
                        FullName = user.FullName,
                        FechaNacimiento = user.FechaNacimiento,
                        Sexo = user.Sexo,

                        // Información de ubicación
                        Ciudad = user.Ciudad,
                        Pais = user.Pais,
                        Direccion = user.Direccion,

                        // Información laboral
                        Department = user.Department,
                        EmployeeCode = user.EmployeeCode,

                        // Información del sistema
                        Roles = userRoles.ToList(),
                        LastLoginAt = user.LastLoginAt,
                        IsActive = user.IsActive,
                        CreatedAt = user.CreatedAt
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Error generando token JWT para usuario: {user.Email}");
                return new AuthResponseDto
                {
                    IsAuthenticated = false,
                    Message = "Error en la autenticación",
                    Errors = new List<string> {
                        "Ocurrió un error durante la autenticación",
                        $"Detalles: {ex.Message}"
                    }
                };
            }
        }

        // ✅ MÉTODO CORREGIDO - Usar IHttpContextAccessor inyectado
        private string GetClientIp()
        {
            try
            {
                var context = _httpContextAccessor.HttpContext;
                if (context != null)
                {
                    return context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error obteniendo IP del cliente");
            }
            return "Unknown";
        }

        public string GenerateRefreshToken()
        {
            var randomNumber = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        public async Task<AuthResponseDto> RefreshToken(string token, string refreshToken)
        {
            try
            {
                _logger.Info("Intentando renovar token JWT");

                // Validar el token actual (puede estar expirado)
                var principal = ValidateToken(token, validateLifetime: false);

                if (principal == null)
                {
                    _logger.Warn("Token inválido para renovación");
                    return new AuthResponseDto
                    {
                        IsAuthenticated = false,
                        Message = "Token inválido"
                    };
                }

                var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userId))
                {
                    return new AuthResponseDto
                    {
                        IsAuthenticated = false,
                        Message = "Usuario no encontrado en el token"
                    };
                }

                // Buscar y validar refresh token
                var storedRefreshToken = await _context.RefreshTokens
                    .FirstOrDefaultAsync(rt => rt.Token == refreshToken && rt.UserId == userId);

                if (storedRefreshToken == null || !storedRefreshToken.IsActive)
                {
                    _logger.Warn($"Refresh token inválido o inactivo para usuario: {userId}");
                    return new AuthResponseDto
                    {
                        IsAuthenticated = false,
                        Message = "Refresh token inválido o expirado"
                    };
                }

                // Revocar el refresh token actual
                storedRefreshToken.IsRevoked = true;
                storedRefreshToken.RevokedAt = DateTime.UtcNow;
                storedRefreshToken.RevokedByIp = GetClientIp();

                // Obtener usuario
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null || !user.IsActive)
                {
                    return new AuthResponseDto
                    {
                        IsAuthenticated = false,
                        Message = "Usuario no encontrado o inactivo"
                    };
                }

                // Generar nuevo token
                var newTokenResponse = await GenerateJwtToken(user);

                // Marcar el refresh token antiguo como reemplazado
                storedRefreshToken.ReplacedByToken = newTokenResponse.RefreshToken;
                await _context.SaveChangesAsync();

                _logger.Info($"Token renovado exitosamente para usuario: {user.Email}");

                return newTokenResponse;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error renovando token JWT");
                return new AuthResponseDto
                {
                    IsAuthenticated = false,
                    Message = "Error renovando el token",
                    Errors = new List<string> { "Ocurrió un error durante la renovación del token" }
                };
            }
        }

        public async Task<bool> RevokeToken(string token)
        {
            try
            {
                var refreshToken = await _context.RefreshTokens
                   .FirstOrDefaultAsync(rt => rt.Token == token);

                if (refreshToken == null || !refreshToken.IsActive)
                    return false;

                refreshToken.IsRevoked = true;
                refreshToken.RevokedAt = DateTime.UtcNow;
                refreshToken.RevokedByIp = GetClientIp();

                await _context.SaveChangesAsync();

                _logger.Info($"Refresh token revocado: {token.Substring(0, 10)}...");

                return true;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error revocando refresh token");
                return false;
            }
        }

        public ClaimsPrincipal? ValidateToken(string token, bool validateLifetime = true)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(_jwtSecret);
                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidIssuer = _jwtIssuer,
                    ValidateAudience = true,
                    ValidAudience = _jwtAudience,
                    ValidateLifetime = validateLifetime,
                    ClockSkew = TimeSpan.Zero
                };

                var principal = tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);
                return principal;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error validando token JWT");
                return null;
            }
        }
    }
}
