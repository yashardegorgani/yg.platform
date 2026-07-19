namespace YG.Modules.Access.Contracts;

/// <summary>An admin revoked a role from a user.</summary>
public sealed record UserRoleRevoked(string Sub, string Role);