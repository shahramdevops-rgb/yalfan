using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Yalfan.Application.Common;
using Yalfan.Application.Common.Handlers;
using Yalfan.Application.Common.Validation;
using Yalfan.Application.Identity.Sessions;
using Yalfan.Domain.Common;
using Yalfan.Domain.Identity;

namespace Yalfan.Application.Identity.Login;

public sealed record LoginCommand(string? MobileNumber, string? Code);

public sealed class LoginValidator : AbstractValidator<LoginCommand>
{
    public LoginValidator()
    {
        RuleFor(command => command.MobileNumber).MustBeMobileNumber();
        RuleFor(command => command.Code)
            .NotEmpty()
            .Must(code => code is not null && Normalize(code) is { Length: 6 } digits && Digits.AreAllLatin(digits))
            .WithErrorCode("validation.invalid_format");
    }

    /// <summary>The code may be typed with Persian digits or pasted with spaces.</summary>
    internal static string Normalize(string code) => Digits.ToLatin(code).Trim();
}

/// <summary>
/// Checks the newest code sent to this number and starts a session. A code sent to another of
/// the user's numbers does not count (ADR 0026): a code taken on a stolen phone before its
/// number was switched off must not work with the owner's other number.
/// </summary>
internal sealed class LoginHandler(IAppDbContext db, SessionIssuer sessions, IClock clock)
    : ICommandHandler<LoginCommand, AuthSession>
{
    public async Task<Result<AuthSession>> Handle(LoginCommand command, CancellationToken cancellationToken)
    {
        var mobile = MobileNumber.Create(command.MobileNumber).Value;

        var user = await db.Users.SingleOrDefaultAsync(UserLogin.ByActiveNumber(mobile), cancellationToken);
        if (user is null)
        {
            // The same answer as a wrong code, so the endpoint cannot be used to test numbers.
            return IdentityErrors.OtpInvalid;
        }

        // Only the newest code counts: asking for a new code silently retires the older ones.
        var otp = await db.OtpCodes
            .Where(code => code.UserId == user.Id && code.MobileNumber == mobile)
            .OrderByDescending(code => code.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (otp is null)
        {
            return IdentityErrors.OtpExpired;
        }

        var verified = otp.Verify(LoginValidator.Normalize(command.Code!), clock.UtcNow);
        if (verified.IsFailure)
        {
            // Saves the wrong try, so the five-try limit holds across requests.
            return await SaveOr(verified.Error, verified.Error, cancellationToken);
        }

        var session = sessions.Start(user);

        // Two requests with the same right code: the code's row version lets only one of them
        // be saved; the other finds the code used.
        return await SaveOr(session, IdentityErrors.OtpExpired, cancellationToken);
    }

    private async Task<Result<AuthSession>> SaveOr(Result<AuthSession> outcome, Error onConflict, CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return outcome;
        }
        catch (DbUpdateConcurrencyException)
        {
            return onConflict;
        }
    }
}
