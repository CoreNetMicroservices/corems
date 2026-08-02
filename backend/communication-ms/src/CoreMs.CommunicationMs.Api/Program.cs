using CoreMs.Common.App;
using CoreMs.Common.Messaging;
using CoreMs.CommunicationMs.Core.Configuration;
using CoreMs.CommunicationMs.Core.Services;
using CoreMs.CommunicationMs.Core.Services.Providers;
using CoreMs.CommunicationMs.Infrastructure.Data;
using CoreMs.ServiceDefaults;
using CoreMs.TemplateMs.Client;

var builder = WebApplication.CreateBuilder(args);

builder.AddCoreMsHost();
builder.AddCoreMsApp(o => o.WithSwagger("Communication Service", "Email, SMS, and notification delivery"));
builder.AddCoreMsDatabase<CommunicationMsDbContext>();
builder.AddCoreMsModules(typeof(MessagingService).Assembly, typeof(Program).Assembly);

builder.Services.AddHttpClient();
builder.AddTemplateMsClient();

builder.AddCoreMsOptionsLite<EmailProviderOptions>();
builder.AddCoreMsOptionsLite<SmsProviderOptions>();
builder.AddCoreMsOptionsLite<SlackProviderOptions>();
builder.AddCoreMsOptionsLite<QueueOptions>();

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
