using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace YG.BuildingBlocks.Modules;

/// <summary>
/// The contract every module fulfills. This interface is the ONLY thing
/// the Host will ultimately know about any module.
/// </summary>
public interface IYGModule
{
    string Name { get; }

    /// <summary>Register everything this module needs. The Host calls this blindly.</summary>
    void ConfigureServices(IServiceCollection services, IConfiguration configuration);

    /// <summary>Escape hatch for non-FastEndpoints routes. Usually empty.</summary>
    void MapEndpoints(IEndpointRouteBuilder app);
}