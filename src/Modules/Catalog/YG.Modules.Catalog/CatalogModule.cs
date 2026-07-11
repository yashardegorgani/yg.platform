using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Wolverine;
using YG.BuildingBlocks.Modules;
using YG.BuildingBlocks.Persistence;
using YG.Modules.Catalog.Persistence;

namespace YG.Modules.Catalog;

public sealed class CatalogModule : IYGModule
{
    public string Name => "Catalog";

    public string Schema => "catalog";

    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddModuleDbContext<CatalogDbContext>(configuration, Schema);
    }

    public void ConfigureWolverine(WolverineOptions options)
        => options.Discovery.IncludeAssembly(GetType().Assembly);

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        // Catalog's endpoints are FastEndpoints classes, discovered via
        // the assembly registration in the Host - nothing to map manually.
    }
}