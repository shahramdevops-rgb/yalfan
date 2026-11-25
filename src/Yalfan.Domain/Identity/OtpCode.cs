using System.Security.Cryptography;
using System.Text;
using Yalfan.Domain.Common;

namespace Yalfan.Domain.Identity;

/// <summary>
/// A one-time login code sent by SMS to one of the user's numbers. Only its hash is stored, so a
/// database backup or a log of the table never shows a code that still works. A code works only
/// with the number it was sent to (ADR 0026).
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

    private OtpCode(Guid userId, MobileNumber mobileNumber, string codeHash, DateTimeOffset createdAt)
    {
        UserId = userId;
        MobileNumber = mobileNumber;
        CodeHash = codeHash;
        CreatedAt = createdAt;
        ExpiresAt = createdAt + Lifetime;
    }

    public Guid UserId { get; private set; }

    /// <summary>
    /// The number the code was sent to. The waits and limits count per number, so a thief asking
    /// for codes on a stolen phone cannot keep the owner from getting one on his other phone.
    /// </summary>
    public MobileNumber MobileNumber { get; private set; } = null!;

    public string CodeHash { get; private set; } = null!;

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset ExpiresAt { get; private set; }

    public int FailedAttempts { get; private set; }

    public DateTimeOffset? ConsumedAt { get; private set; }

    public static OtpCode Issue(Guid userId, MobileNumber mobileNumber, string code, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(mobileNumber);
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        return new OtpCode(userId, mobileNumber, Hash(userId, mobileNumber, code), now);
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

        var candidate = Encoding.ASCII.GetBytes(Hash(UserId, MobileNumber, code));
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

    private static string Hash(Guid userId, MobileNumber mobileNumber, string code) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{userId:N}:{mobileNumber.Value}:{code}")));
}
