using Yalfan.Domain.Common;

namespace Yalfan.Domain.Parties;

/// <summary>
/// An outside contractor paid by the piece: the person who takes the work and is paid, even when
/// he has helpers of his own, because the account is with him (decided 1405/09/02, ADR 0019).
/// <para>
/// Two contractors may share a name; the mobile tells them apart and is unique among contractors,
/// because in Phase 10 it is his login to his own panel. A contractor is never deleted, only
/// deactivated: work orders and his account (Phase 2) will point at him.
/// </para>
/// </summary>
public sealed class Contractor : Entity
{
    public const int FullNameMaxLength = PartyProfile.FullNameMaxLength;

    public const int NotesMaxLength = PartyProfile.NotesMaxLength;

    private readonly List<ContractorStage> _stages = [];

    private Contractor()
    {
    }

    private Contractor(DateTimeOffset createdAt)
    {
        IsActive = true;
        CreatedAt = createdAt;
    }

    /// <summary>Normalized like catalog names, so a search finds it whichever keyboard typed it.</summary>
    public string FullName { get; private set; } = null!;

    public MobileNumber MobileNumber { get; private set; } = null!;

    /// <summary>Rare details with no field of their own, such as the address or a workshop name.</summary>
    public string? Notes { get; private set; }

    /// <summary>
    /// Optional. Stored encrypted and shown masked; changed only through <see cref="ChangeIban"/>
    /// and <see cref="RemoveIban"/>, so a profile edit can never touch it (ADR 0019).
    /// </summary>
    public Iban? Iban { get; private set; }

    /// <summary>An inactive contractor is left out of the pickers; his history stays.</summary>
    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>The stages he does; at least one.</summary>
    public IReadOnlyCollection<ContractorStage> Stages => _stages.AsReadOnly();

    /// <summary>The ids of <see cref="Stages"/>, in a stable order.</summary>
    public IReadOnlyList<Guid> StageIds => _stages.Select(stage => stage.StageId).Order().ToList();

    /// <summary>
    /// A new, active contractor. The stages must exist; the caller checks that, because they are
    /// rows of another module.
    /// </summary>
    public static Result<Contractor> Create(
        string? fullName,
        MobileNumber mobileNumber,
        IReadOnlyCollection<Guid> stageIds,
        string? notes,
        DateTimeOffset createdAt)
    {
        var contractor = new Contractor(createdAt);
        var updated = contractor.UpdateProfile(fullName, mobileNumber, stageIds, notes);

        return updated.IsFailure ? updated.Error : contractor;
    }

    /// <summary>
    /// Replaces everything on the profile except the IBAN. Stages no longer ticked are removed and
    /// new ones added; stages kept stay as they are.
    /// </summary>
    public Result UpdateProfile(string? fullName, MobileNumber mobileNumber, IReadOnlyCollection<Guid> stageIds, string? notes)
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

        var stages = stageIds.ToHashSet();
        if (stages.Count == 0)
        {
            return PartiesErrors.StageRequired;
        }

        FullName = name.Value;
        MobileNumber = mobileNumber;
        Notes = checkedNotes.Value;

        _stages.RemoveAll(stage => !stages.Contains(stage.StageId));
        foreach (var stageId in stages.Where(id => _stages.TrueForAll(stage => stage.StageId != id)))
        {
            _stages.Add(new ContractorStage(stageId));
        }

        return Result.Success();
    }

    /// <summary>Sets a new IBAN or replaces the old one. Phase 15 adds verification and a holding period here.</summary>
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
