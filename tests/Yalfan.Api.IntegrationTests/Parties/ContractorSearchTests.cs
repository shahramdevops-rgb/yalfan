using System.Net;
using Yalfan.Api.IntegrationTests.Catalog;
using Yalfan.Domain.Identity;
using static Yalfan.Api.IntegrationTests.Auth.AuthTestHelpers;
using static Yalfan.Api.IntegrationTests.Parties.ContractorApi;

namespace Yalfan.Api.IntegrationTests.Parties;

/// <summary>
/// The contractor list: one search box for part of the name or of the mobile, Persian
/// alphabetical order, pages. Each test narrows the list to its own rows with a unique word.
/// </summary>
public sealed class ContractorSearchTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task The_list_is_in_persian_alphabetical_order()
    {
        using var client = await factory.CreateClientAsAsync(Role.Operator);
        var word = UniqueWord();
        string[] alphabetical = ["آرش", "بهرام", "پرویز", "چنگیز", "ژاله", "کاوه", "گلی", "یاسر"];
        foreach (var name in alphabetical.Reverse())
        {
            await client.CreateContractorAsync(fullName: $"{name} {word}");
        }

        var page = await Search(client, $"?search={Uri.EscapeDataString(word)}");

        var names = page.GetProperty("items").EnumerateArray().Select(item => item.GetProperty("fullName").GetString()).ToList();
        Assert.Equal(alphabetical.Select(name => $"{name} {word}"), names);
    }

    [Fact]
    public async Task Part_of_a_name_typed_on_an_arabic_keyboard_finds_the_contractor()
    {
        using var client = await factory.CreateClientAsAsync(Role.Operator);
        var word = UniqueWord();
        var id = (await client.CreateContractorAsync(fullName: $"یوسف کیانی {word}")).Id();

        var page = await Search(client, $"?search={Uri.EscapeDataString($"كياني {word}")}");

        Assert.Equal([id], page.Ids());
    }

    [Fact]
    public async Task The_last_digits_of_a_mobile_find_the_contractor()
    {
        using var client = await factory.CreateClientAsAsync(Role.Operator);
        var mobile = ApiFactory.NextMobile();
        var id = (await client.CreateContractorAsync(mobileNumber: mobile)).Id();

        var page = await Search(client, $"?search={Uri.EscapeDataString(PersianDigits(mobile[^5..]))}&pageSize=100");

        Assert.Contains(id, page.Ids());
    }

    [Fact]
    public async Task A_whole_mobile_in_another_form_finds_the_contractor()
    {
        using var client = await factory.CreateClientAsAsync(Role.Operator);
        var mobile = ApiFactory.NextMobile();
        var id = (await client.CreateContractorAsync(mobileNumber: mobile)).Id();

        var page = await Search(client, $"?search={Uri.EscapeDataString("+98 " + mobile[1..4] + " " + mobile[4..])}");

        Assert.Equal([id], page.Ids());
    }

    [Fact]
    public async Task Active_and_inactive_contractors_are_listed_apart_or_together()
    {
        using var client = await factory.CreateClientAsAsync(Role.Operator);
        var word = UniqueWord();
        var active = (await client.CreateContractorAsync(fullName: $"فعال {word}")).Id();
        var inactive = (await client.CreateContractorAsync(fullName: $"غیرفعال {word}")).Id();
        (await client.PostAsync($"{Contractors}/{inactive}/deactivate", null, Cancel)).EnsureSuccessStatusCode();
        var search = $"?search={Uri.EscapeDataString(word)}";

        Assert.Equal([active], (await Search(client, search + "&isActive=true")).Ids());
        Assert.Equal([inactive], (await Search(client, search + "&isActive=false")).Ids());
        Assert.Equal(2, (await Search(client, search)).Ids().Count);
    }

    [Fact]
    public async Task The_list_comes_in_pages_with_the_total()
    {
        using var client = await factory.CreateClientAsAsync(Role.Operator);
        var word = UniqueWord();
        for (var i = 0; i < 3; i++)
        {
            await client.CreateContractorAsync(fullName: $"ناصر {word}");
        }

        var second = await Search(client, $"?search={Uri.EscapeDataString(word)}&page=2&pageSize=2");

        Assert.Equal(3, second.GetProperty("totalCount").GetInt32());
        Assert.Single(second.Ids());
    }

    [Fact]
    public async Task A_listed_contractor_shows_his_stages_and_only_the_masked_iban()
    {
        using var client = await factory.CreateClientAsAsync(Role.Operator);
        var word = UniqueWord();
        await client.CreateContractorAsync(fullName: $"مهدی {word}", iban: Iban);

        var item = Assert.Single((await Search(client, $"?search={Uri.EscapeDataString(word)}")).GetProperty("items").EnumerateArray());

        Assert.Equal(["دوخت"], item.StageNames());
        Assert.Equal("IR** **** **** **** **** **10 01", item.GetProperty("maskedIban").GetString());
        Assert.DoesNotContain("0570", item.GetRawText(), StringComparison.Ordinal);
    }

    /// <summary>
    /// A word no other contractor's name contains, so a test can find only its own rows. It ends
    /// in a letter, so «آزمونـ1ـ» is not part of «آزمونـ12ـ».
    /// </summary>
    private static string UniqueWord() => CatalogApi.UniqueName("آزمون").Replace(" ", "ـ", StringComparison.Ordinal) + "ـ";

    private static string PersianDigits(string latin) => string.Concat(latin.Select(c => (char)('۰' + (c - '0'))));

    private static async Task<System.Text.Json.JsonElement> Search(HttpClient client, string queryString)
    {
        var response = await client.GetAsync(Contractors + queryString, Cancel);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Body();
    }
}
