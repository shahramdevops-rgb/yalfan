using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Yalfan.Application.Common;
using Yalfan.Domain.Common;
using Yalfan.Domain.Identity;
using Yalfan.Infrastructure.Persistence;

namespace Yalfan.Infrastructure.Identity;

/// <summary>
/// The owner accounts, from <c>Identity:Owners</c>: each a name and the numbers it logs in with
/// (no secret). Every other user is added by an owner in the panel. ADR 0026.
/// </summary>
public sealed class OwnerAccountsOptions
{
    public const string SectionName = "Identity";

    public List<OwnerAccountOptions> Owners { get; set; } = [];
}

public sealed class OwnerAccountOptions
{
    public string? FullName { get; set; }

    /// <summary>Blank entries are ignored, so compose can pass empty variables.</summary>
    public List<string?> MobileNumbers { get; set; } = [];
}

/// <summary>
/// Brings the configured owner accounts into the database at start, after the migrator. It only
/// adds: an account none of whose numbers exists yet is created, and numbers missing from an
/// existing account are added. It never renames, removes or switches a number back on, because a
/// restart must not undo a stolen phone's number being switched off in the panel (ADR 0026).
/// A configuration that cannot be applied stops the start, because a silently missing owner
/// means nobody can log in.
/// </summary>
internal sealed partial class OwnerSeeder(
    IServiceScopeFactory scopeFactory,
    IOptions<OwnerAccountsOptions> options,
    ILogger<OwnerSeeder> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var accounts = options.Value.Owners
            .Select((account, index) => (Account: account, Numbers: Numbers(account, index)))
            .Where(entry => entry.Numbers.Count > 0)
            .ToList();
        if (accounts.Count == 0)
        {
            return;
        }

        // A number under two accounts would join them into one without a word: the developer's
        // changes would then be recorded as the owner's.
        var configured = accounts.SelectMany(entry => entry.Numbers).ToList();
        if (configured.Count != configured.Distinct().Count())
        {
            throw new InvalidOperationException("Identity:Owners: the same number is listed under two owner accounts.");
        }

        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var clock = scope.ServiceProvider.GetRequiredService<IClock>();

        foreach (var (account, numbers) in accounts)
        {
            await ApplyAsync(db, clock, account, numbers, cancellationToken);
        }

        await db.SaveChangesAsync(cancellationToken);
        await WarnAboutForgottenAccountsAsync(db, configured, cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static List<MobileNumber> Numbers(OwnerAccountOptions account, int index)
    {
        var numbers = new List<MobileNumber>();
        foreach (var raw in account.MobileNumbers.Where(raw => !string.IsNullOrWhiteSpace(raw)))
        {
            var mobile = MobileNumber.Create(raw);
            if (mobile.IsFailure)
            {
                throw new InvalidOperationException($"Identity:Owners:{index}: a number is not an Iranian mobile number.");
            }

            if (!numbers.Contains(mobile.Value))
            {
                numbers.Add(mobile.Value);
            }
        }

        return numbers;
    }

    private async Task ApplyAsync(
        AppDbContext db,
        IClock clock,
        OwnerAccountOptions account,
        List<MobileNumber> numbers,
        CancellationToken cancellationToken)
    {
        // The account is whichever user already holds one of its numbers. A few numbers, so one
        // small query each; users will one day include customers, so they are not all loaded.
        var holders = new List<User>();
        foreach (var number in numbers)
        {
            var holder = db.Users.Local.FirstOrDefault(user => user.HasLoginNumber(number))
                ?? await db.Users.SingleOrDefaultAsync(user => user.LoginNumbers.Any(n => n.MobileNumber == number), cancellationToken);
            if (holder is not null && !holders.Contains(holder))
            {
                holders.Add(holder);
            }
        }

        if (holders.Count > 1)
        {
            throw new InvalidOperationException("Identity:Owners: the numbers of one owner account belong to different users.");
        }

        if (holders.Count == 1)
        {
            var existing = holders[0];
            if (!existing.IsOwnerAccount)
            {
                throw new InvalidOperationException("Identity:Owners: a number already belongs to a user who is not an owner.");
            }

            foreach (var number in numbers.Where(number => !existing.HasLoginNumber(number)))
            {
                existing.AddLoginNumber(number);
                LogNumberAdded(logger, existing.Id);
            }

            return;
        }

        var created = User.Create(account.FullName, numbers[0], Role.Owner, clock.UtcNow);
        if (created.IsFailure)
        {
            throw new InvalidOperationException($"Identity:Owners: an owner account is not valid: {created.Error.Code}.");
        }

        foreach (var number in numbers.Skip(1))
        {
            created.Value.AddLoginNumber(number);
        }

        db.Users.Add(created.Value);
        LogCreated(logger, created.Value.Id);
    }

    /// <summary>
    /// An owner account none of whose numbers is configured any more: most likely a number was
    /// replaced in the configuration instead of added next to the old one, which made a new account
    /// and left the old one able to log in (ADR 0026 says how to change a number). Only a warning,
    /// because acting on it at start could lock the owner out after a mistyped setting.
    /// </summary>
    private async Task WarnAboutForgottenAccountsAsync(AppDbContext db, List<MobileNumber> configured, CancellationToken cancellationToken)
    {
        var owners = await db.Users.AsNoTracking().Where(user => user.Role == Role.Owner).ToListAsync(cancellationToken);
        foreach (var owner in owners.Where(owner => !configured.Exists(owner.HasLoginNumber)))
        {
            LogForgotten(logger, owner.Id);
        }
    }

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "Owner account {UserId} has none of its numbers in Identity:Owners; it can still log in with them (ADR 0026)")]
    private static partial void LogForgotten(ILogger logger, Guid userId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Owner account created: user {UserId}")]
    private static partial void LogCreated(ILogger logger, Guid userId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Login number added to owner account {UserId}")]
    private static partial void LogNumberAdded(ILogger logger, Guid userId);
}
