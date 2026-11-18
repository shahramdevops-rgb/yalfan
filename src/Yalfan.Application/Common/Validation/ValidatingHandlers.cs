using FluentValidation;
using Yalfan.Application.Common.Handlers;
using Yalfan.Domain.Common;

namespace Yalfan.Application.Common.Validation;

/// <summary>
/// First step of the command pipeline: an invalid command never reaches its handler and comes
/// back as a <see cref="ValidationError"/>, not as an exception.
/// </summary>
internal sealed class ValidatingCommandHandler<TCommand, TResponse>(
    ICommandHandler<TCommand, TResponse> inner,
    IEnumerable<IValidator<TCommand>> validators) : ICommandHandler<TCommand, TResponse>
{
    public async Task<Result<TResponse>> Handle(TCommand command, CancellationToken cancellationToken)
    {
        var error = await RequestValidation.ValidateAsync(command, validators, cancellationToken);
        if (error is not null)
        {
            return error;
        }

        return await inner.Handle(command, cancellationToken);
    }
}

/// <summary>First step of the query pipeline; the same rule as for commands.</summary>
internal sealed class ValidatingQueryHandler<TQuery, TResponse>(
    IQueryHandler<TQuery, TResponse> inner,
    IEnumerable<IValidator<TQuery>> validators) : IQueryHandler<TQuery, TResponse>
{
    public async Task<Result<TResponse>> Handle(TQuery query, CancellationToken cancellationToken)
    {
        var error = await RequestValidation.ValidateAsync(query, validators, cancellationToken);
        if (error is not null)
        {
            return error;
        }

        return await inner.Handle(query, cancellationToken);
    }
}
