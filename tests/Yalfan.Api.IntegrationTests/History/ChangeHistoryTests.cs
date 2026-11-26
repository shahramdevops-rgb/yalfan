using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Yalfan.Api.IntegrationTests.Catalog;
using Yalfan.Api.IntegrationTests.Parties;
using Yalfan.Application.History;
using Yalfan.Domain.Audit;
using Yalfan.Domain.Identity;
using Yalfan.Infrastructure.Persistence;
using static Yalfan.Api.IntegrationTests.Auth.AuthTestHelpers;
using static Yalfan.Api.IntegrationTests.Parties.ContractorApi;
using static Yalfan.Api.IntegrationTests.Parties.WorkerApi;

namespace Yalfan.Api.IntegrationTests.History;

/// <summary>
/// The owner's change history of a model, a contractor and a worker (task 1.6.6, ADR 0027): one
/// block per save, newest first, names instead of ids, an IBAN with no number, and owner only.
/// The test clock stands still, so each change moves it on a second, as real saves would be.
/// </summary>
public sealed class ChangeHistoryTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private const string Products = "/panel/catalog/products";
    private const string History = "/panel/history";

    /// <summary>The account-number part of <see cref="ContractorApi.Iban"/>, which must appear nowhere.</summary>
    private const string IbanDigits = "0570029971601460641001";

    private static readonly string[] Small = ["کوچک"];

    [Fact]
    public async Task A_model_history_shows_each_save_newest_first_with_names_instead_of_ids()
    {
        using var client = await factory.CreateClientAsAsync(Role.Operator);
        var fabricName = CatalogApi.UniqueName("برزنت");
        var colourName = CatalogApi.UniqueName("مشکی");
        var fabric = await client.CreateFabricAsync(fabricName);
        var colour = await client.CreateColourAsync(colourName);
        var firstCategory = CatalogApi.UniqueName("کوله");
        var secondCategory = CatalogApi.UniqueName("کیف دستی");
        var name = CatalogApi.UniqueName("کیف");
        var (id, number) = await client.CreateProductAsync(new
        {
            name,
            categoryId = await client.CreateCategoryAsync(firstCategory),
            sizes = Small,
            variants = new[] { new { fabricId = fabric, colourId = colour, size = "کوچک" } },
        });
        factory.Time.Advance(TimeSpan.FromSeconds(1));
        var bag = Assert.Single((await (await client.GetAsync($"{Products}/{id}", Cancel)).Body()).GetProperty("variants").EnumerateArray());
        var newName = CatalogApi.UniqueName("کیف کمری");
        var category = await client.CreateCategoryAsync(secondCategory);

        await Step(client.PutAsJsonAsync($"{Products}/{id}", new { name = newName, categoryId = category }, Cancel));
        await Step(client.PostAsync($"{Products}/{id}/variants/{bag.GetProperty("id").GetGuid()}/deactivate", null, Cancel));
        await Step(client.PostAsJsonAsync($"{Products}/{id}/sizes", new { name = "بزرگ" }, Cancel));
        await Step(client.PostAsJsonAsync(
            "/panel/production/rates",
            new { productId = id, stageId = await client.SeededStageAsync("برش"), amount = 1_200_000, startsOn = "2026-12-01" },
            Cancel));

        using var owner = await OwnerAsync();
        var response = await owner.GetAsync($"{History}/models/{id}", Cancel);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var text = await response.Content.ReadAsStringAsync(Cancel);
        var page = await response.Body();
        Assert.Equal(JsonValueKind.Null, page.GetProperty("before").ValueKind);
        var saves = page.GetProperty("saves").EnumerateArray().ToList();
        Assert.Equal(5, saves.Count);
        Assert.All(saves, save => Assert.Equal("Test Operator", save.GetProperty("by").GetString()));
        Assert.True(saves.Select(save => save.GetProperty("at").GetDateTimeOffset()).SequenceEqual(
            saves.Select(save => save.GetProperty("at").GetDateTimeOffset()).OrderDescending()));

        var rate = Assert.Single(Changes(saves[0]));
        Assert.Equal(new Head("rate", "created", "برش"), HeadOf(rate));
        Assert.Equal([new Field("amount", null, "1200000"), new Field("startsOn", null, "2026-12-01")], Fields(rate));

        var size = Assert.Single(Changes(saves[1]));
        Assert.Equal(new Head("size", "created", "بزرگ"), HeadOf(size));
        Assert.Equal([new Field("name", null, "بزرگ")], Fields(size));

        var off = Assert.Single(Changes(saves[2]));
        Assert.Equal(new Head("bag", "updated", $"{number}-1"), HeadOf(off));
        Assert.Equal([new Field("active", "true", "false")], Fields(off));

        var renamed = Assert.Single(Changes(saves[3]));
        Assert.Equal(new Head("model", "updated", null), HeadOf(renamed));
        // In the history's own order, not the audit's: name, description, category.
        Assert.Equal([new Field("name", name, newName), new Field("category", firstCategory, secondCategory)], Fields(renamed));

        var created = Changes(saves[4]);
        Assert.Equal([new Head("model", "created", null), new Head("size", "created", "کوچک"), new Head("bag", "created", $"{number}-1")], created.Select(HeadOf));
        Assert.Equal([new Field("name", null, name), new Field("category", null, firstCategory)], Fields(created[0]));
        Assert.Equal(
            [new Field("fabric", null, fabricName), new Field("colour", null, colourName), new Field("size", null, "کوچک")],
            Fields(created[2]));

        Assert.DoesNotContain(fabric.ToString(), text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(category.ToString(), text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task A_contractor_history_shows_his_stages_and_the_iban_only_as_set_replaced_or_removed()
    {
        using var client = await factory.CreateClientAsAsync(Role.Operator);
        var sewing = await client.SeededStageAsync("دوخت");
        var packing = await client.SeededStageAsync("بسته‌بندی");
        var contractor = await client.CreateContractorAsync(iban: OtherIban, stageIds: [sewing]);
        var id = contractor.Id();
        var mobile = contractor.GetProperty("mobileNumber").GetString();
        var fullName = contractor.GetProperty("fullName").GetString();
        factory.Time.Advance(TimeSpan.FromSeconds(1));

        await Step(client.PutAsJsonAsync(
            $"{Contractors}/{id}", new { fullName, mobileNumber = mobile, stageIds = new[] { packing } }, Cancel));
        await Step(client.PutAsJsonAsync($"{Contractors}/{id}/iban", new { iban = Iban }, Cancel));
        await Step(client.DeleteAsync($"{Contractors}/{id}/iban", Cancel));
        await Step(client.PutAsJsonAsync($"{Contractors}/{id}/iban", new { iban = Iban }, Cancel));

        using var owner = await OwnerAsync();
        var response = await owner.GetAsync($"{History}/contractors/{id}", Cancel);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var saves = (await response.Body()).GetProperty("saves").EnumerateArray().ToList();
        Assert.Equal(5, saves.Count);
        Assert.Equal([new Field("iban", "false", "true")], Fields(Assert.Single(Changes(saves[0]))));
        Assert.Equal([new Field("iban", "true", "false")], Fields(Assert.Single(Changes(saves[1]))));
        Assert.Equal([new Field("iban", "true", "true")], Fields(Assert.Single(Changes(saves[2]))));

        // In the order of the work: sewing before packing.
        Assert.Equal([new Head("stage", "deleted", "دوخت"), new Head("stage", "created", "بسته‌بندی")], Changes(saves[3]).Select(HeadOf));

        var created = Changes(saves[4]);
        Assert.Equal([new Head("contractor", "created", null), new Head("stage", "created", "دوخت")], created.Select(HeadOf));
        Assert.Equal(
            [new Field("fullName", null, fullName), new Field("mobileNumber", null, mobile), new Field("iban", null, "true")],
            Fields(created[0]));

        Assert.DoesNotContain(IbanDigits[^8..], await response.Content.ReadAsStringAsync(Cancel), StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_worker_history_shows_a_new_start_date_and_a_worker_added_without_an_iban_has_none()
    {
        using var client = await factory.CreateClientAsAsync(Role.Operator);
        var worker = await client.CreateWorkerAsync();
        var id = worker.GetProperty("id").GetGuid();
        factory.Time.Advance(TimeSpan.FromSeconds(1));

        await Step(client.PutAsJsonAsync(
            $"{Workers}/{id}",
            new
            {
                fullName = worker.GetProperty("fullName").GetString(),
                mobileNumber = worker.GetProperty("mobileNumber").GetString(),
                stageIds = new[] { await client.SeededStageAsync("برش") },
                startDate = "2019-09-23",
            },
            Cancel));

        using var owner = await OwnerAsync();
        var saves = (await (await owner.GetAsync($"{History}/workers/{id}", Cancel)).Body()).GetProperty("saves").EnumerateArray().ToList();

        Assert.Equal(2, saves.Count);
        Assert.Equal([new Field("startDate", StartDate, "2019-09-23")], Fields(Assert.Single(Changes(saves[0]))));
        var created = Changes(saves[1]);
        Assert.Equal([new Head("worker", "created", null), new Head("stage", "created", "برش")], created.Select(HeadOf));
        Assert.DoesNotContain(Fields(created[0]), field => field.Name is "iban" or "active" or "notes");
        Assert.Contains(new Field("startDate", null, StartDate), Fields(created[0]));
    }

    [Fact]
    public async Task A_long_history_is_read_twenty_saves_at_a_time()
    {
        using var client = await factory.CreateClientAsAsync(Role.Operator);
        var (id, _) = await client.CreateProductAsync(new { name = CatalogApi.UniqueName("کیف") });
        factory.Time.Advance(TimeSpan.FromSeconds(1));
        for (var change = 0; change < 24; change++)
        {
            await Step(client.PostAsync($"{Products}/{id}/{(change % 2 == 0 ? "deactivate" : "activate")}", null, Cancel));
        }

        using var owner = await OwnerAsync();
        var first = await (await owner.GetAsync($"{History}/models/{id}", Cancel)).Body();
        var before = first.GetProperty("before").GetString();
        var second = await (await owner.GetAsync($"{History}/models/{id}?before={Uri.EscapeDataString(before!)}", Cancel)).Body();

        var firstSaves = first.GetProperty("saves").EnumerateArray().Select(save => save.GetProperty("at").GetDateTimeOffset()).ToList();
        var secondSaves = second.GetProperty("saves").EnumerateArray().Select(save => save.GetProperty("at").GetDateTimeOffset()).ToList();
        Assert.Equal(20, firstSaves.Count);
        Assert.Equal(5, secondSaves.Count);
        Assert.True(firstSaves.Min() > secondSaves.Max());
        Assert.Equal(JsonValueKind.Null, second.GetProperty("before").ValueKind);
        Assert.Equal("created", Changes(second.GetProperty("saves").EnumerateArray().Last())[0].GetProperty("action").GetString());
    }

    [Theory]
    [InlineData(Role.Manager)]
    [InlineData(Role.Operator)]
    public async Task Only_the_owner_reads_a_history(Role role)
    {
        using var client = await factory.CreateClientAsAsync(role);
        var (model, _) = await client.CreateProductAsync(new { name = CatalogApi.UniqueName("کیف") });
        var contractor = (await client.CreateContractorAsync()).Id();
        var worker = (await client.CreateWorkerAsync()).GetProperty("id").GetGuid();

        foreach (var path in new[] { $"models/{model}", $"contractors/{contractor}", $"workers/{worker}" })
        {
            var response = await client.GetAsync($"{History}/{path}", Cancel);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
    }

    [Theory]
    [InlineData("models", "catalog.product_not_found")]
    [InlineData("contractors", "parties.contractor_not_found")]
    [InlineData("workers", "parties.worker_not_found")]
    public async Task The_history_of_a_record_that_does_not_exist_is_not_found(string kind, string code)
    {
        using var owner = await OwnerAsync();

        var response = await owner.GetAsync($"{History}/{kind}/{Guid.NewGuid()}", Cancel);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(code, await response.ErrorCode());
    }

    [Fact]
    public async Task Every_field_the_audit_records_for_a_history_is_known_to_it()
    {
        // A created row records every field it has, so creating one of each kind shows them all.
        using var client = await factory.CreateClientAsAsync(Role.Operator);
        var (product, _) = await client.CreateProductAsync(new
        {
            name = CatalogApi.UniqueName("کیف"),
            description = "دو جیب",
            categoryId = await client.CreateCategoryAsync(),
            sizes = Small,
            variants = new[] { new { fabricId = await client.CreateFabricAsync(), colourId = await client.CreateColourAsync(), size = "کوچک" } },
        });
        await Step(client.PostAsJsonAsync(
            "/panel/production/rates",
            new { productId = product, stageId = await client.SeededStageAsync("برش"), amount = 10_000, startsOn = "2026-01-01" },
            Cancel));
        await client.CreateContractorAsync(iban: Iban);
        await client.CreateWorkerAsync(iban: Iban);

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var tables = HistoryVocabulary.Tables.Keys.ToList();
        var entries = await db.AuditEntries.AsNoTracking()
            .Where(entry => tables.Contains(entry.EntityType) && entry.Action == AuditAction.Created)
            .ToListAsync(Cancel);

        Assert.Equal(tables.Order(StringComparer.Ordinal), entries.Select(entry => entry.EntityType).Distinct().Order(StringComparer.Ordinal));
        foreach (var entry in entries)
        {
            using var changes = JsonDocument.Parse(entry.Changes);
            var known = HistoryVocabulary.Tables[entry.EntityType].Fields;
            Assert.All(changes.RootElement.EnumerateObject(), field => Assert.True(
                known.ContainsKey(field.Name),
                $"{entry.EntityType}.{field.Name} is recorded by the audit but not named in HistoryVocabulary."));
        }
    }

    /// <summary>Sends one change and moves the clock on, so the next change is a save of its own.</summary>
    private async Task Step(Task<HttpResponseMessage> change)
    {
        using var response = await change;
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        factory.Time.Advance(TimeSpan.FromSeconds(1));
    }

    /// <summary>An owner account of its own, as the configuration would add one (ADR 0026).</summary>
    private async Task<HttpClient> OwnerAsync()
    {
        var mobile = ApiFactory.NextMobile();
        await factory.CreateOwnerAccountAsync(mobile);
        var client = factory.CreateBrowserClient();
        await factory.LoginAsync(client, mobile);
        return client;
    }

    private static List<JsonElement> Changes(JsonElement save) => save.GetProperty("changes").EnumerateArray().ToList();

    private static Head HeadOf(JsonElement change) =>
        new(change.GetProperty("part").GetString(), change.GetProperty("action").GetString(), change.GetProperty("label").GetString());

    private static List<Field> Fields(JsonElement change) =>
        change.GetProperty("fields").EnumerateArray()
            .Select(field => new Field(field.GetProperty("name").GetString(), field.GetProperty("old").GetString(), field.GetProperty("new").GetString()))
            .ToList();

    /// <summary>What a change is about: its part, its action and its label.</summary>
    private sealed record Head(string? Part, string? Action, string? Label);

    private sealed record Field(string? Name, string? Old, string? New);
}
