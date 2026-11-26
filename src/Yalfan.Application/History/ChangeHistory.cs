namespace Yalfan.Application.History;

/// <summary>
/// One page of a record's change history, newest save first (ADR 0027). <see cref="Before"/> is
/// what to send for the next, older page; null when there is none.
/// </summary>
public sealed record HistoryPage(IReadOnlyList<HistorySave> Saves, DateTimeOffset? Before);

/// <summary>
/// One save: everything one person stored at one moment. <see cref="By"/> is the user's name;
/// null when the system made it (start-up, a job).
/// </summary>
public sealed record HistorySave(DateTimeOffset At, string? By, IReadOnlyList<HistoryChange> Changes);

/// <summary>
/// What one save did to one part of the record: the model or person itself, a size, a bag, a
/// rate or a stage. <see cref="Label"/> names the part (the size, the bag's code, the stage) with
/// today's name; null for the record itself.
/// </summary>
public sealed record HistoryChange(HistoryPart Part, HistoryAction Action, string? Label, IReadOnlyList<HistoryField> Fields);

/// <summary>
/// One field before and after, as text: ids already replaced by names, a yes or no as
/// <c>true</c>/<c>false</c>, a day as <c>2026-11-20</c>, an amount as its Rial digits. A created
/// part has only <see cref="New"/>. An IBAN says only whether there was one, never the number,
/// and nothing at all in entries written before that was recorded (ADR 0027).
/// </summary>
public sealed record HistoryField(HistoryFieldName Name, string? Old, string? New);

public enum HistoryPart
{
    Model,
    Size,
    Bag,
    Rate,
    Contractor,
    Worker,
    Stage,
}

public enum HistoryAction
{
    Created,
    Updated,
    Deleted,
}

public enum HistoryFieldName
{
    Name,
    Description,
    Category,
    Active,
    Position,
    Fabric,
    Colour,
    Size,
    Amount,
    StartsOn,
    FullName,
    MobileNumber,
    Notes,
    StartDate,
    Iban,
}
