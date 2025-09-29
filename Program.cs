using Lab05WebApiML.Datos;
using Lab05WebApiML.Models;
using Lab05WebApiML.Services;
using Lab05WebApiML.Services.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using NLog;
using NLog.Web;
using System.Text;


// Configurar NLog para logging
var logger = NLog.LogManager.Setup().LoadConfigurationFromFile("nlog.config").GetCurrentClassLogger();
logger.Debug("Iniciando aplicación");

try
{
    var builder = WebApplication.CreateBuilder(args);

    // 1. Configurar NLog
    builder.Logging.ClearProviders();
    builder.Host.UseNLog();

    // 2. Configurar DbContext
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

    // 3. Configurar Identity con entidades personalizadas
    builder.Services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
    {
        // Configuraci�n de contrase�a
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;
        options.Password.RequiredLength = 8;
        options.Password.RequiredUniqueChars = 4;

        // Configuraci�n de usuario
        options.User.RequireUniqueEmail = true;
        options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";

        // Configuraci�n de bloqueo
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(30);
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.AllowedForNewUsers = true;

        // Configuraci�n de SignIn
        options.SignIn.RequireConfirmedEmail = false;
        options.SignIn.RequireConfirmedPhoneNumber = false;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

    // 4. Configurar JWT Authentication
    var jwtSettings = builder.Configuration.GetSection("JWT");
    var secretKey = jwtSettings["Secret"] ?? throw new InvalidOperationException("JWT Secret no configurado");
    var key = Encoding.UTF8.GetBytes(secretKey);

    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false; // false para desarrollo
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.Zero,
            ValidIssuer = jwtSettings["Issuer"] ?? "Lab05WebApiML",
            ValidAudience = jwtSettings["Audience"] ?? "Lab05WebApiMLUsers",
            IssuerSigningKey = new SymmetricSecurityKey(key)
        };

        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
                {
                    context.Response.Headers.Append("Token-Expired", "true");
                }
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                var userName = context.Principal?.Identity?.Name;
                logger.Info($"Token validado para usuario: {userName}");
                return Task.CompletedTask;
            },
            OnChallenge = context =>
            {
                logger.Warn($"Challenge de autenticaci�n: {context.Error}, {context.ErrorDescription}");
                return Task.CompletedTask;
            }
        };
    });

    // 5. Configurar polóticas de autorización
    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("AdminOnly", policy =>
            policy.RequireRole("Admin"));

        options.AddPolicy("EmployeeOrAdmin", policy =>
            policy.RequireRole("Admin", "Empleado"));

        options.AddPolicy("ConfirmedEmail", policy =>
            policy.RequireAssertion(context =>
                context.User.HasClaim(c => c.Type == "EmailConfirmed" && c.Value == "true")));

        options.AddPolicy("CanWrite", policy =>
            policy.RequireAssertion(context =>
                context.User.IsInRole("Admin") || context.User.IsInRole("Empleado")));

        options.AddPolicy("CanRead", policy =>
            policy.RequireAuthenticatedUser());

        options.AddPolicy("ITDepartment", policy =>
            policy.RequireAssertion(context =>
                context.User.HasClaim(c => c.Type == "Department" && c.Value == "IT")));
    });

    // 6. Configurar CORS
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("CorsPolicy",
            corsBuilder => corsBuilder
                .WithOrigins("https://witty-plant-05e55b21e.1.azurestaticapps.net", "https://lemon-glacier-05a80d40f.1.azurestaticapps.net")
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials()
                .WithExposedHeaders("X-Total-Count", "Authorization"));
    });

    // 7. CRÍTICO: Registrar HttpContextAccessor que necesita JwtService
    builder.Services.AddHttpContextAccessor();

    // 8. Registrar servicios personalizados
    builder.Services.AddScoped<IJwtService, JwtService>();

    // 9. Configurar controladores y API
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();

    // 10. Configurar Swagger
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "Lab05 Web API - JWT",
            Version = "v1",
            Description = "API RESTful con autenticación JWT y autorización basada en roles",
            Contact = new OpenApiContact
            {
                Name = "Equipo de Desarrollo",
                Email = "dev@lab05api.com"
            }
        });

        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.ApiKey,
            Scheme = "Bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Ingrese 'Bearer' [espacio] y luego su token JWT.\r\n\r\nEjemplo: \"Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...\""
        });

        options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                new string[] {}
            }
        });

        // Incluir comentarios XML si existen
        var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        if (File.Exists(xmlPath))
        {
            options.IncludeXmlComments(xmlPath);
        }
    });

    var app = builder.Build();

    // 11. MIDDLEWARE PIPELINE
    if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
    { 
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Lab05 Web API v1");
            c.RoutePrefix = string.Empty; // Para que Swagger sea la página por defecto
        });
    }

    // Aplicar migraciones automáticamente en desarrollo
    if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
    {
        using (var scope = app.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            try
            {
                logger.Info("Verificando estado de la base de datos...");

                // Asegurar que la base de datos existe
                if (context.Database.EnsureCreated())
                {
                    logger.Info("Base de datos creada exitosamente");
                }
                else
                {
                    logger.Info("Base de datos ya existe - verificando estructura");

                    // Solo aplicar migraciones si realmente hay pendientes Y no existen conflictos
                    try
                    {
                        var pendingMigrations = context.Database.GetPendingMigrations();
                        if (pendingMigrations.Any())
                        {
                            logger.Info($"Aplicando {pendingMigrations.Count()} migraciones pendientes...");
                            context.Database.Migrate();
                            logger.Info("Migraciones aplicadas exitosamente");
                        }
                        else
                        {
                            logger.Info("Base de datos ya está actualizada");
                        }
                    }
                    catch (Exception migrationEx)
                    {
                        logger.Warn($"Error al aplicar migraciones: {migrationEx.Message}. Continuando con la aplicación...");
                    }
                }

                // Crear roles y usuario administrador después de las migraciones
                logger.Info("Creando roles y usuario administrador si no existen...");
                await ApplicationDbContext.SeedAdminUserAsync(app.Services);
                logger.Info("Roles y usuario administrador verificados/creados exitosamente");

            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error al aplicar migraciones durante el inicio");
                throw; // Re-lanzar para que la aplicación no inicie con problemas de BD
            }
        }
    }

    // ORDEN CORRECTO DE MIDDLEWARE
    app.UseHttpsRedirection();
    app.UseCors("CorsPolicy");
    app.UseAuthentication();  // CRÍTICO: debe ir antes de UseAuthorization
    app.UseAuthorization();

    app.MapControllers();

    logger.Info("Aplicación iniciada exitosamente");
    app.Run();
}
catch (Exception ex)
{
    logger.Error(ex, "Error crítico durante el inicio de la aplicación");
    throw;
}
finally
{
    NLog.LogManager.Shutdown();
}