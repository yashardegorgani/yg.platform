namespace YG.Modules.Access.Domain;

public sealed class RolePermission
{
    public Guid RoleId { get; set; }

    /// <summary>
    /// <module>.<resource>.<action>
    /// </summary>
    public string Permission { get; set; } = "";   // e.g. "catalog.products.create"
}