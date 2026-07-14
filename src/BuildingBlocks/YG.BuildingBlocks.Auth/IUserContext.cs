namespace YG.BuildingBlocks.Auth;

public interface IUserContext
{
    bool IsAuthenticated { get; }
    string? Sub { get; }
    string? Username { get; }
    IReadOnlySet<string> Roles { get; }
    bool HasRole(string role);
}