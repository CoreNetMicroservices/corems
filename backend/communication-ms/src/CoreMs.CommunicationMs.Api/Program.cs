using CoreMs.Common.App;
using CoreMs.Common.Messaging;
using CoreMs.CommunicationMs.Core.Configuration;
using CoreMs.CommunicationMs.Core.Services;
using CoreMs.CommunicationMs.Core.Services.Providers;
using CoreMs.CommunicationMs.Infrastructure.Data;
using CoreMs.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddCoreMsHost();
builder.AddCoreMsApp(o => o.WithSwagger("Communication Service", "Email, SMS, and notification delivery"));
builder.AddCoreMsDatabase<CommunicationMsDbContext>();
builder.AddCoreMsModules(typeof(MessagingService).Assembly, typeof(Program).Assembly);

builder.Services.AddHttpClient();

builder.Services.AddOptions<EmailProviderOptions>()
    .Bind(builder.Configuration.GetSection(EmailProviderOptions.SectionName))
    .ValidateOnStart();

builder.Services.AddOptions<SmsProviderOptions>()
    .Bind(builder.Configuration.GetSection(SmsProviderOptions.SectionName))
    .ValidateOnStart();

builder.Services.AddOptions<SlackProviderOptions>()
    .Bind(builder.Configuration.GetSection(SlackProviderOptions.SectionName))
    .ValidateOnStart();

builder.Services.AddOptions<QueueOptions>()
    .Bind(builder.Configuration.GetSection(QueueOptions.SectionName))
    .ValidateOnStart();

builder.Services.AddScoped<IChannelProvider, EmailProvider>();
builder.Services.AddScoped<IChannelProvider, SmsProvider>();
builder.Services.AddScoped<IChannelProvider, SlackProvider>();

builder.Services.AddCoreMsMessaging(builder.Configuration, cfg =>
{
    cfg.AddConsumer<SendMessageConsumer>();
});

var app = builder.Build();

if (await app.RunCoreMsDatabaseAsync<CommunicationMsDbContext>()) return;

app.UseCoreMsApp();
app.MapCoreMsEndpoints();

app.Run();
