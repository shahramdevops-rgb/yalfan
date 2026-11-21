using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Yalfan.Domain.Audit;
using Yalfan.Domain.Identity;
using Yalfan.Infrastructure.Persistence;
using static Yalfan.Api.IntegrationTests.Auth.AuthTestHelpers;

namespace Yalfan.Api.IntegrationTests.Catalog;

public sealed class ProductTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private static readonly string[] ThreeSizes = ["کوچک", "متوسط", "بزرگ"];
    private static readonly string[] TwoSizes = ["کوچک", "بزرگ"];

    /// <summary>The roadmap's check for task 1.1, entered by the operator, who enters the models.</summary>
    [Fact]
    public async Task A_model_with_three_colours_is_created_and_read_back()
    {
        using var client = await factory.CreateClientAsAsync(Role.Operator);
        var category = await client.CreateCategoryAsync();
        var fabric = await client.CreateFabricAsync();
        Guid[] colours = [await client.CreateColourAsync(), await client.CreateColourAsync(), await client.CreateColourAsync()];
        var name = CatalogApi.UniqueName("کیف دوشی");

        var (id, number) = await client.CreateProductAsync(new
        {
            name,
            description = "دسته‌ی چرمی",
            categoryId = category,
            variants = colours.Select(colour => new { fabricId = fabric, colourId = colour }),
        });

        var product = await (await client.GetAsync($"/panel/catalog/products/{id}", Cancel)).Body();
        Assert.Equal(number, product.GetProperty("number").GetInt32());
        Assert.Equal(name, product.GetProperty("name").GetString());
        Assert.Equal("دسته‌ی چرمی", product.GetProperty("description").GetString());
        Assert.Equal(category, product.GetProperty("category").GetProperty("id").GetGuid());
        Assert.True(product.GetProperty("isActive").GetBoolean());
        Assert.Empty(product.GetProperty("sizes").EnumerateArray());

        var variants = product.GetProperty("variants").EnumerateArray().ToList();
        Assert.Equal([$"{number}-1", $"{number}-2", $"{number}-3"], variants.Select(v => v.GetProperty("sku").GetString()));
        Assert.Equal(colours, variants.Select(v => v.GetProperty("colour").GetProperty("id").GetGuid()));
        Assert.All(variants, v =>
        {
            Assert.Equal(fabric, v.GetProperty("fabric").GetProperty("id").GetGuid());
            Assert.StartsWith("برزنت", v.GetProperty("fabric").GetProperty("name").GetString(), StringComparison.Ordinal);
            Assert.Equal(JsonValueKind.Null, v.GetProperty("size").ValueKind);
        });
    }

    [Fact]
    public async Task Models_are_numbered_one_after_another_from_101()
    {
        using var client = await factory.CreateClientAsAsync(Role.Operator);

        var first = await client.CreateProductAsync(new { name = CatalogApi.UniqueName("کیف") });
        var second = await client.CreateProductAsync(new { name = CatalogApi.UniqueName("کیف") });

        Assert.True(first.Number >= 101);
        Assert.Equal(first.Number + 1, second.Number);
    }

    [Fact]
    public async Task A_model_name_is_used_once_even_when_typed_on_an_arabic_keyboard()
    {
        using var client = await factory.CreateClientAsAsync(Role.Operator);
        var suffix = CatalogApi.UniqueName(string.Empty).Trim();
        await client.CreateProductAsync(new { name = $"کیف کمری {suffix}" });

        var response = await client.PostAsJsonAsync("/panel/catalog/products", new { name = $"كيف  كمری {suffix}" }, Cancel);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("catalog.name_taken", await response.ErrorCode());
    }

    [Fact]
    public async Task A_model_with_sizes_has_a_size_on_every_variant()
    {
        using var client = await factory.CreateClientAsAsync(Role.Operator);
        var fabric = await client.CreateFabricAsync();
        var black = await client.CreateColourAsync();

        var (id, number) = await client.CreateProductAsync(new
        {
            name = CatalogApi.UniqueName("کوله مدرسه"),
            sizes = ThreeSizes,
            variants = new[]
            {
                new { fabricId = fabric, colourId = black, size = "کوچک" },
                new { fabricId = fabric, colourId = black, size = "بزرگ" },
            },
        });

        var product = await (await client.GetAsync($"/panel/catalog/products/{id}", Cancel)).Body();
        var sizes = product.GetProperty("sizes").EnumerateArray().ToList();
        Assert.Equal(["کوچک", "متوسط", "بزرگ"], sizes.Select(s => s.GetProperty("name").GetString()));
        Assert.Equal(
            ["کوچک", "بزرگ"],
            product.GetProperty("variants").EnumerateArray().Select(v => v.GetProperty("size").GetProperty("name").GetString()));

        var medium = sizes[1].GetProperty("id").GetGuid();
        var added = await client.PostAsJsonAsync(
            $"/panel/catalog/products/{id}/variants", new { fabricId = fabric, colourId = black, sizeId = medium }, Cancel);
        Assert.Equal(HttpStatusCode.OK, added.StatusCode);
        Assert.Equal($"{number}-3", (await added.Body()).GetProperty("sku").GetString());

        var withoutSize = await client.PostAsJsonAsync(
            $"/panel/catalog/products/{id}/variants", new { fabricId = fabric, colourId = await client.CreateColourAsync() }, Cancel);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, withoutSize.StatusCode);
        Assert.Equal("catalog.size_required", await withoutSize.ErrorCode());
    }

    [Fact]
    public async Task A_model_without_sizes_takes_no_size()
    {
        using var client = await factory.CreateClientAsAsync(Role.Operator);
        var (id, _) = await client.CreateProductAsync(new { name = CatalogApi.UniqueName("کیف") });

        var response = await client.PostAsJsonAsync(
            $"/panel/catalog/products/{id}/variants",
            new { fabricId = await client.CreateFabricAsync(), colourId = await client.CreateColourAsync(), sizeId = Guid.CreateVersion7() },
            Cancel);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal("catalog.size_not_allowed", await response.ErrorCode());
    }

    [Fact]
    public async Task A_new_model_without_sizes_takes_no_size_on_its_variants()
    {
        using var client = await factory.CreateClientAsAsync(Role.Operator);

        var response = await client.PostAsJsonAsync(
            "/panel/catalog/products",
            new
            {
                name = CatalogApi.UniqueName("کیف"),
                variants = new[] { new { fabricId = await client.CreateFabricAsync(), colourId = await client.CreateColourAsync(), size = "بزرگ" } },
            },
            Cancel);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal("catalog.size_not_allowed", await response.ErrorCode());
    }

    [Fact]
    public async Task A_size_name_that_is_not_one_of_the_models_sizes_is_not_found()
    {
        using var client = await factory.CreateClientAsAsync(Role.Operator);

        var response = await client.PostAsJsonAsync(
            "/panel/catalog/products",
            new
            {
                name = CatalogApi.UniqueName("کوله"),
                sizes = TwoSizes,
                variants = new[] { new { fabricId = await client.CreateFabricAsync(), colourId = await client.CreateColourAsync(), size = "متوسط" } },
            },
            Cancel);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("catalog.size_not_found", await response.ErrorCode());
    }

    [Fact]
    public async Task The_same_fabric_and_colour_cannot_be_added_twice()
    {
        using var client = await factory.CreateClientAsAsync(Role.Operator);
        var fabric = await client.CreateFabricAsync();
        var colour = await client.CreateColourAsync();
        var (id, _) = await client.CreateProductAsync(new
        {
            name = CatalogApi.UniqueName("کیف"),
            variants = new[] { new { fabricId = fabric, colourId = colour } },
        });

        var response = await client.PostAsJsonAsync(
            $"/panel/catalog/products/{id}/variants", new { fabricId = fabric, colourId = colour }, Cancel);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("catalog.variant_exists", await response.ErrorCode());
    }

    [Fact]
    public async Task Missing_fabric_colour_category_or_model_is_not_found()
    {
        using var client = await factory.CreateClientAsAsync(Role.Operator);
        var fabric = await client.CreateFabricAsync();
        var colour = await client.CreateColourAsync();
        var missing = Guid.CreateVersion7();

        async Task<string?> Create(object body) =>
            await (await client.PostAsJsonAsync("/panel/catalog/products", body, Cancel)).ErrorCode();

        Assert.Equal(
            "catalog.fabric_not_found",
            await Create(new { name = CatalogApi.UniqueName("کیف"), variants = new[] { new { fabricId = missing, colourId = colour } } }));
        Assert.Equal(
            "catalog.colour_not_found",
            await Create(new { name = CatalogApi.UniqueName("کیف"), variants = new[] { new { fabricId = fabric, colourId = missing } } }));
        Assert.Equal("catalog.category_not_found", await Create(new { name = CatalogApi.UniqueName("کیف"), categoryId = missing }));

        var get = await client.GetAsync($"/panel/catalog/products/{missing}", Cancel);
        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
        Assert.Equal("catalog.product_not_found", await get.ErrorCode());
    }

    [Fact]
    public async Task A_variant_without_a_fabric_is_a_validation_error_on_that_field()
    {
        using var client = await factory.CreateClientAsAsync(Role.Operator);

        var response = await client.PostAsJsonAsync(
            "/panel/catalog/products",
            new { name = CatalogApi.UniqueName("کیف"), variants = new[] { new { colourId = await client.CreateColourAsync() } } },
            Cancel);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = Assert.Single((await response.Body()).GetProperty("errors").EnumerateArray());
        Assert.Equal("variants[0].fabricId", error.GetProperty("field").GetString());
    }

    [Fact]
    public async Task Creating_a_model_is_audited_with_its_persian_name()
    {
        using var client = factory.CreateBrowserClient();
        var operatorSession = await factory.LoginAsync(client, await factory.CreateUserAsync(Role.Operator));
        var name = CatalogApi.UniqueName("کیف");

        var (id, number) = await client.CreateProductAsync(new { name });

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var productId = id.ToString();
        var translationId = $"{id},fa";
        var entries = await db.AuditEntries.AsNoTracking()
            .Where(e => (e.EntityType == "products" && e.EntityId == productId)
                || (e.EntityType == "product_translations" && e.EntityId == translationId))
            .ToListAsync(Cancel);

        Assert.Equal(2, entries.Count);
        Assert.All(entries, e =>
        {
            Assert.Equal(AuditAction.Created, e.Action);
            Assert.Equal(operatorSession.User.Id, e.UserId);
        });
        var product = Changes(entries.Single(e => e.EntityType == "products"));
        Assert.Equal(number, product.GetProperty("number").GetProperty("new").GetInt32());
        var translation = Changes(entries.Single(e => e.EntityType == "product_translations"));
        Assert.Equal(name, translation.GetProperty("name").GetProperty("new").GetString());
    }

    private static JsonElement Changes(AuditEntry entry)
    {
        using var document = JsonDocument.Parse(entry.Changes);
        return document.RootElement.Clone();
    }
}
