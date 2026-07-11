using FastEndpoints;

namespace YG.Host.Features.Hello;

public sealed record HelloResponse(string Message, DateTimeOffset ServerTime);

public sealed class HelloEndpoint : EndpointWithoutRequest<HelloResponse>
{
    public override void Configure()
    {
        Get("/hello");
        AllowAnonymous(); // FastEndpoints is secure-by-default: without this line, 401
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        await Send.OkAsync(
            new HelloResponse("YG walking skeleton is alive", DateTimeOffset.UtcNow), ct);
    }
}