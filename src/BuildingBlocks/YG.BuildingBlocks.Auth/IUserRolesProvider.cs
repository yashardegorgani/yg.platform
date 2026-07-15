namespace YG.BuildingBlocks.Auth;

public interface IUserRolesProvider
{
    Task<IReadOnlyCollection<string>> GetRolesAsync(string sub, CancellationToken ct);
    Task<IReadOnlyCollection<string>> GetPermissionsAsync(string sub, CancellationToken ct);
}