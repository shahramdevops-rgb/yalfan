using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Yalfan.Domain.Audit;
using Yalfan.Domain.Identity;
using Yalfan.Infrastructure.Persistence;
using static Yalfan.Api.IntegrationTests.Auth.AuthTestHelpers;

namespace Yalfan.Api.IntegrationTests.Identity;

/// <summary>
/// An owner account with two numbers on two phones, and what is done when one is stolen
/// (ADR 0026). Each test makes its own account, so switching numbers off never leaks.
/// </summary>
public sealed class LoginNumberTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task The_seeded_owner_logs_in_with_either_of_his_numbers()
    {
        using var first = factory.CreateBrowserClient();
        using var second = factory.CreateBrowserClient();

        var one = await factory.LoginAsync(first, ApiFactory.SeededOwnerMobile);
        var other = await factory.LoginAsync(second, ApiFactory.SeededOwnerSecondMobile);

        Assert.Equal(one.User.Id, other.User.Id);
        Assert.Equal(Role.Owner, other.User.Role);
    }

    [Fact]
    public async Task A_switched_off_number_gets_no_code_and_cannot_log_in_while_the_other_works()
    {
        var (stolen, spare, accountId) = await NewOwnerAccount();
        using var owner = await factory.CreateClientAsAsync(Role.Owner);

        var off = await SwitchOff(owner, accountId, stolen);

        Assert.Equal(HttpStatusCode.OK, off.StatusCode);
        var numbers = (await off.Body()).GetProperty("loginNumbers").EnumerateArray().ToList();
        Assert.False(numbers.Single(n => n.GetProperty("mobileNumber").GetString() == stolen).GetProperty("isActive").GetBoolean());
        Assert.True(numbers.Single(n => n.GetProperty("mobileNumber").GetString() == spare).GetProperty("isActive").GetBoolean());

        using var thief = factory.CreateBrowserClient();
        await thief.RequestCode(stolen);
        Assert.Equal(0, factory.Sms.CountFor(stolen));
        Assert.Equal("identity.otp_invalid", await (await thief.Login(stolen, "123456")).ErrorCode());

        using var realOwner = factory.CreateBrowserClient();
        Assert.Equal(accountId, (await factory.LoginAsync(realOwner, spare)).User.Id);
    }

    [Fact]
    public async Task Switching_off_a_number_ends_every_session_of_the_account()
    {
        var (stolen, spare, accountId) = await NewOwnerAccount();
        using var stolenPhone = factory.CreateBrowserClient();
        using var laptop = factory.CreateBrowserClient();
        await factory.LoginAsync(stolenPhone, stolen);
        await factory.LoginAsync(laptop, spare);
        using var owner = await factory.CreateClientAsAsync(Role.Owner);

        await SwitchOff(owner, accountId, stolen);

        // Every device, the laptop opened with the other number too: nobody knows which code the
        // stolen phone used.
        foreach (var device in new[] { stolenPhone, laptop })
        {
            var renewal = await device.PostAsync("/panel/auth/refresh", null, Cancel);
            Assert.Equal("identity.refresh_invalid", await renewal.ErrorCode());
        }
    }

    [Fact]
    public async Task The_account_switches_off_its_own_stolen_number_from_its_other_phone()
    {
        var (stolen, spare, accountId) = await NewOwnerAccount();
        using var otherPhone = factory.CreateBrowserClient();
        await factory.LoginAsync(otherPhone, spare);

        var off = await SwitchOff(otherPhone, accountId, stolen);

        Assert.Equal(HttpStatusCode.OK, off.StatusCode);

        // His own sessions end too; he logs in again with the number he still holds.
        Assert.Equal("identity.refresh_invalid", await (await otherPhone.PostAsync("/panel/auth/refresh", null, Cancel)).ErrorCode());
        factory.Time.Advance(TimeSpan.FromMinutes(3));
        using var again = factory.CreateBrowserClient();
        Assert.Equal(accountId, (await factory.LoginAsync(again, spare)).User.Id);
    }

    [Fact]
    public async Task A_code_sent_to_one_number_does_not_work_with_the_other()
    {
        var (first, second, _) = await NewOwnerAccount();
        using var client = factory.CreateBrowserClient();
        await client.RequestCode(first);

        var response = await client.Login(second, factory.Sms.LastCodeFor(first));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("identity.otp_expired", await response.ErrorCode());
    }

    [Fact]
    public async Task A_code_taken_before_the_number_was_switched_off_no_longer_works()
    {
        var (stolen, _, accountId) = await NewOwnerAccount();
        using var thief = factory.CreateBrowserClient();
        await thief.RequestCode(stolen);
        using var owner = await factory.CreateClientAsAsync(Role.Owner);

        await SwitchOff(owner, accountId, stolen);
        var response = await thief.Login(stolen, factory.Sms.LastCodeFor(stolen));

        Assert.Equal("identity.otp_invalid", await response.ErrorCode());
    }

    [Fact]
    public async Task The_wait_between_codes_counts_per_number()
    {
        var (stolen, spare, _) = await NewOwnerAccount();
        using var thief = factory.CreateBrowserClient();
        using var realOwner = factory.CreateBrowserClient();

        await thief.RequestCode(stolen);
        await realOwner.RequestCode(spare);

        Assert.Equal(1, factory.Sms.CountFor(stolen));
        Assert.Equal(1, factory.Sms.CountFor(spare));
    }

    [Fact]
    public async Task The_last_switched_on_number_stays_on()
    {
        var (first, second, accountId) = await NewOwnerAccount();
        using var owner = await factory.CreateClientAsAsync(Role.Owner);
        var developer = await factory.UserIdOfAsync(ApiFactory.SeededSecondOwnerMobile);

        Assert.Equal(HttpStatusCode.OK, (await SwitchOff(owner, accountId, first)).StatusCode);
        var last = await SwitchOff(owner, accountId, second);
        var single = await SwitchOff(owner, developer, ApiFactory.SeededSecondOwnerMobile);

        foreach (var response in new[] { last, single })
        {
            Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
            Assert.Equal("identity.last_login_number", await response.ErrorCode());
        }
    }

    [Fact]
    public async Task A_number_switched_back_on_logs_in_again()
    {
        var (number, _, accountId) = await NewOwnerAccount();
        using var owner = await factory.CreateClientAsAsync(Role.Owner);
        var numberId = await factory.LoginNumberIdAsync(number);
        await SwitchOff(owner, accountId, number);

        var on = await owner.PostAsync($"/panel/users/{accountId}/numbers/{numberId}/activate", null, Cancel);

        Assert.Equal(HttpStatusCode.OK, on.StatusCode);
        using var phone = factory.CreateBrowserClient();
        Assert.Equal(accountId, (await factory.LoginAsync(phone, number)).User.Id);
    }

    [Fact]
    public async Task A_thief_whose_page_still_works_cannot_switch_the_stolen_number_back_on()
    {
        var (stolen, spare, accountId) = await NewOwnerAccount();
        using var thief = factory.CreateBrowserClient();
        using var realOwner = factory.CreateBrowserClient();
        await factory.LoginAsync(thief, stolen);
        await factory.LoginAsync(realOwner, spare);
        var numberId = await factory.LoginNumberIdAsync(stolen);
        await SwitchOff(realOwner, accountId, stolen);

        // The thief's access token lives a few more minutes; the owner's own one too.
        var byThief = await thief.PostAsync($"/panel/users/{accountId}/numbers/{numberId}/activate", null, Cancel);
        var byOwner = await realOwner.PostAsync($"/panel/users/{accountId}/numbers/{numberId}/activate", null, Cancel);

        foreach (var response in new[] { byThief, byOwner })
        {
            Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
            Assert.Equal("identity.own_number_switch_on", await response.ErrorCode());
        }

        // Nor can he switch off the number the owner still holds: it is the last one on.
        var otherOff = await SwitchOff(thief, accountId, spare);
        Assert.Equal("identity.last_login_number", await otherOff.ErrorCode());

        // Another owner account, the developer's, switches it back on.
        using var developer = await factory.CreateClientAsAsync(Role.Owner);
        var byDeveloper = await developer.PostAsync($"/panel/users/{accountId}/numbers/{numberId}/activate", null, Cancel);
        Assert.Equal(HttpStatusCode.OK, byDeveloper.StatusCode);
    }

    [Fact]
    public async Task The_database_refuses_to_leave_an_account_with_no_number_on()
    {
        var (first, _, accountId) = await NewOwnerAccount();
        await using var connection = new NpgsqlConnection(factory.ConnectionString);
        await connection.OpenAsync(Cancel);

        // One number off is allowed; both off, as two requests at once could do, is not.
        await using (var one = new NpgsqlCommand("update user_login_numbers set is_active = false where mobile_number = @m", connection))
        {
            one.Parameters.AddWithValue("m", first);
            await one.ExecuteNonQueryAsync(Cancel);
        }

        await using var all = new NpgsqlCommand("update user_login_numbers set is_active = false where user_id = @u", connection);
        all.Parameters.AddWithValue("u", accountId);
        var exception = await Assert.ThrowsAsync<PostgresException>(() => all.ExecuteNonQueryAsync(Cancel));
        Assert.Equal(PostgresErrorCodes.CheckViolation, exception.SqlState);
    }

    [Fact]
    public async Task An_unknown_number_is_not_found()
    {
        var (_, _, accountId) = await NewOwnerAccount();
        using var owner = await factory.CreateClientAsAsync(Role.Owner);

        var response = await owner.PostAsync($"/panel/users/{accountId}/numbers/{Guid.CreateVersion7()}/deactivate", null, Cancel);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("identity.login_number_not_found", await response.ErrorCode());
    }

    [Fact]
    public async Task Switching_off_a_number_is_audited_with_who_did_it()
    {
        var (stolen, _, accountId) = await NewOwnerAccount();
        using var owner = factory.CreateBrowserClient();
        var session = await factory.LoginAsync(owner, await factory.CreateUserAsync(Role.Owner));
        var numberId = await factory.LoginNumberIdAsync(stolen);

        await SwitchOff(owner, accountId, stolen);

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var entityId = numberId.ToString();
        var entry = await db.AuditEntries.AsNoTracking()
            .SingleAsync(e => e.EntityType == "user_login_numbers" && e.EntityId == entityId && e.Action == AuditAction.Updated, Cancel);
        Assert.Equal(session.User.Id, entry.UserId);
        using var changes = JsonDocument.Parse(entry.Changes);
        var isActive = Assert.Single(changes.RootElement.EnumerateObject());
        Assert.Equal("isActive", isActive.Name);
        Assert.False(isActive.Value.GetProperty("new").GetBoolean());
    }

    private async Task<(string First, string Second, Guid AccountId)> NewOwnerAccount()
    {
        var first = ApiFactory.NextMobile();
        var second = ApiFactory.NextMobile();
        return (first, second, await factory.CreateOwnerAccountAsync(first, second));
    }

    private async Task<HttpResponseMessage> SwitchOff(HttpClient client, Guid accountId, string mobile) =>
        await client.PostAsync($"/panel/users/{accountId}/numbers/{await factory.LoginNumberIdAsync(mobile)}/deactivate", null, Cancel);
}
