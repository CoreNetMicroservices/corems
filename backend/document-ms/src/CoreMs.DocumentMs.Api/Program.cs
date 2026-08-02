using CoreMs.Common.App;
using CoreMs.DocumentMs.Api.Services;
using CoreMs.DocumentMs.Core.Configuration;
using CoreMs.DocumentMs.Core.Services;
using CoreMs.DocumentMs.Infrastructure.Data;
using CoreMs.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddCoreMsHost();
builder.AddCoreMsApp(o => o
    .WithSwagger("Document Management Service", "File storage and document management with visibility-based access control")
    .WithEnumsAsStrings());
builder.AddCoreMsDatabase<DocumentMsDbContext>();
builder.AddCoreMsModules(typeof(DocumentService).Assembly, typeof(Program).Assembly);

builder.AddCoreMsOptions<StorageOptions>();
builder.AddCoreMsOptions<DocumentOptions>();

builder.Services.AddHostedService<BucketInitializationService>();

var app = builder.Build();

if (await app.RunCoreMsDatabaseAsync<DocumentMsDbContext>()) return;

app.UseCoreMsApp();
app.MapCoreMsEndpoints();

app.Run();
