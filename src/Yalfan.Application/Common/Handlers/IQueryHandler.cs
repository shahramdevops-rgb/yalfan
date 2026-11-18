using Yalfan.Domain.Common;

namespace Yalfan.Application.Common.Handlers;

/// <summary>
/// A use case that only reads. Kept apart from <see cref="ICommandHandler{TCommand,TResponse}"/>
/// because later pipeline steps (transaction, audit) apply to commands only.
/// </summary>
public interface IQueryHandler<in TQuery, TResponse>
{
    Task<Result<TResponse>> Handle(TQuery query, CancellationToken cancellationToken);
}
