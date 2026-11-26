using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Yalfan.Application.Common;
using Yalfan.Domain.Audit;

namespace Yalfan.Application.History;

/// <summary>Today's name of a part (a size, a bag's code, a stage) and its place among its kind.</summary>
internal sealed record PartName(string Label, int Order);

/// <summary>
/// The names a history shows instead of ids: of each part, by the part's key, and of each id a
/// field holds (a category, fabric, colour or size). Today's names: when a name itself changed,
/// the history shows that change with the old name (ADR 0027).
/// </summary>
internal sealed class HistoryNames
{
    public Dictionary<string, PartName> Parts { get; } = new(StringComparer.Ordinal);

    public Dictionary<string, string> Ids { get; } = new(StringComparer.Ordinal);
}

/// <summary>
/// Turns the audit entries of each save into the history's words: the entries about one part
/// become one change, fields that mean nothing to the owner are left out, ids become names, and
/// the record itself comes first, then its sizes, bags, rates and stages, in their own order.
/// </summary>
internal static class HistoryAssembler
{
    private static readonly HashSet<HistoryFieldName> IdFields =
    [
        HistoryFieldName.Category,
        HistoryFieldName.Fabric,
        HistoryFieldName.Colour,
        HistoryFieldName.Size,
    ];

    public static async Task<HistoryPage> AssembleAsync(
        IAppDbContext db,
        AuditTrailPage page,
        HistoryNames names,
        CancellationToken cancellationToken)
    {
        // Users are never deleted, so every user who saved something is found.
        var userIds = page.Saves.Select(save => save.UserId).OfType<Guid>().Distinct().ToList();
        var users = await db.Users.AsNoTracking()
            .Where(user => userIds.Contains(user.Id))
            .ToDictionaryAsync(user => user.Id, user => user.FullName, cancellationToken);

        var saves = page.Saves
            .Select(save => new HistorySave(
                save.At,
                save.UserId is { } userId ? users.GetValueOrDefault(userId) : null,
                Describe(save.Changes, names)))
            .Where(save => save.Changes.Count > 0)
            .ToList();

        return new HistoryPage(saves, page.Before);
    }

    private static List<HistoryChange> Describe(IReadOnlyList<AuditedChange> changes, HistoryNames names) =>
        changes
            .Select(change => (Change: change, Vocabulary: HistoryVocabulary.Tables[change.Table]))
            .GroupBy(entry => (entry.Vocabulary.Part, Owner: entry.Vocabulary.OwnerOf(entry.Change.Key)))
            .Select(part =>
            {
                // A model and its name are two rows saved together; the part was created if any
                // of its rows was.
                var actions = part.Select(entry => entry.Change.Action).ToList();
                var action = actions.Contains(AuditAction.Created) ? HistoryAction.Created
                    : actions.Contains(AuditAction.Deleted) ? HistoryAction.Deleted
                    : HistoryAction.Updated;

                // In a fixed order, not the audit's: jsonb keeps its keys in its own order (ADR 0013).
                var fields = part
                    .SelectMany(entry => Fields(entry.Change, entry.Vocabulary, action, names))
                    .OrderBy(field => field.Name)
                    .ToList();
                var name = IsRecord(part.Key.Part) ? null : names.Parts.GetValueOrDefault(part.Key.Owner);

                // The place the save gave the part, if it gave one: the sizes of a new model in the
                // order they were typed, even after they were dragged into another; else today's.
                var order = part.Select(entry => RecordedPosition(entry.Change)).FirstOrDefault(position => position is not null)
                    ?? name?.Order
                    ?? 0;

                return (Change: new HistoryChange(part.Key.Part, action, name?.Label, fields), Order: order);
            })
            .Where(part => part.Change.Action != HistoryAction.Updated || part.Change.Fields.Count > 0)
            .OrderBy(part => Rank(part.Change.Part))
            .ThenBy(part => part.Order)
            .ThenBy(part => part.Change.Label, StringComparer.Ordinal)
            .Select(part => part.Change)
            .ToList();

    private static IEnumerable<HistoryField> Fields(
        AuditedChange change,
        HistoryVocabulary vocabulary,
        HistoryAction action,
        HistoryNames names)
    {
        foreach (var (recorded, value) in change.Fields)
        {
            // A field the vocabulary does not know is left out; its test makes sure there is none.
            if (!vocabulary.Fields.TryGetValue(recorded, out var name) || name is not { } field)
            {
                continue;
            }

            if (action == HistoryAction.Created && !SaysSomethingNew(field, value))
            {
                continue;
            }

            yield return IdFields.Contains(field)
                ? new HistoryField(field, NameOf(value.Old, names), NameOf(value.New, names))
                : new HistoryField(field, value.Old, value.New);
        }
    }

    /// <summary>
    /// What a new part was created with, without what every new part has: on, in the last place,
    /// and no IBAN or notes when none were given.
    /// </summary>
    private static bool SaysSomethingNew(HistoryFieldName field, AuditedValue value) => field switch
    {
        HistoryFieldName.Active or HistoryFieldName.Position => false,
        HistoryFieldName.Iban => value.New == "true",
        _ => value.New is not null,
    };

    private static int? RecordedPosition(AuditedChange change) =>
        change.Fields.TryGetValue("position", out var position)
        && int.TryParse(position.New, NumberStyles.None, CultureInfo.InvariantCulture, out var place)
            ? place
            : null;

    private static string? NameOf(string? id, HistoryNames names) =>
        id is null ? null : names.Ids.GetValueOrDefault(id, id);

    private static bool IsRecord(HistoryPart part) =>
        part is HistoryPart.Model or HistoryPart.Contractor or HistoryPart.Worker;

    private static int Rank(HistoryPart part) => part switch
    {
        HistoryPart.Model or HistoryPart.Contractor or HistoryPart.Worker => 0,
        HistoryPart.Size => 1,
        HistoryPart.Bag => 2,
        HistoryPart.Rate => 3,
        _ => 4,
    };
}
