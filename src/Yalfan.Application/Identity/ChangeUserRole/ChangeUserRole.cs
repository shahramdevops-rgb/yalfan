using FluentValidation;
using Yalfan.Application.Common;
using Yalfan.Application.Common.Handlers;
using Yalfan.Domain.Common;
using Yalfan.Domain.Identity;

namespace Yalfan.Application.Identity.ChangeUserRole;

public sealed record ChangeUserRoleCommand(Guid Id, Role? Role);

public sealed class ChangeUserRoleValidator : AbstractValidator<ChangeUserRoleCommand>
{
    public ChangeUserRoleValidator()
    {
        RuleFor(command => command.Role)
            .NotNull()
            .Must(role => role is Role.Manager or Role.Operator)
            .WithErrorCode(IdentityErrors.RoleNotAllowed.Code);
    }
}

/// <summary>
/// Moves a user between manager and operator (decided 1405/09/05, ADR 0026): without it a role
/// chosen by mistake could never be fixed, since his number stays taken by his own account. His
/// sessions go on; the new role is in his next access token, within ten minutes.
/// </summary>
internal sealed class ChangeUserRoleHandler(IAppDbContext db) : ICommandHandler<ChangeUserRoleCommand, StaffUserDetails>
{
    public async Task<Result<StaffUserDetails>> Handle(ChangeUserRoleCommand command, CancellationToken cancellationToken)
    {
        var user = await StaffUsers.FindAsync(db, command.Id, cancellationToken);
        if (user is null)
        {
            return IdentityErrors.UserNotFound;
        }

        var changed = user.ChangeRole(command.Role!.Value);
        if (changed.IsFailure)
        {
            return changed.Error;
        }

        await db.SaveChangesAsync(cancellationToken);
        return StaffUsers.Details(user);
    }
}
