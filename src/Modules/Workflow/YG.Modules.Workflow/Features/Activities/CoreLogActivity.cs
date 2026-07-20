using Microsoft.Extensions.Logging;
using System.Text.Json;
using YG.Modules.Workflow.Contracts;

namespace YG.Modules.Workflow.Features.Activities;

/// <summary>
/// The "hello world" capability: logs a designer-provided message.
/// Settings shape: { "message": "..." }
/// </summary>
public sealed class CoreLogActivity(ILogger<CoreLogActivity> logger) : IWorkflowActivity
{
    public string Type => "core.log";

    public Task<ActivityResult> ExecuteAsync(ActivityContext context, CancellationToken ct)
    {
        string? message = null;
        if (context.Settings is { } settings && settings.TryGetProperty("message", out var m))
            message = m.GetString();

        logger.LogInformation("[core.log] instance {InstanceId} step {StepId}: {Message}",
            context.InstanceId, context.StepId, message ?? "(no message)");

        return Task.FromResult(ActivityResult.Success(
            JsonSerializer.SerializeToElement(new { loggedAt = DateTimeOffset.UtcNow })));
    }
}