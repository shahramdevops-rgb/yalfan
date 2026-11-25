using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Yalfan.Application.Common;
using Yalfan.Domain.Common;
using Yalfan.Domain.Identity;

namespace Yalfan.Application.Identity.Sessions;

/// <summary>Who may log in with a number, and ending a user's sessions.</summary>
internal static class UserLogin
{
    /// <summary>
    /// The active user holding this number, switched on. Numbers are unique among all users, so
    /// there is at most one.
    /// </summary>
    public static Expression<Func<User, bool>> ByActiveNumber(MobileNumber mobile) =>
        user => user.IsActive && user.LoginNumbers.Any(number => number.MobileNumber == mobile && number.IsActive);

    /// <summary>
    /// Revokes every session of the user, on every device, in the caller's save. His open pages
    /// keep their access token until it expires (at most ten minutes, ADR 0026), then cannot renew.
    /// </summary>
    public static async Task EndAllSessionsAsync(IAppDbContext db, Guid userId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var active = await db.RefreshTokens
            .Where(token => token.UserId == userId && token.RevokedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var token in active)
        {
            token.Revoke(now);
        }
    }
}
