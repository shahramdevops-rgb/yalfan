using Yalfan.Domain.Common;

namespace Yalfan.Domain.Audit;

/// <summary>
/// One recorded change: who changed which row, when, and the old and new values. Entries are
/// written in the same save as the change and are never edited or deleted (ADR 0013).
/// </summary>
public sealed class AuditEntry : Entity
{
    private AuditEntry()
    {
    }

    private AuditEntry(
        DateTimeOffset occurredAt,
        Guid? userId,
        string? traceId,
        string entityType,
        string entityId,
        AuditAction action,
        string changes)
    {
        OccurredAt = occurredAt;
        UserId = userId;
        Actor = userId is null ? AuditActor.System : AuditActor.User;
        TraceId = traceId;
        EntityType = entityType;
        EntityId = entityId;
        Action = action;
        Changes = changes;
    }

    public DateTimeOffset OccurredAt { get; private set; }

    /// <summary>The logged-in user who made the change; null when the system made it.</summary>
    public Guid? UserId { get; private set; }

    public AuditActor Actor { get; private set; }

    /// <summary>The request's trace id, the same one the log carries, so the full request can be found.</summary>
    public string? TraceId { get; private set; }

    /// <summary>The table of the changed row, such as <c>users</c>.</summary>
    public string EntityType { get; private set; } = null!;

    public string EntityId { get; private set; } = null!;

    public AuditAction Action { get; private set; }

    /// <summary>JSON: <c>{"field": {"old": …, "new": …}}</c>, only the fields that changed.</summary>
    public string Changes { get; private set; } = null!;

    public static AuditEntry Record(
        DateTimeOffset occurredAt,
        Guid? userId,
        string? traceId,
        string entityType,
        string entityId,
        AuditAction action,
        string changes)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityType);
        ArgumentException.ThrowIfNullOrWhiteSpace(entityId);
        ArgumentException.ThrowIfNullOrWhiteSpace(changes);

        return new AuditEntry(occurredAt, userId, traceId, entityType, entityId, action, changes);
    }
}

public enum AuditAction
{
    Created,
    Updated,
    Deleted,
}

public enum AuditActor
{
    /// <summary>A logged-in user, named in <see cref="AuditEntry.UserId"/>.</summary>
    User,

    /// <summary>The system itself: start-up seeding, background jobs.</summary>
    System,
}
