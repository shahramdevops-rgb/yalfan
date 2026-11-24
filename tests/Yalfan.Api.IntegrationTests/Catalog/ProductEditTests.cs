using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Yalfan.Api.IntegrationTests.Parties;
using Yalfan.Api.IntegrationTests.Production;
using Yalfan.Domain.Audit;
using Yalfan.Domain.Identity;
using Yalfan.Infrastructure.Persistence;
using static Yalfan.Api.IntegrationTests.Auth.AuthTestHelpers;

namespace Yalfan.Api.IntegrationTests.Catalog;

/// <summary>
/// Changing a model from its page (task 1.6.2, ADR 0023): its name, description and category,
/// its switch and its bags' switches, more bags, and its sizes. Each test changes only models it
/// made itself, because the tests of a class share one database.
/// </summary>
public sealed class ProductEditTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private const string Products = "/panel/catalog/products";

    private static readonly string[] Small = ["کوچک"];
    private static readonly string[] SmallAndLarge = ["کوچک", "بزرگ"];
    private static readonly string[] ThreeSizesOutOfOrder = ["کوچک", "بزرگ", "متوسط"];

    [Fact]
    public async Task A_model_is_renamed_described_and_moved_to_another_category_and_keeps_its_number()
    {
        using var client = await factory.CreateClientAsAsync(Role.Operator);
        var (id, number) = await client.CreateProductAsync(new
        {
            name = CatalogApi.UniqueName("کیف"),
            description = "قدیمی",
            categoryId = await client.CreateCategoryAsync(),
        });
        var category = await client.CreateCategoryAsync();
        var name = CatalogApi.UniqueName("کیف کمری");

        var saved = await client.PutAsJsonAsync($"{Products}/{id}", new { name, description = "دو جیب", categoryId = category }, Cancel);

        Assert.Equal(HttpStatusCode.OK, saved.StatusCode);
        var product = await (await client.GetAsync($"{Products}/{id}", Cancel)).Body();
        Assert.Equal(number, product.GetProperty("number").GetInt32());
        Assert.Equal(name, product.GetProperty("name").GetString());
        Assert.Equal("دو جیب", product.GetProperty("description").GetString());
        Assert.Equal(category, product.GetProperty("category").GetProperty("id").GetGuid());
        Assert.True(product.GetProperty("category").GetProperty("isActive").GetBoolean());
    }

    [Fact]
    public async Task A_model_cannot_take_another_models_name_but_may_keep_its_own()
    {
        using var client = await factory.CreateClientAsAsync(Role.Operator);
        var suffix = CatalogApi.UniqueName(string.Empty).Trim();
        await client.CreateProductAsync(new { name = $"کیف کمری {suffix}" });
        var (other, _) = await client.CreateProductAsync(new { name = $"کیف دوشی {suffix}" });

        var taken = await client.PutAsJsonAsync($"{Products}/{other}", new { name = $"كيف  كمری {suffix}" }, Cancel);
        var own = await client.PutAsJsonAsync($"{Products}/{other}", new { name = $"کیف دوشی {suffix}", description = "تازه" }, Cancel);

        Assert.Equal(HttpStatusCode.Conflict, taken.StatusCode);
        Assert.Equal("catalog.name_taken", await taken.ErrorCode());
        Assert.Equal(HttpStatusCode.OK, own.StatusCode);
        Assert.Equal("تازه", (await own.Body()).GetProperty("description").GetString());
    }

    [Fact]
    public async Task A_kept_inactive_category_saves_but_a_newly_chosen_one_is_refused()
    {
        using var client = await factory.CreateClientAsAsync(Role.Operator);
        var category = await client.CreateCategoryAsync();
        var name = CatalogApi.UniqueName("کوله");
        var (id, _) = await client.CreateProductAsync(new { name, categoryId = category });
        var otherCategory = await client.CreateCategoryAsync();
        await client.DeactivateAsync($"/panel/catalog/categories/{category}");
        await client.DeactivateAsync($"/panel/catalog/categories/{otherCategory}");

        var kept = await client.PutAsJsonAsync($"{Products}/{id}", new { name, description = "تازه", categoryId = category }, Cancel);
        var chosen = await client.PutAsJsonAsync($"{Products}/{id}", new { name, categoryId = otherCategory }, Cancel);
        var missing = await client.PutAsJsonAsync($"{Products}/{id}", new { name, categoryId = Guid.CreateVersion7() }, Cancel);

        Assert.Equal(HttpStatusCode.OK, kept.StatusCode);
        Assert.False((await kept.Body()).GetProperty("category").GetProperty("isActive").GetBoolean());
        Assert.Equal(HttpStatusCode.UnprocessableEntity, chosen.StatusCode);
        Assert.Equal("catalog.category_inactive", await chosen.ErrorCode());
        Assert.Equal("catalog.category_not_found", await missing.ErrorCode());
    }

    [Fact]
    public async Task A_deactivated_model_takes_no_new_bag_size_or_rate_until_it_is_activated()
    {
        using var client = await factory.CreateClientAsAsync(Role.Operator);
        var fabric = await client.CreateFabricAsync();
        var (id, _) = await client.CreateProductAsync(new
        {
            name = CatalogApi.UniqueName("کیف پول"),
            variants = new[] { new { fabricId = fabric, colourId = await client.CreateColourAsync() } },
        });
        var sewing = await client.SeededStageAsync("دوخت");

        var deactivated = await client.PostAsync($"{Products}/{id}/deactivate", null, Cancel);
        var bag = await AddBag(client, id, fabric, await client.CreateColourAsync());
        var size = await client.PostAsJsonAsync($"{Products}/{id}/sizes", new { name = "بزرگ" }, Cancel);
        var rate = await client.PostAsJsonAsync(
            "/panel/production/rates", new { productId = id, stageId = sewing, amount = 500_000, startsOn = "2026-12-01" }, Cancel);

        var details = await deactivated.Body();
        Assert.False(details.GetProperty("isActive").GetBoolean());
        Assert.True(Assert.Single(details.GetProperty("variants").EnumerateArray()).GetProperty("isActive").GetBoolean());
        Assert.Equal("catalog.product_inactive", await bag.ErrorCode());
        Assert.Equal(HttpStatusCode.UnprocessableEntity, size.StatusCode);
        Assert.Equal("catalog.product_inactive", await size.ErrorCode());
        Assert.Equal(HttpStatusCode.UnprocessableEntity, rate.StatusCode);
        Assert.Equal("production.product_inactive", await rate.ErrorCode());

        var activated = await client.PostAsync($"{Products}/{id}/activate", null, Cancel);
        Assert.True((await activated.Body()).GetProperty("isActive").GetBoolean());
        Assert.Equal(HttpStatusCode.OK, (await AddBag(client, id, fabric, await client.CreateColourAsync())).StatusCode);
    }

    [Fact]
    public async Task A_bag_is_switched_off_and_on_but_never_through_another_models_address()
    {
        using var client = await factory.CreateClientAsAsync(Role.Operator);
        var (id, _) = await client.CreateProductAsync(new
        {
            name = CatalogApi.UniqueName("کیف"),
            variants = new[] { new { fabricId = await client.CreateFabricAsync(), colourId = await client.CreateColourAsync() } },
        });
        var (other, _) = await client.CreateProductAsync(new { name = CatalogApi.UniqueName("کیف") });
        var bag = Assert.Single((await (await client.GetAsync($"{Products}/{id}", Cancel)).Body()).GetProperty("variants").EnumerateArray()).Id();

        var off = await client.PostAsync($"{Products}/{id}/variants/{bag}/deactivate", null, Cancel);
        var wrongModel = await client.PostAsync($"{Products}/{other}/variants/{bag}/activate", null, Cancel);
        var on = await client.PostAsync($"{Products}/{id}/variants/{bag}/activate", null, Cancel);

        Assert.False(Assert.Single((await off.Body()).GetProperty("variants").EnumerateArray()).GetProperty("isActive").GetBoolean());
        Assert.Equal(HttpStatusCode.NotFound, wrongModel.StatusCode);
        Assert.Equal("catalog.variant_not_found", await wrongModel.ErrorCode());
        Assert.True(Assert.Single((await on.Body()).GetProperty("variants").EnumerateArray()).GetProperty("isActive").GetBoolean());
    }

    [Fact]
    public async Task Several_bags_are_added_in_one_request_or_none_at_all()
    {
        using var client = await factory.CreateClientAsAsync(Role.Operator);
        var (id, number) = await client.CreateProductAsync(new { name = CatalogApi.UniqueName("کوله"), sizes = SmallAndLarge });
        var sizes = (await (await client.GetAsync($"{Products}/{id}", Cancel)).Body()).GetProperty("sizes").EnumerateArray().Select(s => s.Id()).ToList();
        var fabric = await client.CreateFabricAsync();
        var black = await client.CreateColourAsync();
        var brown = await client.CreateColourAsync();

        var added = await client.PostAsJsonAsync(
            $"{Products}/{id}/variants",
            new
            {
                variants = new[]
                {
                    new { fabricId = fabric, colourId = black, sizeId = sizes[0] },
                    new { fabricId = fabric, colourId = black, sizeId = sizes[1] },
                    new { fabricId = fabric, colourId = brown, sizeId = sizes[0] },
                },
            },
            Cancel);
        var halfDuplicate = await client.PostAsJsonAsync(
            $"{Products}/{id}/variants",
            new
            {
                variants = new[]
                {
                    new { fabricId = fabric, colourId = brown, sizeId = sizes[1] },
                    new { fabricId = fabric, colourId = black, sizeId = sizes[0] },
                },
            },
            Cancel);

        Assert.Equal(HttpStatusCode.OK, added.StatusCode);
        Assert.Equal(
            [$"{number}-1", $"{number}-2", $"{number}-3"],
            (await added.Body()).GetProperty("variants").EnumerateArray().Select(v => v.GetProperty("sku").GetString()));
        Assert.Equal(HttpStatusCode.Conflict, halfDuplicate.StatusCode);
        Assert.Equal("catalog.variant_exists", await halfDuplicate.ErrorCode());
        var product = await (await client.GetAsync($"{Products}/{id}", Cancel)).Body();
        Assert.Equal(3, product.GetProperty("variants").GetArrayLength());
    }

    [Fact]
    public async Task An_empty_request_adds_no_bag()
    {
        using var client = await factory.CreateClientAsAsync(Role.Operator);
        var (id, _) = await client.CreateProductAsync(new { name = CatalogApi.UniqueName("کیف") });

        var response = await client.PostAsJsonAsync($"{Products}/{id}/variants", new { variants = Array.Empty<object>() }, Cancel);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task The_first_size_of_a_model_becomes_the_size_of_its_bags_and_their_codes_stay()
    {
        using var client = await factory.CreateClientAsAsync(Role.Operator);
        var fabric = await client.CreateFabricAsync();
        var (id, number) = await client.CreateProductAsync(new
        {
            name = CatalogApi.UniqueName("کیف"),
            variants = new[]
            {
                new { fabricId = fabric, colourId = await client.CreateColourAsync() },
                new { fabricId = fabric, colourId = await client.CreateColourAsync() },
            },
        });

        var response = await client.PostAsJsonAsync($"{Products}/{id}/sizes", new { name = "متوسط" }, Cancel);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var product = await (await client.GetAsync($"{Products}/{id}", Cancel)).Body();
        var medium = Assert.Single(product.GetProperty("sizes").EnumerateArray());
        Assert.Equal("متوسط", medium.GetProperty("name").GetString());
        var variants = product.GetProperty("variants").EnumerateArray().ToList();
        Assert.Equal([$"{number}-1", $"{number}-2"], variants.Select(v => v.GetProperty("sku").GetString()));
        Assert.All(variants, v => Assert.Equal(medium.Id(), v.GetProperty("size").Id()));
    }

    [Fact]
    public async Task A_further_size_is_made_in_every_fabric_and_colour_still_made()
    {
        using var client = await factory.CreateClientAsAsync(Role.Operator);
        var fabric = await client.CreateFabricAsync();
        var laterInactiveFabric = await client.CreateFabricAsync();
        var black = await client.CreateColourAsync();
        var brown = await client.CreateColourAsync();
        var (id, number) = await client.CreateProductAsync(new
        {
            name = CatalogApi.UniqueName("کوله"),
            sizes = Small,
            variants = new[]
            {
                new { fabricId = fabric, colourId = black, size = "کوچک" },
                new { fabricId = fabric, colourId = brown, size = "کوچک" },
                new { fabricId = laterInactiveFabric, colourId = black, size = "کوچک" },
            },
        });
        var switchedOff = (await (await client.GetAsync($"{Products}/{id}", Cancel)).Body()).GetProperty("variants")[1].Id();
        await client.DeactivateAsync($"{Products}/{id}/variants/{switchedOff}");
        await client.DeactivateAsync($"/panel/catalog/fabrics/{laterInactiveFabric}");

        var response = await client.PostAsJsonAsync($"{Products}/{id}/sizes", new { name = "بزرگ" }, Cancel);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var product = await response.Body();
        Assert.Equal(["کوچک", "بزرگ"], product.GetProperty("sizes").EnumerateArray().Select(s => s.GetProperty("name").GetString()));
        var inLarge = Assert.Single(
            product.GetProperty("variants").EnumerateArray(),
            v => v.GetProperty("size").GetProperty("name").GetString() == "بزرگ");
        Assert.Equal($"{number}-4", inLarge.GetProperty("sku").GetString());
        Assert.Equal(fabric, inLarge.GetProperty("fabric").Id());
        Assert.Equal(black, inLarge.GetProperty("colour").Id());
    }

    [Fact]
    public async Task Sizes_are_renamed_and_dragged_into_a_new_order()
    {
        using var client = await factory.CreateClientAsAsync(Role.Operator);
        var (id, _) = await client.CreateProductAsync(new { name = CatalogApi.UniqueName("کوله"), sizes = ThreeSizesOutOfOrder });
        var (other, _) = await client.CreateProductAsync(new { name = CatalogApi.UniqueName("کوله"), sizes = Small });
        var sizes = (await (await client.GetAsync($"{Products}/{id}", Cancel)).Body()).GetProperty("sizes").EnumerateArray().Select(s => s.Id()).ToList();
        var otherSize = Assert.Single((await (await client.GetAsync($"{Products}/{other}", Cancel)).Body()).GetProperty("sizes").EnumerateArray()).Id();

        // A swap of two neighbours: it would fail half way with a unique index on the position.
        var reordered = await client.PutAsJsonAsync($"{Products}/{id}/sizes/order", new { sizeIds = new[] { sizes[0], sizes[2], sizes[1] } }, Cancel);
        var renamed = await client.PutAsJsonAsync($"{Products}/{id}/sizes/{sizes[0]}", new { name = "کوچک‌تر" }, Cancel);
        var duplicate = await client.PutAsJsonAsync($"{Products}/{id}/sizes/{sizes[0]}", new { name = "بزرگ" }, Cancel);
        var outdated = await client.PutAsJsonAsync($"{Products}/{id}/sizes/order", new { sizeIds = new[] { sizes[1], sizes[0] } }, Cancel);
        var wrongModel = await client.PutAsJsonAsync($"{Products}/{id}/sizes/{otherSize}", new { name = "خرد" }, Cancel);

        Assert.Equal(HttpStatusCode.OK, reordered.StatusCode);
        Assert.Equal(HttpStatusCode.OK, renamed.StatusCode);
        Assert.Equal(
            ["کوچک‌تر", "متوسط", "بزرگ"],
            (await renamed.Body()).GetProperty("sizes").EnumerateArray().Select(s => s.GetProperty("name").GetString()));
        Assert.Equal(HttpStatusCode.BadRequest, duplicate.StatusCode);
        Assert.Equal("catalog.size_name_duplicate", await duplicate.ErrorCode());
        Assert.Equal(HttpStatusCode.Conflict, outdated.StatusCode);
        Assert.Equal("catalog.size_order_outdated", await outdated.ErrorCode());
        Assert.Equal(HttpStatusCode.NotFound, wrongModel.StatusCode);
        Assert.Equal("catalog.size_not_found", await wrongModel.ErrorCode());
    }

    [Fact]
    public async Task A_renamed_model_and_a_bag_given_its_first_size_are_audited_with_old_and_new_values()
    {
        using var client = factory.CreateBrowserClient();
        var operatorSession = await factory.LoginAsync(client, await factory.CreateUserAsync(Role.Operator));
        var oldName = CatalogApi.UniqueName("کیف");
        var newName = CatalogApi.UniqueName("کیف");
        var (id, _) = await client.CreateProductAsync(new
        {
            name = oldName,
            variants = new[] { new { fabricId = await client.CreateFabricAsync(), colourId = await client.CreateColourAsync() } },
        });

        await client.PutAsJsonAsync($"{Products}/{id}", new { name = newName }, Cancel);
        var sized = await (await client.PostAsJsonAsync($"{Products}/{id}/sizes", new { name = "بزرگ" }, Cancel)).Body();
        var bag = Assert.Single(sized.GetProperty("variants").EnumerateArray());

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var translationId = $"{id},fa";
        var bagId = bag.Id().ToString();
        var updates = await db.AuditEntries.AsNoTracking()
            .Where(e => e.Action == AuditAction.Updated && e.UserId == operatorSession.User.Id)
            .Where(e => (e.EntityType == "product_translations" && e.EntityId == translationId)
                || (e.EntityType == "product_variants" && e.EntityId == bagId))
            .ToListAsync(Cancel);

        var name = Changes(updates.Single(e => e.EntityType == "product_translations")).GetProperty("name");
        Assert.Equal(oldName, name.GetProperty("old").GetString());
        Assert.Equal(newName, name.GetProperty("new").GetString());
        var size = Changes(updates.Single(e => e.EntityType == "product_variants")).GetProperty("sizeId");
        Assert.Equal(JsonValueKind.Null, size.GetProperty("old").ValueKind);
        Assert.Equal(bag.GetProperty("size").Id(), size.GetProperty("new").GetGuid());
    }

    [Fact]
    public async Task An_anonymous_caller_cannot_change_a_model()
    {
        using var operatorClient = await factory.CreateClientAsAsync(Role.Operator);
        var name = CatalogApi.UniqueName("کیف");
        var (id, _) = await operatorClient.CreateProductAsync(new { name });
        using var anonymous = factory.CreateBrowserClient();

        var renamed = await anonymous.PutAsJsonAsync($"{Products}/{id}", new { name = CatalogApi.UniqueName("کیف") }, Cancel);
        var deactivated = await anonymous.PostAsync($"{Products}/{id}/deactivate", null, Cancel);

        Assert.Equal(HttpStatusCode.Unauthorized, renamed.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, deactivated.StatusCode);
        var product = await (await operatorClient.GetAsync($"{Products}/{id}", Cancel)).Body();
        Assert.Equal(name, product.GetProperty("name").GetString());
        Assert.True(product.GetProperty("isActive").GetBoolean());
    }

    private static Task<HttpResponseMessage> AddBag(HttpClient client, Guid product, Guid fabric, Guid colour) =>
        client.PostAsJsonAsync(
            $"{Products}/{product}/variants", new { variants = new[] { new { fabricId = fabric, colourId = colour } } }, Cancel);

    private static JsonElement Changes(AuditEntry entry)
    {
        using var document = JsonDocument.Parse(entry.Changes);
        return document.RootElement.Clone();
    }
}
