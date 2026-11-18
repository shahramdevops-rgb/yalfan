using System.Globalization;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Yalfan.Application.Common.Security;
using Yalfan.Domain.Identity;

namespace Yalfan.Infrastructure.Identity;

internal sealed class JwtAccessTokenIssuer(IOptions<JwtOptions> options) : IAccessTokenIssuer
{
    // The handler would otherwise stamp the times from the system clock; they come from IClock.
    private static readonly JsonWebTokenHandler Handler = new() { SetDefaultTimesOnTokenCreation = false };

    public string Issue(User user, DateTimeOffset issuedAt, DateTimeOffset expiresAt)
    {
        ArgumentNullException.ThrowIfNull(user);
        var settings = options.Value;

        return Handler.CreateToken(new SecurityTokenDescriptor
        {
            Issuer = settings.Issuer,
            Audience = settings.Audience,
            IssuedAt = issuedAt.UtcDateTime,
            NotBefore = issuedAt.UtcDateTime,
            Expires = expiresAt.UtcDateTime,
            Claims = new Dictionary<string, object>
            {
                [JwtClaimNames.Subject] = user.Id.ToString("D", CultureInfo.InvariantCulture),
                [JwtClaimNames.Name] = user.FullName,
                [JwtClaimNames.Role] = user.Role.ToString(),
            },
            SigningCredentials = new SigningCredentials(settings.CreateSigningKey(), SecurityAlgorithms.HmacSha256),
        });
    }
}
