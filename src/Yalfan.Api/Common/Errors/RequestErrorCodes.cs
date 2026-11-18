namespace Yalfan.Api.Common.Errors;

/// <summary>
/// Codes for failures that happen outside a use case: a body that cannot be read, a route that
/// does not exist, a bug. Every error response carries a code, so a frontend has one way to
/// turn any error into text.
/// </summary>
public static class RequestErrorCodes
{
    public const string Malformed = "request.malformed";
    public const string Unauthenticated = "request.unauthenticated";
    public const string Forbidden = "request.forbidden";
    public const string NotFound = "request.not_found";
    public const string MethodNotAllowed = "request.method_not_allowed";
    public const string TooLarge = "request.too_large";
    public const string UnsupportedMediaType = "request.unsupported_media_type";
    public const string Failed = "request.failed";
    public const string Unexpected = "server.unexpected";

    public static string ForStatus(int status) => status switch
    {
        StatusCodes.Status400BadRequest => Malformed,
        StatusCodes.Status401Unauthorized => Unauthenticated,
        StatusCodes.Status403Forbidden => Forbidden,
        StatusCodes.Status404NotFound => NotFound,
        StatusCodes.Status405MethodNotAllowed => MethodNotAllowed,
        StatusCodes.Status413PayloadTooLarge => TooLarge,
        StatusCodes.Status415UnsupportedMediaType => UnsupportedMediaType,
        >= 500 => Unexpected,
        _ => Failed,
    };
}
