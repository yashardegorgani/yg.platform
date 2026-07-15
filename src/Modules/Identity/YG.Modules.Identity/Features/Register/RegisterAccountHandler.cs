using Wolverine;
using YG.Modules.Identity.Infrastructure;

namespace YG.Modules.Identity.Features.Register;

public sealed record RegisterAccount(string Username, string Email, string Password);
public sealed record RegisterAccountResult(string Sub, string Username, bool IsNew);

public static class RegisterAccountHandler
{
    public static async Task<RegisterAccountResult> Handle(
        RegisterAccount command, IUserDirectory directory, IMessageBus bus, CancellationToken ct)
    {
        var sub = await directory.EnsureUserAsync(
            command.Username, command.Email, command.Password, ct);

        var isNew = await bus.InvokeAsync<bool>(new RegisterUser(sub, command.Username), ct);

        return new RegisterAccountResult(sub, command.Username, isNew);
    }
}