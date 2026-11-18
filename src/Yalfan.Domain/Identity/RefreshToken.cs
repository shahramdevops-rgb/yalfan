using Yalfan.Domain.Common;

namespace Yalfan.Domain.Identity;

/// <summary>
/// One link in a login session. Each renewal revokes the presented token and issues the next
/// one in the same family (rotation). Only a hash of the token is stored.
/// </summary>
public sealed class RefreshToken : Entity
{
    /// <summary>
    /// Two tabs of the panel can renew at the same moment and send the same token. Inside this
    /// window a reused token is treated as that race, not as theft.
    /// </summary>
    public static readonly TimeSpan ReuseGrace = TimeSpan.FromSeconds(30);

    private RefreshToken()
    {
    }

    private RefreshToken(Guid userId, Guid familyId, string tokenHash, DateTimeOffset createdAt, TimeSpan lifetime)
    {
        UserId = userId;
        FamilyId = familyId;
        TokenHash = tokenHash;
        CreatedAt = createdAt;
        ExpiresAt = createdAt + lifetime;
    }

    public Guid UserId { get; private set; }

    /// <summary>Every token of one login shares the family; theft revokes the whole family.</summary>
    public Guid FamilyId { get; private set; }

    public string TokenHash { get; private set; } = null!;

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset ExpiresAt { get; private set; }

    public DateTimeOffset? RevokedAt { get; private set; }

    /// <summary>The token that replaced this one, when it was revoked by a renewal.</summary>
    public Guid? ReplacedById { get; private set; }

    /// <summary>The first token of a new login.</summary>
    public static RefreshToken StartFamily(Guid userId, string tokenHash, DateTimeOffset now, TimeSpan lifetime)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tokenHash);
        return new RefreshToken(userId, Guid.CreateVersion7(), tokenHash, now, lifetime);
    }

    public bool IsActive(DateTimeOffset now) => RevokedAt is null && now < ExpiresAt;

    /// <summary>True when this token was replaced by a renewal only a moment ago (two tabs).</summary>
    public bool WasJustRotated(DateTimeOffset now) =>
        ReplacedById is not null && RevokedAt is { } revokedAt && now - revokedAt <= ReuseGrace;

    /// <summary>Revokes this token and returns its successor, valid for <paramref name="lifetime"/> from now.</summary>
    public RefreshToken Rotate(string newTokenHash, DateTimeOffset now, TimeSpan lifetime)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(newTokenHash);
        if (!IsActive(now))
        {
            throw new InvalidOperationException("Only an active refresh token can be rotated.");
        }

        var next = new RefreshToken(UserId, FamilyId, newTokenHash, now, lifetime);
        RevokedAt = now;
        ReplacedById = next.Id;
        return next;
    }

    public void Revoke(DateTimeOffset now)
    {
        RevokedAt ??= now;
    }
}
