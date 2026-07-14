namespace YG.BuildingBlocks.Auth;

public interface IUserContext
{
    bool IsAuthenticated { get; }
    string? Sub { get; }
    string? Username { get; }
}