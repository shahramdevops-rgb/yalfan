using Microsoft.EntityFrameworkCore;
using Yalfan.Application.Common;
using Yalfan.Application.Production;
using Yalfan.Domain.Parties;

namespace Yalfan.Application.Parties;

/// <summary>
/// A worker as the panel shows them. The IBAN is only ever masked here (ADR 0019); the whole
/// number is a separate, owner-only request.
/// </summary>
public sealed record WorkerDetails(
    Guid Id,
    string FullName,
    string MobileNumber,
    IReadOnlyList<StageSummary> Stages,
    DateOnly StartDate,
    string? Notes,
    string? MaskedIban,
    bool IsActive,
    DateTimeOffset CreatedAt);

internal static class WorkerViews
{
    public static async Task<WorkerDetails> DetailsAsync(
        IAppDbContext db,
        Worker worker,
        CancellationToken cancellationToken)
    {
        var stages = await PartyStages.InOrderAsync(db, worker.StageIds, cancellationToken);

        return new WorkerDetails(
            worker.Id,
            worker.FullName,
            worker.MobileNumber.Value,
            PartyStages.Of(worker.StageIds, stages),
            worker.StartDate,
            worker.Notes,
            worker.Iban?.Masked,
            worker.IsActive,
            worker.CreatedAt);
    }

    /// <summary>
    /// Only among workers: a contractor or a staff user with the same mobile may be the same
    /// person, such as a manager on the payroll (ADR 0020).
    /// </summary>
    public static Task<bool> MobileTakenAsync(
        IAppDbContext db,
        Domain.Common.MobileNumber mobile,
        Guid? exceptWorkerId,
        CancellationToken cancellationToken) =>
        db.Workers.AnyAsync(w => w.MobileNumber == mobile && w.Id != exceptWorkerId, cancellationToken);
}
