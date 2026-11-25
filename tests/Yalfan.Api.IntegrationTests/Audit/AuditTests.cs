using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Yalfan.Domain.Audit;
using Yalfan.Domain.Identity;
using Yalfan.Infrastructure.Persistence;

namespace Yalfan.Api.IntegrationTests.Audit;

/// <summary>The audit as the running API writes it, on the real schema with its trigger.</summary>
public sealed class AuditTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private static CancellationToken Cancel => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Adding_a_user_records_who_did_it_when_and_the_new_values()
    {
        using var client = factory.CreateBrowserClient();
        var owner = await factory.LoginAsync(client, await factory.CreateUserAsync(Role.Owner));
        var mobile = ApiFactory.NextMobile();

        var response = await client.PostAsJsonAsync(
            "/panel/users", new { fullName = "Zahra Ahmadi", mobileNumber = mobile, role = "operator" }, Cancel);
        var created = await response.Content.ReadFromJsonAsync<JsonElement>(Cancel);
        var userId = created.GetProperty("id").GetGuid();

        var entry = Assert.Single(await EntriesFor("users", userId));
        Assert.Equal(AuditAction.Created, entry.Action);
        Assert.Equal(AuditActor.User, entry.Actor);
        Assert.Equal(owner.User.Id, entry.UserId);
        Assert.Equal(factory.Time.GetUtcNow(), entry.OccurredAt, TimeSpan.FromMilliseconds(1));
        Assert.Matches("^[0-9a-f]{32}$", entry.TraceId);

        var changes = Parse(entry.Changes);
        Assert.Equal("Zahra Ahmadi", changes.GetProperty("fullName").GetProperty("new").GetString());
        Assert.Equal("Operator", changes.GetProperty("role").GetProperty("new").GetString());
        Assert.True(changes.GetProperty("isActive").GetProperty("new").GetBoolean());
        Assert.False(changes.GetProperty("fullName").TryGetProperty("old", out _));
        Assert.False(changes.TryGetProperty("id", out _));

        // The number is a row of its own (ADR 0026), created in the same save, naming its user.
        var numberId = created.GetProperty("loginNumbers")[0].GetProperty("id").GetGuid();
        var number = Assert.Single(await EntriesFor("user_login_numbers", numberId));
        Assert.Equal(AuditAction.Created, number.Action);
        Assert.Equal(entry.OccurredAt, number.OccurredAt);
        var numberChanges = Parse(number.Changes);
        Assert.Equal(mobile, numberChanges.GetProperty("mobileNumber").GetProperty("new").GetString());
        Assert.Equal(userId.ToString(), numberChanges.GetProperty("userId").GetProperty("new").GetString());
    }

    [Fact]
    public async Task A_change_records_only_the_field_that_changed_with_old_and_new()
    {
        var mobile = await factory.CreateUserAsync(Role.Operator);
        var userId = await UserIdOf(mobile);

        await factory.DeactivateAsync(mobile);

        var entry = Assert.Single(await EntriesFor("users", userId), e => e.Action == AuditAction.Updated);
        // Compared as JSON: jsonb stores keys in its own order.
        var changes = Parse(entry.Changes);
        var isActive = Assert.Single(changes.EnumerateObject());
        Assert.Equal("isActive", isActive.Name);
        Assert.True(isActive.Value.GetProperty("old").GetBoolean());
        Assert.False(isActive.Value.GetProperty("new").GetBoolean());

        // Outside a request nobody is logged in: the system made the change.
        Assert.Equal(AuditActor.System, entry.Actor);
        Assert.Null(entry.UserId);
    }

    [Fact]
    public async Task Saving_without_a_real_change_records_nothing()
    {
        var mobile = await factory.CreateUserAsync(Role.Operator);
        var userId = await UserIdOf(mobile);
        await factory.DeactivateAsync(mobile);

        await factory.DeactivateAsync(mobile);

        Assert.Single(await EntriesFor("users", userId), e => e.Action == AuditAction.Updated);
    }

    [Fact]
    public async Task Logging_in_and_renewing_record_no_audit_entries()
    {
        using var client = factory.CreateBrowserClient();
        await factory.LoginAsync(client, await factory.CreateUserAsync(Role.Operator));
        (await client.PostAsync("/panel/auth/refresh", null, Cancel)).EnsureSuccessStatusCode();

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.False(await db.AuditEntries.AnyAsync(
            e => e.EntityType == "otp_codes" || e.EntityType == "refresh_tokens", Cancel));
    }

    [Fact]
    public async Task The_seeded_owner_is_recorded_as_created_by_the_system()
    {
        var ownerId = await UserIdOf(ApiFactory.SeededOwnerMobile);

        var entry = Assert.Single(await EntriesFor("users", ownerId));
        Assert.Equal(AuditAction.Created, entry.Action);
        Assert.Equal(AuditActor.System, entry.Actor);
        Assert.Null(entry.UserId);
    }

    [Theory]
    [InlineData("update audit_entries set changes = '{}'")]
    [InlineData("delete from audit_entries")]
    [InlineData("truncate audit_entries")]
    public async Task The_database_refuses_to_change_or_delete_audit_entries(string sql)
    {
        // Make sure there is at least one entry to change.
        await factory.CreateUserAsync(Role.Operator);

        await using var connection = new NpgsqlConnection(factory.ConnectionString);
        await connection.OpenAsync(Cancel);
        await using var command = new NpgsqlCommand(sql, connection);

        var exception = await Assert.ThrowsAsync<PostgresException>(() => command.ExecuteNonQueryAsync(Cancel));
        Assert.Contains("append-only", exception.MessageText, StringComparison.Ordinal);
    }

    private async Task<List<AuditEntry>> EntriesFor(string entityType, Guid id)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var entityId = id.ToString();
        return await db.AuditEntries.AsNoTracking()
            .Where(e => e.EntityType == entityType && e.EntityId == entityId)
            .OrderBy(e => e.OccurredAt)
            .ToListAsync(Cancel);
    }

    private Task<Guid> UserIdOf(string mobile) => factory.UserIdOfAsync(mobile);

    private static JsonElement Parse(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }
}
