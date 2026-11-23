using Yalfan.Domain.Common;

namespace Yalfan.Domain.Parties;

/// <summary>
/// An in-house worker: one of the people who work inside the workshop, clock in (Phase 4) and are
/// paid through the payroll (Phase 5). Decided 1405/09/02, ADR 0020.
/// <para>
/// The job is the stages the worker does, from the shared list, because in-house work is recorded
/// by stage with the contractors' screens (5.2); a worker with no production stage, such as a
/// storekeeper, has none. Two workers may share a name; the mobile tells them apart and is unique
/// among workers, because it will be the worker's login (Phase 10). A worker is never deleted,
/// only deactivated: attendance and payslips will point at the worker.
/// </para>
/// </summary>
public sealed class Worker : Entity
{
    public const int FullNameMaxLength = PartyProfile.FullNameMaxLength;

    public const int NotesMaxLength = PartyProfile.NotesMaxLength;

    private readonly List<WorkerStage> _stages = [];

    private Worker()
    {
    }

    private Worker(DateTimeOffset createdAt)
    {
        IsActive = true;
        CreatedAt = createdAt;
    }

    /// <summary>Normalized like catalog names, so a search finds it whichever keyboard typed it.</summary>
    public string FullName { get; private set; } = null!;

    public MobileNumber MobileNumber { get; private set; } = null!;

    /// <summary>
    /// The workshop day the worker started. Payroll and the length of service (Phase 5) count from
    /// it; when the exact day is not known, an approximate one is typed and the notes say so.
    /// </summary>
    public DateOnly StartDate { get; private set; }

    /// <summary>Rare details with no field of their own, such as what a worker with no stage does.</summary>
    public string? Notes { get; private set; }

    /// <summary>
    /// Optional. Stored encrypted and shown masked; changed only through <see cref="ChangeIban"/>
    /// and <see cref="RemoveIban"/>, so a profile edit can never touch it (ADR 0019).
    /// </summary>
    public Iban? Iban { get; private set; }

    /// <summary>An inactive worker (one who has left) is left out of the pickers; the history stays.</summary>
    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>The stages the worker does; none is allowed.</summary>
    public IReadOnlyCollection<WorkerStage> Stages => _stages.AsReadOnly();

    /// <summary>The ids of <see cref="Stages"/>, in a stable order.</summary>
    public IReadOnlyList<Guid> StageIds => _stages.Select(stage => stage.StageId).Order().ToList();

    /// <summary>
    /// A new, active worker. The stages must exist; the caller checks that, because they are rows
    /// of another module. <paramref name="today"/> is the workshop's day, for the start date check.
    /// </summary>
    public static Result<Worker> Create(
        string? fullName,
        MobileNumber mobileNumber,
        IReadOnlyCollection<Guid> stageIds,
        DateOnly startDate,
        string? notes,
        DateOnly today,
        DateTimeOffset createdAt)
    {
        var worker = new Worker(createdAt);
        var updated = worker.UpdateProfile(fullName, mobileNumber, stageIds, startDate, notes, today);

        return updated.IsFailure ? updated.Error : worker;
    }

    /// <summary>
    /// Replaces everything on the profile except the IBAN. Stages no longer ticked are removed and
    /// new ones added; stages kept stay as they are.
    /// </summary>
    public Result UpdateProfile(
        string? fullName,
        MobileNumber mobileNumber,
        IReadOnlyCollection<Guid> stageIds,
        DateOnly startDate,
        string? notes,
        DateOnly today)
    {
        ArgumentNullException.ThrowIfNull(mobileNumber);
        ArgumentNullException.ThrowIfNull(stageIds);

        var name = PartyProfile.CheckFullName(fullName);
        if (name.IsFailure)
        {
            return name.Error;
        }

        var checkedNotes = PartyProfile.CheckNotes(notes);
        if (checkedNotes.IsFailure)
        {
            return checkedNotes.Error;
        }

        // A day after today is almost always a mistyped year; a worker is entered once at work.
        if (startDate > today)
        {
            return PartiesErrors.StartDateInFuture;
        }

        FullName = name.Value;
        MobileNumber = mobileNumber;
        StartDate = startDate;
        Notes = checkedNotes.Value;

        var stages = stageIds.ToHashSet();
        _stages.RemoveAll(stage => !stages.Contains(stage.StageId));
        foreach (var stageId in stages.Where(id => _stages.TrueForAll(stage => stage.StageId != id)))
        {
            _stages.Add(new WorkerStage(stageId));
        }

        return Result.Success();
    }

    /// <summary>
    /// Sets a new IBAN or replaces the old one. Phase 15 adds verification against the holder's
    /// national ID and a holding period here.
    /// </summary>
    public void ChangeIban(Iban iban)
    {
        ArgumentNullException.ThrowIfNull(iban);
        Iban = iban;
    }

    public void RemoveIban()
    {
        Iban = null;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void Activate()
    {
        IsActive = true;
    }
}
