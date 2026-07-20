using Microsoft.EntityFrameworkCore;

namespace YG.BuildingBlocks.Messaging;

/// <summary>
/// Transactional publishing for code that saves through a DbContext outside
/// Wolverine's pipeline (endpoints). Enroll the DbContext, publish facts, save once:
/// entities and messages commit in ONE transaction; delivery starts after commit.
/// </summary>
public interface IYGOutbox
{
    void Enroll(DbContext dbContext);

    ValueTask PublishAsync<TMessage>(TMessage message);

    /// <summary>SaveChanges + envelope flush — the atomic moment.</summary>
    Task SaveChangesAndPublishAsync(CancellationToken ct = default);
}