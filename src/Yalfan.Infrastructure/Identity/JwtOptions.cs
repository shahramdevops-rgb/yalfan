using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Yalfan.Infrastructure.Identity;

/// <summary>
/// Settings of the access token. The signing key is a secret: it comes from user-secrets or
/// the environment (<c>Jwt__SigningKey</c>), never from the repository. Without a key of at
/// least 32 bytes the API refuses to start.
/// </summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    /// <summary>HMAC-SHA256 needs a key at least as long as its output.</summary>
    public const int MinimumKeyBytes = 32;

    public string Issuer { get; set; } = "yalfan";

    public string Audience { get; set; } = "yalfan";

    public string SigningKey { get; set; } = string.Empty;

    public SymmetricSecurityKey CreateSigningKey() => new(Encoding.UTF8.GetBytes(SigningKey));
}

/// <summary>Claim names inside the access token: short JWT names, not the long .NET URIs.</summary>
public static class JwtClaimNames
{
    public const string Subject = "sub";
    public const string Name = "name";
    public const string Role = "role";
}
