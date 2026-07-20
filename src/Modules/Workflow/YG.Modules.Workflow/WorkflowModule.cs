using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using YG.BuildingBlocks.Modules;
using YG.BuildingBlocks.Persistence;
using YG.Modules.Workflow.Contracts;
using YG.Modules.Workflow.Engine;
using YG.Modules.Workflow.Features.Activities;
using YG.Modules.Workflow.Persistence;

namespace YG.Modules.Workflow;

public sealed class WorkflowModule : IYGModule
{
    public string Name => "Workflow";

    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddModuleDbContext<WorkflowDbContext>(configuration);
        services.AddScoped<ActivityRegistry>();
        services.AddScoped<IWorkflowActivity, CoreLogActivity>();
    }

    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        
    }
}