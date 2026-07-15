namespace YG.Modules.Identity.Features.Me;

// Query: pure read, no side effects. The /me refactor made this a promise, not a habit.
public sealed record GetCurrentUser(string Sub);
