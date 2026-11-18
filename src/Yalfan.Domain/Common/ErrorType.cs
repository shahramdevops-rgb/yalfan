namespace Yalfan.Domain.Common;

/// <summary>
/// The kind of a business failure. The Api layer turns the kind into an HTTP status; the
/// Domain only says what went wrong.
/// </summary>
public enum ErrorType
{
    /// <summary>The input is malformed or incomplete.</summary>
    Validation,

    /// <summary>The thing asked for does not exist, or is not visible to the caller.</summary>
    NotFound,

    /// <summary>The request collides with the current state, such as a duplicate code.</summary>
    Conflict,

    /// <summary>The caller is known but may not do this.</summary>
    Forbidden,

    /// <summary>The input is well formed but a business rule forbids the action.</summary>
    Rule,

    /// <summary>The caller could not be identified: a wrong login code, an invalid session.</summary>
    Unauthenticated,
}
