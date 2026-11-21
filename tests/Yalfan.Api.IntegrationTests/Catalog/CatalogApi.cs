using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using static Yalfan.Api.IntegrationTests.Auth.AuthTestHelpers;

namespace Yalfan.Api.IntegrationTests.Catalog;

/// <summary>Calls the catalog endpoints the way the panel will, for tests that need some rows first.</summary>
internal static class CatalogApi
{
    private static int _counter;

    /// <summary>
    /// A name no other test uses. Names are unique per kind, and the tests of a class share one
    /// database (ARCHITECTURE.md 12).
    /// </summary>
    public static string UniqueName(string prefix) =>
        string.Create(CultureInfo.InvariantCulture, $"{prefix} {Interlocked.Increment(ref _counter)}");

    public static Task<Guid> CreateFabricAsync(this HttpClient client, string? name = null) =>
        client.CreateOptionAsync("fabrics", name ?? UniqueName("برزنت"));

    public static Task<Guid> CreateColourAsync(this HttpClient client, string? name = null) =>
        client.CreateOptionAsync("colours", name ?? UniqueName("مشکی"));

    public static Task<Guid> CreateCategoryAsync(this HttpClient client, string? name = null) =>
        client.CreateOptionAsync("categories", name ?? UniqueName("کوله"));

    /// <summary>Creates a model and returns its id and number; fails the test if it is refused.</summary>
    public static async Task<(Guid Id, int Number)> CreateProductAsync(this HttpClient client, object body)
    {
        var response = await client.PostAsJsonAsync("/panel/catalog/products", body, Cancel);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var created = await response.Body();
        return (created.GetProperty("id").GetGuid(), created.GetProperty("number").GetInt32());
    }

    private static async Task<Guid> CreateOptionAsync(this HttpClient client, string kind, string name)
    {
        var response = await client.PostAsJsonAsync($"/panel/catalog/{kind}", new { name }, Cancel);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        return (await response.Body()).GetProperty("id").GetGuid();
    }
}
