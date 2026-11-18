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

public sealed record UserSummary(Guid Id, string FullName, string MobileNumber, Role Role)
{
    public static UserSummary From(User user)
    {
        ArgumentNullException.ThrowIfNull(user);
        return new UserSummary(user.Id, user.FullName, user.MobileNumber.Value, user.Role);
    }
}
