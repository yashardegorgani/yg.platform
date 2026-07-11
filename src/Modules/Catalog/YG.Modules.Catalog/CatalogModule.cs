using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using YG.BuildingBlocks.Modules;
using YG.Modules.Catalog.Services;

namespace YG.Modules.Catalog;

public sealed class CatalogModule : IYGModule
{
    public string Name => "Catalog";

    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // The module owns its registrations. The Host never learns these types exist.
        services.AddSingleton<IProductCatalog, InMemoryProductCatalog>();
    }

    public void MapEndpoints(IEndpointRouteBuilder app)
    {
    }
}