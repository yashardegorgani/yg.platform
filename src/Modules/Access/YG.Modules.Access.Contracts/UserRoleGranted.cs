namespace YG.Modules.Access.Contracts;

/// <summary>An admin granted a role to a user. Access has already committed it; listeners mirror it.</summary>
public sealed record UserRoleGranted(string Sub, string Role);