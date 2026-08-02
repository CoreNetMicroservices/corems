using System.Text;
using CoreMs.Common.Data;
using CoreMs.Common.Extensions;
using CoreMs.Common.Middleware;
using CoreMs.Common.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

namespace CoreMs.Common.App;

// ---------------------------------------------------------------------------
// Options
// ---------------------------------------------------------------------------

public sealed class CoreMsSwaggerOptions
{
    public string Title { get; set; } = "CoreMS Service";
    public string Version { get; set; } = "v1";
    public string Description { get; set; } = string.Empty;
}

public sealed class CoreMsAppOptions
{
    public CoreMsSwaggerOptions? Swagger { get; private set; } = new();
    public bool UseJwtAuth { get; private set; } = true;
    public bool SerializeEnumsAsStrings { get; private set; } = false;
    public string[]? CorsOrigins { get; private set; }

    public CoreMsAppOptions WithSwagger(string title, string description = "")
    {
        Swagger ??= new();
        Swagger.Title = title;
        Swagger.Description = description;
        return this;
    }

    public CoreMsAppOptions WithoutSwagger()
    {
        Swagger = null;
        return this;
    }

    public CoreMsAppOptions WithEnumsAsStrings()
    {
        SerializeEnumsAsStrings = true;
        return this;
    }

    public CoreMsAppOptions WithoutJwtAuth()
    {
        UseJwtAuth = false;
        return this;
    }

    public CoreMsAppOptions WithCorsOrigins(params string[] origins)
    {
        CorsOrigins = origins;
        return this;
    }
}

// ---------------------------------------------------------------------------
// Builder extensions
// ---------------------------------------------------------------------------

public static class CoreMsApp
{
    /// <summary>
    /// Registers all CoreMS service defaults: CORS, controllers, Swagger, exception handling,
    /// security, and JWT Bearer consumer auth.
    ///
    /// Call after builder.AddCoreMsHost() and before service-specific registrations.
    /// </summary>
    public static IHostApplicationBuilder AddCoreMsApp(
        this IHostApplicationBuilder builder,
        Action<CoreMsAppOptions>? configure = null)
    {
        var options = new CoreMsAppOptions();
        configure?.Invoke(options);

        builder.Services.AddCors(o =>
        {
            o.AddDefaultPolicy(policy =>
            {
                var origins = options.CorsOrigins
                    ?? builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                    ?? ["http://localhost:8080"];

                policy.WithOrigins(origins)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });

        builder.Services.AddControllers()
            .AddJsonOptions(o =>
            {
                o.JsonSerializerOptions.DefaultIgnoreCondition =
                    System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
                o.JsonSerializerOptions.PropertyNamingPolicy =
                    System.Text.Json.JsonNamingPolicy.CamelCase;

                if (options.SerializeEnumsAsStrings)
                    o.JsonSerializerOptions.Converters.Add(
                        new System.Text.Json.Serialization.JsonStringEnumConverter());
            });

        if (options.Swagger is not null)
        {
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(o =>
            {
                o.SwaggerDoc(options.Swagger.Version, new OpenApiInfo
                {
                    Title = options.Swagger.Title,
                    Version = options.Swagger.Version,
                    Description = options.Swagger.Description
                });

                o.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    Description = "Enter your JWT token"
                });

                o.AddSecurityRequirement(new OpenApiSecurityRequirement
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
                        Array.Empty<string>()
                    }
                });
            });
        }

        builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
        builder.Services.AddProblemDetails();

        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
        builder.Services.AddCoreMsAuthorization();

        if (options.UseJwtAuth)
        {
            var jwtSection = builder.Configuration.GetSection("Jwt");
            var issuer = jwtSection["Issuer"] ?? "http://localhost:5100";
            var audience = jwtSection["Audience"] ?? "corems";
            var secretKey = jwtSection["SecretKey"] ?? "";

            var signingKey = string.IsNullOrEmpty(secretKey)
                ? new SymmetricSecurityKey(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32))
                : new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));

            builder.Services.AddAuthentication(o =>
                {
                    o.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    o.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(o =>
                {
                    o.MapInboundClaims = false;
                    o.Events = JwtBearerEventsHandler.Create();
                    o.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = issuer,
                        ValidateAudience = true,
                        ValidAudience = audience,
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = signingKey,
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.Zero,
                        NameClaimType = "sub",
                        RoleClaimType = "role"
                    };
                });
        }

        return builder;
    }

    /// <summary>
    /// Registers the service's DbContext with Aspire Npgsql integration and wires up
    /// the CoreMsDbContext and DbContext DI aliases.
    ///
    /// Replaces the 3-line pattern:
    ///   builder.AddNpgsqlDbContext&lt;TDbContext&gt;("corems");
    ///   builder.Services.AddScoped&lt;CoreMsDbContext&gt;(sp => sp.GetRequiredService&lt;TDbContext&gt;());
    ///   builder.Services.AddScoped&lt;DbContext&gt;(sp => sp.GetRequiredService&lt;TDbContext&gt;());
    /// </summary>
    public static IHostApplicationBuilder AddCoreMsDatabase<TDbContext>(
        this IHostApplicationBuilder builder,
        string connectionName = "corems")
        where TDbContext : CoreMsDbContext
    {
        builder.AddNpgsqlDbContext<TDbContext>(connectionName);
        builder.Services.AddScoped<CoreMsDbContext>(sp => sp.GetRequiredService<TDbContext>());
        builder.Services.AddScoped<DbContext>(sp => sp.GetRequiredService<TDbContext>());
        return builder;
    }

    /// <summary>
    /// Registers CoreMS services/repositories by convention and FluentValidation validators in one call.
    ///
    /// Replaces:
    ///   builder.Services.AddCoreMsServices(coreAssembly);
    ///   builder.Services.AddCoreMsValidation(apiAssembly);
    ///
    /// Usage:
    ///   builder.AddCoreMsModules(typeof(MyService).Assembly, typeof(Program).Assembly);
    /// </summary>
    /// <param name="builder">The host application builder.</param>
    /// <param name="coreAssembly">Assembly containing [Service] and [Repository] classes (Core layer).</param>
    /// <param name="apiAssembly">Assembly containing FluentValidation validators (Api layer).</param>
    public static IHostApplicationBuilder AddCoreMsModules(
        this IHostApplicationBuilder builder,
        System.Reflection.Assembly coreAssembly,
        System.Reflection.Assembly apiAssembly)
    {
        builder.Services.AddCoreMsServices(coreAssembly);
        builder.Services.AddCoreMsValidation(apiAssembly);
        return builder;
    }

    /// <summary>
    /// Registers an options class with configuration binding, data annotation validation, and startup validation.
    /// The options class must have a <c>public const string SectionName</c> field.
    ///
    /// Replaces:
    ///   builder.Services.AddOptions&lt;T&gt;().Bind(...).ValidateDataAnnotations().ValidateOnStart();
    ///
    /// Usage:
    ///   builder.AddCoreMsOptions&lt;StorageOptions&gt;();
    /// </summary>
    public static IHostApplicationBuilder AddCoreMsOptions<TOptions>(
        this IHostApplicationBuilder builder)
        where TOptions : class
    {
        var sectionName = GetSectionName<TOptions>();
        builder.Services.AddOptions<TOptions>()
            .Bind(builder.Configuration.GetSection(sectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        return builder;
    }

    /// <summary>
    /// Registers an options class without DataAnnotation validation (only startup binding check).
    /// </summary>
    public static IHostApplicationBuilder AddCoreMsOptionsLite<TOptions>(
        this IHostApplicationBuilder builder)
        where TOptions : class
    {
        var sectionName = GetSectionName<TOptions>();
        builder.Services.AddOptions<TOptions>()
            .Bind(builder.Configuration.GetSection(sectionName))
            .ValidateOnStart();
        return builder;
    }

    private static string GetSectionName<TOptions>()
    {
        var field = typeof(TOptions).GetField("SectionName",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        return field?.GetValue(null) as string
            ?? throw new InvalidOperationException(
                $"{typeof(TOptions).Name} must declare 'public const string SectionName'.");
    }

    /// <summary>
    /// Configures the CoreMS middleware pipeline in the correct order.
    /// Call after dev-mode setup and before app.Run().
    /// Aspire health endpoints are mapped by the subsequent app.MapCoreMsEndpoints() call.
    /// </summary>
    public static WebApplication UseCoreMsApp(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseExceptionHandler();
        app.UseCoreMsStatusCodePages();
        app.UseCors();
        app.UseMiddleware<AutoSaveChangesMiddleware>();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();

        return app;
    }

    // ---------------------------------------------------------------------------
    // Database lifecycle
    // ---------------------------------------------------------------------------

    /// <summary>
    /// Handles database migration and optional seeding with support for CLI commands.
    ///
    /// Behaviour:
    ///   - Development: always migrates, runs seeder if provided
    ///   - --migrate arg: migrates and exits (returns true → caller should return)
    ///   - --seed arg:    runs seeder and exits (returns true → caller should return)
    ///   - Production without args: does nothing (migrations should be run by CI/CD)
    ///
    /// Usage (no seeding):
    ///   if (await app.RunCoreMsDatabaseAsync&lt;MyDbContext&gt;()) return;
    ///
    /// Usage (with seeding):
    ///   if (await app.RunCoreMsDatabaseAsync&lt;MyDbContext&gt;(
    ///       seed: async (db, sp) => await new SeedDataService(db, sp.GetRequiredService&lt;ILoggerFactory&gt;()
    ///           .CreateLogger&lt;SeedDataService&gt;()).SeedAsync())) return;
    /// </summary>
    /// <returns>True if a CLI command was handled and the process should exit.</returns>
    public static async Task<bool> RunCoreMsDatabaseAsync<TDbContext>(
        this WebApplication app,
        Func<TDbContext, IServiceProvider, Task>? seed = null)
        where TDbContext : CoreMsDbContext
    {
        if (app.Environment.Args().Contains("--migrate"))
        {
            await MigrateAsync<TDbContext>(app);
            app.Logger.LogInformation("Migrations applied successfully.");
            return true;
        }

        if (app.Environment.Args().Contains("--seed"))
        {
            if (seed is null)
                throw new InvalidOperationException("--seed was passed but no seed delegate was provided.");

            await RunSeedAsync<TDbContext>(app, seed);
            app.Logger.LogInformation("Seed completed successfully.");
            return true;
        }

        if (app.Environment.IsDevelopment())
        {
            await MigrateAsync<TDbContext>(app);

            if (seed is not null)
                await RunSeedAsync<TDbContext>(app, seed);
        }

        return false;
    }

    private static async Task MigrateAsync<TDbContext>(WebApplication app)
        where TDbContext : DbContext
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TDbContext>();
        await db.Database.MigrateAsync();
    }

    private static async Task RunSeedAsync<TDbContext>(
        WebApplication app,
        Func<TDbContext, IServiceProvider, Task> seed)
        where TDbContext : DbContext
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TDbContext>();
        await seed(db, scope.ServiceProvider);
    }
}

// ---------------------------------------------------------------------------
// Internal helper — reads CLI args from the WebApplication
// ---------------------------------------------------------------------------
file static class WebApplicationExtensions
{
    internal static IReadOnlyList<string> Args(this IHostEnvironment _)
        => Environment.GetCommandLineArgs();
}
