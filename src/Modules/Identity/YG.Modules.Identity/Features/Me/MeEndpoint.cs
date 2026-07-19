using FastEndpoints;
using Wolverine;
using YG.BuildingBlocks.Auth;
using YG.BuildingBlocks.Messaging;
using YG.Modules.Identity.Domain;

namespace YG.Modules.Identity.Features.Me;

public sealed record MeResponse(string Sub, string Username, DateTimeOffset FirstSeenAt);

public sealed class MeEndpoint(IYGMessageBus bus, IUserContext user)
    : EndpointWithoutRequest<MeResponse>
{
    public override void Configure()
    {
        Get("/identity/me");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        if (user.Sub is null) { await Send.UnauthorizedAsync(ct); return; }

        var found = await bus.InvokeAsync<User?>(new GetCurrentUser(user.Sub), ct);
        if (found is null) 
        { 
            await Send.NotFoundAsync(ct);
            return; 
        }

        await Send.OkAsync(new MeResponse(found.Sub, found.Username, found.FirstSeenAt), ct);
    }
}