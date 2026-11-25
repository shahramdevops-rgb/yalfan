using System.Net;
using System.Net.Http.Headers;
using Yalfan.Domain.Identity;
using static Yalfan.Api.IntegrationTests.Auth.AuthTestHelpers;

namespace Yalfan.Api.IntegrationTests.Auth;

public sealed class LoginTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client = factory.CreateBrowserClient();

    [Fact]
    public async Task The_sms_code_logs_the_user_in()
    {
        var mobile = await factory.CreateUserAsync(Role.Operator);

        var asked = await _client.RequestCode(mobile);
        Assert.Equal(HttpStatusCode.OK, asked.StatusCode);
        var countdown = await asked.Body();
        Assert.Equal(120, countdown.GetProperty("expiresInSeconds").GetInt32());
        Assert.Equal(120, countdown.GetProperty("resendAfterSeconds").GetInt32());

        var response = await _client.Login(mobile, factory.Sms.LastCodeFor(mobile));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Body();
        Assert.False(string.IsNullOrEmpty(body.GetProperty("accessToken").GetString()));
        Assert.Equal("operator", body.GetProperty("user").GetProperty("role").GetString());
        var userId = body.GetProperty("user").GetProperty("id").GetGuid();
        Assert.Equal(await factory.UserIdOfAsync(mobile), userId);

        // The refresh token is only in the cookie, and the cookie is locked down.
        Assert.False(body.TryGetProperty("refreshToken", out _));
        var cookie = response.RefreshSetCookie()!.ToLowerInvariant();
        Assert.Contains("httponly", cookie, StringComparison.Ordinal);
        Assert.Contains("secure", cookie, StringComparison.Ordinal);
        Assert.Contains("samesite=strict", cookie, StringComparison.Ordinal);
        Assert.Contains("path=/panel/auth", cookie, StringComparison.Ordinal);

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", body.GetProperty("accessToken").GetString());
        var me = await _client.GetAsync("/panel/auth/me", Cancel);
        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
        Assert.Equal(userId, (await me.Body()).GetProperty("id").GetGuid());
    }

    [Fact]
    public async Task Persian_digits_work_for_the_number_and_the_code()
    {
        var mobile = await factory.CreateUserAsync(Role.Operator);
        var persianMobile = ToPersian(mobile);

        Assert.Equal(HttpStatusCode.OK, (await _client.RequestCode(persianMobile)).StatusCode);
        var response = await _client.Login(persianMobile, ToPersian(factory.Sms.LastCodeFor(mobile)));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task The_seeded_owner_can_log_in()
    {
        await factory.LoginAsync(_client, ApiFactory.SeededOwnerMobile);

        var me = await _client.GetAsync("/panel/auth/me", Cancel);
        Assert.Equal("owner", (await me.Body()).GetProperty("role").GetString());
    }

    [Fact]
    public async Task An_unknown_number_gets_the_same_answer_and_no_sms()
    {
        var stranger = ApiFactory.NextMobile();
        var known = await factory.CreateUserAsync(Role.Operator);

        var strangerAnswer = await (await _client.RequestCode(stranger)).Body();
        var knownAnswer = await (await _client.RequestCode(known)).Body();

        Assert.Equal(knownAnswer.ToString(), strangerAnswer.ToString());
        Assert.Equal(0, factory.Sms.CountFor(stranger));

        var login = await _client.Login(stranger, "123456");
        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
        Assert.Equal("identity.otp_invalid", await login.ErrorCode());
    }

    [Fact]
    public async Task A_wrong_code_is_refused_and_five_wrong_codes_kill_the_code()
    {
        var mobile = await factory.CreateUserAsync(Role.Operator);
        await _client.RequestCode(mobile);
        var code = factory.Sms.LastCodeFor(mobile);
        var wrong = code == "000000" ? "111111" : "000000";

        for (var attempt = 1; attempt <= 5; attempt++)
        {
            var response = await _client.Login(mobile, wrong);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.Equal("identity.otp_invalid", await response.ErrorCode());
        }

        var right = await _client.Login(mobile, code);
        Assert.Equal(HttpStatusCode.Unauthorized, right.StatusCode);
        Assert.Equal("identity.otp_expired", await right.ErrorCode());
    }

    [Fact]
    public async Task A_code_expires_after_two_minutes()
    {
        var mobile = await factory.CreateUserAsync(Role.Operator);
        await _client.RequestCode(mobile);

        factory.Time.Advance(TimeSpan.FromMinutes(2));
        var response = await _client.Login(mobile, factory.Sms.LastCodeFor(mobile));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("identity.otp_expired", await response.ErrorCode());
    }

    [Fact]
    public async Task A_code_works_only_once()
    {
        var mobile = await factory.CreateUserAsync(Role.Operator);
        await _client.RequestCode(mobile);
        var code = factory.Sms.LastCodeFor(mobile);

        Assert.Equal(HttpStatusCode.OK, (await _client.Login(mobile, code)).StatusCode);
        var again = await _client.Login(mobile, code);

        Assert.Equal(HttpStatusCode.Unauthorized, again.StatusCode);
        Assert.Equal("identity.otp_expired", await again.ErrorCode());
    }

    [Fact]
    public async Task A_new_code_needs_two_minutes_and_retires_the_old_one()
    {
        var mobile = await factory.CreateUserAsync(Role.Operator);
        await _client.RequestCode(mobile);
        var first = factory.Sms.LastCodeFor(mobile);

        await _client.RequestCode(mobile);
        Assert.Equal(1, factory.Sms.CountFor(mobile));

        factory.Time.Advance(TimeSpan.FromMinutes(2));
        await _client.RequestCode(mobile);
        Assert.Equal(2, factory.Sms.CountFor(mobile));
        var second = factory.Sms.LastCodeFor(mobile);

        if (first != second)
        {
            Assert.Equal("identity.otp_invalid", await (await _client.Login(mobile, first)).ErrorCode());
        }

        Assert.Equal(HttpStatusCode.OK, (await _client.Login(mobile, second)).StatusCode);
    }

    [Fact]
    public async Task At_most_five_codes_are_sent_per_hour()
    {
        var mobile = await factory.CreateUserAsync(Role.Operator);

        for (var request = 0; request < 7; request++)
        {
            await _client.RequestCode(mobile);
            factory.Time.Advance(TimeSpan.FromMinutes(2));
        }

        Assert.Equal(5, factory.Sms.CountFor(mobile));
    }

    [Fact]
    public async Task A_deactivated_user_cannot_log_in()
    {
        var mobile = await factory.CreateUserAsync(Role.Operator);
        await factory.DeactivateAsync(mobile);

        await _client.RequestCode(mobile);

        Assert.Equal(0, factory.Sms.CountFor(mobile));
    }

    [Theory]
    [InlineData("0912123", "validation.invalid_format")]
    [InlineData("12345", "validation.invalid_format")]
    [InlineData("abcdef", "validation.invalid_format")]
    public async Task A_malformed_code_is_a_validation_error(string code, string expected)
    {
        var response = await _client.Login(ApiFactory.NextMobile(), code);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = Assert.Single((await response.Body()).GetProperty("errors").EnumerateArray());
        Assert.Equal("code", error.GetProperty("field").GetString());
        Assert.Equal(expected, error.GetProperty("code").GetString());
    }

    [Fact]
    public async Task A_number_that_is_not_an_iranian_mobile_is_a_validation_error()
    {
        var response = await _client.RequestCode("02112345678");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = Assert.Single((await response.Body()).GetProperty("errors").EnumerateArray());
        Assert.Equal("mobileNumber", error.GetProperty("field").GetString());
        Assert.Equal("mobile.invalid", error.GetProperty("code").GetString());
    }

    private static string ToPersian(string digits) =>
        string.Concat(digits.Select(c => char.IsAsciiDigit(c) ? (char)('۰' + (c - '0')) : c));
}
