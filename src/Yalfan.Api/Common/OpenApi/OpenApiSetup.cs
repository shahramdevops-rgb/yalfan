namespace Yalfan.Api.Common.OpenApi;

/// <summary>
/// One OpenAPI document per frontend (ADR 0014). The panel's document holds only the
/// <c>/panel</c> routes, so the panel's generated types never show store or contractor
/// endpoints. The documents are served in Development and Testing only: the live API has no
/// reason to publish its own map.
/// </summary>
public static class OpenApiSetup
{
    public const string PanelDocument = "panel";

    public const string Route = "/openapi/{documentName}.json";

    public static IServiceCollection AddApiDocuments(this IServiceCollection services)
    {
        services.AddOpenApi(PanelDocument, options =>
        {
            options.ShouldInclude = description =>
                description.RelativePath?.StartsWith("panel/", StringComparison.Ordinal) == true;

            options.AddDocumentTransformer((document, _, _) =>
            {
                document.Info.Title = "Yalfan panel API";
                document.Info.Version = "v1";

                // The panel calls its own origin (ADR 0014), so the document names no server.
                // Without this the file would change with the host that served it.
                document.Servers?.Clear();
                return Task.CompletedTask;
            });
        });

        return services;
    }

    public static WebApplication MapApiDocuments(this WebApplication app)
    {
        if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Testing"))
        {
            app.MapOpenApi(Route).AllowAnonymous();
        }

        return app;
    }
}
