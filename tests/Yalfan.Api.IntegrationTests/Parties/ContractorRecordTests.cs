using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Yalfan.Domain.Audit;
using Yalfan.Domain.Identity;
using Yalfan.Infrastructure.Persistence;
using static Yalfan.Api.IntegrationTests.Auth.AuthTestHelpers;
using static Yalfan.Api.IntegrationTests.Parties.ContractorApi;

namespace Yalfan.Api.IntegrationTests.Parties;

/// <summary>
/// What the database keeps about a contractor: the IBAN only encrypted, and an audit entry for
/// every change that never holds the IBAN (ADR 0019, ADR 0013).
/// </summary>
public sealed class ContractorRecordTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    /// <summary>The account-number part of <see cref="ContractorApi.Iban"/>, which must appear nowhere.</summary>
    private const string IbanDigits = "0570029971601460641001";

    [Fact]
    public async Task The_iban_column_holds_only_ciphertext_and_the_same_iban_is_stored_differently_each_time()
    {
        using var client = await factory.CreateClientAsAsync(Role.Operator);
        var first = (await client.CreateContractorAsync(iban: Iban)).Id();
        var second = (await client.CreateContractorAsync(iban: Iban)).Id();

        var firstStored = await StoredIban(first);
        var secondStored = await StoredIban(second);

        Assert.StartsWith("v1:", firstStored, StringComparison.Ordinal);
        Assert.DoesNotContain(IbanDigits[^8..], firstStored, StringComparison.Ordinal);
        Assert.NotEqual(firstStored, secondStored);
    }

    [Fact]
    public async Task Adding_a_contractor_is_audited_with_his_stages_but_without_the_iban()
    {
        using var client = factory.CreateBrowserClient();
        var operatorSession = await factory.LoginAsync(client, await factory.CreateUserAsync(Role.Operator));
        var sewing = await client.SeededStageAsync("دوخت");

        var contractor = await client.CreateContractorAsync(iban: Iban, stageIds: [sewing]);
        var id = contractor.Id();

        var created = Assert.Single(await EntriesFor("contractors", id.ToString()));
        Assert.Equal(AuditAction.Created, created.Action);
        Assert.Equal(operatorSession.User.Id, created.UserId);
        var changes = Parse(created.Changes);
        Assert.Equal(contractor.GetProperty("fullName").GetString(), changes.GetProperty("fullName").GetProperty("new").GetString());
        Assert.True(changes.GetProperty("iban").GetProperty("redacted").GetBoolean());
        Assert.Equal(JsonValueKind.Null, changes.GetProperty("iban").GetProperty("new").ValueKind);

        var stage = Assert.Single(await EntriesFor("contractor_stages", $"{id},{sewing}"));
        Assert.Equal(AuditAction.Created, stage.Action);
        Assert.Equal(sewing, Parse(stage.Changes).GetProperty("stageId").GetProperty("new").GetGuid());
        Assert.Equal(id, Parse(stage.Changes).GetProperty("contractorId").GetProperty("new").GetGuid());

        await AssertNoIbanInTheAudit();
    }

    [Fact]
    public async Task Changing_the_iban_is_audited_as_changed_without_either_number()
    {
        using var client = await factory.CreateClientAsAsync(Role.Operator);
        var id = (await client.CreateContractorAsync(iban: OtherIban)).Id();

        (await client.PutAsJsonAsync($"{Contractors}/{id}/iban", new { iban = Iban }, Cancel)).EnsureSuccessStatusCode();

        // The test clock stands still; moved on, the two changes are read back in their order.
        factory.Time.Advance(TimeSpan.FromSeconds(1));
        (await client.DeleteAsync($"{Contractors}/{id}/iban", Cancel)).EnsureSuccessStatusCode();

        var updates = (await EntriesFor("contractors", id.ToString())).Where(e => e.Action == AuditAction.Updated).ToList();
        Assert.Equal(2, updates.Count);
        foreach (var update in updates)
        {
            var field = Assert.Single(Parse(update.Changes).EnumerateObject());
            Assert.Equal("iban", field.Name);
            Assert.True(field.Value.GetProperty("redacted").GetBoolean());
            Assert.Equal(JsonValueKind.Null, field.Value.GetProperty("old").ValueKind);
            Assert.Equal(JsonValueKind.Null, field.Value.GetProperty("new").ValueKind);
        }

        // Replaced, then removed: only whether there was one, so the history tells them apart (ADR 0027).
        var replaced = Parse(updates[0].Changes).GetProperty("iban");
        var removed = Parse(updates[1].Changes).GetProperty("iban");
        Assert.True(replaced.GetProperty("oldSet").GetBoolean());
        Assert.True(replaced.GetProperty("newSet").GetBoolean());
        Assert.True(removed.GetProperty("oldSet").GetBoolean());
        Assert.False(removed.GetProperty("newSet").GetBoolean());

        await AssertNoIbanInTheAudit();
    }

    [Fact]
    public async Task Saving_the_profile_records_the_removed_stage_and_does_not_touch_the_iban()
    {
        using var client = await factory.CreateClientAsAsync(Role.Operator);
        var sewing = await client.SeededStageAsync("دوخت");
        var packing = await client.SeededStageAsync("بسته‌بندی");
        var contractor = await client.CreateContractorAsync(iban: Iban, stageIds: [sewing, packing]);
        var id = contractor.Id();
        var storedBefore = await StoredIban(id);

        (await client.PutAsJsonAsync(
            $"{Contractors}/{id}",
            new { fullName = "حسن کریمی", mobileNumber = contractor.GetProperty("mobileNumber").GetString(), stageIds = new[] { sewing } },
            Cancel)).EnsureSuccessStatusCode();

        Assert.Single(await EntriesFor("contractor_stages", $"{id},{packing}"), e => e.Action == AuditAction.Deleted);
        Assert.DoesNotContain(await EntriesFor("contractor_stages", $"{id},{sewing}"), e => e.Action == AuditAction.Deleted);
        var update = Assert.Single(await EntriesFor("contractors", id.ToString()), e => e.Action == AuditAction.Updated);
        Assert.Equal(["fullName"], Parse(update.Changes).EnumerateObject().Select(field => field.Name));
        Assert.Equal(storedBefore, await StoredIban(id));
    }

    private async Task<string> StoredIban(Guid contractorId)
    {
        await using var connection = new NpgsqlConnection(factory.ConnectionString);
        await connection.OpenAsync(Cancel);
        await using var command = new NpgsqlCommand("select iban from contractors where id = @id", connection);
        command.Parameters.AddWithValue("id", contractorId);
        return (string)(await command.ExecuteScalarAsync(Cancel))!;
    }

    private async Task AssertNoIbanInTheAudit()
    {
        await using var connection = new NpgsqlConnection(factory.ConnectionString);
        await connection.OpenAsync(Cancel);
        await using var command = new NpgsqlCommand(
            "select count(*) from audit_entries where changes::text like '%' || @digits || '%'", connection);
        command.Parameters.AddWithValue("digits", IbanDigits[^8..]);
        Assert.Equal(0L, (long)(await command.ExecuteScalarAsync(Cancel))!);
    }

    private async Task<List<AuditEntry>> EntriesFor(string entityType, string entityId)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.AuditEntries.AsNoTracking()
            .Where(e => e.EntityType == entityType && e.EntityId == entityId)
            .OrderBy(e => e.OccurredAt)
            .ToListAsync(Cancel);
    }

    private static JsonElement Parse(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }
}
