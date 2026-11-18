using System.Net.Http.Json;
using System.Text.Json;

namespace Yalfan.Api.IntegrationTests.Auth;

internal static class AuthTestHelpers
{
    public const string RefreshCookie = "yalfan_refresh";

    public static CancellationToken Cancel => TestContext.Current.CancellationToken;

    public static Task<HttpResponseMessage> RequestCode(this HttpClient client, string mobile) =>
        client.PostAsJsonAsync("/panel/auth/otp", new { mobileNumber = mobile }, Cancel);

    public static Task<HttpResponseMessage> Login(this HttpClient client, string mobile, string code) =>
        client.PostAsJsonAsync("/panel/auth/login", new { mobileNumber = mobile, code }, Cancel);

    /// <summary>Renews with an explicit refresh token, as a copied cookie would be sent.</summary>
    public static Task<HttpResponseMessage> RefreshWith(this HttpClient client, string? refreshToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/panel/auth/refresh");
        if (refreshToken is not null)
        {
            request.Headers.Add("Cookie", $"{RefreshCookie}={refreshToken}");
        }

        return client.SendAsync(request, Cancel);
    }

    /// <summary>The raw Set-Cookie header for the refresh cookie, or null.</summary>
    public static string? RefreshSetCookie(this HttpResponseMessage response) =>
        response.Headers.TryGetValues("Set-Cookie", out var values)
            ? values.FirstOrDefault(value => value.StartsWith(RefreshCookie + "=", StringComparison.Ordinal))
            : null;

    /// <summary>The refresh token value set by this response.</summary>
    public static string RefreshToken(this HttpResponseMessage response)
    {
        var header = response.RefreshSetCookie() ?? throw new InvalidOperationException("No refresh cookie was set.");
        return header[(RefreshCookie.Length + 1)..header.IndexOf(';', StringComparison.Ordinal)];
    }

    public static async Task<string?> ErrorCode(this HttpResponseMessage response)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(Cancel));
        return document.RootElement.TryGetProperty("code", out var code) ? code.GetString() : null;
    }

    public static async Task<JsonElement> Body(this HttpResponseMessage response)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(Cancel));
        return document.RootElement.Clone();
    }
}
