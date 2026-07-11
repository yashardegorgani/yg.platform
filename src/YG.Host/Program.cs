using FastEndpoints;
using YG.BuildingBlocks.Modules;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("modules.json", optional: true, reloadOnChange: true);

var modules = ModuleLoader.LoadModules(
    builder.Configuration,
    Path.Combine(AppContext.BaseDirectory, "plugins"),                        // deployed layout
    Path.Combine(builder.Environment.ContentRootPath, "..", "..", "plugins")); // F5 dev layout

foreach (var module in modules)
    module.ConfigureServices(builder.Services, builder.Configuration);

builder.Services.AddFastEndpoints(o =>
    o.Assemblies = modules.Select(m => m.GetType().Assembly).ToArray());

var app = builder.Build();

app.UseFastEndpoints(c => c.Endpoints.RoutePrefix = "api");

foreach (var module in modules)
    module.MapEndpoints(app);

app.MapGet("/", () => new
{
    Application = "YG Modular Monolith",
    Modules = modules.Select(m => m.Name).ToArray()
});

app.Run();
