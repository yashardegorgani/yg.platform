namespace YG.Modules.Access.Domain;

public sealed class Role
{
    public static readonly Guid MemberRoleId = Guid.Parse("6c9e0f5a-2b71-4b8e-9f3d-1a2b3c4d5e6f");

    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
}