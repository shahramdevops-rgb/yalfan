using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Yalfan.Domain.Identity;
using static Yalfan.Api.IntegrationTests.Auth.AuthTestHelpers;

namespace Yalfan.Api.IntegrationTests.Identity;

/// <summary>The owner's users screen (ADR 0012, ADR 0026), on the real API and database.</summary>
public sealed class UsersTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private const string Users = "/panel/users";

    [Theory]
    [InlineData(Role.Manager)]
    [InlineData(Role.Operator)]
    public async Task Only_an_owner_reaches_the_users_screen(Role role)
    {
        using var client = await factory.CreateClientAsAsync(role);
        var someone = await factory.UserIdOfAsync(await factory.CreateUserAsync(Role.Operator));

        var responses = new[]
        {
            await client.GetAsync(Users, Cancel),
            await client.PutAsJsonAsync($"{Users}/{someone}/name", new { fullName = "X" }, Cancel),
            await client.PutAsJsonAsync($"{Users}/{someone}/role", new { role = "manager" }, Cancel),
            await client.PutAsJsonAsync($"{Users}/{someone}/mobile", new { mobileNumber = ApiFactory.NextMobile() }, Cancel),
            await client.PostAsync($"{Users}/{someone}/deactivate", null, Cancel),
            await client.PostAsync($"{Users}/{someone}/numbers/{Guid.CreateVersion7()}/deactivate", null, Cancel),
        };

        Assert.All(responses, response => Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode));
    }

    [Fact]
    public async Task The_list_shows_the_owner_accounts_first_with_their_numbers()
    {
        using var owner = await LoginAsOwner();
        await factory.CreateUserAsync(Role.Operator);

        var users = (await (await owner.GetAsync(Users, Cancel)).Body()).EnumerateArray().ToList();

        var seeded = users.Single(u => u.GetProperty("fullName").GetString() == "Seeded Owner");
        Assert.True(seeded.GetProperty("isOwnerAccount").GetBoolean());
        Assert.Equal("owner", seeded.GetProperty("role").GetString());
        Assert.Equal(
            [ApiFactory.SeededOwnerMobile, ApiFactory.SeededOwnerSecondMobile],
            seeded.GetProperty("loginNumbers").EnumerateArray().Select(n => n.GetProperty("mobileNumber").GetString()));
        Assert.All(seeded.GetProperty("loginNumbers").EnumerateArray(), n => Assert.True(n.GetProperty("isActive").GetBoolean()));

        var firstOther = users.FindIndex(u => !u.GetProperty("isOwnerAccount").GetBoolean());
        var lastOwner = users.FindLastIndex(u => u.GetProperty("isOwnerAccount").GetBoolean());
        Assert.True(lastOwner < firstOther);
    }

    [Fact]
    public async Task The_owner_corrects_a_name()
    {
        using var owner = await LoginAsOwner();
        var id = await factory.UserIdOfAsync(await factory.CreateUserAsync(Role.Operator));

        var renamed = await owner.PutAsJsonAsync($"{Users}/{id}/name", new { fullName = "  Zahra Karimi " }, Cancel);
        var empty = await owner.PutAsJsonAsync($"{Users}/{id}/name", new { fullName = " " }, Cancel);

        Assert.Equal(HttpStatusCode.OK, renamed.StatusCode);
        Assert.Equal("Zahra Karimi", (await renamed.Body()).GetProperty("fullName").GetString());
        Assert.Equal(HttpStatusCode.BadRequest, empty.StatusCode);
    }

    [Fact]
    public async Task A_new_role_reaches_the_user_at_his_next_renewal_without_logging_him_out()
    {
        using var owner = await LoginAsOwner();
        var mobile = await factory.CreateUserAsync(Role.Operator);
        using var user = factory.CreateBrowserClient();
        await factory.LoginAsync(user, mobile);

        var changed = await owner.PutAsJsonAsync($"{Users}/{await factory.UserIdOfAsync(mobile)}/role", new { role = "manager" }, Cancel);
        var renewed = await user.PostAsync("/panel/auth/refresh", null, Cancel);

        Assert.Equal("manager", (await changed.Body()).GetProperty("role").GetString());
        Assert.Equal(HttpStatusCode.OK, renewed.StatusCode);
        Assert.Equal("manager", (await renewed.Body()).GetProperty("user").GetProperty("role").GetString());
    }

    [Fact]
    public async Task The_panel_never_makes_an_owner_nor_changes_an_owner_account()
    {
        using var owner = await LoginAsOwner();
        var operatorId = await factory.UserIdOfAsync(await factory.CreateUserAsync(Role.Operator));
        var secondOwner = await factory.UserIdOfAsync(ApiFactory.SeededSecondOwnerMobile);

        var toOwner = await owner.PutAsJsonAsync($"{Users}/{operatorId}/role", new { role = "owner" }, Cancel);
        var ownerRole = await owner.PutAsJsonAsync($"{Users}/{secondOwner}/role", new { role = "manager" }, Cancel);
        var ownerMobile = await owner.PutAsJsonAsync($"{Users}/{secondOwner}/mobile", new { mobileNumber = ApiFactory.NextMobile() }, Cancel);
        var ownerOff = await owner.PostAsync($"{Users}/{secondOwner}/deactivate", null, Cancel);

        Assert.Equal(HttpStatusCode.BadRequest, toOwner.StatusCode);
        var error = Assert.Single((await toOwner.Body()).GetProperty("errors").EnumerateArray());
        Assert.Equal("identity.role_not_allowed", error.GetProperty("code").GetString());
        foreach (var response in new[] { ownerRole, ownerMobile, ownerOff })
        {
            Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
            Assert.Equal("identity.owner_locked", await response.ErrorCode());
        }
    }

    [Fact]
    public async Task A_new_number_ends_every_session_and_only_the_new_number_logs_in()
    {
        using var owner = await LoginAsOwner();
        var oldMobile = await factory.CreateUserAsync(Role.Manager);
        var id = await factory.UserIdOfAsync(oldMobile);
        using var lostPhone = factory.CreateBrowserClient();
        using var laptop = factory.CreateBrowserClient();
        await factory.LoginAsync(lostPhone, oldMobile);
        factory.Time.Advance(TimeSpan.FromMinutes(3));
        await factory.LoginAsync(laptop, oldMobile);
        var newMobile = ApiFactory.NextMobile();

        var changed = await owner.PutAsJsonAsync($"{Users}/{id}/mobile", new { mobileNumber = newMobile }, Cancel);

        Assert.Equal(HttpStatusCode.OK, changed.StatusCode);
        Assert.Equal(newMobile, (await changed.Body()).GetProperty("loginNumbers")[0].GetProperty("mobileNumber").GetString());
        foreach (var device in new[] { lostPhone, laptop })
        {
            var renewal = await device.PostAsync("/panel/auth/refresh", null, Cancel);
            Assert.Equal("identity.refresh_invalid", await renewal.ErrorCode());
        }

        factory.Time.Advance(TimeSpan.FromMinutes(3));
        var codesBefore = factory.Sms.CountFor(oldMobile);
        using var stranger = factory.CreateBrowserClient();
        await stranger.RequestCode(oldMobile);
        Assert.Equal(codesBefore, factory.Sms.CountFor(oldMobile));

        using var newPhone = factory.CreateBrowserClient();
        Assert.Equal(id, (await factory.LoginAsync(newPhone, newMobile)).User.Id);
    }

    [Fact]
    public async Task The_same_number_again_changes_nothing_and_ends_no_session()
    {
        using var owner = await LoginAsOwner();
        var mobile = await factory.CreateUserAsync(Role.Operator);
        using var user = factory.CreateBrowserClient();
        await factory.LoginAsync(user, mobile);

        var same = await owner.PutAsJsonAsync($"{Users}/{await factory.UserIdOfAsync(mobile)}/mobile", new { mobileNumber = mobile }, Cancel);

        Assert.Equal(HttpStatusCode.OK, same.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await user.PostAsync("/panel/auth/refresh", null, Cancel)).StatusCode);
    }

    [Fact]
    public async Task A_number_another_user_logs_in_with_is_refused()
    {
        using var owner = await LoginAsOwner();
        var id = await factory.UserIdOfAsync(await factory.CreateUserAsync(Role.Operator));

        // Taken by the owner's second number, so every number of every user counts.
        var response = await owner.PutAsJsonAsync($"{Users}/{id}/mobile", new { mobileNumber = ApiFactory.SeededOwnerSecondMobile }, Cancel);
        var created = await owner.PostAsJsonAsync(
            Users, new { fullName = "Someone", mobileNumber = ApiFactory.SeededOwnerSecondMobile, role = "operator" }, Cancel);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("identity.mobile_taken", await response.ErrorCode());
        Assert.Equal("identity.mobile_taken", await created.ErrorCode());
    }

    [Fact]
    public async Task Deactivating_ends_every_session_and_activating_lets_the_user_log_in_again()
    {
        using var owner = await LoginAsOwner();
        var mobile = await factory.CreateUserAsync(Role.Operator);
        var id = await factory.UserIdOfAsync(mobile);
        using var user = factory.CreateBrowserClient();
        await factory.LoginAsync(user, mobile);

        var off = await owner.PostAsync($"{Users}/{id}/deactivate", null, Cancel);

        Assert.False((await off.Body()).GetProperty("isActive").GetBoolean());
        Assert.Equal("identity.refresh_invalid", await (await user.PostAsync("/panel/auth/refresh", null, Cancel)).ErrorCode());

        var on = await owner.PostAsync($"{Users}/{id}/activate", null, Cancel);

        Assert.True((await on.Body()).GetProperty("isActive").GetBoolean());
        factory.Time.Advance(TimeSpan.FromMinutes(3));
        using var again = factory.CreateBrowserClient();
        Assert.Equal(id, (await factory.LoginAsync(again, mobile)).User.Id);
    }

    [Fact]
    public async Task An_unknown_user_is_not_found()
    {
        using var owner = await LoginAsOwner();

        var response = await owner.PutAsJsonAsync($"{Users}/{Guid.CreateVersion7()}/name", new { fullName = "X" }, Cancel);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("identity.user_not_found", await response.ErrorCode());
    }

    [Fact]
    public async Task Me_no_longer_carries_a_mobile_number()
    {
        using var owner = await LoginAsOwner();

        var me = await (await owner.GetAsync("/panel/auth/me", Cancel)).Body();

        Assert.Equal("Test Owner", me.GetProperty("fullName").GetString());
        Assert.False(me.TryGetProperty("mobileNumber", out _));
    }

    /// <summary>
    /// An owner account of its own for each test: logging in as the seeded owner every time would
    /// reach the five codes an hour of his number.
    /// </summary>
    private Task<HttpClient> LoginAsOwner() => factory.CreateClientAsAsync(Role.Owner);
}
