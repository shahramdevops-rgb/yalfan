using System.Security.Cryptography;
using System.Text;
using Yalfan.Domain.Common;

namespace Yalfan.Domain.Identity;

/// <summary>
/// A one-time login code sent by SMS. Only its hash is stored, so a database backup or a log of
/// the table never shows a code that still works.
/// </summary>
public sealed class OtpCode : Entity
{
    /// <summary>Long enough to read an SMS and type six digits; short enough to be useless when stolen later.</summary>
    public static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(2);

    /// <summary>After this many wrong tries the code is dead. Six digits and five tries: one chance in 200,000.</summary>
    public const int MaxFailedAttempts = 5;

    private OtpCode()
    {
    }

    private OtpCode(Guid userId, string codeHash, DateTimeOffset createdAt)
    {
        UserId = userId;
        CodeHash = codeHash;
        CreatedAt = createdAt;
        ExpiresAt = createdAt + Lifetime;
    }

    public Guid UserId { get; private set; }

    public string CodeHash { get; private set; } = null!;

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset ExpiresAt { get; private set; }

    public int FailedAttempts { get; private set; }

    public DateTimeOffset? ConsumedAt { get; private set; }

    public static OtpCode Issue(Guid userId, string code, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        return new OtpCode(userId, Hash(userId, code), now);
    }

    public bool IsUsable(DateTimeOffset now) =>
        ConsumedAt is null && FailedAttempts < MaxFailedAttempts && now < ExpiresAt;

    /// <summary>
    /// Checks a typed code. A right code is used up; a wrong one counts against the code.
    /// </summary>
    public Result Verify(string code, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(code);

        if (!IsUsable(now))
        {
            return IdentityErrors.OtpExpired;
        }

        var candidate = Encoding.ASCII.GetBytes(Hash(UserId, code));
        var expected = Encoding.ASCII.GetBytes(CodeHash);

        // Constant-time comparison: the time taken must not reveal how much of the code matched.
        if (!CryptographicOperations.FixedTimeEquals(candidate, expected))
        {
            FailedAttempts++;
            return IdentityErrors.OtpInvalid;
        }

        ConsumedAt = now;
        return Result.Success();
    }

    private static string Hash(Guid userId, string code) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{userId:N}:{code}")));
}
