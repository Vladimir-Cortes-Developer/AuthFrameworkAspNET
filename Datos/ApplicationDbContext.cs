using Lab05WebApiML.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Lab05WebApiML.Datos
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, string, IdentityUserClaim<string>, ApplicationUserRole, IdentityUserLogin<string>, IdentityRoleClaim<string>, IdentityUserToken<string>>
    {
        public DbSet<RefreshTokens> RefreshTokens { get; set; }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configurar índice único para número de identificación (excluyendo valores nulos)
            builder.Entity<ApplicationUser>()
                .HasIndex(u => u.NumeroIdentificacion)
                .IsUnique()
                .HasFilter("[NumeroIdentificacion] IS NOT NULL AND [NumeroIdentificacion] != ''");

            // Configurar ApplicationUserRole
            builder.Entity<ApplicationUserRole>(entity =>
            {
                entity.HasKey(ur => new { ur.UserId, ur.RoleId });

                entity.HasOne(ur => ur.User)
                    .WithMany(u => u.UserRoles)
                    .HasForeignKey(ur => ur.UserId)
                    .IsRequired();

                entity.HasOne(ur => ur.Role)
                    .WithMany(r => r.UserRoles)
                    .HasForeignKey(ur => ur.RoleId)
                    .IsRequired();
            });

            // Configurar RefreshTokens
            builder.Entity<RefreshTokens>(entity =>
            {
                entity.HasKey(rt => rt.Id);
                entity.Property(rt => rt.Token).IsRequired();
                entity.Property(rt => rt.UserId).IsRequired().HasMaxLength(450);
                entity.Property(rt => rt.ExpiresAt).IsRequired();
                entity.Property(rt => rt.CreatedByIp).HasMaxLength(45);
                entity.Property(rt => rt.RevokedByIp).HasMaxLength(45);

                // Configurar relación con ApplicationUser de forma más explícita
                entity.HasOne(rt => rt.User)
                    .WithMany(u => u.RefreshTokens)
                    .HasForeignKey(rt => rt.UserId)
                    .HasConstraintName("FK_RefreshTokens_AspNetUsers_UserId")
                    .OnDelete(DeleteBehavior.Cascade);

                // Índices
                entity.HasIndex(rt => rt.Token).IsUnique();
                entity.HasIndex(rt => rt.UserId);
            });

        }


        /// <summary>
        /// Método para crear roles y usuario administrador por defecto si no existen
        /// </summary>
        public static async Task SeedAdminUserAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();

            // Crear roles si no existen
            var roles = new[]
            {
                new ApplicationRole
                {
                    Name = "Admin",
                    Description = "Administrador del sistema con acceso total",
                    Priority = 100,
                    IsActive = true
                },
                new ApplicationRole
                {
                    Name = "Empleado",
                    Description = "Empleado con permisos de lectura, escritura y edición",
                    Priority = 50,
                    IsActive = true
                },
                new ApplicationRole
                {
                    Name = "UsuarioFinal",
                    Description = "Usuario final con permisos de solo lectura",
                    Priority = 10,
                    IsActive = true
                }
            };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role.Name))
                {
                    await roleManager.CreateAsync(role);
                }
            }

            // Crear usuario administrador si no existe
            var adminEmail = "admin@lab05api.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);

            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true,
                    TipoIdentificacion = "CC",
                    NumeroIdentificacion = "1234567890",
                    Names = "Admin",
                    Surnames = "Sistema",
                    FechaNacimiento = new DateOnly(1990, 1, 1),
                    Sexo = "M",
                    Ciudad = "Cucuta",
                    Pais = "Colombia",
                    PhoneNumber = "3000000000",
                    Direccion = "Calle 123 # 45-67",
                    Department = "IT",
                    EmployeeCode = "ADM001",
                    IsActive = true
                };

                var result = await userManager.CreateAsync(adminUser, "Admin@123456");
                if (result.Succeeded)
                {
                    if (await roleManager.RoleExistsAsync("Admin"))
                    {
                        await userManager.AddToRoleAsync(adminUser, "Admin");
                    }
                }
            }
        }
    }
}