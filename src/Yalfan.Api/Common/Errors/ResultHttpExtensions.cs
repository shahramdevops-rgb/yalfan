using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http.HttpResults;
using Yalfan.Domain.Common;

namespace Yalfan.Api.Common.Errors;

/// <summary>Turns a use-case <see cref="Result{T}"/> into an HTTP response.</summary>
public static class ResultHttpExtensions
{
    /// <summary>200 with the value, or the error as a coded ProblemDetails.</summary>
    public static Results<Ok<T>, ProblemHttpResult> ToOk<T>(this Result<T> result)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (result.IsSuccess)
        {
            return TypedResults.Ok(result.Value);
        }

        return result.Error.ToProblem();
    }

    public static ProblemHttpResult ToProblem(this Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        var extensions = new Dictionary<string, object?> { [ProblemDetailsSetup.CodeKey] = error.Code };

        if (error is ValidationError validation)
        {
            extensions[ProblemDetailsSetup.ErrorsKey] = validation.Errors
                .Select(field => new FieldErrorResponse(
                    ToJsonPath(field.Field),
                    field.Code,
                    field.Args.Count == 0 ? null : field.Args))
                .ToArray();
        }

        return TypedResults.Problem(statusCode: StatusFor(error.Type), extensions: extensions);
    }

    /// <summary>
    /// Rule is 422, not 400: the request was well formed and understood, but the workshop's
    /// rules refuse it, and a frontend treats the two differently.
    /// </summary>
    public static int StatusFor(ErrorType type) => type switch
    {
        ErrorType.Validation => StatusCodes.Status400BadRequest,
        ErrorType.NotFound => StatusCodes.Status404NotFound,
        ErrorType.Conflict => StatusCodes.Status409Conflict,
        ErrorType.Forbidden => StatusCodes.Status403Forbidden,
        ErrorType.Rule => StatusCodes.Status422UnprocessableEntity,
        ErrorType.Unauthenticated => StatusCodes.Status401Unauthorized,
        _ => StatusCodes.Status500InternalServerError,
    };

    /// <summary>
    /// <c>Lines[0].UnitPrice</c> becomes <c>lines[0].unitPrice</c>, the names the frontend sent
    /// in the JSON body, so it can put each message next to its own field.
    /// </summary>
    private static string ToJsonPath(string field)
    {
        if (field.Length == 0)
        {
            return field;
        }

        var segments = field.Split('.');
        for (var i = 0; i < segments.Length; i++)
        {
            var segment = segments[i];
            var bracket = segment.IndexOf('[', StringComparison.Ordinal);
            var name = bracket < 0 ? segment : segment[..bracket];
            var index = bracket < 0 ? string.Empty : segment[bracket..];
            segments[i] = JsonNamingPolicy.CamelCase.ConvertName(name) + index;
        }

        return string.Join('.', segments);
    }
}

/// <summary>One failing field in a validation ProblemDetails (<c>errors</c>).</summary>
public sealed record FieldErrorResponse(
    string Field,
    string Code,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    IReadOnlyDictionary<string, object>? Args);
