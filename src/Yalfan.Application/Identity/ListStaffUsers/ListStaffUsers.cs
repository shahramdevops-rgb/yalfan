using Microsoft.EntityFrameworkCore;
using Yalfan.Application.Common;
using Yalfan.Application.Common.Handlers;
using Yalfan.Domain.Common;
using Yalfan.Domain.Identity;

namespace Yalfan.Application.Identity.ListStaffUsers;

public sealed record ListStaffUsersQuery;

/// <summary>
/// Every user, for the owner's users screen: the owner accounts first, then the others in Persian
/// alphabetical order, the inactive ones in place. A handful of people, so no search or pages.
/// </summary>
internal sealed class ListStaffUsersHandler(IAppDbContext db)
    : IQueryHandler<ListStaffUsersQuery, IReadOnlyList<StaffUserDetails>>
{
    public async Task<Result<IReadOnlyList<StaffUserDetails>>> Handle(ListStaffUsersQuery query, CancellationToken cancellationToken)
    {
        var users = await db.Users.AsNoTracking()
            .OrderBy(user => user.Role == Role.Owner ? 0 : 1)
            .ThenBy(user => user.FullName)
            .ThenBy(user => user.Id)
            .ToListAsync(cancellationToken);

        return users.Select(StaffUsers.Details).ToList();
    }
}
