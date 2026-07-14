using System.Security.Claims;
using YG.BuildingBlocks.Auth;

namespace YG.Host.Infrastructure;

internal sealed class HttpUserContextAccessor(IHttpContextAccessor httpContextAccessor)
    : IUserContextAccessor
{
    public IUserContext Current
    {
        get
        {
            var principal = httpContextAccessor.HttpContext?.User;
            if (principal?.Identity?.IsAuthenticated != true)
                return UserContext.Anonymous;

            return new UserContext(
                IsAuthenticated: true,
                Sub: principal.FindFirstValue("sub"),
                Username: principal.FindFirstValue("preferred_username"),
                Roles: principal.FindAll("role").Select(c => c.Value).ToHashSet());
        }
    }

    private sealed record UserContext(
        bool IsAuthenticated, string? Sub, string? Username, IReadOnlySet<string> Roles)
        : IUserContext
    {
        public static readonly UserContext Anonymous = new(false, null, null, new HashSet<string>());
        public bool HasRole(string role) => Roles.Contains(role);
    }
}