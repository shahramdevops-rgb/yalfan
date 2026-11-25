using System.Globalization;
using System.Security.Cryptography;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Yalfan.Application.Common;
using Yalfan.Application.Common.Handlers;
using Yalfan.Application.Common.Sms;
using Yalfan.Application.Common.Validation;
using Yalfan.Application.Identity.Sessions;
using Yalfan.Domain.Common;
using Yalfan.Domain.Identity;

namespace Yalfan.Application.Identity.RequestLoginCode;

public sealed record RequestLoginCodeCommand(string? MobileNumber);

/// <summary>What the panel needs for its countdown. The same answer comes back for every number.</summary>
public sealed record LoginCodeRequested(int ExpiresInSeconds, int ResendAfterSeconds);

public sealed class RequestLoginCodeValidator : AbstractValidator<RequestLoginCodeCommand>
{
    public RequestLoginCodeValidator()
    {
        RuleFor(command => command.MobileNumber).MustBeMobileNumber();
    }
}

/// <summary>
/// Sends a login code to a switched-on number of an active user. A number with no active account,
/// a switched-off number, or one that asked too often, gets the same answer and no SMS: the
/// endpoint must not reveal which numbers belong to staff, and must not send SMS at the
/// workshop's cost on a stranger's request. The wait and the hourly limit count per number
/// (ADR 0026), so codes asked on a stolen phone do not block the owner's other phone.
/// </summary>
internal sealed partial class RequestLoginCodeHandler(
    IAppDbContext db,
    ISmsSender sms,
    IClock clock,
    ILogger<RequestLoginCodeHandler> logger) : ICommandHandler<RequestLoginCodeCommand, LoginCodeRequested>
{
    private static readonly LoginCodeRequested Answer = new(
        (int)OtpCode.Lifetime.TotalSeconds,
        (int)SessionPolicy.CodeResendInterval.TotalSeconds);

    public async Task<Result<LoginCodeRequested>> Handle(RequestLoginCodeCommand command, CancellationToken cancellationToken)
    {
        var mobile = MobileNumber.Create(command.MobileNumber).Value;
        var now = clock.UtcNow;

        var user = await db.Users.SingleOrDefaultAsync(UserLogin.ByActiveNumber(mobile), cancellationToken);
        if (user is null)
        {
            LogUnknownNumber(logger);
            return Answer;
        }

        var hourAgo = now - TimeSpan.FromHours(1);
        var recent = await db.OtpCodes
            .Where(code => code.UserId == user.Id && code.MobileNumber == mobile && code.CreatedAt > hourAgo)
            .Select(code => code.CreatedAt)
            .ToListAsync(cancellationToken);

        if (recent.Count >= SessionPolicy.MaxCodesPerHour
            || recent.Exists(createdAt => now - createdAt < SessionPolicy.CodeResendInterval))
        {
            LogThrottled(logger, user.Id);
            return Answer;
        }

        var code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6", CultureInfo.InvariantCulture);
        db.OtpCodes.Add(OtpCode.Issue(user.Id, mobile, code, now));
        await db.SaveChangesAsync(cancellationToken);

        await sms.SendAsync(
            new SmsMessage(mobile, SmsTemplates.LoginCode, new Dictionary<string, string> { ["code"] = code }),
            cancellationToken);

        return Answer;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Login code asked for a number with no active account or switched off")]
    private static partial void LogUnknownNumber(ILogger logger);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Login code refused for user {UserId}: asked too often")]
    private static partial void LogThrottled(ILogger logger, Guid userId);
}
