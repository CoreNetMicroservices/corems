using CoreMs.Common.Extensions;
using CoreMs.Common.Middleware;
using CoreMs.Common.Security;
using CoreMs.UserMs.Api.Configuration;
using CoreMs.UserMs.Api.Services;
using CoreMs.UserMs.Domain.Services;
using CoreMs.UserMs.Infrastructure.Data;
using CoreMs.UserMs.Infrastructure.Repositories;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Controllers + JSON options
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database
builder.Services.AddDbContext<UserMsDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Auto-register services and repositories
builder.Services.AddCoreMsServices(
    typeof(UserService).Assembly,
    typeof(UserRepository).Assembly
);

// FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// Exception handling
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// Configuration
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.Configure<OAuth2ClientOptions>(builder.Configuration.GetSection(OAuth2ClientOptions.SectionName));
builder.Services.Configure<SocialAuthOptions>(builder.Configuration.GetSection(SocialAuthOptions.SectionName));
builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection(RabbitMqOptions.SectionName));

// Security (ICurrentUserService + HttpContextAccessor)
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

// Health checks
builder.Services.AddHealthChecks();

// Background services
builder.Services.AddHostedService<TokenCleanupService>();

var app = builder.Build();

// CLI: seed data command
if (args.Contains("--seed"))
{
    using var scope = app.Services.CreateScope();
    var seeder = new CoreMs.UserMs.Infrastructure.Data.SeedDataService(
        scope.ServiceProvider.GetRequiredService<CoreMs.UserMs.Infrastructure.Data.UserMsDbContext>(),
        scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger<CoreMs.UserMs.Infrastructure.Data.SeedDataService>());
    await seeder.SeedAsync();
    return;
}

// CLI: migrate command
if (args.Contains("--migrate"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<CoreMs.UserMs.Infrastructure.Data.UserMsDbContext>();
    await db.Database.MigrateAsync();
    Console.WriteLine("Migrations applied successfully.");
    return;
}

// Middleware pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseExceptionHandler();
app.UseMiddleware<AutoSaveChangesMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");
app.MapHealthChecks("/alive");

app.Run();

public partial class Program { }
