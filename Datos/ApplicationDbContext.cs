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

            // IMPORTANTE: Llamar al método SeedData
           SeedData(builder);
        }

        public static void SeedData(ModelBuilder modelBuilder)
        {
            // ✅ CORREGIDO: Usar valores estáticos en lugar de dinámicos

            // Seed de roles básicos - IDs fijos
            var adminRoleId = "1a2b3c4d-5e6f-7g8h-9i0j-k1l2m3n4o5p6";
            var empleadoRoleId = "2b3c4d5e-6f7g-8h9i-0j1k-l2m3n4o5p6q7";
            var usuarioFinalRoleId = "3c4d5e6f-7g8h-9i0j-1k2l-m3n4o5p6q7r8";

            // Fecha fija para datos de semilla
            var seedDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            modelBuilder.Entity<ApplicationRole>().HasData(
                new ApplicationRole
                {
                    Id = adminRoleId,
                    Name = "Admin",
                    NormalizedName = "ADMIN",
                    Description = "Administrador del sistema con acceso total",
                    Priority = 100,
                    IsActive = true,
                    CreatedAt = seedDate, // ✅ Valor fijo
                    ConcurrencyStamp = "admin-role-stamp-001" // ✅ Valor fijo
                },
                new ApplicationRole
                {
                    Id = empleadoRoleId,
                    Name = "Empleado",
                    NormalizedName = "EMPLEADO",
                    Description = "Empleado con permisos de lectura, escritura y edición",
                    Priority = 50,
                    IsActive = true,
                    CreatedAt = seedDate, // ✅ Valor fijo
                    ConcurrencyStamp = "empleado-role-stamp-002" // ✅ Valor fijo
                },
                new ApplicationRole
                {
                    Id = usuarioFinalRoleId,
                    Name = "UsuarioFinal",
                    NormalizedName = "USUARIOFINAL",
                    Description = "Usuario final con permisos de solo lectura",
                    Priority = 10,
                    IsActive = true,
                    CreatedAt = seedDate, // ✅ Valor fijo
                    ConcurrencyStamp = "usuariofinal-role-stamp-003" // ✅ Valor fijo
                }
            );

            // Nota: Los usuarios (incluyendo administradores) deben crearse a través de los endpoints
            // de registro de la API para garantizar el hash correcto de contraseñas
        }
    }
}