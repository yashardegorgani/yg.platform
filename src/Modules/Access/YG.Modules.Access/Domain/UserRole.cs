namespace YG.Modules.Access.Domain;

public sealed class UserRole
{
    public string Sub { get; set; } = "";   // the Keycloak reference — same key Identity uses
    public Guid RoleId { get; set; }

    // Audit trail. Null in both = granted by the system (registration auto-member,
    // or rows that predate auditing). Human grants always fill both.
    public string? GrantedBy { get; set; }          // sub of the admin who granted it
    public DateTimeOffset? GrantedAt { get; set; }
}