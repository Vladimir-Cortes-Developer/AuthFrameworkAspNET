using Lab05WebApiML.Datos;
using Lab05WebApiML.Models;
using Lab05WebApiML.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NLog;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Lab05WebApiML.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RolesController : ControllerBase
    {
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        public RolesController(RoleManager<ApplicationRole> roleManager, UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            this._roleManager = roleManager;
            this._userManager = userManager;
            this._context  = context;
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Empleado")]
        public async Task<ActionResult<IEnumerable<RoleDto>>> GetRoles()
        {
            try
            {
                var roles = await _roleManager.Roles
                    .Where(r => r.IsActive)
                    .OrderBy(r => r.Priority)
                    .ThenBy(r => r.Name)
                    .ToListAsync();


                var roleDtos = new List<RoleDto>();

                foreach (var role in roles)
                {
                    var userCount = await _context.UserRoles
                        .CountAsync(ur => ur.RoleId == role.Id);

                    roleDtos.Add(new RoleDto
                    {
                        Id = role.Id,
                        Name = role.Name ?? string.Empty,
                        Description = role.Description,
                        Priority = role.Priority,
                        IsActive = role.IsActive,
                        CreatedAt = role.CreatedAt,
                        UserCount = userCount
                    });
                }

                _logger.Info($"Roles consultados por usuario: {User.Identity?.Name}");

                return Ok(roleDtos);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error obteniendo roles");
                return StatusCode(500, new { message = "Error al obtener los roles" });
            }
        }


        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,Empleado")]
        public async Task<ActionResult<RoleDto>> GetRole(string id)
        {
            try
            {
                var role = await _roleManager.FindByIdAsync(id);

                if (role == null)
                {
                    return NotFound(new { message = "Rol no encontrado" });
                }

                var userCount = await _context.UserRoles
                    .CountAsync(ur => ur.RoleId == role.Id);

                var roleDto = new RoleDto
                {
                    Id = role.Id,
                    Name = role.Name ?? string.Empty,
                    Description = role.Description,
                    Priority = role.Priority,
                    IsActive = role.IsActive,
                    CreatedAt = role.CreatedAt,
                    UserCount = userCount
                };


                return Ok(roleDto);
            }
            catch (Exception ex)
            {

                _logger.Error(ex, $"Error obteniendo rol con ID: {id}");
                return StatusCode(500, new { message = "Error al obtener el rol" });
            }
        }

        [HttpPost]
        public async Task<ActionResult<RoleDto>> CreateRole([FromBody] CreateRoleDto model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Verificar si el rol ya existe
                var existingRole = await _roleManager.FindByNameAsync(model.Name);

                if (existingRole != null)
                {
                    return BadRequest(new { message = "El rol ya existe" });
                }

                //Conversión del DTO a un objeto concreto de la clase ApplicationRole. OJO: Automapper
                var role = new ApplicationRole
                {
                    Name = model.Name,
                    NormalizedName = model.Name.ToUpper(),
                    Description = model.Description,
                    Priority = model.Priority,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                var result = await _roleManager.CreateAsync(role);

                if (!result.Succeeded)
                {
                    return BadRequest(new
                    {
                        message = "Error al crear el rol",
                        errors = result.Errors.Select(e => e.Description)
                    });
                }

                _logger.Info($"Rol creado: {role.Name} por usuario: {User.Identity?.Name}");

                return CreatedAtAction(nameof(GetRole), new { id = role.Id }, new RoleDto
                {
                    Id = role.Id,
                    Name = role.Name,
                    Description = role.Description,
                    Priority = role.Priority,
                    IsActive = role.IsActive,
                    CreatedAt = role.CreatedAt,
                    UserCount = 0
                });

            }
            catch (Exception ex)
            {

                _logger.Error(ex, "Error creando rol");
                return StatusCode(500, new { message = "Error al crear el rol" });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateRole(string id, [FromBody] CreateRoleDto model)
        {
            try
            {
                var role = await _roleManager.FindByIdAsync(id);

                if (role == null)
                {
                    return NotFound(new { message = "Rol no encontrado" });
                }

                // Verificar si el nuevo nombre ya existe (si cambió)
                if (role.Name != model.Name)
                {
                    var existingRole = await _roleManager.FindByNameAsync(model.Name);
                    if (existingRole != null)
                    {
                        return BadRequest(new { message = "Ya existe un rol con ese nombre" });
                    }
                }

                //DTO to Concret Object. Automapper
                role.Name = model.Name;
                role.NormalizedName = model.Name.ToUpper();
                role.Description = model.Description;
                role.Priority = model.Priority;

                var result = await _roleManager.UpdateAsync(role);

                if (!result.Succeeded)
                {
                    return BadRequest(new
                    {
                        message = "Error al actualizar el rol",
                        errors = result.Errors.Select(e => e.Description)
                    });
                }

                _logger.Info($"Rol actualizado: {role.Name} por usuario: {User.Identity?.Name}");

                return NoContent();
            }
            catch (Exception ex)
            {

                _logger.Error(ex, $"Error actualizando rol con ID: {id}");
                return StatusCode(500, new { message = "Error al actualizar el rol" });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRole(string id)
        {
            try
            {
                var role = await _roleManager.FindByIdAsync(id);
                if (role == null)
                {
                    return NotFound(new { message = "Rol no encontrado" });
                }

                // Verificar si hay usuarios con este rol
                var userCount = await _context.UserRoles
                    .CountAsync(ur => ur.RoleId == role.Id);


                if (userCount > 0) {
                    // Desactivar en lugar de eliminar si hay usuarios
                    role.IsActive = false;
                    await _roleManager.UpdateAsync(role);

                    _logger.Info($"Rol desactivado (tiene usuarios): {role.Name} por usuario: {User.Identity?.Name}");

                    return Ok(new { message = $"Rol desactivado. Tiene {userCount} usuarios asignados." });
                }
                else
                {
                    // Eliminar si no hay usuarios
                    var result = await _roleManager.DeleteAsync(role);

                    if (!result.Succeeded)
                    {
                        return BadRequest(new
                        {
                            message = "Error al eliminar el rol",
                            errors = result.Errors.Select(e => e.Description)
                        });
                    }

                    _logger.Info($"Rol eliminado: {role.Name} por usuario: {User.Identity?.Name}");
                }

                return NoContent();
            }
            catch (Exception ex)
            {

                _logger.Error(ex, $"Error eliminando rol con ID: {id}");
                return StatusCode(500, new { message = "Error al eliminar el rol" });
            }
        }


        [HttpPost("assign")]
        public async Task<IActionResult> AssignRolesToUser([FromBody] AssignRoleDto model)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(model.UserId);

                if (user == null)
                {
                    return NotFound(new { message = "Usuario no encontrado" });
                }

                // Obtener roles actuales del usuario
                var currentRoles = await _userManager.GetRolesAsync(user);

                // Remover todos los roles actuales ??????
                if (currentRoles.Any())
                {
                    var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
                    if (!removeResult.Succeeded)
                    {
                        return BadRequest(new
                        {
                            message = "Error al remover roles actuales",
                            errors = removeResult.Errors.Select(e => e.Description)
                        });
                    }
                }


                // Asignar nuevos roles
                foreach (var roleName in model.Roles)
                {
                    if (await _roleManager.RoleExistsAsync(roleName))
                    {
                        var addResult = await _userManager.AddToRoleAsync(user, roleName);

                        if (addResult.Succeeded)
                        {
                            // Registrar asignación en tabla personalizada
                            var role = await _roleManager.FindByNameAsync(roleName);
                            var userRole = new ApplicationUserRole
                            {
                                UserId = user.Id.ToString(),
                                RoleId = role!.Id,
                                AssignedAt = DateTime.UtcNow,
                                AssignedBy = User.Identity?.Name,
                                ExpiresAt = model.ExpiresAt
                            };

                            // Nota: Esta parte requiere personalización adicional de Identity
                            _logger.Info($"Rol {roleName} asignado a usuario {user.Email}");
                        }
                    }
                    else
                    {
                        _logger.Warn($"Intento de asignar rol inexistente: {roleName}");
                    }
                }

                _logger.Info($"Roles actualizados para usuario {user.Email} por: {User.Identity?.Name}");

                return Ok(new { message = "Roles asignados exitosamente" });
            }
            catch (Exception ex)
            {

                _logger.Error(ex, "Error asignando roles a usuario");
                return StatusCode(500, new { message = "Error al asignar roles" });
            }
        }

        [HttpGet("{roleId}/users")]
        [Authorize(Roles = "Admin,Empleado")]
        public async Task<ActionResult<IEnumerable<UserInfoDto>>> GetUsersInRole(string roleId)
        {
            try
            {
                var role = await _roleManager.FindByIdAsync(roleId);
                if (role == null)
                {
                    return NotFound(new { message = "Rol no encontrado" });
                }

                var usersInRole = await _userManager.GetUsersInRoleAsync(role.Name!);

                // Mapear a un DTO

                var userDtos = usersInRole.Select(u => new UserInfoDto
                {
                    Id = u.Id.ToString(),
                    Email = u.Email ?? string.Empty,
                    UserName = u.UserName ?? string.Empty,
                    Names = u.Names,
                    Surnames = u.Surnames,
                    FullName = u.FullName,
                    Department = u.Department,
                    EmployeeCode = u.EmployeeCode,
                    LastLoginAt = u.LastLoginAt,
                    IsActive = u.IsActive
                }).ToList();

                return Ok(userDtos);

            }
            catch (Exception ex)
            {

                _logger.Error(ex, $"Error obteniendo usuarios del rol: {roleId}");
                return StatusCode(500, new { message = "Error al obtener usuarios del rol" });
            }
        }


        [HttpPost("initialize")]
        [AllowAnonymous] // Solo para inicialización del sistema
        public async Task<IActionResult> InitializeRoles()
        {
            try
            {
                // Verificar si ya existen roles
                if (await _roleManager.Roles.AnyAsync())
                {
                    return BadRequest(new { message = "Los roles ya han sido inicializados" });
                }

                var roles = new List<ApplicationRole>
                {
                     new ApplicationRole
                    {
                        Name = "Admin",
                        NormalizedName = "ADMIN",
                        Description = "Administrador del sistema con acceso total",
                        Priority = 100,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    },
                     new ApplicationRole
                    {
                        Name = "Empleado",
                        NormalizedName = "EMPLEADO",
                        Description = "Empleado con permisos de lectura, escritura y edición",
                        Priority = 50,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    },
                     new ApplicationRole
                    {
                        Name = "UsuarioFinal",
                        NormalizedName = "USUARIOFINAL",
                        Description = "Usuario final con permisos de solo lectura",
                        Priority = 10,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    }
                };

                foreach (var role in roles)
                {
                    await _roleManager.CreateAsync(role);
                    _logger.Info($"Rol inicializado: {role.Name}");
                }
                return Ok(new { message = "Roles inicializados exitosamente" });
            }
            catch (Exception ex)
            {

                _logger.Error(ex, "Error inicializando roles");
                return StatusCode(500, new { message = "Error al inicializar roles" });
            }
        }

    }
}
