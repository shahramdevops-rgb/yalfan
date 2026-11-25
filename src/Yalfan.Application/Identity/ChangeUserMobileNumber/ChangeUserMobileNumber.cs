using FluentValidation;
using Yalfan.Application.Common;
using Yalfan.Application.Common.Handlers;
using Yalfan.Application.Common.Validation;
using Yalfan.Application.Identity.Sessions;
using Yalfan.Domain.Common;
using Yalfan.Domain.Identity;

namespace Yalfan.Application.Identity.ChangeUserMobileNumber;

public sealed record ChangeUserMobileNumberCommand(Guid Id, string? MobileNumber);

public sealed class ChangeUserMobileNumberValidator : AbstractValidator<ChangeUserMobileNumberCommand>
{
    public ChangeUserMobileNumberValidator()
    {
        RuleFor(command => command.MobileNumber).MustBeMobileNumber();
    }
}

/// <summary>
/// Gives a manager or an operator a new number when his phone is lost (ADR 0012). Every session
/// he has ends in the same save, so the lost phone cannot renew; he logs in again with the new
/// number. An owner account's numbers come from the server's configuration (ADR 0026).
/// </summary>
internal sealed class ChangeUserMobileNumberHandler(IAppDbContext db, IClock clock)
    : ICommandHandler<ChangeUserMobileNumberCommand, StaffUserDetails>
{
    public async Task<Result<StaffUserDetails>> Handle(ChangeUserMobileNumberCommand command, CancellationToken cancellationToken)
    {
        var mobile = MobileNumber.Create(command.MobileNumber).Value;

        var user = await StaffUsers.FindAsync(db, command.Id, cancellationToken);
        if (user is null)
        {
            return IdentityErrors.UserNotFound;
        }

        if (user.IsOwnerAccount)
        {
            return IdentityErrors.OwnerLocked;
        }

        // The same number again changes nothing, and ends no session.
        if (user.HasLoginNumber(mobile))
        {
            return StaffUsers.Details(user);
        }

        if (await StaffUsers.MobileTakenAsync(db, mobile, user.Id, cancellationToken))
        {
            return IdentityErrors.MobileTaken;
        }

        var changed = user.ChangeMobileNumber(mobile);
        if (changed.IsFailure)
        {
            return changed.Error;
        }

        await UserLogin.EndAllSessionsAsync(db, user.Id, clock.UtcNow, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return StaffUsers.Details(user);
    }
}
