using FluentValidation;
using Yalfan.Application.Common;
using Yalfan.Application.Common.Handlers;
using Yalfan.Domain.Common;
using Yalfan.Domain.Identity;

namespace Yalfan.Application.Identity.RenameUser;

public sealed record RenameUserCommand(Guid Id, string? FullName);

public sealed class RenameUserValidator : AbstractValidator<RenameUserCommand>
{
    public RenameUserValidator()
    {
        RuleFor(command => command.FullName).NotEmpty().MaximumLength(User.FullNameMaxLength);
    }
}

/// <summary>Corrects a user's name, an owner account's too: the name only labels the person.</summary>
internal sealed class RenameUserHandler(IAppDbContext db) : ICommandHandler<RenameUserCommand, StaffUserDetails>
{
    public async Task<Result<StaffUserDetails>> Handle(RenameUserCommand command, CancellationToken cancellationToken)
    {
        var user = await StaffUsers.FindAsync(db, command.Id, cancellationToken);
        if (user is null)
        {
            return IdentityErrors.UserNotFound;
        }

        var renamed = user.Rename(command.FullName);
        if (renamed.IsFailure)
        {
            return renamed.Error;
        }

        await db.SaveChangesAsync(cancellationToken);
        return StaffUsers.Details(user);
    }
}
