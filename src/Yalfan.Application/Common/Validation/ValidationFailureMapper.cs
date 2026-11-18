using System.Text.RegularExpressions;
using FluentValidation.Results;
using Yalfan.Domain.Common;

namespace Yalfan.Application.Common.Validation;

/// <summary>
/// Turns FluentValidation failures into field errors with stable codes.
/// </summary>
/// <remarks>
/// A rule may name its own code with <c>WithErrorCode("module.reason")</c>, and that code is
/// kept. The built-in rules report the name of a C# class (<c>NotEmptyValidator</c>), which must
/// not become the contract with the frontends, so they are translated to the generic
/// <c>validation.*</c> codes below. FluentValidation's English messages are never used.
/// </remarks>
internal static partial class ValidationFailureMapper
{
    public const string Required = "validation.required";
    public const string TooLong = "validation.too_long";
    public const string TooShort = "validation.too_short";
    public const string Length = "validation.length";
    public const string TooSmall = "validation.too_small";
    public const string TooLarge = "validation.too_large";
    public const string OutOfRange = "validation.out_of_range";
    public const string InvalidFormat = "validation.invalid_format";
    public const string Invalid = "validation.invalid";

    public static ValidationError ToValidationError(IEnumerable<ValidationFailure> failures) =>
        new(failures.Select(ToFieldError).ToList());

    private static FieldError ToFieldError(ValidationFailure failure)
    {
        var field = failure.PropertyName ?? string.Empty;
        var code = failure.ErrorCode ?? string.Empty;

        if (OwnCode().IsMatch(code))
        {
            return new FieldError(field, code);
        }

        var values = failure.FormattedMessagePlaceholderValues ?? new Dictionary<string, object>();

        return code switch
        {
            "NotEmptyValidator" or "NotNullValidator" => new(field, Required),
            "MaximumLengthValidator" => new(field, TooLong, Args(values, ("maxLength", "MaxLength"))),
            "MinimumLengthValidator" => new(field, TooShort, Args(values, ("minLength", "MinLength"))),
            "LengthValidator" or "ExactLengthValidator" =>
                new(field, Length, Args(values, ("minLength", "MinLength"), ("maxLength", "MaxLength"))),
            "GreaterThanValidator" => new(field, TooSmall, Args(values, ("min", "ComparisonValue")).With("inclusive", false)),
            "GreaterThanOrEqualValidator" => new(field, TooSmall, Args(values, ("min", "ComparisonValue")).With("inclusive", true)),
            "LessThanValidator" => new(field, TooLarge, Args(values, ("max", "ComparisonValue")).With("inclusive", false)),
            "LessThanOrEqualValidator" => new(field, TooLarge, Args(values, ("max", "ComparisonValue")).With("inclusive", true)),
            "InclusiveBetweenValidator" => new(field, OutOfRange, Args(values, ("min", "From"), ("max", "To")).With("inclusive", true)),
            "ExclusiveBetweenValidator" => new(field, OutOfRange, Args(values, ("min", "From"), ("max", "To")).With("inclusive", false)),
            "EmailValidator" or "RegularExpressionValidator" or "CreditCardValidator" => new(field, InvalidFormat),
            _ => new(field, Invalid),
        };
    }

    /// <summary>
    /// Copies only the named placeholders. The others include <c>PropertyValue</c>, the value
    /// the user typed, which must never leave the server in an error.
    /// </summary>
    private static Dictionary<string, object> Args(
        IReadOnlyDictionary<string, object> values,
        params (string Name, string Placeholder)[] wanted)
    {
        var args = new Dictionary<string, object>(StringComparer.Ordinal);
        foreach (var (name, placeholder) in wanted)
        {
            if (values.TryGetValue(placeholder, out var value) && value is not null)
            {
                args[name] = value;
            }
        }

        return args;
    }

    private static Dictionary<string, object> With(this Dictionary<string, object> args, string name, object value)
    {
        args[name] = value;
        return args;
    }

    /// <summary>A project code: lower-case <c>module.reason</c>, as in <see cref="Error"/>.</summary>
    [GeneratedRegex("^[a-z][a-z0-9_]*(\\.[a-z][a-z0-9_]*)+$", RegexOptions.CultureInvariant)]
    private static partial Regex OwnCode();
}
