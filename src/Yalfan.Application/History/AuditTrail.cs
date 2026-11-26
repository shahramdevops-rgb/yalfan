using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Yalfan.Application.Common;
using Yalfan.Domain.Audit;

namespace Yalfan.Application.History;

/// <summary>The rows of one table that belong to a record, by their audit key (ADR 0013).</summary>
internal sealed record AuditedRows(string Table, IReadOnlyCollection<string> Keys);

/// <summary>A field of an audit entry: its old and new value as text (see <see cref="HistoryField"/>).</summary>
internal sealed record AuditedValue(string? Old, string? New);

/// <summary>One audit entry read back.</summary>
internal sealed record AuditedChange(string Table, string Key, AuditAction Action, IReadOnlyDictionary<string, AuditedValue> Fields);

/// <summary>The entries one save wrote.</summary>
internal sealed record AuditedSave(DateTimeOffset At, Guid? UserId, IReadOnlyList<AuditedChange> Changes);

internal sealed record AuditTrailPage(IReadOnlyList<AuditedSave> Saves, DateTimeOffset? Before)
{
    /// <summary>Every id a field of a table holds, old or new: the categories, fabrics, colours to name.</summary>
    public IReadOnlyList<Guid> IdsIn(string table, string field) =>
        Saves.SelectMany(save => save.Changes)
            .Where(change => change.Table == table && change.Fields.ContainsKey(field))
            .SelectMany(change => new[] { change.Fields[field].Old, change.Fields[field].New })
            .Select(value => Guid.TryParse(value, out var id) ? id : (Guid?)null)
            .OfType<Guid>()
            .Distinct()
            .ToList();
}

/// <summary>
/// Reads the audit of a record one page of saves at a time, newest first (ADR 0027). A save is
/// every entry stamped with the same moment, user and request: the interceptor reads the clock
/// once per save, so the entries of one save share their moment exactly.
/// </summary>
internal static class AuditTrail
{
    public const int SavesPerPage = 20;

    public static async Task<AuditTrailPage> ReadAsync(
        IAppDbContext db,
        IReadOnlyList<AuditedRows> rows,
        DateTimeOffset? before,
        CancellationToken cancellationToken)
    {
        // One query per table joined with UNION ALL: each part uses the (entity_type, entity_id,
        // occurred_at) index with exact keys, and no key can match a row of another table.
        IQueryable<AuditEntry>? entries = null;
        foreach (var (table, keys) in rows.Where(ofTable => ofTable.Keys.Count > 0))
        {
            var keyList = keys.ToArray();
            var ofTable = db.AuditEntries.Where(entry => entry.EntityType == table && keyList.Contains(entry.EntityId));
            entries = entries is null ? ofTable : entries.Concat(ofTable);
        }

        if (entries is null)
        {
            return new AuditTrailPage([], null);
        }

        // PostgreSQL takes a moment only in UTC; a cursor sent with another offset is the same moment.
        var olderThan = before?.ToUniversalTime();
        var moments = await entries
            .Where(entry => olderThan == null || entry.OccurredAt < olderThan)
            .Select(entry => entry.OccurredAt)
            .Distinct()
            .OrderByDescending(moment => moment)
            .Take(SavesPerPage + 1)
            .ToListAsync(cancellationToken);

        var page = moments.Take(SavesPerPage).ToList();
        if (page.Count == 0)
        {
            return new AuditTrailPage([], null);
        }

        var stored = await entries.Where(entry => page.Contains(entry.OccurredAt)).ToListAsync(cancellationToken);

        var saves = stored
            .GroupBy(entry => (entry.OccurredAt, entry.UserId, entry.TraceId))
            .OrderByDescending(save => save.Key.OccurredAt)
            .Select(save => new AuditedSave(
                save.Key.OccurredAt,
                save.Key.UserId,
                save.Select(entry => new AuditedChange(entry.EntityType, entry.EntityId, entry.Action, Parse(entry.Changes))).ToList()))
            .ToList();

        return new AuditTrailPage(saves, moments.Count > SavesPerPage ? page[^1] : null);
    }

    private static Dictionary<string, AuditedValue> Parse(string changes)
    {
        using var document = JsonDocument.Parse(changes);
        var fields = new Dictionary<string, AuditedValue>(StringComparer.Ordinal);

        foreach (var field in document.RootElement.EnumerateObject())
        {
            var value = field.Value;

            // A redacted field (an IBAN) has no values; since ADR 0027 it says whether there was
            // one before and after, and older entries say nothing.
            fields[field.Name] = value.TryGetProperty("redacted", out var redacted) && redacted.ValueKind == JsonValueKind.True
                ? new AuditedValue(Text(value, "oldSet"), Text(value, "newSet"))
                : new AuditedValue(Text(value, "old"), Text(value, "new"));
        }

        return fields;
    }

    private static string? Text(JsonElement field, string name)
    {
        if (!field.TryGetProperty(name, out var value))
        {
            return null;
        }

        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Number => value.GetRawText(),
            _ => null,
        };
    }
}
