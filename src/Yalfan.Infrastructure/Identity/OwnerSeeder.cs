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
/// The first owner, from <c>Identity:SeedOwner</c> (name and mobile, no secret). Every other
/// user is added by the owner in the panel.
/// </summary>
public sealed class SeedOwnerOptions
{
    public const string SectionName = "Identity:SeedOwner";

    public string? FullName { get; set; }

    public string? MobileNumber { get; set; }
}

/// <summary>
/// Creates the first owner at start when one is configured and no owner exists yet. Runs after
/// the migrator. Does nothing, and does not touch the database, when nothing is configured.
/// A configured but wrong owner stops the start, because a silently missing owner means nobody
/// can log in.
/// </summary>
internal sealed partial class OwnerSeeder(
    IServiceScopeFactory scopeFactory,
    IOptions<SeedOwnerOptions> options,
    ILogger<OwnerSeeder> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var seed = options.Value;
        if (string.IsNullOrWhiteSpace(seed.MobileNumber))
        {
            return;
        }

        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var clock = scope.ServiceProvider.GetRequiredService<IClock>();

        if (await db.Users.AnyAsync(user => user.Role == Role.Owner, cancellationToken))
        {
            return;
        }

        var mobile = MobileNumber.Create(seed.MobileNumber);
        if (mobile.IsFailure)
        {
            throw new InvalidOperationException("Identity:SeedOwner:MobileNumber is not an Iranian mobile number.");
        }

        if (await db.Users.AnyAsync(user => user.MobileNumber == mobile.Value, cancellationToken))
        {
            throw new InvalidOperationException("Identity:SeedOwner:MobileNumber already belongs to a user who is not the owner.");
        }

        var owner = User.Create(seed.FullName, mobile.Value, Role.Owner, clock.UtcNow);
        if (owner.IsFailure)
        {
            throw new InvalidOperationException($"Identity:SeedOwner is not valid: {owner.Error.Code}.");
        }

        db.Users.Add(owner.Value);
        await db.SaveChangesAsync(cancellationToken);
        LogSeeded(logger, owner.Value.Id);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    [LoggerMessage(Level = LogLevel.Information, Message = "First owner created: user {UserId}")]
    private static partial void LogSeeded(ILogger logger, Guid userId);
}
