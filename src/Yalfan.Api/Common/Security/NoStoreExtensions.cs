namespace Yalfan.Api.Common.Security;

public static class NoStoreExtensions
{
    /// <summary>
    /// Every answer under <c>/panel</c> tells the browser, and any proxy on the way, not to keep a
    /// copy (<c>Cache-Control: no-store</c>). Each one is personal or business data, such as the
    /// whole IBAN shown to the owner on his phone, and the panel keeps what it needs in memory, so
    /// the browser's cache only adds a place where it could be read later (ADR 0025). The header is
    /// set when the response starts, so a refusal or an error carries it too.
    /// </summary>
    public static IApplicationBuilder UseNoStoreForPanel(this IApplicationBuilder app) =>
        app.Use(static (context, next) =>
        {
            if (context.Request.Path.StartsWithSegments("/panel"))
            {
                context.Response.OnStarting(
                    static state =>
                    {
                        ((HttpResponse)state).Headers.CacheControl = "no-store";
                        return Task.CompletedTask;
                    },
                    context.Response);
            }

            return next(context);
        });
}
