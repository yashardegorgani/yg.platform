using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using YG.BuildingBlocks.Modules;
using YG.BuildingBlocks.Persistence;
using YG.Modules.Inventory.Features.Activities;
using YG.Modules.Inventory.Persistence;
using YG.Modules.Workflow.Contracts;

namespace YG.Modules.Inventory;

public sealed class InventoryModule : IYGModule
{
    public string Name => "Inventory";

    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddModuleDbContext<InventoryDbContext>(configuration);
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints) { }
}
