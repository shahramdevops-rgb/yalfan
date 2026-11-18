using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Yalfan.Domain.Identity;
using static Yalfan.Api.IntegrationTests.Auth.AuthTestHelpers;

namespace Yalfan.Api.IntegrationTests.Auth;

public sealed class AccessControlTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private static object NewOperator(string? mobile = null) =>
        new { fullName = "Zahra Ahmadi", mobileNumber = mobile ?? ApiFactory.NextMobile(), role = "operator" };

    [Fact]
    public async Task An_anonymous_caller_gets_a_coded_401()
    {
        using var client = factory.CreateBrowserClient();

        var me = await client.GetAsync("/panel/auth/me", Cancel);
        var create = await client.PostAsJsonAsync("/panel/users", NewOperator(), Cancel);

        Assert.Equal(HttpStatusCode.Unauthorized, me.StatusCode);
        Assert.Equal("request.unauthenticated", await me.ErrorCode());
        Assert.Equal(HttpStatusCode.Unauthorized, create.StatusCode);
    }

    [Theory]
    [InlineData(Role.Manager)]
    [InlineData(Role.Operator)]
    public async Task Only_the_owner_adds_users(Role role)
    {
        using var client = await factory.CreateClientAsAsync(role);

        var response = await client.PostAsJsonAsync("/panel/users", NewOperator(), Cancel);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("request.forbidden", await response.ErrorCode());
    }

    [Fact]
    public async Task The_owner_adds_an_operator_who_can_then_log_in()
    {
        using var owner = await factory.CreateClientAsAsync(Role.Owner);
        var mobile = ApiFactory.NextMobile();

        var created = await owner.PostAsJsonAsync("/panel/users", NewOperator(mobile), Cancel);

        Assert.Equal(HttpStatusCode.OK, created.StatusCode);
        Assert.NotEqual(Guid.Empty, (await created.Body()).GetProperty("id").GetGuid());

        using var newcomer = factory.CreateBrowserClient();
        var session = await factory.LoginAsync(newcomer, mobile);
        Assert.Equal(Role.Operator, session.User.Role);
    }

    [Fact]
    public async Task A_mobile_already_in_use_is_a_conflict()
    {
        using var owner = await factory.CreateClientAsAsync(Role.Owner);
        var taken = await factory.CreateUserAsync(Role.Manager);

        var response = await owner.PostAsJsonAsync("/panel/users", NewOperator(taken), Cancel);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("identity.mobile_taken", await response.ErrorCode());
    }

    [Fact]
    public async Task The_owner_cannot_add_another_owner()
    {
        using var owner = await factory.CreateClientAsAsync(Role.Owner);

        var response = await owner.PostAsJsonAsync(
            "/panel/users",
            new { fullName = "Second Owner", mobileNumber = ApiFactory.NextMobile(), role = "owner" },
            Cancel);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = Assert.Single((await response.Body()).GetProperty("errors").EnumerateArray());
        Assert.Equal("role", error.GetProperty("field").GetString());
        Assert.Equal("identity.role_not_allowed", error.GetProperty("code").GetString());
    }

    [Fact]
    public async Task A_role_sent_as_a_number_is_refused()
    {
        using var owner = await factory.CreateClientAsAsync(Role.Owner);

        var response = await owner.PostAsJsonAsync(
            "/panel/users",
            new { fullName = "Someone", mobileNumber = ApiFactory.NextMobile(), role = 2 },
            Cancel);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("request.malformed", await response.ErrorCode());
    }

    [Fact]
    public async Task A_token_signed_with_another_key_is_refused()
    {
        using var client = factory.CreateBrowserClient();
        var forged = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = "yalfan",
            Audience = "yalfan",
            Claims = new Dictionary<string, object> { ["sub"] = Guid.NewGuid().ToString(), ["role"] = "Owner" },
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(RandomNumberGenerator.GetBytes(32)), SecurityAlgorithms.HmacSha256),
        });
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", forged);

        var response = await client.PostAsJsonAsync("/panel/users", NewOperator(), Cancel);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task An_unsigned_token_is_refused()
    {
        using var client = factory.CreateBrowserClient();
        var header = Base64Url("{\"alg\":\"none\",\"typ\":\"JWT\"}");
        var payload = Base64Url($"{{\"sub\":\"{Guid.NewGuid()}\",\"role\":\"Owner\",\"iss\":\"yalfan\",\"aud\":\"yalfan\"}}");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", $"{header}.{payload}.");

        var response = await client.GetAsync("/panel/auth/me", Cancel);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static string Base64Url(string json) =>
        Convert.ToBase64String(Encoding.UTF8.GetBytes(json)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
