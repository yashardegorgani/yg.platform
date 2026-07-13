namespace YG.BuildingBlocks.Persistence;

public interface ISchemaOwner
{
    static abstract string SchemaName { get; }
}