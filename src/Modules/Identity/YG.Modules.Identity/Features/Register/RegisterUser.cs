namespace YG.Modules.Identity.Features.Register;

// Command: "make sure this sub exists in our users table." Returns true on first sighting.
public sealed record RegisterUser(string Sub, string Username);
