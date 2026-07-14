namespace YG.Modules.Access.Domain;

public sealed class UserRole
{
    public string Sub { get; set; } = "";   // the Keycloak reference — same key Identity uses
    public Guid RoleId { get; set; }
}