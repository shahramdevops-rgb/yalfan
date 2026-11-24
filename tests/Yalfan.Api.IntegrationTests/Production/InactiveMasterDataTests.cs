using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Yalfan.Api.IntegrationTests.Catalog;
using Yalfan.Api.IntegrationTests.Parties;
using Yalfan.Domain.Identity;
using static Yalfan.Api.IntegrationTests.Auth.AuthTestHelpers;

namespace Yalfan.Api.IntegrationTests.Production;

/// <summary>
/// What a deactivated category, fabric, colour or stage means (ADR 0022): nothing new points at
/// it, and what already pointed at it keeps it. Each test deactivates only rows it made itself,
/// because the tests of a class share one database.
/// </summary>
public sealed class InactiveMasterDataTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task A_new_model_in_an_inactive_category_fabric_or_colour_is_refused()
    {
        using var client = await factory.CreateClientAsAsync(Role.Operator);
        var category = await client.CreateCategoryAsync();
        var fabric = await client.CreateFabricAsync();
        var colour = await client.CreateColourAsync();
        var inactiveCategory = await client.CreateCategoryAsync();
        var inactiveFabric = await client.CreateFabricAsync();
        var inactiveColour = await client.CreateColourAsync();
        await client.DeactivateAsync($"/panel/catalog/categories/{inactiveCategory}");
        await client.DeactivateAsync($"/panel/catalog/fabrics/{inactiveFabric}");
        await client.DeactivateAsync($"/panel/catalog/colours/{inactiveColour}");

        var inCategory = await CreateModel(client, inactiveCategory, fabric, colour);
        var inFabric = await CreateModel(client, category, inactiveFabric, colour);
        var inColour = await CreateModel(client, category, fabric, inactiveColour);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, inCategory.StatusCode);
        Assert.Equal("catalog.category_inactive", await inCategory.ErrorCode());
        Assert.Equal(HttpStatusCode.UnprocessableEntity, inFabric.StatusCode);
        Assert.Equal("catalog.fabric_inactive", await inFabric.ErrorCode());
        Assert.Equal(HttpStatusCode.UnprocessableEntity, inColour.StatusCode);
        Assert.Equal("catalog.colour_inactive", await inColour.ErrorCode());
        Assert.Equal(HttpStatusCode.OK, (await CreateModel(client, category, fabric, colour)).StatusCode);
    }

    [Fact]
    public async Task A_variant_already_in_a_colour_stays_when_the_colour_is_deactivated_but_no_new_one_is_added()
    {
        using var client = await factory.CreateClientAsAsync(Role.Operator);
        var fabric = await client.CreateFabricAsync();
        var otherFabric = await client.CreateFabricAsync();
        var colour = await client.CreateColourAsync();
        var (product, _) = await client.CreateProductAsync(new
        {
            name = CatalogApi.UniqueName("کیف پول"),
            variants = new[] { new { fabricId = fabric, colourId = colour } },
        });
        await client.DeactivateAsync($"/panel/catalog/colours/{colour}");

        var added = await client.PostAsJsonAsync(
            $"/panel/catalog/products/{product}/variants", new { fabricId = otherFabric, colourId = colour }, Cancel);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, added.StatusCode);
        Assert.Equal("catalog.colour_inactive", await added.ErrorCode());
        var details = await (await client.GetAsync($"/panel/catalog/products/{product}", Cancel)).Body();
        var variant = Assert.Single(details.GetProperty("variants").EnumerateArray());
        Assert.Equal(colour, variant.GetProperty("colour").GetProperty("id").GetGuid());
    }

    [Fact]
    public async Task A_contractor_or_worker_cannot_newly_tick_an_inactive_stage()
    {
        using var client = await factory.CreateClientAsAsync(Role.Operator);
        var stage = await client.CreateStageAsync(CatalogApi.UniqueName("دوخت جیب"));
        await client.DeactivateAsync($"/panel/production/stages/{stage}");
        var sewing = await client.SeededStageAsync("دوخت");
        var contractor = await client.CreateContractorAsync(stageIds: [sewing]);

        var newContractor = await client.PostAsJsonAsync(
            ContractorApi.Contractors,
            new { fullName = CatalogApi.UniqueName("علی محمدی"), mobileNumber = ApiFactory.NextMobile(), stageIds = new[] { stage } },
            Cancel);
        var newWorker = await client.PostAsJsonAsync(
            WorkerApi.Workers,
            new
            {
                fullName = CatalogApi.UniqueName("مریم احمدی"),
                mobileNumber = ApiFactory.NextMobile(),
                stageIds = new[] { stage },
                startDate = WorkerApi.StartDate,
            },
            Cancel);
        var ticked = await SaveContractor(client, contractor, [sewing, stage]);

        foreach (var response in new[] { newContractor, newWorker, ticked })
        {
            Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
            Assert.Equal("parties.stage_inactive", await response.ErrorCode());
        }
    }

    [Fact]
    public async Task A_stage_ticked_before_it_was_deactivated_stays_ticked_and_the_profile_still_saves()
    {
        using var client = await factory.CreateClientAsAsync(Role.Operator);
        var stage = await client.CreateStageAsync(CatalogApi.UniqueName("دوخت زیپ"));
        var contractor = await client.CreateContractorAsync(stageIds: [stage]);
        await client.DeactivateAsync($"/panel/production/stages/{stage}");

        var saved = await SaveContractor(client, contractor, [stage]);

        Assert.Equal(HttpStatusCode.OK, saved.StatusCode);
        var ticked = Assert.Single((await saved.Body()).GetProperty("stages").EnumerateArray());
        Assert.Equal(stage, ticked.GetProperty("id").GetGuid());
        Assert.False(ticked.GetProperty("isActive").GetBoolean());
    }

    [Fact]
    public async Task An_inactive_stage_leaves_the_rate_card_takes_no_new_rate_and_keeps_its_history()
    {
        using var client = await factory.CreateClientAsAsync(Role.Operator);
        var (product, _) = await client.CreateProductAsync(new { name = CatalogApi.UniqueName("کیف مدرسه") });
        var stage = await client.CreateStageAsync(CatalogApi.UniqueName("دوخت بند"));
        await client.AddRateAsync(product, stage, 500_000, "2026-01-01");
        await client.DeactivateAsync($"/panel/production/stages/{stage}");

        var card = await (await client.GetAsync($"/panel/production/rates?productId={product}", Cancel)).Body();
        var newRate = await client.PostAsJsonAsync(
            "/panel/production/rates", new { productId = product, stageId = stage, amount = 600_000, startsOn = "2026-12-01" }, Cancel);
        var history = await (await client.GetAsync($"/panel/production/rates/history?productId={product}&stageId={stage}", Cancel)).Body();

        Assert.DoesNotContain(card.GetProperty("stages").EnumerateArray(), s => s.GetProperty("stage").GetProperty("id").GetGuid() == stage);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, newRate.StatusCode);
        Assert.Equal("production.stage_inactive", await newRate.ErrorCode());
        var entry = Assert.Single(history.EnumerateArray());
        Assert.Equal(500_000, entry.GetProperty("amount").GetInt64());
        Assert.False(entry.GetProperty("stage").GetProperty("isActive").GetBoolean());
    }

    private static Task<HttpResponseMessage> CreateModel(HttpClient client, Guid category, Guid fabric, Guid colour) =>
        client.PostAsJsonAsync(
            "/panel/catalog/products",
            new
            {
                name = CatalogApi.UniqueName("کوله سفری"),
                categoryId = category,
                variants = new[] { new { fabricId = fabric, colourId = colour } },
            },
            Cancel);

    private static Task<HttpResponseMessage> SaveContractor(HttpClient client, JsonElement contractor, Guid[] stageIds) =>
        client.PutAsJsonAsync(
            $"{ContractorApi.Contractors}/{contractor.Id()}",
            new
            {
                fullName = contractor.GetProperty("fullName").GetString(),
                mobileNumber = contractor.GetProperty("mobileNumber").GetString(),
                stageIds,
            },
            Cancel);
}

internal static class ActiveFlagApi
{
    /// <summary>Deactivates the row at this path; fails the test if it is refused.</summary>
    public static async Task DeactivateAsync(this HttpClient client, string path)
    {
        var response = await client.PostAsync($"{path}/deactivate", null, Cancel);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
