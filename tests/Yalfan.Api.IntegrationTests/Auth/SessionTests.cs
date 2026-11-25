using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc.Testing;
using Yalfan.Domain.Identity;
using static Yalfan.Api.IntegrationTests.Auth.AuthTestHelpers;

namespace Yalfan.Api.IntegrationTests.Auth;

/// <summary>Refresh, rotation, theft detection, expiry and logout. Cookies are handled by hand to replay old tokens.</summary>
public sealed class SessionTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        BaseAddress = new Uri("https://localhost"),
        HandleCookies = false,
    });

    [Fact]
    public async Task Refresh_gives_a_new_access_token_and_rotates_the_refresh_token()
    {
        var first = await LoginNewUser();

        var renewed = await _client.RefreshWith(first.RefreshToken());

        Assert.Equal(HttpStatusCode.OK, renewed.StatusCode);
        Assert.NotEqual(first.RefreshToken(), renewed.RefreshToken());
        Assert.False(string.IsNullOrEmpty((await renewed.Body()).GetProperty("accessToken").GetString()));
    }

    [Fact]
    public async Task A_replaced_token_used_again_revokes_the_whole_session()
    {
        var login = await LoginNewUser();
        var stolen = login.RefreshToken();
        var renewed = await _client.RefreshWith(stolen);
        var current = renewed.RefreshToken();

        factory.Time.Advance(TimeSpan.FromSeconds(31));
        var replay = await _client.RefreshWith(stolen);

        Assert.Equal(HttpStatusCode.Unauthorized, replay.StatusCode);
        Assert.Equal("identity.refresh_invalid", await replay.ErrorCode());

        // The legitimate holder is logged out too, and must log in again with a new code.
        var afterTheft = await _client.RefreshWith(current);
        Assert.Equal("identity.refresh_invalid", await afterTheft.ErrorCode());
    }

    [Fact]
    public async Task Two_tabs_renewing_together_do_not_end_the_session()
    {
        var login = await LoginNewUser();
        var shared = login.RefreshToken();
        var firstTab = await _client.RefreshWith(shared);

        var secondTab = await _client.RefreshWith(shared);

        Assert.Equal(HttpStatusCode.Unauthorized, secondTab.StatusCode);
        Assert.Equal("identity.refresh_superseded", await secondTab.ErrorCode());
        Assert.Null(secondTab.RefreshSetCookie());

        // The token the first tab received still works.
        Assert.Equal(HttpStatusCode.OK, (await _client.RefreshWith(firstTab.RefreshToken())).StatusCode);
    }

    [Fact]
    public async Task A_session_ends_after_seven_days_without_use()
    {
        var login = await LoginNewUser();

        factory.Time.Advance(TimeSpan.FromDays(7));
        var response = await _client.RefreshWith(login.RefreshToken());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("identity.refresh_invalid", await response.ErrorCode());
        Assert.Contains("expires=thu, 01 jan 1970", response.RefreshSetCookie()!.ToLowerInvariant(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Each_renewal_extends_the_session_by_seven_days()
    {
        var token = (await LoginNewUser()).RefreshToken();

        for (var week = 0; week < 3; week++)
        {
            factory.Time.Advance(TimeSpan.FromDays(6));
            var renewed = await _client.RefreshWith(token);
            Assert.Equal(HttpStatusCode.OK, renewed.StatusCode);
            token = renewed.RefreshToken();
        }
    }

    [Fact]
    public async Task Logout_ends_the_session_and_clears_the_cookie()
    {
        var token = (await LoginNewUser()).RefreshToken();

        var request = new HttpRequestMessage(HttpMethod.Post, "/panel/auth/logout");
        request.Headers.Add("Cookie", $"{RefreshCookie}={token}");
        var logout = await _client.SendAsync(request, Cancel);

        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        Assert.Contains("expires=thu, 01 jan 1970", logout.RefreshSetCookie()!.ToLowerInvariant(), StringComparison.Ordinal);
        Assert.Equal("identity.refresh_invalid", await (await _client.RefreshWith(token)).ErrorCode());
    }

    [Fact]
    public async Task A_deactivated_user_cannot_renew()
    {
        var mobile = await factory.CreateUserAsync(Role.Operator);
        var token = (await LoginWithCode(mobile)).RefreshToken();
        await factory.DeactivateAsync(mobile);

        var response = await _client.RefreshWith(token);

        Assert.Equal("identity.refresh_invalid", await response.ErrorCode());
    }

    [Fact]
    public async Task Refresh_without_a_cookie_is_refused()
    {
        var response = await _client.RefreshWith(null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("identity.refresh_invalid", await response.ErrorCode());
    }

    [Fact]
    public async Task An_access_token_stops_working_after_ten_minutes()
    {
        var login = await LoginNewUser();
        var accessToken = (await login.Body()).GetProperty("accessToken").GetString();

        factory.Time.Advance(TimeSpan.FromMinutes(10) + TimeSpan.FromSeconds(31));
        var request = new HttpRequestMessage(HttpMethod.Get, "/panel/auth/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var response = await _client.SendAsync(request, Cancel);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("request.unauthenticated", await response.ErrorCode());
    }

    private async Task<HttpResponseMessage> LoginNewUser() =>
        await LoginWithCode(await factory.CreateUserAsync(Role.Operator));

    private async Task<HttpResponseMessage> LoginWithCode(string mobile)
    {
        await _client.RequestCode(mobile);
        var response = await _client.Login(mobile, factory.Sms.LastCodeFor(mobile));
        response.EnsureSuccessStatusCode();
        return response;
    }
}
