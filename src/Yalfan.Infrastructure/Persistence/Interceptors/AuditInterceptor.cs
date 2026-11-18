using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Yalfan.Application.Common;
using Yalfan.Application.Common.Security;
using Yalfan.Domain.Audit;
using Yalfan.Infrastructure.Persistence.Auditing;

namespace Yalfan.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Writes an <see cref="AuditEntry"/> for every row added, changed or deleted, inside the same
/// save, so a change and its audit entry are stored together or not at all (ADR 0013).
/// </summary>
/// <remarks>
/// One instance per DbContext (scoped). Changes that skip the change tracker (ExecuteUpdate,
/// ExecuteDelete, raw SQL) are not seen, which is why the build forbids them.
/// </remarks>
public sealed class AuditInterceptor(ICurrentUser currentUser, IClock clock) : SaveChangesInterceptor
{
    private readonly List<AuditEntry> _pending = [];

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Capture(eventData.Context);
        return result;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Capture(eventData.Context);
        return ValueTask.FromResult(result);
    }

    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        _pending.Clear();
        return result;
    }

    public override ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        _pending.Clear();
        return ValueTask.FromResult(result);
    }

    public override void SaveChangesFailed(DbContextErrorEventData eventData) => Discard(eventData.Context);

    public override Task SaveChangesFailedAsync(DbContextErrorEventData eventData, CancellationToken cancellationToken = default)
    {
        Discard(eventData.Context);
        return Task.CompletedTask;
    }

    private void Capture(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        context.ChangeTracker.DetectChanges();

        var now = clock.UtcNow;
        var userId = currentUser.UserId;
        var traceId = Activity.Current?.TraceId.ToHexString();

        var changed = context.ChangeTracker.Entries()
            .Where(entry => entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToList();

        foreach (var entry in changed)
        {
            if (entry.Entity is AuditEntry)
            {
                if (entry.State != EntityState.Added)
                {
                    throw new InvalidOperationException("Audit entries are append-only: they are never changed or deleted.");
                }

                continue;
            }

            if (entry.Metadata.IsExcludedFromAudit())
            {
                continue;
            }

            var action = entry.State switch
            {
                EntityState.Added => AuditAction.Created,
                EntityState.Deleted => AuditAction.Deleted,
                _ => AuditAction.Updated,
            };

            var changes = DescribeChanges(entry, action);
            if (changes.Count == 0)
            {
                // Saved, but nothing really changed (a value set to what it already was).
                continue;
            }

            _pending.Add(AuditEntry.Record(
                now,
                userId,
                traceId,
                entry.Metadata.GetTableName() ?? entry.Metadata.ClrType.Name,
                KeyOf(entry),
                action,
                changes.ToJsonString()));
        }

        context.AddRange(_pending);
    }

    /// <summary>A failed save must not leave audit entries behind to be saved by a later attempt.</summary>
    private void Discard(DbContext? context)
    {
        if (context is not null)
        {
            foreach (var pending in _pending)
            {
                context.Entry(pending).State = EntityState.Detached;
            }
        }

        _pending.Clear();
    }

    private static JsonObject DescribeChanges(EntityEntry entry, AuditAction action)
    {
        var changes = new JsonObject();

        foreach (var property in entry.Properties)
        {
            var metadata = property.Metadata;

            // The key is already the entry's EntityId; the row version (xmin) means nothing to a reader.
            if (metadata.IsPrimaryKey() || metadata.IsConcurrencyToken)
            {
                continue;
            }

            if (action == AuditAction.Updated
                && (!property.IsModified || Equals(property.OriginalValue, property.CurrentValue)))
            {
                continue;
            }

            var field = new JsonObject();
            var redacted = metadata.IsAuditRedacted();

            if (action != AuditAction.Created)
            {
                field["old"] = redacted ? null : ToJson(property, property.OriginalValue);
            }

            if (action != AuditAction.Deleted)
            {
                field["new"] = redacted ? null : ToJson(property, property.CurrentValue);
            }

            if (redacted)
            {
                field["redacted"] = true;
            }

            // camelCase, the names the API uses, so a history screen can reuse its field labels.
            changes[JsonNamingPolicy.CamelCase.ConvertName(metadata.Name)] = field;
        }

        return changes;
    }

    /// <summary>The value as it is stored ("09121234567", "Owner"), not the C# object.</summary>
    private static JsonNode? ToJson(PropertyEntry property, object? value)
    {
        if (value is null)
        {
            return null;
        }

        var converter = property.Metadata.GetValueConverter() ?? property.Metadata.GetTypeMapping().Converter;
        var stored = converter is null ? value : converter.ConvertToProvider(value);

        return JsonSerializer.SerializeToNode(stored);
    }

    private static string KeyOf(EntityEntry entry)
    {
        var key = entry.Metadata.FindPrimaryKey()
            ?? throw new InvalidOperationException($"{entry.Metadata.DisplayName()} has no key and cannot be audited.");

        return string.Join(
            ",",
            key.Properties.Select(property => Convert.ToString(entry.Property(property.Name).CurrentValue, CultureInfo.InvariantCulture)));
    }
}
