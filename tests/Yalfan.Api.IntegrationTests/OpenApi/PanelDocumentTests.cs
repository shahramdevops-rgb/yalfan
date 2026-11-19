using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Yalfan.Infrastructure.Sms;

namespace Yalfan.Api.IntegrationTests.OpenApi;

/// <summary>
/// The panel's typed client is generated from <c>web/panel/openapi/panel.json</c> (ADR 0014).
/// These tests keep that file equal to what the API really serves, so the panel's types cannot
/// drift away from the API without a red test.
/// </summary>
public sealed class PanelDocumentTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private const string DocumentUrl = "/openapi/panel.json";

    private static readonly JsonSerializerOptions Indented = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private static CancellationToken Cancel => TestContext.Current.CancellationToken;

    [Fact]
    public async Task The_committed_panel_contract_matches_the_api()
    {
        var served = await ServedDocument();
        var path = ContractPath();
        var committed = File.Exists(path) ? JsonNode.Parse(await File.ReadAllTextAsync(path, Cancel)) : null;

        if (!JsonNode.DeepEquals(served, committed))
        {
            // Rewritten so that `git diff` shows exactly what changed in the contract.
            await File.WriteAllTextAsync(path, served.ToJsonString(Indented) + "\n", Cancel);
            Assert.Fail(
                $"The panel's API contract changed, so {path} was rewritten. Review it with git diff, " +
                "commit it with the API change, and run the tests again. The panel's types are " +
                "regenerated from it on its next dev, build or test run.");
        }
    }

    [Fact]
    public async Task The_panel_document_holds_only_panel_routes()
    {
        var paths = (await ServedDocument())["paths"]!.AsObject().Select(path => path.Key).ToList();

        Assert.Contains("/panel/auth/login", paths);
        // The test host also maps /sample (ApiFactory); it must stay out of the panel's document.
        Assert.All(paths, path => Assert.StartsWith("/panel/", path, StringComparison.Ordinal));
    }

    [Fact]
    public async Task The_documents_are_not_served_in_production()
    {
        using var production = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Production");
            // The log SMS provider stops the start outside Development and Testing (ADR 0012).
            // This test is about the OpenAPI route, so that guard is lifted here only.
            builder.ConfigureTestServices(services => services.RemoveAll<IValidateOptions<SmsOptions>>());
        });

        var response = await production.CreateClient().GetAsync(DocumentUrl, Cancel);

        Assert.False(response.IsSuccessStatusCode);
    }

    private async Task<JsonNode> ServedDocument()
    {
        using var client = factory.CreateClient();
        return JsonNode.Parse(await client.GetStringAsync(DocumentUrl, Cancel))!;
    }

    private static string ContractPath()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Yalfan.slnx")))
        {
            directory = directory.Parent;
        }

        if (directory is null)
        {
            throw new InvalidOperationException("The repository root (Yalfan.slnx) was not found above the test output.");
        }

        return Path.Combine(directory.FullName, "web", "panel", "openapi", "panel.json");
    }
}
