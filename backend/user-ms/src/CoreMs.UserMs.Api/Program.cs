using System.Reflection;
using System.Text;
using System.Threading.RateLimiting;
using CoreMs.Common.App;
using CoreMs.Common.Security;
using CoreMs.CommunicationMs.Client;
using CoreMs.ServiceDefaults;
using CoreMs.UserMs.Api.Configuration;
using CoreMs.UserMs.Api.Services;
using CoreMs.UserMs.Core.Configuration;
using CoreMs.UserMs.Core.Services;
using CoreMs.UserMs.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.AddCoreMsHost();
builder.AddCoreMsApp(o => o
    .WithSwagger("User Management Service", "OAuth2/OIDC Authorization Server with user management")
    .WithoutJwtAuth());
builder.AddCoreMsDatabase<UserMsDbContext>();
builder.AddCoreMsModules(typeof(UserService).Assembly, typeof(Program).Assembly);

builder.Services.AddSwaggerGen(o =>
{
    var xmlPath = Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetExecutingAssembly().GetName().Name}.xml");
    if (File.Exists(xmlPath)) o.IncludeXmlComments(xmlPath);
});

builder.AddCommunicationMsClient();

builder.Services.AddHttpClient();

builder.AddCoreMsOptions<JwtOptions>();
builder.AddCoreMsOptions<OAuth2ClientOptions>();
builder.AddCoreMsOptions<SocialAuthOptions>();
builder.AddCoreMsOptionsLite<OAuth2ProviderOptions>();
builder.AddCoreMsOptions<RabbitMqOptions>();
builder.AddCoreMsOptions<AppOptions>();
builder.AddCoreMsOptions<NotificationTemplateOptions>();

builder.Services.AddCoreMsTokenProvider(builder.Configuration);

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()!;
var signingKey = string.IsNullOrEmpty(jwtOptions.SecretKey)
    ? new SymmetricSecurityKey(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32))
    : new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecretKey));

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
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = signingKey,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
            NameClaimType = "sub",
            RoleClaimType = "role"
        };
    });

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy("login", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions { PermitLimit = 5, Window = TimeSpan.FromMinutes(1) }));

    options.AddPolicy("registration", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions { PermitLimit = 3, Window = TimeSpan.FromHours(1) }));

    options.AddPolicy("password-reset", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions { PermitLimit = 3, Window = TimeSpan.FromHours(1) }));
});

builder.Services.AddHostedService<TokenCleanupService>();

var app = builder.Build();

if (await app.RunCoreMsDatabaseAsync<UserMsDbContext>(
    seed: async (db, sp) => await new SeedDataService(db,
        sp.GetRequiredService<ILoggerFactory>().CreateLogger<SeedDataService>()).SeedAsync())) return;

app.UseCoreMsApp();
app.UseRateLimiter();
app.MapCoreMsEndpoints();

app.Run();
