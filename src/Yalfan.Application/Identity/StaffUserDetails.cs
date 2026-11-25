using Microsoft.EntityFrameworkCore;
using Yalfan.Application.Common;
using Yalfan.Domain.Common;
using Yalfan.Domain.Identity;

namespace Yalfan.Application.Identity;

/// <summary>
/// A user as the owner's users screen shows him. <see cref="IsOwnerAccount"/> tells the screen
/// which controls to leave out: an owner account's role, active flag and numbers come from the
/// server's configuration, and only its numbers' switches are offered (ADR 0026).
/// </summary>
public sealed record StaffUserDetails(
    Guid Id,
    string FullName,
    Role Role,
    bool IsActive,
    bool IsOwnerAccount,
    IReadOnlyList<LoginNumberDetails> LoginNumbers,
    DateTimeOffset CreatedAt);

/// <summary>One number the user logs in with.</summary>
public sealed record LoginNumberDetails(Guid Id, string MobileNumber, bool IsActive);

internal static class StaffUsers
{
    public static StaffUserDetails Details(User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        // In number order: stable from one answer to the next. Not by id, because two ids made in
        // the same millisecond (the numbers of one account, added at one start) have no order.
        var numbers = user.LoginNumbers
            .OrderBy(number => number.MobileNumber.Value, StringComparer.Ordinal)
            .Select(number => new LoginNumberDetails(number.Id, number.MobileNumber.Value, number.IsActive))
            .ToList();

        return new StaffUserDetails(user.Id, user.FullName, user.Role, user.IsActive, user.IsOwnerAccount, numbers, user.CreatedAt);
    }

    public static Task<User?> FindAsync(IAppDbContext db, Guid id, CancellationToken cancellationToken) =>
        db.Users.SingleOrDefaultAsync(user => user.Id == id, cancellationToken);

    /// <summary>Whether another user already logs in with this number, switched on or off.</summary>
    public static Task<bool> MobileTakenAsync(IAppDbContext db, MobileNumber mobile, Guid? exceptUserId, CancellationToken cancellationToken) =>
        db.Users.AnyAsync(
            user => user.Id != exceptUserId && user.LoginNumbers.Any(number => number.MobileNumber == mobile),
            cancellationToken);
}
