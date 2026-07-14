namespace YG.Modules.Identity.Infrastructure;

public interface IUserDirectory
{
    /// <summary>Creates the user if absent; returns the sub either way.</summary>
    Task<string> EnsureUserAsync(string username, string email, string password, CancellationToken ct);
}