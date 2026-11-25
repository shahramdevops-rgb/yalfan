using Yalfan.Application.Common;
using Yalfan.Application.Common.Handlers;
using Yalfan.Application.Common.Security;
using Yalfan.Application.Identity.Sessions;
using Yalfan.Domain.Common;
using Yalfan.Domain.Identity;

namespace Yalfan.Application.Identity.SetLoginNumberActive;

public sealed record SetLoginNumberActiveCommand(Guid UserId, Guid LoginNumberId, bool IsActive);

/// <summary>
/// Switches one of an account's numbers off or on: what is done when the owner's phone is stolen
/// (ADR 0026). Switching off ends every session of the account, on every device, not only those
/// opened with that number, because nobody knows which number's code the stolen phone used; the
/// owner logs in again with his other number. One number always stays on.
/// </summary>
/// <remarks>
/// Only another owner account switches a number back on. The stolen phone's open page keeps its
/// access token for up to ten minutes after its number is switched off; were the account allowed
/// to switch its own numbers on, the thief would switch the stolen number on again and ask for a
/// new code (found by the review of 1.6.5).
/// </remarks>
internal sealed class SetLoginNumberActiveHandler(IAppDbContext db, IClock clock, ICurrentUser currentUser)
    : ICommandHandler<SetLoginNumberActiveCommand, StaffUserDetails>
{
    public async Task<Result<StaffUserDetails>> Handle(SetLoginNumberActiveCommand command, CancellationToken cancellationToken)
    {
        var user = await StaffUsers.FindAsync(db, command.UserId, cancellationToken);
        if (user is null)
        {
            return IdentityErrors.UserNotFound;
        }

        var number = user.LoginNumbers.FirstOrDefault(n => n.Id == command.LoginNumberId);
        if (command.IsActive && number is { IsActive: false } && currentUser.UserId == user.Id)
        {
            return IdentityErrors.OwnNumberSwitchOn;
        }

        var wasActive = number?.IsActive;

        var changed = command.IsActive
            ? user.ActivateLoginNumber(command.LoginNumberId)
            : user.DeactivateLoginNumber(command.LoginNumberId);
        if (changed.IsFailure)
        {
            return changed.Error;
        }

        // Switching off a number that was already off changes nothing and ends no session.
        if (!command.IsActive && wasActive == true)
        {
            await UserLogin.EndAllSessionsAsync(db, user.Id, clock.UtcNow, cancellationToken);
        }

        await db.SaveChangesAsync(cancellationToken);
        return StaffUsers.Details(user);
    }
}
