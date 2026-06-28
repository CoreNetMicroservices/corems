using System.Text;
using CoreMs.Common.Data;
using CoreMs.Common.Extensions;
using CoreMs.Common.Middleware;
using CoreMs.Common.Security;
using CoreMs.CommunicationMs.Api.Configuration;
using CoreMs.CommunicationMs.Core.Services;
using CoreMs.CommunicationMs.Infrastructure.Data;
using CoreMs.ServiceDefaults;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(
                builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                ?? ["http://localhost:8080"])
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// Controllers + JSON
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
builder.AddNpgsqlDbContext<CommunicationMsDbContext>("corems");
builder.Services.AddScoped<CoreMsDbContext>(sp => sp.GetRequiredService<CommunicationMsDbContext>());
builder.Services.AddScoped<DbContext>(sp => sp.GetRequiredService<CommunicationMsDbContext>());

// Auto-register services and repositories
builder.Services.AddCoreMsServices(typeof(MessagingService).Assembly);

// FluentValidation
builder.Services.AddCoreMsValidation(typeof(Program).Assembly);

// Exception handling
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// Configuration
builder.Services.AddOptions<MailOptions>()
    .Bind(builder.Configuration.GetSection(MailOptions.SectionName))
    .ValidateOnStart();

builder.Services.AddOptions<SmsOptions>()
    .Bind(builder.Configuration.GetSection(SmsOptions.SectionName))
    .ValidateOnStart();

builder.Services.AddOptions<SlackOptions>()
    .Bind(builder.Configuration.GetSection(SlackOptions.SectionName))
    .ValidateOnStart();

// Security
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

// Authentication (validates JWT tokens issued by user-ms)
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "http://localhost:5100";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "corems";
var jwtSecret = builder.Configuration["Jwt:SecretKey"] ?? "";
var signingKey = string.IsNullOrEmpty(jwtSecret)
    ? new SymmetricSecurityKey(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32))
    : new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));

builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = signingKey,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
            NameClaimType = "sub",
            RoleClaimType = "role"
        };
    });

builder.Services.AddAuthorization();

// Health checks
builder.Services.AddHealthChecks();

var app = builder.Build();

// Auto-migrate in Development
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<CommunicationMsDbContext>();
    await db.Database.MigrateAsync();
}

// Middleware pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseExceptionHandler();
app.UseCors();
app.UseMiddleware<AutoSaveChangesMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapDefaultEndpoints();

app.Run();

public partial class Program { }
