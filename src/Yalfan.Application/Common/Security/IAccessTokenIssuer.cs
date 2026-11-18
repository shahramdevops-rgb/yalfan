using Yalfan.Domain.Identity;

namespace Yalfan.Application.Common.Security;

/// <summary>Creates the signed, short-lived access token (a JWT) that the API checks on every request.</summary>
public interface IAccessTokenIssuer
{
    string Issue(User user, DateTimeOffset issuedAt, DateTimeOffset expiresAt);
}
