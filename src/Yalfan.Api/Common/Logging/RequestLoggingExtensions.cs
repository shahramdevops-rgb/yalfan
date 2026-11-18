using Serilog;
using Serilog.Events;

namespace Yalfan.Api.Common.Logging;

public static class RequestLoggingExtensions
{
    /// <summary>
    /// One structured line per request: method, path, status, time taken and trace id. Bodies
    /// and query strings are never logged, because they will carry passwords, phone numbers and
    /// IBANs. The health probe, called every few seconds, is logged only at Verbose.
    /// </summary>
    public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder app) =>
        app.UseSerilogRequestLogging(options => options.GetLevel = (httpContext, _, exception) =>
            exception is not null || httpContext.Response.StatusCode >= StatusCodes.Status500InternalServerError
                ? LogEventLevel.Error
                : httpContext.Request.Path.StartsWithSegments("/health")
                    ? LogEventLevel.Verbose
                    : LogEventLevel.Information);
}
