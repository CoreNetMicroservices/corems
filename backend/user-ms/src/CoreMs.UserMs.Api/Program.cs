using System.Reflection;
using System.Threading.RateLimiting;
using CoreMs.Common.App;
using CoreMs.Common.Security;
using CoreMs.CommunicationMs.Client;
using CoreMs.ServiceDefaults;
using CoreMs.UserMs.Api.Configuration;
using CoreMs.UserMs.Api.Services;
using CoreMs.UserMs.Core.Services;
using CoreMs.UserMs.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;

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

builder.Services.AddCoreMsTokenProvider(builder.Configuration);

var jwtOptions = builder.Configuration.GetSection(CoreMsApp.SectionNameFor<JwtOptions>()).Get<JwtOptions>()!;

builder.Services.AddAuthentication(o =>
    {
        o.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        o.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer();

// Derive the validation key/algorithm from TokenProvider so signing and validation always
// agree (HS256/RS256/ES256). TokenProvider fails fast if the configured algorithm's key
// material is missing — no silent random-key fallback. Audience validation stays enabled here.
builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<TokenProvider>((o, tokenProvider) =>
    {
        o.MapInboundClaims = false;
        o.Events = JwtBearerEventsHandler.Create();

        var parameters = tokenProvider.GetValidationParameters();
        parameters.ValidateAudience = true;
        parameters.ValidAudience = jwtOptions.Audience;
        o.TokenValidationParameters = parameters;
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
builder.Services.AddCoreMsSeeder<SeedDataService>();

var app = builder.Build();

if (await app.RunCoreMsDatabaseAsync<UserMsDbContext>()) return;

app.UseCoreMsApp();
app.UseRateLimiter();
app.MapCoreMsEndpoints();

app.Run();
