namespace Yalfan.Domain.Common;

/// <summary>
/// A business failure, identified by a stable code such as <c>quantity.too_precise</c>.
/// The code is the contract with the frontends: each one maps it to text in its own language,
/// so an error carries no message. Codes are lower-case <c>module.reason</c> and, once
/// released, are never renamed.
/// <para>
/// The record is open only for <see cref="ValidationError"/>, which adds the failing fields.
/// </para>
/// </summary>
public record Error
{
    public Error(string code, ErrorType type)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        Code = code;
        Type = type;
    }

    public string Code { get; }

    public ErrorType Type { get; }

    public static Error Validation(string code) => new(code, ErrorType.Validation);

    public static Error NotFound(string code) => new(code, ErrorType.NotFound);

    public static Error Conflict(string code) => new(code, ErrorType.Conflict);

    public static Error Forbidden(string code) => new(code, ErrorType.Forbidden);

    public static Error Rule(string code) => new(code, ErrorType.Rule);

    public static Error Unauthenticated(string code) => new(code, ErrorType.Unauthenticated);

    public sealed override string ToString() => Code;
}
