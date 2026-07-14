using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using YG.BuildingBlocks.Modules;
using YG.BuildingBlocks.Persistence;
using YG.Modules.Access.Persistence;

namespace YG.Modules.Access;

public sealed class AccessModule : IYGModule
{
    public string Name => "Access";

    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddModuleDbContext<AccessDbContext>(configuration);
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints) { }
}