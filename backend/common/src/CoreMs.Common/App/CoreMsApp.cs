using System.Reflection;
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
using CoreMs.Common.Observability;
using Serilog;

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

        // Serilog: structured logging with correlation ID enrichment.
        // Format (Console/Json) and minimum level are driven by the CoreMsLogging config section.
        var serviceName = options.Swagger?.Title ?? "CoreMS";
        builder.Services.AddSerilog(cfg =>
            CoreMsLogging.Configure(cfg, builder.Configuration, builder.Environment, serviceName));

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
    /// Registers CoreMS services/repositories by convention, FluentValidation validators, and
    /// <c>[Options]</c>-marked configuration classes in one call.
    ///
    /// Replaces:
    ///   builder.Services.AddCoreMsServices(coreAssembly);
    ///   builder.Services.AddCoreMsValidation(apiAssembly);
    ///   builder.AddCoreMsOptions(coreAssembly, apiAssembly);
    ///
    /// Usage:
    ///   builder.AddCoreMsModules(typeof(MyService).Assembly, typeof(Program).Assembly);
    /// </summary>
    /// <param name="builder">The host application builder.</param>
    /// <param name="coreAssembly">Assembly containing [Service], [Repository], and [Options] classes (Core layer).</param>
    /// <param name="apiAssembly">Assembly containing FluentValidation validators and [Options] classes (Api layer).</param>
    public static IHostApplicationBuilder AddCoreMsModules(
        this IHostApplicationBuilder builder,
        System.Reflection.Assembly coreAssembly,
        System.Reflection.Assembly apiAssembly)
    {
        builder.Services.AddCoreMsServices(coreAssembly);
        builder.Services.AddCoreMsValidation(apiAssembly);
        builder.AddCoreMsOptions(coreAssembly, apiAssembly);
        return builder;
    }

    /// <summary>
    /// Registers a single options class with configuration binding, data annotation validation,
    /// and startup validation. Prefer marking the class with <c>[Options]</c> for automatic
    /// registration; use this only for one-off explicit registration.
    ///
    /// Usage:
    ///   builder.AddCoreMsOptions&lt;StorageOptions&gt;();
    /// </summary>
    public static IHostApplicationBuilder AddCoreMsOptions<TOptions>(
        this IHostApplicationBuilder builder)
        where TOptions : class
    {
        BindOptionsCore<TOptions>(builder, GetSectionName<TOptions>());
        return builder;
    }

    /// <summary>
    /// Scans the given assemblies for classes marked with <c>[Options]</c> and registers each with
    /// configuration binding, data annotation validation, and startup validation. Classes without
    /// any DataAnnotation attributes simply pass validation. Section name is resolved per the
    /// <c>[Options]</c> attribute rules.
    ///
    /// Usage:
    ///   builder.AddCoreMsOptions(typeof(JwtOptions).Assembly);
    /// </summary>
    public static IHostApplicationBuilder AddCoreMsOptions(
        this IHostApplicationBuilder builder,
        params System.Reflection.Assembly[] assemblies)
    {
        foreach (var assembly in assemblies)
        {
            var optionTypes = assembly.GetTypes()
                .Where(t => t is { IsClass: true, IsAbstract: false, IsGenericType: false })
                .Select(t => (Type: t, Attr: t.GetCustomAttribute<OptionsAttribute>()))
                .Where(x => x.Attr is not null);

            foreach (var (type, attr) in optionTypes)
                BindOptions(builder, type, ResolveSectionName(type, attr!.SectionName));
        }
        return builder;
    }

    private static readonly System.Reflection.MethodInfo BindOptionsGeneric =
        typeof(CoreMsApp).GetMethod(nameof(BindOptionsCore),
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;

    private static void BindOptions(IHostApplicationBuilder builder, Type optionType, string sectionName)
        => BindOptionsGeneric.MakeGenericMethod(optionType).Invoke(null, [builder, sectionName]);

    private static void BindOptionsCore<TOptions>(IHostApplicationBuilder builder, string sectionName)
        where TOptions : class
    {
        builder.Services.AddOptions<TOptions>()
            .Bind(builder.Configuration.GetSection(sectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
    }

    /// <summary>
    /// Resolves the config section name for an options type using the same rules as
    /// <c>[Options]</c> scanning: an <c>[Options("...")]</c> attribute value, then a
    /// <c>public const string SectionName</c> field, then the class name minus a trailing
    /// "Options"/"Option" suffix. Use at startup when the section is needed before the
    /// options system is available (e.g. building a signing key or choosing an implementation).
    /// </summary>
    public static string SectionNameFor<TOptions>()
        => ResolveSectionName(typeof(TOptions),
            typeof(TOptions).GetCustomAttribute<OptionsAttribute>()?.SectionName);

    private static string GetSectionName<TOptions>() => SectionNameFor<TOptions>();

    /// <summary>
    /// Resolves an options config section name: explicit attribute value, then a
    /// <c>public const string SectionName</c> field, then the class name minus a trailing
    /// "Options"/"Option" suffix.
    /// </summary>
    private static string ResolveSectionName(Type optionsType, string? attributeSectionName)
    {
        if (!string.IsNullOrWhiteSpace(attributeSectionName))
            return attributeSectionName;

        var field = optionsType.GetField("SectionName",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        if (field?.GetValue(null) is string constSection && !string.IsNullOrWhiteSpace(constSection))
            return constSection;

        var name = optionsType.Name;
        if (name.EndsWith("Options", StringComparison.Ordinal))
            return name[..^"Options".Length];
        if (name.EndsWith("Option", StringComparison.Ordinal))
            return name[..^"Option".Length];
        return name;
    }

    /// <summary>
    /// Configures the CoreMS middleware pipeline in the correct order.
    /// Call after dev-mode setup and before app.Run().
    /// Aspire health endpoints are mapped by the subsequent app.MapCoreMsEndpoints() call.
    /// </summary>
    public static WebApplication UseCoreMsApp(this WebApplication app)
    {
        app.UseMiddleware<CorrelationIdMiddleware>();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseExceptionHandler();
        app.UseCoreMsStatusCodePages();
        app.UseCors();
        app.UseAuthentication();
        app.UseMiddleware<UserContextMiddleware>();
        app.UseAuthorization();
        app.MapControllers();

        return app;
    }

    // ---------------------------------------------------------------------------
    // Database lifecycle
    // ---------------------------------------------------------------------------

    /// <summary>
    /// Registers a database seeder for auto-discovery by <c>RunCoreMsDatabaseAsync</c>.
    /// A service can register more than one; all are run (in registration order) on seed.
    ///
    /// Usage in Program.cs:
    ///   builder.Services.AddCoreMsSeeder&lt;MySeeder&gt;();
    /// </summary>
    public static IServiceCollection AddCoreMsSeeder<TSeeder>(this IServiceCollection services)
        where TSeeder : class, ICoreMsSeeder
    {
        services.AddScoped<ICoreMsSeeder, TSeeder>();
        return services;
    }

    /// <summary>
    /// Handles database migration and optional seeding with support for CLI commands.
    /// Seeders are auto-discovered from DI — register them with <c>AddCoreMsSeeder&lt;T&gt;()</c>.
    ///
    /// Behaviour:
    ///   - Development: always migrates, then runs all registered <see cref="ICoreMsSeeder"/>s
    ///   - --migrate arg: migrates and exits (returns true → caller should return)
    ///   - --seed arg:    runs all seeders and exits (returns true → caller should return)
    ///   - --reseed arg:  clears (truncates) then re-runs all seeders; Development-only, throws otherwise
    ///   - Production without args: does nothing (migrations should be run by CI/CD)
    ///
    /// Usage:
    ///   if (await app.RunCoreMsDatabaseAsync&lt;MyDbContext&gt;()) return;
    /// </summary>
    /// <returns>True if a CLI command was handled and the process should exit.</returns>
    public static async Task<bool> RunCoreMsDatabaseAsync<TDbContext>(this WebApplication app)
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
            await RunSeedersAsync(app);
            app.Logger.LogInformation("Seed completed successfully.");
            return true;
        }

        if (app.Environment.Args().Contains("--reseed"))
        {
            if (!app.Environment.IsDevelopment())
                throw new InvalidOperationException(
                    "--reseed is destructive and only allowed in the Development environment.");

            await ClearSeedersAsync(app);
            await RunSeedersAsync(app);
            app.Logger.LogInformation("Reseed completed successfully.");
            return true;
        }

        if (app.Environment.IsDevelopment())
        {
            await MigrateAsync<TDbContext>(app);
            await RunSeedersAsync(app);
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

    private static async Task RunSeedersAsync(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var seeders = scope.ServiceProvider.GetServices<ICoreMsSeeder>();
        foreach (var seeder in seeders)
            await seeder.SeedAsync();
    }

    private static async Task ClearSeedersAsync(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        // Clear in reverse registration order so dependents are removed before dependencies.
        var seeders = scope.ServiceProvider.GetServices<ICoreMsSeeder>().Reverse();
        foreach (var seeder in seeders)
            await seeder.ClearAsync();
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
