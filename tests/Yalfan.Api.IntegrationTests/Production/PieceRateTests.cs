using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Yalfan.Api.IntegrationTests.Catalog;
using Yalfan.Application.Common;
using Yalfan.Domain.Audit;
using Yalfan.Domain.Identity;
using Yalfan.Infrastructure.Persistence;
using static Yalfan.Api.IntegrationTests.Auth.AuthTestHelpers;

namespace Yalfan.Api.IntegrationTests.Production;

/// <summary>
/// The rate card (ADR 0021): append-only piece rates per model and stage, with start dates.
/// Each test makes its own model and stage, because the tests of a class share one database.
/// </summary>
public sealed class PieceRateTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task The_rate_in_force_on_any_day_comes_from_the_start_dates()
    {
        using var client = await factory.CreateClientAsAsync(Role.Operator);
        var (product, stage) = await ModelAndStageAsync(client);
        await client.AddRateAsync(product, stage, 1_000_000, "2026-01-01");
        await client.AddRateAsync(product, stage, 1_200_000, "2026-12-01");

        Assert.Null((await RateOn(client, product, stage, "2025-12-31")).Amount);
        Assert.Equal((1_000_000, "2026-01-01"), await RateOn(client, product, stage, "2026-01-01"));
        Assert.Equal((1_000_000, "2026-01-01"), await RateOn(client, product, stage, "2026-11-30"));
        Assert.Equal((1_200_000, "2026-12-01"), await RateOn(client, product, stage, "2026-12-01"));
    }

    [Fact]
    public async Task The_rate_card_lists_every_stage_in_the_order_of_the_work_with_or_without_a_rate()
    {
        using var client = await factory.CreateClientAsAsync(Role.Operator);
        var (product, stage) = await ModelAndStageAsync(client);
        await client.AddRateAsync(product, stage, 450_000, "2026-01-01");

        var card = await (await client.GetAsync($"/panel/production/rates?productId={product}&on=2026-06-01", Cancel)).Body();

        Assert.Equal(product, card.GetProperty("productId").GetGuid());
        Assert.True(card.GetProperty("productNumber").GetInt32() >= 101);
        Assert.Equal("2026-06-01", card.GetProperty("on").GetString());
        var stages = card.GetProperty("stages").EnumerateArray().ToList();
        Assert.Equal(["برش", "دوخت", "بسته‌بندی"], stages.Take(3).Select(s => s.GetProperty("stage").GetProperty("name").GetString()));
        Assert.All(stages.Take(3), s => Assert.Equal(JsonValueKind.Null, s.GetProperty("amount").ValueKind));
        var rated = Assert.Single(stages, s => s.GetProperty("stage").GetProperty("id").GetGuid() == stage);
        Assert.Equal(450_000, rated.GetProperty("amount").GetInt64());
    }

    [Fact]
    public async Task Without_a_day_the_rate_card_is_for_the_workshops_today()
    {
        using var client = await factory.CreateClientAsAsync(Role.Operator);
        var (product, stage) = await ModelAndStageAsync(client);
        var today = factory.Services.GetRequiredService<IClock>().Today;
        await client.AddRateAsync(product, stage, 300_000, Iso(today));
        await client.AddRateAsync(product, stage, 350_000, Iso(today.AddDays(1)));

        var card = await (await client.GetAsync($"/panel/production/rates?productId={product}", Cancel)).Body();

        Assert.Equal(Iso(today), card.GetProperty("on").GetString());
        var rated = Assert.Single(card.GetProperty("stages").EnumerateArray(), s => s.GetProperty("stage").GetProperty("id").GetGuid() == stage);
        Assert.Equal(300_000, rated.GetProperty("amount").GetInt64());
    }

    [Fact]
    public async Task A_correction_with_the_same_start_date_wins_and_the_history_keeps_both()
    {
        using var client = await factory.CreateClientAsAsync(Role.Operator);
        var (product, stage) = await ModelAndStageAsync(client);
        var mistyped = await client.AddRateAsync(product, stage, 120_000, "2026-11-01");
        factory.Time.Advance(TimeSpan.FromSeconds(1));
        var corrected = await client.AddRateAsync(product, stage, 1_200_000, "2026-11-01");

        Assert.Equal((1_200_000, "2026-11-01"), await RateOn(client, product, stage, "2026-11-23"));

        var history = await History(client, $"productId={product}");
        Assert.Equal([corrected, mistyped], history.Select(entry => entry.GetProperty("id").GetGuid()));
        Assert.False(history[0].GetProperty("isReplaced").GetBoolean());
        Assert.True(history[1].GetProperty("isReplaced").GetBoolean());
        Assert.Equal(120_000, history[1].GetProperty("amount").GetInt64());
    }

    [Fact]
    public async Task The_history_keeps_every_rate_grouped_by_stage_newest_first()
    {
        using var client = await factory.CreateClientAsAsync(Role.Operator);
        var (product, sewing) = await ModelAndStageAsync(client);
        var handles = await client.CreateStageAsync(CatalogApi.UniqueName("دوخت دسته"));
        var oldSewing = await client.AddRateAsync(product, sewing, 900_000, "2025-03-21");
        var handlesRate = await client.AddRateAsync(product, handles, 150_000, "2025-03-21");
        var newSewing = await client.AddRateAsync(product, sewing, 1_100_000, "2026-03-21");
        var futureSewing = await client.AddRateAsync(product, sewing, 1_300_000, "2027-03-21");

        var all = await History(client, $"productId={product}");
        var sewingOnly = await History(client, $"productId={product}&stageId={sewing}");

        Assert.Equal([futureSewing, newSewing, oldSewing, handlesRate], all.Select(entry => entry.GetProperty("id").GetGuid()));
        Assert.All(all, entry => Assert.False(entry.GetProperty("isReplaced").GetBoolean()));
        Assert.Equal([futureSewing, newSewing, oldSewing], sewingOnly.Select(entry => entry.GetProperty("id").GetGuid()));

        var first = sewingOnly[0];
        Assert.Equal(sewing, first.GetProperty("stage").GetProperty("id").GetGuid());
        Assert.Equal(1_300_000, first.GetProperty("amount").GetInt64());
        Assert.Equal("2027-03-21", first.GetProperty("startsOn").GetString());
        Assert.Equal(factory.Time.GetUtcNow(), first.GetProperty("enteredAt").GetDateTimeOffset(), TimeSpan.FromMilliseconds(1));
    }

    [Theory]
    [InlineData("update piece_rates set amount = 1")]
    [InlineData("delete from piece_rates")]
    [InlineData("truncate piece_rates")]
    public async Task The_database_refuses_to_change_or_delete_a_rate(string sql)
    {
        using var client = await factory.CreateClientAsAsync(Role.Operator);
        var (product, stage) = await ModelAndStageAsync(client);
        await client.AddRateAsync(product, stage, 500_000, "2026-01-01");

        await using var connection = new NpgsqlConnection(factory.ConnectionString);
        await connection.OpenAsync(Cancel);
        await using var command = new NpgsqlCommand(sql, connection);

        var exception = await Assert.ThrowsAsync<PostgresException>(() => command.ExecuteNonQueryAsync(Cancel));
        Assert.Contains("piece_rates is append-only", exception.MessageText, StringComparison.Ordinal);
        Assert.Equal((500_000, "2026-01-01"), await RateOn(client, product, stage, "2026-06-01"));
    }

    [Fact]
    public async Task Adding_a_rate_is_audited_with_the_amount_in_rial()
    {
        using var client = factory.CreateBrowserClient();
        var session = await factory.LoginAsync(client, await factory.CreateUserAsync(Role.Operator));
        var (product, stage) = await ModelAndStageAsync(client);

        var id = await client.AddRateAsync(product, stage, 1_250_000, "2026-02-01");

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var entityId = id.ToString();
        var entry = Assert.Single(await db.AuditEntries.AsNoTracking()
            .Where(e => e.EntityType == "piece_rates" && e.EntityId == entityId)
            .ToListAsync(Cancel));

        Assert.Equal(AuditAction.Created, entry.Action);
        Assert.Equal(session.User.Id, entry.UserId);
        using var document = JsonDocument.Parse(entry.Changes);
        var changes = document.RootElement;
        Assert.Equal(1_250_000, changes.GetProperty("amount").GetProperty("new").GetInt64());
        Assert.Equal("IRR", changes.GetProperty("currency").GetProperty("new").GetString());
        Assert.Equal(product, changes.GetProperty("productId").GetProperty("new").GetGuid());
        Assert.Equal(stage, changes.GetProperty("stageId").GetProperty("new").GetGuid());
        Assert.Equal("2026-02-01", changes.GetProperty("startsOn").GetProperty("new").GetString());
    }

    [Theory]
    [InlineData(Role.Owner)]
    [InlineData(Role.Manager)]
    [InlineData(Role.Operator)]
    public async Task Every_staff_role_may_enter_a_rate(Role role)
    {
        using var operatorClient = await factory.CreateClientAsAsync(Role.Operator);
        var (product, stage) = await ModelAndStageAsync(operatorClient);
        using var client = await factory.CreateClientAsAsync(role);

        var response = await client.PostAsJsonAsync(
            "/panel/production/rates", new { productId = product, stageId = stage, amount = 10_000, startsOn = "2026-01-01" }, Cancel);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5000)]
    public async Task A_rate_of_zero_or_less_is_refused_on_the_amount_field(long amount)
    {
        using var client = await factory.CreateClientAsAsync(Role.Operator);
        var (product, stage) = await ModelAndStageAsync(client);

        var response = await client.PostAsJsonAsync(
            "/panel/production/rates", new { productId = product, stageId = stage, amount, startsOn = "2026-01-01" }, Cancel);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = Assert.Single((await response.Body()).GetProperty("errors").EnumerateArray());
        Assert.Equal("amount", error.GetProperty("field").GetString());
        Assert.Equal("validation.too_small", error.GetProperty("code").GetString());
    }

    [Fact]
    public async Task A_rate_needs_a_model_a_stage_an_amount_and_a_start_date()
    {
        using var client = await factory.CreateClientAsAsync(Role.Operator);

        var response = await client.PostAsJsonAsync("/panel/production/rates", new { }, Cancel);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var fields = (await response.Body()).GetProperty("errors").EnumerateArray()
            .Select(error => (error.GetProperty("field").GetString(), error.GetProperty("code").GetString()))
            .Order()
            .ToList();
        Assert.Equal(
            [
                ("amount", "validation.required"),
                ("productId", "validation.required"),
                ("stageId", "validation.required"),
                ("startsOn", "validation.required"),
            ],
            fields);
    }

    [Fact]
    public async Task A_rate_for_a_missing_model_or_stage_is_not_found()
    {
        using var client = await factory.CreateClientAsAsync(Role.Operator);
        var (product, stage) = await ModelAndStageAsync(client);
        var missing = Guid.CreateVersion7();

        var noModel = await client.PostAsJsonAsync(
            "/panel/production/rates", new { productId = missing, stageId = stage, amount = 1, startsOn = "2026-01-01" }, Cancel);
        var noStage = await client.PostAsJsonAsync(
            "/panel/production/rates", new { productId = product, stageId = missing, amount = 1, startsOn = "2026-01-01" }, Cancel);
        var noCard = await client.GetAsync($"/panel/production/rates?productId={missing}", Cancel);
        var noHistory = await client.GetAsync($"/panel/production/rates/history?productId={product}&stageId={missing}", Cancel);

        Assert.Equal("production.product_not_found", await noModel.ErrorCode());
        Assert.Equal("production.stage_not_found", await noStage.ErrorCode());
        Assert.Equal("production.product_not_found", await noCard.ErrorCode());
        Assert.Equal("production.stage_not_found", await noHistory.ErrorCode());
        Assert.All([noModel, noStage, noCard, noHistory], response => Assert.Equal(HttpStatusCode.NotFound, response.StatusCode));
    }

    [Fact]
    public async Task An_anonymous_caller_cannot_read_or_enter_rates()
    {
        using var client = factory.CreateBrowserClient();

        var read = await client.GetAsync($"/panel/production/rates?productId={Guid.CreateVersion7()}", Cancel);
        var enter = await client.PostAsJsonAsync("/panel/production/rates", new { }, Cancel);

        Assert.Equal(HttpStatusCode.Unauthorized, read.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, enter.StatusCode);
    }

    private static async Task<(Guid Product, Guid Stage)> ModelAndStageAsync(HttpClient client)
    {
        var (product, _) = await client.CreateProductAsync(new { name = CatalogApi.UniqueName("کیف دوشی") });
        var stage = await client.CreateStageAsync(CatalogApi.UniqueName("دوخت بدنه"));
        return (product, stage);
    }

    /// <summary>The amount and start date of one stage's rate on a day; both null when it has none.</summary>
    private static async Task<(long? Amount, string? StartsOn)> RateOn(HttpClient client, Guid product, Guid stage, string day)
    {
        var response = await client.GetAsync($"/panel/production/rates?productId={product}&on={day}", Cancel);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var rate = Assert.Single(
            (await response.Body()).GetProperty("stages").EnumerateArray(),
            s => s.GetProperty("stage").GetProperty("id").GetGuid() == stage);
        var amount = rate.GetProperty("amount");
        return amount.ValueKind == JsonValueKind.Null
            ? (null, null)
            : (amount.GetInt64(), rate.GetProperty("startsOn").GetString());
    }

    private static async Task<List<JsonElement>> History(HttpClient client, string query)
    {
        var response = await client.GetAsync($"/panel/production/rates/history?{query}", Cancel);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        return (await response.Body()).EnumerateArray().ToList();
    }

    private static string Iso(DateOnly day) => day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
}

internal static class PieceRateApi
{
    /// <summary>Enters a rate and returns its id; fails the test if it is refused.</summary>
    public static async Task<Guid> AddRateAsync(this HttpClient client, Guid product, Guid stage, long amount, string startsOn)
    {
        var response = await client.PostAsJsonAsync(
            "/panel/production/rates", new { productId = product, stageId = stage, amount, startsOn }, Cancel);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        return (await response.Body()).GetProperty("id").GetGuid();
    }
}
