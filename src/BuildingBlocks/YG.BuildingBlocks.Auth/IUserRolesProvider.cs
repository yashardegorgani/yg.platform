namespace YG.BuildingBlocks.Auth;

public interface IUserRolesProvider
{
    Task<IReadOnlyCollection<string>> GetRolesAsync(string sub, CancellationToken ct);
}