using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using YG.BuildingBlocks.Modules;
using YG.BuildingBlocks.Persistence;
using YG.Modules.Purchase.Features.Activities;
using YG.Modules.Purchase.Persistence;
using YG.Modules.Workflow.Contracts;

namespace YG.Modules.Purchase;

public sealed class PurchaseModule : IYGModule
{
    public string Name => "Purchase";

    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddModuleDbContext<PurchaseDbContext>(configuration);
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints) { }
}
