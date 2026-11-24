using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Yalfan.Api.IntegrationTests.Catalog;
using Yalfan.Api.IntegrationTests.Parties;
using Yalfan.Domain.Audit;
using Yalfan.Domain.Identity;
using Yalfan.Infrastructure.Persistence;
using static Yalfan.Api.IntegrationTests.Auth.AuthTestHelpers;

namespace Yalfan.Api.IntegrationTests.Production;

/// <summary>
/// The order of the work, changed by dragging a stage (ADR 0022). A class of its own, because a
/// new order moves every stage of the database, and the other stage tests read the seeded ones
/// in their first order. Each test starts from the order it reads, never from an assumed one.
/// </summary>
public sealed class StageOrderTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private const string Stages = "/panel/production/stages";

    [Fact]
    public async Task A_stage_dragged_into_place_is_listed_there_everywhere()
    {
        using var client = await factory.CreateClientAsAsync(Role.Operator);
        var handles = await client.CreateStageAsync(CatalogApi.UniqueName("دوخت دسته"));
        var sewing = await client.SeededStageAsync("دوخت");
        var contractor = await client.CreateContractorAsync(stageIds: [handles, sewing]);
        var (product, _) = await client.CreateProductAsync(new { name = CatalogApi.UniqueName("کیف دستی") });

        // «دوخت دسته» goes from the end of the list to right after «دوخت».
        var order = await IdsAsync(client);
        order.Remove(handles);
        order.Insert(order.IndexOf(sewing) + 1, handles);

        var response = await client.PutAsJsonAsync($"{Stages}/order", new { stageIds = order }, Cancel);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(order, (await response.Body()).EnumerateArray().Select(item => item.GetProperty("id").GetGuid()));
        Assert.Equal(order, await IdsAsync(client));

        var details = await (await client.GetAsync($"{ContractorApi.Contractors}/{contractor.Id()}", Cancel)).Body();
        Assert.Equal([sewing, handles], details.GetProperty("stages").EnumerateArray().Select(s => s.GetProperty("id").GetGuid()));

        var card = await (await client.GetAsync($"/panel/production/rates?productId={product}", Cancel)).Body();
        var cardOrder = card.GetProperty("stages").EnumerateArray().Select(s => s.GetProperty("stage").GetProperty("id").GetGuid()).ToList();
        Assert.Equal(cardOrder.IndexOf(sewing) + 1, cardOrder.IndexOf(handles));
    }

    [Fact]
    public async Task An_order_made_from_an_old_copy_of_the_list_is_refused_and_changes_nothing()
    {
        using var client = await factory.CreateClientAsAsync(Role.Operator);
        var oldCopy = await IdsAsync(client);

        // Another user adds a stage after this one loaded the list.
        await client.CreateStageAsync(CatalogApi.UniqueName("اتوکشی"));
        var before = await IdsAsync(client);

        oldCopy.Reverse();
        var response = await client.PutAsJsonAsync($"{Stages}/order", new { stageIds = oldCopy }, Cancel);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("production.stage_order_outdated", await response.ErrorCode());
        Assert.Equal(before, await IdsAsync(client));
    }

    [Fact]
    public async Task A_stage_named_twice_is_a_validation_error_on_the_list()
    {
        using var client = await factory.CreateClientAsAsync(Role.Operator);
        var order = await IdsAsync(client);
        order[^1] = order[0];

        var response = await client.PutAsJsonAsync($"{Stages}/order", new { stageIds = order }, Cancel);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = Assert.Single((await response.Body()).GetProperty("errors").EnumerateArray());
        Assert.Equal("stageIds", error.GetProperty("field").GetString());
        Assert.Equal("production.stage_order_outdated", error.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Each_stage_that_moved_is_audited_with_its_old_and_new_place()
    {
        using var client = factory.CreateBrowserClient();
        var operatorSession = await factory.LoginAsync(client, await factory.CreateUserAsync(Role.Operator));
        var first = await client.CreateStageAsync(CatalogApi.UniqueName("برچسب‌زنی"));
        var second = await client.CreateStageAsync(CatalogApi.UniqueName("کنترل کیفیت"));

        // The last two stages swap places; nothing else moves.
        var order = await IdsAsync(client);
        Assert.Equal([first, second], order[^2..]);
        order[^2] = second;
        order[^1] = first;
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync($"{Stages}/order", new { stageIds = order }, Cancel)).StatusCode);

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var moved = await db.AuditEntries.AsNoTracking()
            .Where(e => e.EntityType == "stages" && e.Action == AuditAction.Updated && e.UserId == operatorSession.User.Id)
            .ToListAsync(Cancel);

        Assert.Equal([first.ToString(), second.ToString()], moved.Select(e => e.EntityId).Order());
        var firstMove = moved.Single(e => e.EntityId == first.ToString());
        using var changes = JsonDocument.Parse(firstMove.Changes);
        var position = changes.RootElement.GetProperty("position");
        Assert.Equal(position.GetProperty("old").GetInt32() + 1, position.GetProperty("new").GetInt32());
    }

    [Fact]
    public async Task An_anonymous_caller_cannot_change_the_order()
    {
        using var operatorClient = await factory.CreateClientAsAsync(Role.Operator);
        var order = await IdsAsync(operatorClient);
        using var anonymous = factory.CreateBrowserClient();

        var response = await anonymous.PutAsJsonAsync($"{Stages}/order", new { stageIds = Enumerable.Reverse(order) }, Cancel);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(order, await IdsAsync(operatorClient));
    }

    private static async Task<List<Guid>> IdsAsync(HttpClient client)
    {
        var list = await (await client.GetAsync(Stages, Cancel)).Body();
        return list.EnumerateArray().Select(item => item.GetProperty("id").GetGuid()).ToList();
    }
}
