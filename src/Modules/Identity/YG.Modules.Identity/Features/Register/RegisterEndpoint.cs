using FastEndpoints;
using Wolverine;

namespace YG.Modules.Identity.Features.Register;

public sealed record RegisterRequest(string Username, string Email, string Password);
public sealed record RegisterResponse(string Sub, string Username, bool IsNew);

public sealed class RegisterEndpoint(IMessageBus bus) : Endpoint<RegisterRequest, RegisterResponse>
{
    public override void Configure()
    {
        Post("/identity/register");
        AllowAnonymous();   // legitimately public — it's the front door
    }

    public override async Task HandleAsync(RegisterRequest req, CancellationToken ct)
    {
        var result = await bus.InvokeAsync<RegisterAccountResult>(
            new RegisterAccount(req.Username, req.Email, req.Password), ct);
        await Send.OkAsync(new RegisterResponse(result.Sub, result.Username, result.IsNew), ct);
    }
}