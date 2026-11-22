using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Yalfan.Api.IntegrationTests.Catalog;
using static Yalfan.Api.IntegrationTests.Auth.AuthTestHelpers;

namespace Yalfan.Api.IntegrationTests.Parties;

/// <summary>Calls the contractor endpoints the way the panel will, for tests that need some rows first.</summary>
internal static class ContractorApi
{
    public const string Contractors = "/panel/parties/contractors";

    /// <summary>The Iranian example of the SWIFT IBAN registry.</summary>
    public const string Iban = "IR710570029971601460641001";

    public const string OtherIban = "IR062960000000100324200001";

    /// <summary>The id of one of the three stages every database starts with (ADR 0018).</summary>
    public static async Task<Guid> SeededStageAsync(this HttpClient client, string name)
    {
        var stages = await (await client.GetAsync("/panel/production/stages", Cancel)).Body();
        return stages.EnumerateArray().Single(stage => stage.GetProperty("name").GetString() == name).GetProperty("id").GetGuid();
    }

    /// <summary>Adds a contractor who sews, with a new mobile, and returns his details.</summary>
    public static async Task<JsonElement> CreateContractorAsync(
        this HttpClient client,
        string? fullName = null,
        string? mobileNumber = null,
        string? iban = null,
        IReadOnlyList<Guid>? stageIds = null)
    {
        var response = await client.PostAsJsonAsync(
            Contractors,
            new
            {
                fullName = fullName ?? CatalogApi.UniqueName("حسن رضایی"),
                mobileNumber = mobileNumber ?? ApiFactory.NextMobile(),
                stageIds = stageIds ?? [await client.SeededStageAsync("دوخت")],
                iban,
            },
            Cancel);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        return await response.Body();
    }

    public static Guid Id(this JsonElement contractor) => contractor.GetProperty("id").GetGuid();

    public static List<string?> StageNames(this JsonElement contractor) =>
        contractor.GetProperty("stages").EnumerateArray().Select(stage => stage.GetProperty("name").GetString()).ToList();

    public static List<Guid> Ids(this JsonElement page) =>
        page.GetProperty("items").EnumerateArray().Select(item => item.GetProperty("id").GetGuid()).ToList();
}
