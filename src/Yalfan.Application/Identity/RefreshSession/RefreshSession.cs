using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Yalfan.Application.Common;
using Yalfan.Application.Common.Handlers;
using Yalfan.Application.Identity.Sessions;
using Yalfan.Domain.Common;
using Yalfan.Domain.Identity;

namespace Yalfan.Application.Identity.RefreshSession;

/// <summary>The refresh token comes from the HttpOnly cookie, never from the body.</summary>
public sealed record RefreshSessionCommand(string? RefreshToken);

/// <summary>
/// Renews a session and rotates its refresh token. A token used again after it was replaced
/// means it was copied: the whole family is revoked, which logs out the thief and the owner,
/// who then logs in again with a new code.
/// </summary>
internal sealed partial class RefreshSessionHandler(
    IAppDbContext db,
    SessionIssuer sessions,
    IClock clock,
    ILogger<RefreshSessionHandler> logger) : ICommandHandler<RefreshSessionCommand, AuthSession>
{
    public async Task<Result<AuthSession>> Handle(RefreshSessionCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(command.RefreshToken))
        {
            return IdentityErrors.RefreshInvalid;
        }

        var hash = SessionIssuer.HashRefreshToken(command.RefreshToken);
        var token = await db.RefreshTokens.SingleOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);
        if (token is null)
        {
            return IdentityErrors.RefreshInvalid;
        }

        var now = clock.UtcNow;

        if (!token.IsActive(now))
        {
            if (token.WasJustRotated(now))
            {
                return IdentityErrors.RefreshSuperseded;
            }

            if (token.ReplacedById is not null)
            {
                LogReuse(logger, token.UserId, token.FamilyId);
                await RevokeFamily(token.FamilyId, now, cancellationToken);
                await db.SaveChangesAsync(cancellationToken);
            }

            return IdentityErrors.RefreshInvalid;
        }

        var user = await db.Users.SingleOrDefaultAsync(u => u.Id == token.UserId, cancellationToken);
        if (user is null || !user.IsActive)
        {
            token.Revoke(now);
            await db.SaveChangesAsync(cancellationToken);
            return IdentityErrors.RefreshInvalid;
        }

        var session = sessions.Renew(user, token);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return session;
        }
        catch (DbUpdateConcurrencyException)
        {
            // Another tab renewed with the same token at the same moment and won.
            return IdentityErrors.RefreshSuperseded;
        }
    }

    private async Task RevokeFamily(Guid familyId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var active = await db.RefreshTokens
            .Where(t => t.FamilyId == familyId && t.RevokedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var token in active)
        {
            token.Revoke(now);
        }
    }

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "Refresh token reused after rotation for user {UserId}; session family {FamilyId} revoked")]
    private static partial void LogReuse(ILogger logger, Guid userId, Guid familyId);
}
