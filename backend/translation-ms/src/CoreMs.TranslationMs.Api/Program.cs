using CoreMs.Common.App;
using CoreMs.ServiceDefaults;
using CoreMs.TranslationMs.Core.Services;
using CoreMs.TranslationMs.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

builder.AddCoreMsHost();
builder.AddCoreMsApp(o => o.WithSwagger("Translation Service", "Translation bundle management for internationalization"));
builder.AddCoreMsDatabase<TranslationMsDbContext>();
builder.AddCoreMsModules(typeof(TranslationService).Assembly, typeof(Program).Assembly);

var app = builder.Build();

if (await app.RunCoreMsDatabaseAsync<TranslationMsDbContext>(
    seed: async (db, sp) => await new SeedDataService(db,
        sp.GetRequiredService<ILoggerFactory>().CreateLogger<SeedDataService>()).SeedAsync())) return;

app.UseCoreMsApp();
app.MapCoreMsEndpoints();

app.Run();
