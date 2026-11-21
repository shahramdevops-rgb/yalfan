using System.Net;
using System.Net.Http.Json;
using Yalfan.Domain.Identity;
using static Yalfan.Api.IntegrationTests.Auth.AuthTestHelpers;

namespace Yalfan.Api.IntegrationTests.Catalog;

/// <summary>Categories, fabrics and colours: the lists a model's variants are built from.</summary>
public sealed class CatalogOptionTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Theory]
    [InlineData("categories")]
    [InlineData("fabrics")]
    [InlineData("colours")]
    public async Task An_operator_adds_renames_and_lists_an_option(string kind)
    {
        using var client = await factory.CreateClientAsAsync(Role.Operator);
        var name = CatalogApi.UniqueName("گزینه");
        var newName = CatalogApi.UniqueName("گزینه");

        var created = await client.PostAsJsonAsync($"/panel/catalog/{kind}", new { name = $"  {name} " }, Cancel);
        Assert.Equal(HttpStatusCode.OK, created.StatusCode);
        var id = (await created.Body()).GetProperty("id").GetGuid();
        Assert.Equal(name, (await created.Body()).GetProperty("name").GetString());

        var renamed = await client.PutAsJsonAsync($"/panel/catalog/{kind}/{id}", new { name = newName }, Cancel);
        Assert.Equal(HttpStatusCode.OK, renamed.StatusCode);

        var list = await (await client.GetAsync($"/panel/catalog/{kind}", Cancel)).Body();
        var listed = Assert.Single(list.EnumerateArray(), item => item.GetProperty("id").GetGuid() == id);
        Assert.Equal(newName, listed.GetProperty("name").GetString());
    }

    [Fact]
    public async Task A_colour_name_is_used_once_even_when_typed_on_an_arabic_keyboard()
    {
        using var client = await factory.CreateClientAsAsync(Role.Operator);
        var suffix = CatalogApi.UniqueName(string.Empty).Trim();
        await client.CreateColourAsync($"مشکی {suffix}");

        var response = await client.PostAsJsonAsync("/panel/catalog/colours", new { name = $"مشكى {suffix}" }, Cancel);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("catalog.name_taken", await response.ErrorCode());
    }

    [Fact]
    public async Task A_rename_to_another_options_name_is_refused_but_keeping_the_own_name_is_not()
    {
        using var client = await factory.CreateClientAsAsync(Role.Operator);
        var taken = CatalogApi.UniqueName("چرم");
        var own = CatalogApi.UniqueName("چرم");
        await client.CreateFabricAsync(taken);
        var id = await client.CreateFabricAsync(own);

        var toTaken = await client.PutAsJsonAsync($"/panel/catalog/fabrics/{id}", new { name = taken }, Cancel);
        var toOwn = await client.PutAsJsonAsync($"/panel/catalog/fabrics/{id}", new { name = own }, Cancel);

        Assert.Equal(HttpStatusCode.Conflict, toTaken.StatusCode);
        Assert.Equal("catalog.name_taken", await toTaken.ErrorCode());
        Assert.Equal(HttpStatusCode.OK, toOwn.StatusCode);
    }

    [Fact]
    public async Task Renaming_a_missing_option_is_not_found()
    {
        using var client = await factory.CreateClientAsAsync(Role.Operator);

        var response = await client.PutAsJsonAsync($"/panel/catalog/colours/{Guid.CreateVersion7()}", new { name = "سبز" }, Cancel);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("catalog.colour_not_found", await response.ErrorCode());
    }

    [Fact]
    public async Task An_empty_name_is_a_validation_error_on_the_name_field()
    {
        using var client = await factory.CreateClientAsAsync(Role.Operator);

        var response = await client.PostAsJsonAsync("/panel/catalog/categories", new { name = "  " }, Cancel);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = Assert.Single((await response.Body()).GetProperty("errors").EnumerateArray());
        Assert.Equal("name", error.GetProperty("field").GetString());
    }

    [Fact]
    public async Task An_anonymous_caller_cannot_read_the_catalog()
    {
        using var client = factory.CreateBrowserClient();

        var response = await client.GetAsync("/panel/catalog/colours", Cancel);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
