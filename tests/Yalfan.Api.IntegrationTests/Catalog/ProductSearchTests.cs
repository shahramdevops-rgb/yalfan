using System.Globalization;
using System.Net;
using System.Text.Json;
using Yalfan.Domain.Identity;
using static Yalfan.Api.IntegrationTests.Auth.AuthTestHelpers;

namespace Yalfan.Api.IntegrationTests.Catalog;

/// <summary>The workshop has many models, so they are found by search, one page at a time.</summary>
public sealed class ProductSearchTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task A_model_is_found_by_its_number_typed_in_persian_digits()
    {
        using var client = await factory.CreateClientAsAsync(Role.Operator);
        var (id, number) = await client.CreateProductAsync(new { name = CatalogApi.UniqueName("کیف") });

        var page = await Search(client, $"search={ToPersianDigits(number)}");

        Assert.Contains(page.GetProperty("items").EnumerateArray(), item => item.GetProperty("id").GetGuid() == id);
    }

    [Fact]
    public async Task Models_are_found_by_part_of_the_name_newest_first_with_their_category()
    {
        using var client = await factory.CreateClientAsAsync(Role.Operator);
        var word = CatalogApi.UniqueName("ساک").Replace(' ', '_');
        var category = await client.CreateCategoryAsync();
        var older = await client.CreateProductAsync(new { name = $"{word} دستی", categoryId = category });
        var newer = await client.CreateProductAsync(new
        {
            name = $"کیف {word}",
            variants = new[] { new { fabricId = await client.CreateFabricAsync(), colourId = await client.CreateColourAsync() } },
        });

        var page = await Search(client, $"search={Uri.EscapeDataString(word)}");

        Assert.Equal(2, page.GetProperty("totalCount").GetInt32());
        var items = page.GetProperty("items").EnumerateArray().ToList();
        Assert.Equal([newer.Id, older.Id], items.Select(i => i.GetProperty("id").GetGuid()));
        Assert.Equal(1, items[0].GetProperty("variantCount").GetInt32());
        Assert.Equal(JsonValueKind.Null, items[0].GetProperty("category").ValueKind);
        Assert.Equal(category, items[1].GetProperty("category").GetProperty("id").GetGuid());
    }

    [Fact]
    public async Task A_long_result_comes_one_page_at_a_time()
    {
        using var client = await factory.CreateClientAsAsync(Role.Operator);
        var word = CatalogApi.UniqueName("صفحه").Replace(' ', '_');
        for (var i = 0; i < 3; i++)
        {
            await client.CreateProductAsync(new { name = CatalogApi.UniqueName(word) });
        }

        var first = await Search(client, $"search={Uri.EscapeDataString(word)}&page=1&pageSize=2");
        var second = await Search(client, $"search={Uri.EscapeDataString(word)}&page=2&pageSize=2");

        Assert.Equal(3, first.GetProperty("totalCount").GetInt32());
        Assert.Equal(2, first.GetProperty("items").GetArrayLength());
        Assert.Equal(1, second.GetProperty("items").GetArrayLength());
        Assert.Equal(2, second.GetProperty("page").GetInt32());
    }

    [Fact]
    public async Task A_page_size_above_the_limit_is_a_validation_error()
    {
        using var client = await factory.CreateClientAsAsync(Role.Operator);

        var response = await client.GetAsync("/panel/catalog/products?pageSize=1000", Cancel);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static async Task<JsonElement> Search(HttpClient client, string query)
    {
        var response = await client.GetAsync($"/panel/catalog/products?{query}", Cancel);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Body();
    }

    private static string ToPersianDigits(int number) =>
        string.Concat(number.ToString(CultureInfo.InvariantCulture).Select(c => (char)('۰' + (c - '0'))));
}
