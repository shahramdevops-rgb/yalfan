namespace Yalfan.Domain.Parties;

/// <summary>
/// A stage a contractor does (ADR 0019). Part of <see cref="Contractor"/>; the stage itself is a
/// row of the Production module's shared list, pointed at by its id.
/// </summary>
public sealed class ContractorStage
{
    internal ContractorStage(Guid stageId)
    {
        StageId = stageId;
    }

    public Guid StageId { get; private set; }
}
