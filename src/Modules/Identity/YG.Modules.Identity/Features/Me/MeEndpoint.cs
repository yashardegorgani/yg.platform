using FastEndpoints;
using Wolverine;
using YG.Modules.Identity.Domain;

namespace YG.Modules.Identity.Features.Me;

public sealed record MeResponse(string Sub, string Username, bool IsNew);

public sealed class MeEndpoint(IMessageBus bus) : EndpointWithoutRequest<MeResponse>
{
    public override void Configure()
    {
        Get("/identity/me");
        // no AllowAnonymous — a token is the whole point of this endpoint
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var sub = User.FindFirst("sub")?.Value;
        if (sub is null) { await Send.UnauthorizedAsync(ct); return; }
        var username = User.FindFirst("preferred_username")?.Value ?? "";

        var isNew = await bus.InvokeAsync<bool>(new RegisterUser(sub, username), ct);
        await Send.OkAsync(new MeResponse(sub, username, isNew));
    }
}