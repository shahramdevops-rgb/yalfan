using Yalfan.Application.Common;
using Yalfan.Application.Production;
using static Yalfan.Application.History.AuditKeys;

namespace Yalfan.Application.History;

/// <summary>The change history of a contractor or a worker: the person and the stages ticked on him (ADR 0027).</summary>
internal static class PartyHistory
{
    public static async Task<HistoryPage> ReadAsync(
        IAppDbContext db,
        string personTable,
        string stagesTable,
        Guid personId,
        DateTimeOffset? before,
        CancellationToken cancellationToken)
    {
        // A stage unticked later has no row today, so every stage is asked for; stages are never
        // deleted, and there are only a few.
        var stages = await StageLists.InOrderAsync(db, cancellationToken);

        var page = await AuditTrail.ReadAsync(
            db,
            [
                new AuditedRows(personTable, [Key(personId)]),
                new AuditedRows(stagesTable, stages.Select(stage => Key(personId, stage.Id)).ToList()),
            ],
            before,
            cancellationToken);

        var names = new HistoryNames();
        for (var position = 0; position < stages.Count; position++)
        {
            names.Parts[Key(stages[position].Id)] = new PartName(stages[position].Name, position);
        }

        return await HistoryAssembler.AssembleAsync(db, page, names, cancellationToken);
    }
}
