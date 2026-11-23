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
using static Yalfan.Api.IntegrationTests.Parties.WorkerApi;

namespace Yalfan.Api.IntegrationTests.Parties;

/// <summary>
/// What the database keeps about a worker: the IBAN only encrypted, with no code of its own for
/// workers (ADR 0019), and an audit entry for every change that never holds the IBAN (ADR 0013).
/// </summary>
public sealed class WorkerRecordTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    /// <summary>The account-number part of <see cref="ContractorApi.Iban"/>, which must appear nowhere.</summary>
    private const string IbanDigits = "0570029971601460641001";

    [Fact]
    public async Task The_iban_column_holds_only_ciphertext()
    {
        using var client = await factory.CreateClientAsAsync(Role.Operator);
        var id = (await client.CreateWorkerAsync(iban: Iban)).Id();

        var stored = await StoredIban(id);

        Assert.StartsWith("v1:", stored, StringComparison.Ordinal);
        Assert.DoesNotContain(IbanDigits[^8..], stored, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Adding_a_worker_is_audited_with_the_start_date_and_stages_but_without_the_iban()
    {
        using var client = factory.CreateBrowserClient();
        var operatorSession = await factory.LoginAsync(client, await factory.CreateUserAsync(Role.Operator));
        var cutting = await client.SeededStageAsync("برش");

        var worker = await client.CreateWorkerAsync(iban: Iban, stageIds: [cutting]);
        var id = worker.Id();

        var created = Assert.Single(await EntriesFor("workers", id.ToString()));
        Assert.Equal(AuditAction.Created, created.Action);
        Assert.Equal(operatorSession.User.Id, created.UserId);
        var changes = Parse(created.Changes);
        Assert.Equal(worker.GetProperty("fullName").GetString(), changes.GetProperty("fullName").GetProperty("new").GetString());
        Assert.Equal(StartDate, changes.GetProperty("startDate").GetProperty("new").GetString());
        Assert.True(changes.GetProperty("iban").GetProperty("redacted").GetBoolean());
        Assert.Equal(JsonValueKind.Null, changes.GetProperty("iban").GetProperty("new").ValueKind);

        var stage = Assert.Single(await EntriesFor("worker_stages", $"{id},{cutting}"));
        Assert.Equal(AuditAction.Created, stage.Action);
        Assert.Equal(cutting, Parse(stage.Changes).GetProperty("stageId").GetProperty("new").GetGuid());

        await AssertNoIbanInTheAudit();
    }

    [Fact]
    public async Task Changing_the_iban_is_audited_as_changed_without_either_number()
    {
        using var client = await factory.CreateClientAsAsync(Role.Operator);
        var id = (await client.CreateWorkerAsync(iban: OtherIban)).Id();

        (await client.PutAsJsonAsync($"{Workers}/{id}/iban", new { iban = Iban }, Cancel)).EnsureSuccessStatusCode();
        (await client.DeleteAsync($"{Workers}/{id}/iban", Cancel)).EnsureSuccessStatusCode();

        var updates = (await EntriesFor("workers", id.ToString())).Where(e => e.Action == AuditAction.Updated).ToList();
        Assert.Equal(2, updates.Count);
        foreach (var update in updates)
        {
            var field = Assert.Single(Parse(update.Changes).EnumerateObject());
            Assert.Equal("iban", field.Name);
            Assert.True(field.Value.GetProperty("redacted").GetBoolean());
            Assert.Equal(JsonValueKind.Null, field.Value.GetProperty("old").ValueKind);
            Assert.Equal(JsonValueKind.Null, field.Value.GetProperty("new").ValueKind);
        }

        await AssertNoIbanInTheAudit();
    }

    [Fact]
    public async Task Saving_the_profile_records_the_changed_start_date_and_the_removed_stage()
    {
        using var client = await factory.CreateClientAsAsync(Role.Operator);
        var cutting = await client.SeededStageAsync("برش");
        var worker = await client.CreateWorkerAsync(iban: Iban, stageIds: [cutting]);
        var id = worker.Id();
        var storedBefore = await StoredIban(id);

        (await client.PutAsJsonAsync(
            $"{Workers}/{id}",
            new { fullName = worker.GetProperty("fullName").GetString(), mobileNumber = worker.GetProperty("mobileNumber").GetString(), startDate = "2020-02-02" },
            Cancel)).EnsureSuccessStatusCode();

        Assert.Single(await EntriesFor("worker_stages", $"{id},{cutting}"), e => e.Action == AuditAction.Deleted);
        var update = Assert.Single(await EntriesFor("workers", id.ToString()), e => e.Action == AuditAction.Updated);
        var field = Assert.Single(Parse(update.Changes).EnumerateObject());
        Assert.Equal("startDate", field.Name);
        Assert.Equal(StartDate, field.Value.GetProperty("old").GetString());
        Assert.Equal("2020-02-02", field.Value.GetProperty("new").GetString());
        Assert.Equal(storedBefore, await StoredIban(id));
    }

    private async Task<string> StoredIban(Guid workerId)
    {
        await using var connection = new NpgsqlConnection(factory.ConnectionString);
        await connection.OpenAsync(Cancel);
        await using var command = new NpgsqlCommand("select iban from workers where id = @id", connection);
        command.Parameters.AddWithValue("id", workerId);
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
