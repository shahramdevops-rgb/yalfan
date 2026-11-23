using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Yalfan.Api.IntegrationTests.Catalog;
using static Yalfan.Api.IntegrationTests.Auth.AuthTestHelpers;

namespace Yalfan.Api.IntegrationTests.Parties;

/// <summary>Calls the worker endpoints the way the panel will, for tests that need some rows first.</summary>
internal static class WorkerApi
{
    public const string Workers = "/panel/parties/workers";

    public const string StartDate = "2021-03-21";

    /// <summary>Adds a worker who cuts, with a new mobile, and returns the details.</summary>
    public static async Task<JsonElement> CreateWorkerAsync(
        this HttpClient client,
        string? fullName = null,
        string? mobileNumber = null,
        string? iban = null,
        IReadOnlyList<Guid>? stageIds = null)
    {
        var response = await client.PostAsJsonAsync(
            Workers,
            new
            {
                fullName = fullName ?? CatalogApi.UniqueName("زهرا کریمی"),
                mobileNumber = mobileNumber ?? ApiFactory.NextMobile(),
                stageIds = stageIds ?? [await client.SeededStageAsync("برش")],
                startDate = StartDate,
                iban,
            },
            Cancel);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        return await response.Body();
    }
}
