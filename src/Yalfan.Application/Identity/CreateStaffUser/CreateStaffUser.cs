using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Yalfan.Application.Common;
using Yalfan.Application.Common.Handlers;
using Yalfan.Application.Common.Validation;
using Yalfan.Domain.Common;
using Yalfan.Domain.Identity;

namespace Yalfan.Application.Identity.CreateStaffUser;

public sealed record CreateStaffUserCommand(string? FullName, string? MobileNumber, Role? Role);

public sealed record CreateStaffUserResponse(Guid Id);

public sealed class CreateStaffUserValidator : AbstractValidator<CreateStaffUserCommand>
{
    public CreateStaffUserValidator()
    {
        RuleFor(command => command.FullName).NotEmpty().MaximumLength(User.FullNameMaxLength);
        RuleFor(command => command.MobileNumber).MustBeMobileNumber();

        // Decided 1405/08/27: the owner adds only managers and operators. A second owner would
        // hold everything the owner holds (later: approving payouts), so that is his own decision
        // and not something the panel offers.
        RuleFor(command => command.Role)
            .NotNull()
            .Must(role => role is Role.Manager or Role.Operator)
            .WithErrorCode("identity.role_not_allowed");
    }
}

/// <summary>The owner adds a manager or an operator. The new user logs in with an SMS code.</summary>
internal sealed class CreateStaffUserHandler(IAppDbContext db, IClock clock)
    : ICommandHandler<CreateStaffUserCommand, CreateStaffUserResponse>
{
    public async Task<Result<CreateStaffUserResponse>> Handle(CreateStaffUserCommand command, CancellationToken cancellationToken)
    {
        var mobile = MobileNumber.Create(command.MobileNumber).Value;

        if (await db.Users.AnyAsync(u => u.MobileNumber == mobile, cancellationToken))
        {
            return IdentityErrors.MobileTaken;
        }

        var created = User.Create(command.FullName, mobile, command.Role!.Value, clock.UtcNow);
        if (created.IsFailure)
        {
            return created.Error;
        }

        db.Users.Add(created.Value);
        await db.SaveChangesAsync(cancellationToken);

        return new CreateStaffUserResponse(created.Value.Id);
    }
}
