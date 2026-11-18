using Microsoft.EntityFrameworkCore;
using Yalfan.Application.Common;
using Yalfan.Application.Common.Handlers;
using Yalfan.Application.Identity.Sessions;
using Yalfan.Domain.Common;

namespace Yalfan.Application.Identity.Logout;

public sealed record LogoutCommand(string? RefreshToken);

/// <summary>Whether a session was found and ended. Logging out always succeeds.</summary>
public sealed record LogoutResult(bool SessionEnded);

/// <summary>Ends the session of this device: every token of its family is revoked.</summary>
internal sealed class LogoutHandler(IAppDbContext db, IClock clock) : ICommandHandler<LogoutCommand, LogoutResult>
{
    public async Task<Result<LogoutResult>> Handle(LogoutCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(command.RefreshToken))
        {
            return new LogoutResult(SessionEnded: false);
        }

        var hash = SessionIssuer.HashRefreshToken(command.RefreshToken);
        var familyId = await db.RefreshTokens
            .Where(t => t.TokenHash == hash)
            .Select(t => (Guid?)t.FamilyId)
            .SingleOrDefaultAsync(cancellationToken);
        if (familyId is null)
        {
            return new LogoutResult(SessionEnded: false);
        }

        var now = clock.UtcNow;
        var active = await db.RefreshTokens
            .Where(t => t.FamilyId == familyId && t.RevokedAt == null)
            .ToListAsync(cancellationToken);
        foreach (var token in active)
        {
            token.Revoke(now);
        }

        await db.SaveChangesAsync(cancellationToken);
        return new LogoutResult(SessionEnded: true);
    }
}
