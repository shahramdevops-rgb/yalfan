using Yalfan.Domain.Common;

namespace Yalfan.Domain.Identity;

/// <summary>
/// A person who can log in. There is no password: a user proves who he is with a one-time code
/// sent by SMS to <see cref="MobileNumber"/> (ADR 0012).
/// </summary>
public sealed class User : Entity
{
    public const int FullNameMaxLength = 100;

    private User()
    {
    }

    private User(string fullName, MobileNumber mobileNumber, Role role, DateTimeOffset createdAt)
    {
        FullName = fullName;
        MobileNumber = mobileNumber;
        Role = role;
        IsActive = true;
        CreatedAt = createdAt;
    }

    public string FullName { get; private set; } = null!;

    /// <summary>The login identity; unique among users.</summary>
    public MobileNumber MobileNumber { get; private set; } = null!;

    public Role Role { get; private set; }

    /// <summary>An inactive user cannot log in or renew a session.</summary>
    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static Result<User> Create(string? fullName, MobileNumber mobileNumber, Role role, DateTimeOffset createdAt)
    {
        ArgumentNullException.ThrowIfNull(mobileNumber);

        var name = fullName?.Trim() ?? string.Empty;
        if (name.Length == 0)
        {
            return IdentityErrors.FullNameRequired;
        }

        if (name.Length > FullNameMaxLength)
        {
            return IdentityErrors.FullNameTooLong;
        }

        return new User(name, mobileNumber, role, createdAt);
    }

    /// <summary>
    /// The user can no longer log in or renew a session; his open access token dies within its
    /// 15 minutes. The account and its history stay.
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
    }
}
