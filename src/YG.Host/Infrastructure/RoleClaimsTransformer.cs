using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using YG.BuildingBlocks.Auth;

namespace YG.Host.Infrastructure;

internal sealed class RoleClaimsTransformer(IEnumerable<IUserRolesProvider> providers)
    : IClaimsTransformation
{
    private const string RolesLoadedClaim = "yg:roles-loaded";

    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.HasClaim(RolesLoadedClaim, "true"))
            return principal;                     // idempotent: may be called more than once

        var sub = principal.FindFirst("sub")?.Value;
        var provider = providers.FirstOrDefault();  // no Access plugin loaded? no roles. graceful.
        if (sub is null || provider is null)
            return principal;

        var roles = await provider.GetRolesAsync(sub, CancellationToken.None);

        var identity = new ClaimsIdentity(roles.Select(r => new Claim("role", r))
                 .Append(new Claim(RolesLoadedClaim, "true")),
            authenticationType: null, nameType: "name", roleType: "role");
        principal.AddIdentity(identity);

        return principal;
    }
}