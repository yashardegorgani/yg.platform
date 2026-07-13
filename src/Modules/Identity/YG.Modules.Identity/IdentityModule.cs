using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using YG.BuildingBlocks.Modules;
using YG.BuildingBlocks.Persistence;
using YG.Modules.Identity.Persistence;

namespace YG.Modules.Identity;

public sealed class IdentityModule : IYGModule
{
    public string Name => "Identity";

    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddModuleDbContext<IdentityDbContext>(configuration);
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints) { }
}