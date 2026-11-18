namespace Yalfan.Domain.Common;

/// <summary>
/// One failing input field inside a <see cref="ValidationError"/>.
/// </summary>
public sealed record FieldError
{
    private static readonly IReadOnlyDictionary<string, object> NoArgs =
        new Dictionary<string, object>();

    /// <param name="field">
    /// Path of the field as the request type names it, such as <c>Lines[0].Quantity</c>. Empty
    /// when the rule is about the request as a whole.
    /// </param>
    /// <param name="code">Stable code, such as <c>validation.required</c>.</param>
    /// <param name="args">
    /// Values the frontend needs to write the message, such as <c>maxLength</c>. Never the value
    /// the user typed, which may be a password or an IBAN.
    /// </param>
    public FieldError(string field, string code, IReadOnlyDictionary<string, object>? args = null)
    {
        ArgumentNullException.ThrowIfNull(field);
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        Field = field;
        Code = code;
        Args = args ?? NoArgs;
    }

    public string Field { get; }

    public string Code { get; }

    public IReadOnlyDictionary<string, object> Args { get; }
}
