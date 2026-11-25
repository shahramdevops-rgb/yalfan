using Yalfan.Domain.Identity;

namespace Yalfan.Application.Identity.Sessions;

/// <summary>
/// A started or renewed session. The Api sends the access token in the body and the refresh
/// token only in an HttpOnly cookie.
/// </summary>
public sealed record AuthSession(
    UserSummary User,
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt);

/// <summary>
/// Who is logged in. No mobile number: an owner account has several, and the panel needs none
/// (ADR 0026).
/// </summary>
public sealed record UserSummary(Guid Id, string FullName, Role Role)
{
    public static UserSummary From(User user)
    {
        ArgumentNullException.ThrowIfNull(user);
        return new UserSummary(user.Id, user.FullName, user.Role);
    }
}
