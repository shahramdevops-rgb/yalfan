using Yalfan.Application.Common;
using Yalfan.Application.Common.Handlers;
using Yalfan.Application.Identity.Sessions;
using Yalfan.Domain.Common;
using Yalfan.Domain.Identity;

namespace Yalfan.Application.Identity.SetUserActive;

public sealed record SetUserActiveCommand(Guid Id, bool IsActive);

/// <summary>
/// Deactivates a manager or an operator, or activates him again. Deactivating ends every session
/// he has in the same save; his open page works until its access token expires (at most ten
/// minutes). There is no delete: the audit names him. An owner account is never deactivated in
/// the panel (ADR 0026).
/// </summary>
internal sealed class SetUserActiveHandler(IAppDbContext db, IClock clock)
    : ICommandHandler<SetUserActiveCommand, StaffUserDetails>
{
    public async Task<Result<StaffUserDetails>> Handle(SetUserActiveCommand command, CancellationToken cancellationToken)
    {
        var user = await StaffUsers.FindAsync(db, command.Id, cancellationToken);
        if (user is null)
        {
            return IdentityErrors.UserNotFound;
        }

        var wasActive = user.IsActive;
        var changed = command.IsActive ? user.Activate() : user.Deactivate();
        if (changed.IsFailure)
        {
            return changed.Error;
        }

        if (wasActive && !command.IsActive)
        {
            await UserLogin.EndAllSessionsAsync(db, user.Id, clock.UtcNow, cancellationToken);
        }

        await db.SaveChangesAsync(cancellationToken);
        return StaffUsers.Details(user);
    }
}
