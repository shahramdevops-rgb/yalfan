using Yalfan.Domain.Common;

namespace Yalfan.Application.Common.Handlers;

/// <summary>
/// A use case that changes data. Endpoints, jobs and later callers inject this interface; the
/// registration wraps every handler in the pipeline (validation first), so no caller can skip it.
/// </summary>
/// <remarks>
/// A command always returns a value, usually the id of the row it created or changed, so one
/// interface and one pipeline cover every command (ADR 0011).
/// </remarks>
public interface ICommandHandler<in TCommand, TResponse>
{
    Task<Result<TResponse>> Handle(TCommand command, CancellationToken cancellationToken);
}
