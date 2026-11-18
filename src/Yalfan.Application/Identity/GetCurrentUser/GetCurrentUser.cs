using Microsoft.EntityFrameworkCore;
using Yalfan.Application.Common;
using Yalfan.Application.Common.Handlers;
using Yalfan.Application.Common.Security;
using Yalfan.Application.Identity.Sessions;
using Yalfan.Domain.Common;
using Yalfan.Domain.Identity;

namespace Yalfan.Application.Identity.GetCurrentUser;

public sealed record GetCurrentUserQuery;

internal sealed class GetCurrentUserHandler(IAppDbContext db, ICurrentUser currentUser)
    : IQueryHandler<GetCurrentUserQuery, UserSummary>
{
    public async Task<Result<UserSummary>> Handle(GetCurrentUserQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return IdentityErrors.UserNotFound;
        }

        var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Id == userId, cancellationToken);

        return user is null ? IdentityErrors.UserNotFound : UserSummary.From(user);
    }
}
