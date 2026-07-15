using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using YG.BuildingBlocks.Auth;

namespace YG.Host.Infrastructure;

public sealed class RoleClaimsTransformer(
    IUserRolesProvider provider,
    ILogger<RoleClaimsTransformer> logger) : IClaimsTransformation
{
    private const string RolesLoadedClaim = "yg:roles-loaded";

    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        // Anonymous request -> nothing to enrich.
        if (principal.Identity is not { IsAuthenticated: true })
            return principal;

        // TransformAsync can run more than once per request -> marker claim guard.
        if (principal.HasClaim(c => c.Type == RolesLoadedClaim))
            return principal;

        var sub = principal.FindFirstValue("sub");
        if (string.IsNullOrEmpty(sub))
            return principal;

        var roles = await provider.GetRolesAsync(sub, CancellationToken.None);
        var permissions = await provider.GetPermissionsAsync(sub, CancellationToken.None);

        var claims = roles.Select(r => new Claim("role", r))
            .Concat(permissions.Select(p => new Claim("permissions", p)))
            .Append(new Claim(RolesLoadedClaim, "true"));

        var identity = new ClaimsIdentity(claims,
            authenticationType: null, nameType: "name", roleType: "role");
        principal.AddIdentity(identity);

        logger.LogInformation("Access: {Sub} -> roles [{Roles}], permissions [{Permissions}]",
            sub, string.Join(", ", roles), string.Join(", ", permissions));

        return principal;
    }
}