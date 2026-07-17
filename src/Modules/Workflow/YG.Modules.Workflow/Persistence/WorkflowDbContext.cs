using Microsoft.EntityFrameworkCore;
using YG.BuildingBlocks.Persistence;
using YG.Modules.Workflow.Domain;

namespace YG.Modules.Workflow.Persistence;

public sealed class WorkflowDbContext(DbContextOptions<WorkflowDbContext> options)
    : ModuleDbContext(options), ISchemaOwner
{
    public static string SchemaName => "workflow";
    public override string Schema => SchemaName;

    public DbSet<WorkflowDefinition> Definitions => Set<WorkflowDefinition>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);   // applies HasDefaultSchema("workflow")

        modelBuilder.Entity<WorkflowDefinition>(b =>
        {
            b.ToTable("workflow_definitions", t => t.HasComment(
                "Designed workflow definitions. One row per version; Published rows are immutable."));
            b.HasKey(d => d.Id);

            b.Property(d => d.Key).HasMaxLength(100)
                .HasComment("Stable workflow identity across versions (kebab-case).");
            b.Property(d => d.Name).HasMaxLength(200);
            b.Property(d => d.Status).HasConversion<string>().HasMaxLength(30)
                .HasComment("Draft | PendingApproval | Published");

            // Typed DefinitionDocument POCO -> one jsonb column.
            // Works because AddModuleDbContext enables dynamic JSON (same as Catalog's ProductAttributes).
            b.Property(d => d.Document).HasColumnType("jsonb")
                .HasComment("The definition graph: steps, transitions, activities, form references.");

            b.Property(d => d.CreatedBy).HasMaxLength(100);
            b.Property(d => d.PublishedBy).HasMaxLength(100);
            b.Property(d => d.ReviewNote).HasMaxLength(2000);

            // The versioning invariant, enforced by the database, not by hope.
            b.HasIndex(d => new { d.Key, d.Version }).IsUnique();
        });
    }
}