using FluentValidation;
using FluentValidation.Results;
using Yalfan.Domain.Common;

namespace Yalfan.Application.Common.Validation;

/// <summary>Runs every validator of a request and turns the failures into one <see cref="ValidationError"/>.</summary>
internal static class RequestValidation
{
    public static async Task<ValidationError?> ValidateAsync<TRequest>(
        TRequest request,
        IEnumerable<IValidator<TRequest>> validators,
        CancellationToken cancellationToken)
    {
        var failures = new List<ValidationFailure>();

        // One after another, not in parallel: validators that check the database share the
        // request's DbContext, which allows one operation at a time.
        foreach (var validator in validators)
        {
            var result = await validator.ValidateAsync(request, cancellationToken);
            failures.AddRange(result.Errors.Where(failure => failure.Severity == Severity.Error));
        }

        return failures.Count == 0 ? null : ValidationFailureMapper.ToValidationError(failures);
    }
}
