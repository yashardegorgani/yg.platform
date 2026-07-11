using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Wolverine;

namespace YG.BuildingBlocks.Modules;

/// <summary>
/// The contract every module fulfills. This interface is the ONLY thing
/// the Host will ultimately know about any module.
/// (It will grow: Schema in step 4, ConfigureWolverine in step 5.)
/// </summary>
public interface IYGModule
{
    string Name { get; }
    string Schema { get; }

    /// <summary>Register everything this module needs. The Host calls this blindly.</summary>
    void ConfigureServices(IServiceCollection services, IConfiguration configuration);

    /// <summary>Escape hatch for non-FastEndpoints routes. Usually empty.</summary>
    void MapEndpoints(IEndpointRouteBuilder app);

    /// <summary>Lets each module register its handlers/messaging with Wolverine.</summary>
    void ConfigureWolverine(WolverineOptions options);
}