using System.Diagnostics;

namespace Yalfan.Api.Common.Errors;

/// <summary>
/// Every error response is an RFC 9457 ProblemDetails with a stable <c>code</c> and a
/// <c>traceId</c>. The frontends read the code; the trace id finds the request in the log.
/// </summary>
public static class ProblemDetailsSetup
{
    public const string CodeKey = "code";
    public const string TraceIdKey = "traceId";
    public const string ErrorsKey = "errors";

    public static IServiceCollection AddErrorHandling(this IServiceCollection services)
    {
        services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
        {
            var problem = context.ProblemDetails;
            var status = problem.Status ?? context.HttpContext.Response.StatusCode;

            // Responses made outside a use case (unknown route, unreadable body) get a code here.
            problem.Extensions.TryAdd(CodeKey, RequestErrorCodes.ForStatus(status));
            problem.Extensions.TryAdd(TraceIdKey, Activity.Current?.Id ?? context.HttpContext.TraceIdentifier);
        });

        services.AddExceptionHandler<ApiExceptionHandler>();

        return services;
    }

    /// <summary>
    /// Exceptions become coded 500s (or 400s for unreadable requests), and error statuses with
    /// an empty body, such as an unknown route, get a ProblemDetails body.
    /// </summary>
    public static IApplicationBuilder UseErrorHandling(this IApplicationBuilder app)
    {
        app.UseExceptionHandler();
        app.UseStatusCodePages();

        return app;
    }
}
