using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Yalfan.Api.IntegrationTests.Catalog;
using Yalfan.Domain.Audit;
using Yalfan.Domain.Identity;
using Yalfan.Infrastructure.Persistence;
using static Yalfan.Api.IntegrationTests.Auth.AuthTestHelpers;

namespace Yalfan.Api.IntegrationTests.Production;

/// <summary>The shared stage list (ADR 0018). Tests never rename the three seeded stages.</summary>
public sealed class StageTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task A_new_database_starts_with_the_workshops_three_stages_in_the_order_of_the_work()
    {
        using var client = await factory.CreateClientAsAsync(Role.Operator);
        await client.CreateStageAsync(CatalogApi.UniqueName("پرچ"));

        var list = await (await client.GetAsync("/panel/production/stages", Cancel)).Body();

        var names = list.EnumerateArray().Select(item => item.GetProperty("name").GetString()).ToList();
        Assert.Equal(["برش", "دوخت", "بسته‌بندی"], names.Take(3));
    }

    [Fact]
    public async Task An_operator_adds_renames_and_lists_a_stage()
    {
        using var client = await factory.CreateClientAsAsync(Role.Operator);
        var name = CatalogApi.UniqueName("دوخت دسته");
        var newName = CatalogApi.UniqueName("دوخت دسته");

        var created = await client.PostAsJsonAsync("/panel/production/stages", new { name = $"  {name} " }, Cancel);
        Assert.Equal(HttpStatusCode.OK, created.StatusCode);
        var id = (await created.Body()).GetProperty("id").GetGuid();
        Assert.Equal(name, (await created.Body()).GetProperty("name").GetString());

        var renamed = await client.PutAsJsonAsync($"/panel/production/stages/{id}", new { name = newName }, Cancel);
        Assert.Equal(HttpStatusCode.OK, renamed.StatusCode);

        var list = await (await client.GetAsync("/panel/production/stages", Cancel)).Body();
        var listed = Assert.Single(list.EnumerateArray(), item => item.GetProperty("id").GetGuid() == id);
        Assert.Equal(newName, listed.GetProperty("name").GetString());
    }

    [Fact]
    public async Task A_stage_name_is_used_once_even_when_typed_on_an_arabic_keyboard()
    {
        using var client = await factory.CreateClientAsAsync(Role.Operator);
        var suffix = CatalogApi.UniqueName(string.Empty).Trim();
        await client.CreateStageAsync($"تکمیل کیف {suffix}");

        var response = await client.PostAsJsonAsync("/panel/production/stages", new { name = $"تكميل كيف {suffix}" }, Cancel);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("production.name_taken", await response.ErrorCode());
    }

    [Fact]
    public async Task A_seeded_stage_name_is_taken()
    {
        using var client = await factory.CreateClientAsAsync(Role.Operator);

        var response = await client.PostAsJsonAsync("/panel/production/stages", new { name = " دوخت " }, Cancel);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("production.name_taken", await response.ErrorCode());
    }

    [Fact]
    public async Task A_rename_to_another_stages_name_is_refused_but_keeping_the_own_name_is_not()
    {
        using var client = await factory.CreateClientAsAsync(Role.Operator);
        var taken = CatalogApi.UniqueName("اتو");
        var own = CatalogApi.UniqueName("اتو");
        await client.CreateStageAsync(taken);
        var id = await client.CreateStageAsync(own);

        var toTaken = await client.PutAsJsonAsync($"/panel/production/stages/{id}", new { name = taken }, Cancel);
        var toOwn = await client.PutAsJsonAsync($"/panel/production/stages/{id}", new { name = own }, Cancel);

        Assert.Equal(HttpStatusCode.Conflict, toTaken.StatusCode);
        Assert.Equal("production.name_taken", await toTaken.ErrorCode());
        Assert.Equal(HttpStatusCode.OK, toOwn.StatusCode);
    }

    [Fact]
    public async Task Renaming_a_missing_stage_is_not_found()
    {
        using var client = await factory.CreateClientAsAsync(Role.Operator);

        var response = await client.PutAsJsonAsync($"/panel/production/stages/{Guid.CreateVersion7()}", new { name = "اتو" }, Cancel);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("production.stage_not_found", await response.ErrorCode());
    }

    [Fact]
    public async Task An_empty_name_is_a_validation_error_on_the_name_field()
    {
        using var client = await factory.CreateClientAsAsync(Role.Operator);

        var response = await client.PostAsJsonAsync("/panel/production/stages", new { name = "  " }, Cancel);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = Assert.Single((await response.Body()).GetProperty("errors").EnumerateArray());
        Assert.Equal("name", error.GetProperty("field").GetString());
    }

    [Fact]
    public async Task An_anonymous_caller_cannot_read_the_stages()
    {
        using var client = factory.CreateBrowserClient();

        var response = await client.GetAsync("/panel/production/stages", Cancel);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Adding_a_stage_is_audited_with_its_name()
    {
        using var client = factory.CreateBrowserClient();
        var operatorSession = await factory.LoginAsync(client, await factory.CreateUserAsync(Role.Operator));
        var name = CatalogApi.UniqueName("رنگ‌کاری لبه");

        var id = await client.CreateStageAsync(name);

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var stageId = id.ToString();
        var entry = Assert.Single(await db.AuditEntries.AsNoTracking()
            .Where(e => e.EntityType == "stages" && e.EntityId == stageId)
            .ToListAsync(Cancel));

        Assert.Equal(AuditAction.Created, entry.Action);
        Assert.Equal(operatorSession.User.Id, entry.UserId);
        using var changes = JsonDocument.Parse(entry.Changes);
        Assert.Equal(name, changes.RootElement.GetProperty("name").GetProperty("new").GetString());
    }
}

internal static class StageApi
{
    /// <summary>Adds a stage and returns its id; fails the test if it is refused.</summary>
    public static async Task<Guid> CreateStageAsync(this HttpClient client, string name)
    {
        var response = await client.PostAsJsonAsync("/panel/production/stages", new { name }, Cancel);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        return (await response.Body()).GetProperty("id").GetGuid();
    }
}
