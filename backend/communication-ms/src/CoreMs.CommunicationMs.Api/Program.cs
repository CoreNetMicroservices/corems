using System.Text;
using CoreMs.Common.Data;
using CoreMs.Common.Extensions;
using CoreMs.Common.Middleware;
using CoreMs.Common.Security;
using CoreMs.CommunicationMs.Api.Configuration;
using CoreMs.CommunicationMs.Core.Services;
using CoreMs.CommunicationMs.Core.Services.Providers;
using CoreMs.CommunicationMs.Infrastructure.Data;
using MassTransit;
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

// Channel providers (registered as IChannelProvider collection)
builder.Services.AddOptions<EmailProviderOptions>()
    .Bind(builder.Configuration.GetSection(EmailProviderOptions.SectionName))
    .ValidateOnStart();

builder.Services.AddOptions<SmsProviderOptions>()
    .Bind(builder.Configuration.GetSection(SmsProviderOptions.SectionName))
    .ValidateOnStart();

builder.Services.AddOptions<SlackProviderOptions>()
    .Bind(builder.Configuration.GetSection(SlackProviderOptions.SectionName))
    .ValidateOnStart();

builder.Services.AddScoped<IChannelProvider, EmailProvider>();
builder.Services.AddScoped<IChannelProvider, SmsProvider>();
builder.Services.AddScoped<IChannelProvider, SlackProvider>();

// Queue configuration
builder.Services.AddOptions<QueueOptions>()
    .Bind(builder.Configuration.GetSection(QueueOptions.SectionName))
    .ValidateOnStart();

// MassTransit (RabbitMQ)
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<SendMessageConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        var rabbitHost = builder.Configuration["RabbitMq:Host"] ?? "localhost";
        var rabbitPort = int.Parse(builder.Configuration["RabbitMq:Port"] ?? "5672");
        var rabbitUser = builder.Configuration["RabbitMq:Username"] ?? "guest";
        var rabbitPass = builder.Configuration["RabbitMq:Password"] ?? "guest";

        cfg.Host(rabbitHost, (ushort)rabbitPort, "/", h =>
        {
            h.Username(rabbitUser);
            h.Password(rabbitPass);
        });

        cfg.ConfigureEndpoints(context);
    });
});

// FluentValidation
builder.Services.AddCoreMsValidation(typeof(Program).Assembly);

// Exception handling
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

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
