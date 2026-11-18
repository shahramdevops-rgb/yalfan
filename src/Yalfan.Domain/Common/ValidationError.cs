namespace Yalfan.Domain.Common;

/// <summary>
/// The input of a request failed validation. The code is always
/// <see cref="FailedCode"/>; what is wrong is in <see cref="Errors"/>, one entry per failing
/// field, so the frontend can show each message next to its field.
/// </summary>
public sealed record ValidationError : Error
{
    public const string FailedCode = "validation.failed";

    public ValidationError(IReadOnlyList<FieldError> errors)
        : base(FailedCode, ErrorType.Validation)
    {
        ArgumentNullException.ThrowIfNull(errors);
        if (errors.Count == 0)
        {
            throw new ArgumentException("A validation error needs at least one field error.", nameof(errors));
        }

        Errors = errors;
    }

    /// <summary>The failing fields. Compared by reference, like any list in a record.</summary>
    public IReadOnlyList<FieldError> Errors { get; }
}
