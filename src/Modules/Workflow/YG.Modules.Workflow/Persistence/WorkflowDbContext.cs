using Microsoft.EntityFrameworkCore;
using YG.BuildingBlocks.Persistence;
using YG.Modules.Workflow.Domain;
using YG.Modules.Workflow.Domain.Runtime;

namespace YG.Modules.Workflow.Persistence;

public sealed class WorkflowDbContext(DbContextOptions<WorkflowDbContext> options)
    : ModuleDbContext(options), ISchemaOwner
{
    public static string SchemaName => "workflow";
    public override string Schema => SchemaName;

    public DbSet<WorkflowDefinition> Definitions => Set<WorkflowDefinition>();
    public DbSet<WorkflowInstance> Instances => Set<WorkflowInstance>();
    public DbSet<WorkflowStepInstance> StepInstances => Set<WorkflowStepInstance>();
    public DbSet<WorkflowTask> Tasks => Set<WorkflowTask>();
    public DbSet<WorkflowHistoryEntry> History => Set<WorkflowHistoryEntry>();


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
                .HasComment("The definition graph: {startStepId, steps: [{id, kind: Human|Automatic, role?, formRef?, branching: Exclusive|Parallel, join: None|All, activity?: {ref, input}, onFailure?: {maxRetries, onExhausted: Pend|Continue}}], transitions: [{from, to, condition?: {field, op, value}}]}");

            b.Property(d => d.CreatedBy).HasMaxLength(100);
            b.Property(d => d.PublishedBy).HasMaxLength(100);
            b.Property(d => d.ReviewNote).HasMaxLength(2000);

            // The versioning invariant, enforced by the database, not by hope.
            b.HasIndex(d => new { d.Key, d.Version }).IsUnique();
        });

        modelBuilder.Entity<WorkflowInstance>(b =>
        {
            b.ToTable("workflow_instances", t => t.HasComment(
                "Executions. Definition version is pinned at start and never changes."));
            b.HasKey(i => i.Id);
            b.Property(i => i.DefinitionKey).HasMaxLength(100);
            b.Property(i => i.Status).HasConversion<string>().HasMaxLength(30)
                .HasComment("Running | Completed");
            b.Property(i => i.BusinessKey).HasMaxLength(200)
                .HasComment("Opaque reference to a business entity in some module. Never a FK.");
            b.Property(i => i.Context).HasColumnType("jsonb")
                .HasComment("Accumulated state keyed by step id: {stepId: {field: value}}. Field shapes vary per definition. Steps write it; transitions only read it.");
            b.Property(i => i.StartedBy).HasMaxLength(100);
            b.HasIndex(i => i.Status);
            b.HasIndex(i => i.BusinessKey);
            b.Property(i => i.ParentInstanceId)
                .HasComment("Spawning instance, when this is a child. Null = top-level.");
            b.Property(i => i.ParentStepInstanceId)
                .HasComment("The parent's SubWorkflow step instance awaiting this child's completion.");
            b.HasIndex(i => i.ParentInstanceId);
        });

        modelBuilder.Entity<WorkflowStepInstance>(b =>
        {
            b.ToTable("workflow_step_instances", t => t.HasComment(
                "One row per activated step. captured_data is frozen once the step completes."));
            b.HasKey(s => s.Id);
            b.Property(s => s.StepId).HasMaxLength(100)
                .HasComment("Step id inside the pinned definition document.");
            b.Property(s => s.Status).HasConversion<string>().HasMaxLength(30);
            b.Property(s => s.CapturedData).HasColumnType("jsonb")
                .HasComment("Form data captured at step completion: {field: value}, shape defined by the step's formRef. Frozen once written.");
            b.Property(s => s.CompletedBy).HasMaxLength(100);
            b.HasOne(s => s.Instance).WithMany().HasForeignKey(s => s.InstanceId);
            b.HasIndex(s => s.InstanceId);
        });

        modelBuilder.Entity<WorkflowTask>(b =>
        {
            b.ToTable("workflow_tasks", t => t.HasComment(
                "The inbox. Open tasks are visible to all role holders; claiming takes ownership."));
            b.HasKey(x => x.Id);
            b.Property(x => x.DefinitionKey).HasMaxLength(100);
            b.Property(x => x.StepId).HasMaxLength(100);
            b.Property(x => x.Role).HasMaxLength(100)
                .HasComment("Opaque role name; holders see the task in their inbox.");
            b.Property(x => x.FormRef).HasMaxLength(200);
            b.Property(x => x.Status).HasConversion<string>().HasMaxLength(30)
                .HasComment("Open | Claimed | Completed");
            b.Property(x => x.ClaimedBy).HasMaxLength(100);
            b.HasOne(x => x.Instance).WithMany().HasForeignKey(x => x.InstanceId);
            b.HasOne(x => x.StepInstance).WithMany().HasForeignKey(x => x.StepInstanceId);
            b.HasIndex(x => new { x.Status, x.Role });   // the inbox query
            b.HasIndex(x => x.ClaimedBy);
        });

        modelBuilder.Entity<WorkflowHistoryEntry>(b =>
        {
            b.ToTable("workflow_history", t => t.HasComment(
                "Append-only audit trail. Never updated, never deleted."));
            b.HasKey(h => h.Id);
            b.Property(h => h.StepId).HasMaxLength(100);
            b.Property(h => h.Action).HasMaxLength(50)
                .HasComment("instance-started | task-created | task-claimed | task-completed | task-reassigned | note-added | activity-scheduled | activity-failed | activity-pended | activity-continued | activity-resumed | branch-completed | join-waiting | instance-completed");
            b.Property(h => h.Actor).HasMaxLength(100);
            b.Property(h => h.Data).HasColumnType("jsonb")
                .HasComment("Action payload, shape per action: task-reassigned {toSub?, toRole?} | task-completed {data} | activity-failed/pended {error, attempts} | note-added {text}.");
            b.Property(h => h.ActorSnapshot).HasColumnType("jsonb")
                .HasComment("Actor identity frozen at write time: {sub, username, roles: [string]}. Null = the engine acted.");
            b.HasOne<WorkflowInstance>().WithMany().HasForeignKey(h => h.InstanceId);   // constraint, no nav — it's a log
            b.HasIndex(h => h.InstanceId);
        });
    }
}