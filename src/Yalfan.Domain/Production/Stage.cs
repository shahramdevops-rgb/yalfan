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

    public static Result<Stage> Create(string? name)
    {
        var checkedName = CheckName(name);
        if (checkedName.IsFailure)
        {
            return checkedName.Error;
        }

        return new Stage { Name = checkedName.Value };
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
