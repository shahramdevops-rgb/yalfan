using Yalfan.Domain.Common;

namespace Yalfan.Domain.Production;

/// <summary>
/// A kind of work a bag goes through, such as cutting, sewing or packing. The stages are one
/// shared list with no route per model: a work order will name the model and the stage directly
/// (ADR 0018). Part of a job given out on its own, such as sewing only the handles, is one more
/// stage in this list.
/// <para>
/// The name is a single Persian column, not translation rows, because stages are shown only in
/// the Persian panels, never in the store (ADR 0006 covers catalog text only). It is unique, so
/// the operator cannot enter the same stage twice.
/// </para>
/// </summary>
public sealed class Stage : Entity
{
    public const int NameMaxLength = 50;

    private Stage()
    {
    }

    public string Name { get; private set; } = null!;

    /// <summary>
    /// The stage's place in the order of the work, from 1. The operator drags the stages into
    /// that order (ADR 0022); every list of stages is shown in it, never sorted by name.
    /// </summary>
    public int Position { get; private set; }

    /// <summary>
    /// An inactive stage is left out of the pickers for new work and of the rate card; the
    /// contractors, workers and rates that already point at it keep it (ADR 0022). There is no
    /// delete: work orders will point at stages.
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>A new stage at <paramref name="position"/>, which is the end of the list when it is added.</summary>
    public static Result<Stage> Create(string? name, int position)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(position, 1);

        var checkedName = CheckName(name);
        if (checkedName.IsFailure)
        {
            return checkedName.Error;
        }

        return new Stage { Name = checkedName.Value, Position = position, IsActive = true };
    }

    public Result Rename(string? name)
    {
        var checkedName = CheckName(name);
        if (checkedName.IsFailure)
        {
            return checkedName.Error;
        }

        Name = checkedName.Value;
        return Result.Success();
    }

    public void Deactivate() => IsActive = false;

    public void Activate() => IsActive = true;

    /// <summary>
    /// Puts every stage in the order of <paramref name="orderedIds"/>, the whole list as the
    /// operator left it after dragging. The list must name each stage exactly once: a list that
    /// misses a stage or names an unknown one was made from an old copy (another user added a
    /// stage meanwhile), and applying it would leave two stages at one place.
    /// </summary>
    public static Result Reorder(IReadOnlyCollection<Stage> stages, IReadOnlyList<Guid> orderedIds)
    {
        ArgumentNullException.ThrowIfNull(stages);
        ArgumentNullException.ThrowIfNull(orderedIds);

        var byId = stages.ToDictionary(stage => stage.Id);
        if (orderedIds.Count != byId.Count
            || orderedIds.Distinct().Count() != orderedIds.Count
            || !orderedIds.All(byId.ContainsKey))
        {
            return ProductionErrors.StageOrderOutdated;
        }

        for (var index = 0; index < orderedIds.Count; index++)
        {
            byId[orderedIds[index]].Position = index + 1;
        }

        return Result.Success();
    }

    /// <summary>The normalized name, or why it cannot be used.</summary>
    private static Result<string> CheckName(string? value)
    {
        var name = PersianText.Normalize(value);

        if (name.Length == 0)
        {
            return ProductionErrors.NameRequired;
        }

        if (name.Length > NameMaxLength)
        {
            return ProductionErrors.NameTooLong;
        }

        return name;
    }
}
