using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using YG.BuildingBlocks.Modules;
using YG.BuildingBlocks.Persistence;
using YG.Modules.Identity.Infrastructure;
using YG.Modules.Identity.Persistence;

namespace YG.Modules.Identity;

public sealed class IdentityModule : IYGModule
{
    public string Name => "Identity";

    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddModuleDbContext<IdentityDbContext>(configuration);

        var keycloak = new KeycloakOptions(
            configuration["Keycloak:BaseUrl"] ?? throw new InvalidOperationException("Keycloak:BaseUrl missing"),
            configuration["Keycloak:Realm"] ?? "yg",
            configuration["Keycloak:AdminClientId"] ?? throw new InvalidOperationException("Keycloak:AdminClientId missing"),
            configuration["Keycloak:AdminClientSecret"] ?? throw new InvalidOperationException("Keycloak:AdminClientSecret missing")
        );

        services.AddSingleton(keycloak);                                    // the options facts
        services.AddHttpClient(KeycloakUserDirectory.HttpClientName,        // named client config (no <>!)
            client => client.BaseAddress = new Uri(keycloak.BaseUrl));
        services.AddSingleton<IUserDirectory, KeycloakUserDirectory>();     // the port → adapter mapping (KEEP)
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints) { }
}