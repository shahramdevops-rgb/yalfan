using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;
using Yalfan.Application.Common;
using Yalfan.Application.Common.Security;
using Yalfan.Domain.Identity;

namespace Yalfan.Application.Identity.Sessions;

/// <summary>Starts and renews sessions: one access token plus the next refresh token of the family.</summary>
internal sealed class SessionIssuer(IAppDbContext db, IAccessTokenIssuer accessTokens, IClock clock)
{
    public AuthSession Start(User user)
    {
        var now = clock.UtcNow;
        var raw = NewRefreshToken();
        var token = RefreshToken.StartFamily(user.Id, HashRefreshToken(raw), now, SessionPolicy.RefreshTokenLifetime);
        db.RefreshTokens.Add(token);

        return Build(user, raw, token, now);
    }

    public AuthSession Renew(User user, RefreshToken current)
    {
        var now = clock.UtcNow;
        var raw = NewRefreshToken();
        var next = current.Rotate(HashRefreshToken(raw), now, SessionPolicy.RefreshTokenLifetime);
        db.RefreshTokens.Add(next);

        return Build(user, raw, next, now);
    }

    /// <summary>
    /// SHA-256 without salt is enough here: the token is 256 random bits, so there is nothing to
    /// guess, and the hash only keeps a stolen backup from holding working tokens.
    /// </summary>
    public static string HashRefreshToken(string raw) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));

    private static string NewRefreshToken() => Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(32));

    private AuthSession Build(User user, string rawRefreshToken, RefreshToken refreshToken, DateTimeOffset now)
    {
        var accessExpiresAt = now + SessionPolicy.AccessTokenLifetime;
        var accessToken = accessTokens.Issue(user, now, accessExpiresAt);

        return new AuthSession(
            UserSummary.From(user),
            accessToken,
            accessExpiresAt,
            rawRefreshToken,
            refreshToken.ExpiresAt);
    }
}
