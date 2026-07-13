namespace YG.Modules.Identity.Contracts;

/// <summary>A user authenticated against this system for the first time.</summary>
public sealed record UserRegistered(string Sub, string Username);