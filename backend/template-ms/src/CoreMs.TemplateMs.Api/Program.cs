using System.Reflection;
using CoreMs.Common.App;
using CoreMs.ServiceDefaults;
using CoreMs.TemplateMs.Core.Services;
using CoreMs.TemplateMs.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

builder.AddCoreMsHost();
builder.AddCoreMsApp(o => o.WithSwagger("Template Management Service", "Template CRUD, rendering, and caching service"));
builder.AddCoreMsDatabase<TemplateMsDbContext>();
builder.AddCoreMsModules(typeof(TemplateService).Assembly, typeof(Program).Assembly);
builder.Services.AddCoreMsSeeder<SeedDataService>();

builder.Services.AddSwaggerGen(o =>
{
    var xmlPath = Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetExecutingAssembly().GetName().Name}.xml");
    if (File.Exists(xmlPath)) o.IncludeXmlComments(xmlPath);
});

var app = builder.Build();

if (await app.RunCoreMsDatabaseAsync<TemplateMsDbContext>()) return;

app.UseCoreMsApp();
app.MapCoreMsEndpoints();

app.Run();
