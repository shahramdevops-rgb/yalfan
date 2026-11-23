namespace Yalfan.Domain.Parties;

/// <summary>
/// A stage a worker does (ADR 0020). Part of <see cref="Worker"/>; the stage itself is a row of
/// the Production module's shared list, pointed at by its id.
/// </summary>
public sealed class WorkerStage
{
    internal WorkerStage(Guid stageId)
    {
        StageId = stageId;
    }

    public Guid StageId { get; private set; }
}
