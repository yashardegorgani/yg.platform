namespace YG.Modules.Identity.Domain;

public sealed class User
{
    public string Sub { get; set; } = "";          // Keycloak's stable ID — our only link to the IdP
    public string Username { get; set; } = "";     // convenience copy, NOT the identity
    public DateTimeOffset FirstSeenAt { get; set; }
}