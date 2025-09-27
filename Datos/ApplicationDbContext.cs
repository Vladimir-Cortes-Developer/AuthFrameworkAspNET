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
            // NOTA: Los datos semilla (roles y usuario administrador) se crearán mediante
            // código de inicialización en el Program.cs para evitar problemas de migraciones
            // con valores dinámicos que Entity Framework considera no determinísticos
        }
    }
}