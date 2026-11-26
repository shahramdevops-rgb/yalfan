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

        // A link row, such as a stage a contractor does, has nothing but its key. Its key columns
        // are then its fields, or adding and removing it would leave an entry that says nothing.
        var isLinkRow = !entry.ComplexProperties.Any()
            && entry.Properties.All(property => property.Metadata.IsPrimaryKey() || property.Metadata.IsConcurrencyToken);

        foreach (var (property, name) in FieldsOf(entry))
        {
            var metadata = property.Metadata;

            // The key is already the entry's EntityId; the row version (xmin) means nothing to a reader.
            if ((metadata.IsPrimaryKey() && !isLinkRow) || metadata.IsConcurrencyToken)
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

                // Whether there was a value, never the value: the history tells an IBAN set,
                // replaced or removed apart, and replacing one is the payout fraud (ADR 0027).
                if (action != AuditAction.Created)
                {
                    field["oldSet"] = property.OriginalValue is not null;
                }

                if (action != AuditAction.Deleted)
                {
                    field["newSet"] = property.CurrentValue is not null;
                }
            }

            changes[name] = field;
        }

        return changes;
    }

    /// <summary>
    /// Every stored field of the row with the name it is recorded under: camelCase, the names the
    /// API uses, so a history screen can reuse its field labels. The parts of a value object
    /// stored in the row's own columns (a complex property, such as the amount and currency of
    /// <c>Money</c>) are fields too, named after their columns, because their own names repeat
    /// ("amount" inside "Amount") and only the column name is unique in the row.
    /// </summary>
    private static IEnumerable<(PropertyEntry Property, string Name)> FieldsOf(EntityEntry entry)
    {
        foreach (var property in entry.Properties)
        {
            yield return (property, JsonNamingPolicy.CamelCase.ConvertName(property.Metadata.Name));
        }

        foreach (var field in entry.ComplexProperties.SelectMany(FieldsOf))
        {
            yield return field;
        }
    }

    private static IEnumerable<(PropertyEntry Property, string Name)> FieldsOf(ComplexPropertyEntry complex)
    {
        foreach (var property in complex.Properties)
        {
            yield return (property, CamelCase(property.Metadata.GetColumnName()));
        }

        foreach (var field in complex.ComplexProperties.SelectMany(FieldsOf))
        {
            yield return field;
        }
    }

    /// <summary>A snake_case column name in camelCase: "rate_currency" becomes "rateCurrency".</summary>
    private static string CamelCase(string columnName) =>
        string.Concat(columnName
            .Split('_', StringSplitOptions.RemoveEmptyEntries)
            .Select((part, index) => index == 0 ? part : char.ToUpperInvariant(part[0]) + part[1..]));

    /// <summary>The value as it is stored ("09121234567", "Owner"), not the C# object.</summary>
    private static JsonNode? ToJson(PropertyEntry property, object? value)
    {
        var stored = ToStored(property, value);
        return stored is null ? null : JsonSerializer.SerializeToNode(stored);
    }

    private static string KeyOf(EntityEntry entry)
    {
        var key = entry.Metadata.FindPrimaryKey()
            ?? throw new InvalidOperationException($"{entry.Metadata.DisplayName()} has no key and cannot be audited.");

        // A composite key reads "<owner id>,fa" for a translation row: each part in its stored
        // form, like the changed values, so the key matches what a query of the table shows.
        return string.Join(
            ",",
            key.Properties.Select(property =>
            {
                var part = entry.Property(property.Name);
                return Convert.ToString(ToStored(part, part.CurrentValue), CultureInfo.InvariantCulture);
            }));
    }

    /// <summary>The value as the database stores it, through the property's converter if it has one.</summary>
    private static object? ToStored(PropertyEntry property, object? value)
    {
        var converter = property.Metadata.GetValueConverter() ?? property.Metadata.GetTypeMapping().Converter;

        return value is null || converter is null ? value : converter.ConvertToProvider(value);
    }
}
