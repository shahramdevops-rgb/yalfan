using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Yalfan.Api.Common.Errors;

/// <summary>
/// Turns an exception into a coded ProblemDetails. A bug answers 500 with
/// <c>server.unexpected</c> and no detail, because stack traces and messages tell an outsider
/// how the system is built; the full exception goes to the log under the same trace id.
/// </summary>
internal sealed partial class ApiExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        int status;
        if (exception is BadHttpRequestException badRequest)
        {
            // An unreadable body or a missing parameter. In Development minimal APIs throw this
            // (elsewhere they answer 400 directly); it is the caller's mistake, not a bug.
            status = badRequest.StatusCode;
            LogBadRequest(logger, httpContext.Request.Method, httpContext.Request.Path, exception.Message);
        }
        else
        {
            status = StatusCodes.Status500InternalServerError;
            LogUnexpected(logger, exception, httpContext.Request.Method, httpContext.Request.Path);
        }

        httpContext.Response.StatusCode = status;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = status,
                Extensions = { [ProblemDetailsSetup.CodeKey] = RequestErrorCodes.ForStatus(status) },
            },
        });
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Bad request {Method} {Path}: {Reason}")]
    private static partial void LogBadRequest(ILogger logger, string method, string path, string reason);

    [LoggerMessage(Level = LogLevel.Error, Message = "Unexpected failure in {Method} {Path}")]
    private static partial void LogUnexpected(ILogger logger, Exception exception, string method, string path);
}
