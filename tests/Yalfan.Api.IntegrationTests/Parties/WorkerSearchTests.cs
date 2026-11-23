using System.Net;
using System.Text.Json;
using Yalfan.Api.IntegrationTests.Catalog;
using Yalfan.Domain.Identity;
using static Yalfan.Api.IntegrationTests.Auth.AuthTestHelpers;
using static Yalfan.Api.IntegrationTests.Parties.ContractorApi;
using static Yalfan.Api.IntegrationTests.Parties.WorkerApi;

namespace Yalfan.Api.IntegrationTests.Parties;

/// <summary>
/// The worker list: the same search box as the contractors (part of the name or of the mobile),
/// Persian alphabetical order, pages. Each test narrows the list to its own rows with a unique word.
/// </summary>
public sealed class WorkerSearchTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task The_list_is_in_persian_alphabetical_order()
    {
        using var client = await factory.CreateClientAsAsync(Role.Operator);
        var word = UniqueWord();
        string[] alphabetical = ["آزاده", "پروین", "ژیلا", "گلناز"];
        foreach (var name in alphabetical.Reverse())
        {
            await client.CreateWorkerAsync(fullName: $"{name} {word}");
        }

        var page = await Search(client, $"?search={Uri.EscapeDataString(word)}");

        var names = page.GetProperty("items").EnumerateArray().Select(item => item.GetProperty("fullName").GetString()).ToList();
        Assert.Equal(alphabetical.Select(name => $"{name} {word}"), names);
    }

    [Fact]
    public async Task Part_of_a_name_typed_on_an_arabic_keyboard_finds_the_worker()
    {
        using var client = await factory.CreateClientAsAsync(Role.Operator);
        var word = UniqueWord();
        var id = (await client.CreateWorkerAsync(fullName: $"مریم کیانی {word}")).Id();

        var page = await Search(client, $"?search={Uri.EscapeDataString($"كياني {word}")}");

        Assert.Equal([id], page.Ids());
    }

    [Fact]
    public async Task The_last_digits_or_the_whole_mobile_in_another_form_find_the_worker()
    {
        using var client = await factory.CreateClientAsAsync(Role.Operator);
        var mobile = ApiFactory.NextMobile();
        var id = (await client.CreateWorkerAsync(mobileNumber: mobile)).Id();
        var lastDigits = string.Concat(mobile[^5..].Select(c => (char)('۰' + (c - '0'))));

        var byPart = await Search(client, $"?search={Uri.EscapeDataString(lastDigits)}&pageSize=100");
        var byWhole = await Search(client, $"?search={Uri.EscapeDataString("+98 " + mobile[1..4] + " " + mobile[4..])}");

        Assert.Contains(id, byPart.Ids());
        Assert.Equal([id], byWhole.Ids());
    }

    [Fact]
    public async Task Active_and_inactive_workers_are_listed_apart_or_together()
    {
        using var client = await factory.CreateClientAsAsync(Role.Operator);
        var word = UniqueWord();
        var active = (await client.CreateWorkerAsync(fullName: $"فعال {word}")).Id();
        var inactive = (await client.CreateWorkerAsync(fullName: $"غیرفعال {word}")).Id();
        (await client.PostAsync($"{Workers}/{inactive}/deactivate", null, Cancel)).EnsureSuccessStatusCode();
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
            await client.CreateWorkerAsync(fullName: $"سمیه {word}");
        }

        var second = await Search(client, $"?search={Uri.EscapeDataString(word)}&page=2&pageSize=2");

        Assert.Equal(3, second.GetProperty("totalCount").GetInt32());
        Assert.Single(second.Ids());
    }

    [Fact]
    public async Task A_listed_worker_shows_the_stages_the_start_date_and_only_the_masked_iban()
    {
        using var client = await factory.CreateClientAsAsync(Role.Operator);
        var word = UniqueWord();
        await client.CreateWorkerAsync(fullName: $"نرگس {word}", iban: Iban);

        var item = Assert.Single((await Search(client, $"?search={Uri.EscapeDataString(word)}")).GetProperty("items").EnumerateArray());

        Assert.Equal(["برش"], item.StageNames());
        Assert.Equal(StartDate, item.GetProperty("startDate").GetString());
        Assert.Equal("IR** **** **** **** **** **10 01", item.GetProperty("maskedIban").GetString());
        Assert.DoesNotContain("0570", item.GetRawText(), StringComparison.Ordinal);
    }

    /// <summary>A word no other worker's name contains, ending in a letter (see the contractor search tests).</summary>
    private static string UniqueWord() => CatalogApi.UniqueName("آزمون").Replace(" ", "ـ", StringComparison.Ordinal) + "ـ";

    private static async Task<JsonElement> Search(HttpClient client, string queryString)
    {
        var response = await client.GetAsync(Workers + queryString, Cancel);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Body();
    }
}
