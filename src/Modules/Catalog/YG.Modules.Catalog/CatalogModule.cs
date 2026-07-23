using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using YG.BuildingBlocks.Modules;
using YG.BuildingBlocks.Persistence;
using YG.Modules.Catalog.Features.Activities;
using YG.Modules.Catalog.Persistence;
using YG.Modules.Workflow.Contracts;

namespace YG.Modules.Catalog;

public sealed class CatalogModule : IYGModule
{
    public string Name => "Catalog";

    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddModuleDbContext<CatalogDbContext>(configuration);
        services.AddScoped<IWorkflowActivity, SnapshotProductActivity>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        // Catalog's endpoints are FastEndpoints classes, discovered via
        // the assembly registration in the Host - nothing to map manually.
    }
}