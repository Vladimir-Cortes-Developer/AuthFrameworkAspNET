using Lab05WebApiML.Datos;
using Lab05WebApiML.Models;
using Lab05WebApiML.Models.DTOs;
using Lab05WebApiML.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NLog;
using System.Security.Claims;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Lab05WebApiML.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthenticationController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly IJwtService _jwtService;
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        //Inyección de dependencias e inicialización por el constructor.
        public AuthenticationController(UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<ApplicationRole> roleManager,
            IJwtService jwtService,
            ApplicationDbContext context,
            IConfiguration configuration)
        {
                this._userManager = userManager;
            this._signInManager = signInManager;
            this._roleManager = roleManager;
            this._jwtService = jwtService;
            this._context = context;
            this._configuration = configuration;

        }

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<ActionResult<AuthResponseDto>> Register([FromBody] RegisterDto model)
        {
            try
            {
                _logger.Info($"Intento de registro para email: {model.Email}");
                
                if (!ModelState.IsValid) {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();

                    return BadRequest(new AuthResponseDto
                    {
                        IsAuthenticated = false,
                        Message = "Datos de registro inválidos",
                        Errors = errors
                    });
                }

                // Verificar si el usuario ya existe
                var existingUser = await _userManager.FindByEmailAsync(model.Email);

                if (existingUser != null)
                {
                    _logger.Warn($"Intento de registro con email existente: {model.Email}");
                    return BadRequest(new AuthResponseDto
                    {
                        IsAuthenticated = false,
                        Message = "El usuario ya existe",
                        Errors = new List<string> { "El email ya está registrado en el sistema" }
                    });
                }

                // Verificar si el número de identificación ya existe (si no está vacío)
                if (!string.IsNullOrEmpty(model.NumeroIdentificacion) && model.NumeroIdentificacion != "string")
                {
                    var existingUserByIdNumber = await _context.Users
                        .FirstOrDefaultAsync(u => u.NumeroIdentificacion == model.NumeroIdentificacion);

                    if (existingUserByIdNumber != null)
                    {
                        _logger.Warn($"Intento de registro con número de identificación existente: {model.NumeroIdentificacion}");
                        return BadRequest(new AuthResponseDto
                        {
                            IsAuthenticated = false,
                            Message = "El número de identificación ya está registrado",
                            Errors = new List<string> { "Este número de identificación ya está asociado a otra cuenta" }
                        });
                    }
                }

                // Crear nuevo usuario
                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,

                    // Información de identificación
                    TipoIdentificacion = model.TipoIdentificacion,
                    NumeroIdentificacion = model.NumeroIdentificacion,

                    // Información personal
                    Names = model.Names,
                    Surnames = model.Surnames,
                    FechaNacimiento = model.FechaNacimiento,
                    Sexo = model.Sexo,

                    // Información de ubicación
                    Ciudad = model.Ciudad,
                    Pais = model.Pais,
                    Direccion = model.Direccion,
                    PhoneNumber = model.PhoneNumber,

                    // Información laboral (opcional)
                    Department = model.Department,
                    EmployeeCode = model.EmployeeCode,

                    // Configuración del sistema
                    EmailConfirmed = false,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                // Crear usuario con contraseña
                var result = await _userManager.CreateAsync(user, model.Password);


                if (!result.Succeeded)
                {
                    _logger.Error($"Error creando usuario: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                    return BadRequest(new AuthResponseDto
                    {
                        IsAuthenticated = false,
                        Message = "Error al crear el usuario",
                        Errors = result.Errors.Select(e => e.Description).ToList()
                    });
                }

                // Asignar rol por defecto
                var defaultRole = "UsuarioFinal";
                if (await _roleManager.RoleExistsAsync(defaultRole))
                {
                    await _userManager.AddToRoleAsync(user, defaultRole);
                    _logger.Info($"Rol por defecto {defaultRole} asignado a usuario {user.Email}");
                }
                else
                {
                    _logger.Warn($"Rol por defecto {defaultRole} no existe en el sistema");
                }

                // Generar token de confirmación de email
                var emailToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);

                // TODO: Enviar email de confirmación
                _logger.Info($"Token de confirmación generado para {user.Email}: {emailToken.Substring(0, 10)}...");

                // Generar JWT
                var authResponse = await _jwtService.GenerateJwtToken(user);

                // Verificar si la generación del token fue exitosa
                if (!authResponse.IsAuthenticated)
                {
                    _logger.Error($"Error generando token para usuario registrado: {user.Email}");
                    return StatusCode(500, authResponse);
                }

                _logger.Info($"Usuario registrado exitosamente: {user.Email}");

                return Ok(authResponse);

            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error en el proceso de registro");
                return StatusCode(500, new AuthResponseDto
                {
                    IsAuthenticated = false,
                    Message = "Error en la autenticación",
                    Errors = new List<string> {
                        "Ocurrió un error durante la autenticación",
                        $"Tipo de error: {ex.GetType().Name}",
                        $"Mensaje: {ex.Message}",
                        ex.InnerException != null ? $"Error interno: {ex.InnerException.Message}" : ""
                    }.Where(s => !string.IsNullOrEmpty(s)).ToList()
                });
            }

        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginDto model)
        {
            try
            {
                _logger.Info($"Intento de login para email: {model.Email}");
                
                if (!ModelState.IsValid)
                {
                    return BadRequest(new AuthResponseDto
                    {
                        IsAuthenticated = false,
                        Message = "Datos de login inválidos"
                    });
                }

                // Buscar usuario
                var user = await _userManager.FindByEmailAsync(model.Email);

                if (user == null)
                {
                    _logger.Warn($"Intento de login con email no registrado: {model.Email}");
                    return Unauthorized(new AuthResponseDto
                    {
                        IsAuthenticated = false,
                        Message = "Credenciales inválidas"
                    });
                }

                // Verificar si el usuario está activo
                if (!user.IsActive)
                {
                    _logger.Warn($"Intento de login con usuario inactivo: {model.Email}");
                    return Unauthorized(new AuthResponseDto
                    {
                        IsAuthenticated = false,
                        Message = "Usuario inactivo. Contacte al administrador."
                    });
                }

                // Verificar si el usuario está bloqueado
                if (user.LockoutEndTime.HasValue && user.LockoutEndTime > DateTime.UtcNow)
                {
                    var remainingTime = (user.LockoutEndTime.Value - DateTime.UtcNow).TotalMinutes;
                    _logger.Warn($"Intento de login con usuario bloqueado: {model.Email}");
                    return Unauthorized(new AuthResponseDto
                    {
                        IsAuthenticated = false,
                        Message = $"Usuario bloqueado. Intente nuevamente en {Math.Ceiling(remainingTime)} minutos."
                    });
                }

                // Verificar contraseña
                var result = await _signInManager.CheckPasswordSignInAsync(user, model.Password, false);

                if (!result.Succeeded)
                {
                    // Incrementar intentos fallidos
                    user.FailedLoginAttempts++;

                    // Bloquear después de 5 intentos
                    if (user.FailedLoginAttempts >= 5)
                    {
                        user.LockoutEndTime = DateTime.UtcNow.AddMinutes(30);
                        _logger.Warn($"Usuario bloqueado por múltiples intentos fallidos: {model.Email}");
                    }

                    try
                    {
                        await _context.SaveChangesAsync();
                    }
                    catch (Exception saveEx)
                    {
                        _logger.Error(saveEx, "Error al guardar intentos fallidos de login");
                    }

                    _logger.Warn($"Login fallido para usuario: {model.Email}. Intentos: {user.FailedLoginAttempts}");

                    return Unauthorized(new AuthResponseDto
                    {
                        IsAuthenticated = false,
                        Message = user.FailedLoginAttempts >= 5
                            ? "Usuario bloqueado por múltiples intentos fallidos"
                            : "Credenciales inválidas"
                    });
                }

                // Reset failed login attempts on successful login
                if (user.FailedLoginAttempts > 0)
                {
                    user.FailedLoginAttempts = 0;
                    user.LockoutEndTime = null;
                    user.LastLoginAt = DateTime.UtcNow;

                    try
                    {
                        await _context.SaveChangesAsync();
                    }
                    catch (Exception saveEx)
                    {
                        _logger.Error(saveEx, "Error al resetear intentos fallidos en login exitoso");
                    }
                }

                // Generar JWT
                var authResponse = await _jwtService.GenerateJwtToken(user);

                // Verificar si la generación del token fue exitosa
                if (!authResponse.IsAuthenticated)
                {
                    _logger.Error($"Error generando token para login de usuario: {user.Email}");
                    return StatusCode(500, authResponse);
                }

                // Registrar información adicional del cliente
                //if (!string.IsNullOrEmpty(model.ClientIp) || !string.IsNullOrEmpty(model.UserAgent))
                //{
                //    _logger.Info($"Login exitoso - Usuario: {user.Email}, IP: {model.ClientIp}, UserAgent: {model.UserAgent}");
                //}

                return Ok(authResponse);

            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error en el proceso de login");
                return StatusCode(500, new AuthResponseDto
                {
                    IsAuthenticated = false,
                    Message = "Error en la autenticación",
                    Errors = new List<string> { "Ocurrió un error durante la autenticación", $"Detalles: {ex.Message}" }
                });
            }
        }


        [HttpPost("refresh-token")]
        [AllowAnonymous]
        public async Task<ActionResult<AuthResponseDto>> RefreshToken([FromBody] RefreshTokenDto model)
        {
            _logger.Info("Solicitud de renovación de token");

            var response = await _jwtService.RefreshToken(model.Token, model.RefreshToken);

            if (!response.IsAuthenticated)
            {
                return Unauthorized(response);
            }

            return Ok(response);
        }

        [HttpPost("revoke-token")]
        [Authorize]
        public async Task<IActionResult> RevokeToken([FromBody] string refreshToken) {
            var result = await _jwtService.RevokeToken(refreshToken);

            if (!result)
            {
                return BadRequest(new { message = "Token no encontrado o ya revocado" });
            }

            _logger.Info("Refresh token revocado exitosamente");
            return Ok(new { message = "Token revocado exitosamente" });
        }


        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (!string.IsNullOrEmpty(userId))
                {
                    // Revocar todos los refresh tokens del usuario
                    var userTokens = await _context.RefreshTokens
                        .Where(rt => rt.UserId == userId && !rt.IsRevoked)
                        .ToListAsync();

                    foreach (var token in userTokens)
                    {
                        token.IsRevoked = true;
                        token.RevokedAt = DateTime.UtcNow;
                    }

                    await _context.SaveChangesAsync();

                    _logger.Info($"Logout exitoso para usuario ID: {userId}");
                }

                return Ok(new { message = "Sesión cerrada exitosamente" });
            }
            catch (Exception ex)
            {

                _logger.Error(ex, "Error durante el logout");
                return StatusCode(500, new { message = "Error al cerrar sesión" });
            }
        }


        [HttpPost("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto model)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "Token inválido" });
                }
                var user = await _userManager.FindByIdAsync(userId);

                if (user == null)
                {
                    return NotFound(new { message = "Usuario no encontrado" });
                }

                var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);

                if (!result.Succeeded)
                {
                    return BadRequest(new
                    {
                        message = "Error al cambiar la contraseña",
                        errors = result.Errors.Select(e => e.Description)
                    });
                }

                _logger.Info($"Contraseña cambiada exitosamente para usuario: {user.Email}");

                return Ok(new { message = "Contraseña cambiada exitosamente" });

            }
            catch (Exception ex)
            {

                _logger.Error(ex, "Error cambiando contraseña");
                return StatusCode(500, new { message = "Error al cambiar la contraseña" });
            }
        }

        [HttpGet("profile")]
        [Authorize]
        public async Task<ActionResult<UserInfoDto>> GetProfile() {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "Token inválido" });
                }
                var user = await _userManager.FindByIdAsync(userId);

                if (user == null)
                {
                    return NotFound(new { message = "Usuario no encontrado" });
                }

                var roles = await _userManager.GetRolesAsync(user);

                var userInfo = new UserInfoDto
                {
                    Id = user.Id.ToString(),
                    Email = user.Email ?? string.Empty,
                    UserName = user.UserName ?? string.Empty,
                    Names = user.Names,
                    Surnames = user.Surnames,
                    FullName = user.FullName,
                    PhoneNumber = user.PhoneNumber,
                    Pais = user.Pais,
                    Ciudad = user.Ciudad,
                    FechaNacimiento = user.FechaNacimiento,
                    Sexo = user.Sexo,
                    TipoIdentificacion = user.TipoIdentificacion,
                    NumeroIdentificacion = user.NumeroIdentificacion,
                    Department = user.Department,
                    EmployeeCode = user.EmployeeCode,
                    Roles = roles.ToList(),
                    LastLoginAt = user.LastLoginAt,
                    IsActive = user.IsActive
                };
                return Ok(userInfo);

            }
            catch (Exception ex)
            {

                _logger.Error(ex, "Error obteniendo perfil de usuario");
                return StatusCode(500, new { message = "Error al obtener el perfil" });
            }
        }


    }
}
