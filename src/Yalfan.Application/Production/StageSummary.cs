using Yalfan.Domain.Production;

namespace Yalfan.Application.Production;

/// <summary>
/// A stage as the panel shows it: its id, Persian name and whether it is active, so a contractor
/// or a rate that still points at a deactivated stage can say so (ADR 0022).
/// </summary>
public sealed record StageSummary(Guid Id, string Name, bool IsActive)
{
    internal static StageSummary Of(Stage stage) => new(stage.Id, stage.Name, stage.IsActive);
}
