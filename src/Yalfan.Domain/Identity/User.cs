using Yalfan.Domain.Common;

namespace Yalfan.Domain.Identity;

/// <summary>
/// A person who can log in. There is no password: a user proves who he is with a one-time code
/// sent by SMS to one of his <see cref="LoginNumbers"/> (ADR 0012).
/// </summary>
/// <remarks>
/// Owner accounts come from the server's configuration, not from the panel (ADR 0026): their
/// role, active flag and numbers are not changed here, except that a number can be switched off
/// and on, and an account always keeps one number switched on.
/// </remarks>
public sealed class User : Entity
{
    public const int FullNameMaxLength = 100;

    private readonly List<LoginNumber> _loginNumbers = [];

    private User()
    {
    }

    private User(string fullName, MobileNumber mobileNumber, Role role, DateTimeOffset createdAt)
    {
        FullName = fullName;
        Role = role;
        IsActive = true;
        CreatedAt = createdAt;
        _loginNumbers.Add(new LoginNumber(mobileNumber));
    }

    public string FullName { get; private set; } = null!;

    public Role Role { get; private set; }

    /// <summary>An inactive user cannot log in or renew a session.</summary>
    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>The numbers he logs in with, in no particular order.</summary>
    public IReadOnlyCollection<LoginNumber> LoginNumbers => _loginNumbers.AsReadOnly();

    /// <summary>Set in the server's configuration and locked in the panel (ADR 0026).</summary>
    public bool IsOwnerAccount => Role == Role.Owner;

    public static Result<User> Create(string? fullName, MobileNumber mobileNumber, Role role, DateTimeOffset createdAt)
    {
        ArgumentNullException.ThrowIfNull(mobileNumber);

        var name = CheckName(fullName);
        if (name.IsFailure)
        {
            return name.Error;
        }

        return new User(name.Value, mobileNumber, role, createdAt);
    }

    public bool HasLoginNumber(MobileNumber mobileNumber) =>
        _loginNumbers.Exists(number => number.MobileNumber == mobileNumber);

    public Result Rename(string? fullName)
    {
        var name = CheckName(fullName);
        if (name.IsFailure)
        {
            return name.Error;
        }

        FullName = name.Value;
        return Result.Success();
    }

    /// <summary>
    /// Between manager and operator only: a second owner is decided in the server's
    /// configuration, never in the panel (ADR 0012), and an owner account keeps its role.
    /// </summary>
    public Result ChangeRole(Role role)
    {
        if (IsOwnerAccount)
        {
            return IdentityErrors.OwnerLocked;
        }

        if (role == Role.Owner)
        {
            return IdentityErrors.RoleNotAllowed;
        }

        Role = role;
        return Result.Success();
    }

    /// <summary>
    /// Gives a manager or an operator a new number, for a lost phone. His sessions must end with
    /// it; the handler does that, since sessions are not part of the user.
    /// </summary>
    public Result ChangeMobileNumber(MobileNumber mobileNumber)
    {
        ArgumentNullException.ThrowIfNull(mobileNumber);

        if (IsOwnerAccount)
        {
            return IdentityErrors.OwnerLocked;
        }

        // A manager or operator has exactly one number: only an owner account gets more.
        _loginNumbers.Single().ChangeTo(mobileNumber);
        return Result.Success();
    }

    /// <summary>
    /// The user can no longer log in or renew a session; his open access token dies within its
    /// ten minutes. The account and its history stay.
    /// </summary>
    public Result Deactivate()
    {
        if (IsOwnerAccount)
        {
            return IdentityErrors.OwnerLocked;
        }

        IsActive = false;
        return Result.Success();
    }

    public Result Activate()
    {
        if (IsOwnerAccount)
        {
            return IdentityErrors.OwnerLocked;
        }

        IsActive = true;
        return Result.Success();
    }

    /// <summary>
    /// Adds a number to an owner account, from the server's configuration at start. A number it
    /// already has is left as it is, so a restart never switches a number back on.
    /// </summary>
    public Result AddLoginNumber(MobileNumber mobileNumber)
    {
        ArgumentNullException.ThrowIfNull(mobileNumber);

        if (!IsOwnerAccount)
        {
            return IdentityErrors.SingleLoginNumber;
        }

        if (!HasLoginNumber(mobileNumber))
        {
            _loginNumbers.Add(new LoginNumber(mobileNumber));
        }

        return Result.Success();
    }

    /// <summary>
    /// Switches a number off. One number always stays on, so an account can never be locked out
    /// from inside the panel: not by mistake, and not by someone holding a stolen phone (ADR 0026).
    /// </summary>
    public Result DeactivateLoginNumber(Guid loginNumberId)
    {
        var number = _loginNumbers.Find(n => n.Id == loginNumberId);
        if (number is null)
        {
            return IdentityErrors.LoginNumberNotFound;
        }

        if (number.IsActive && _loginNumbers.Count(n => n.IsActive) == 1)
        {
            return IdentityErrors.LastLoginNumber;
        }

        number.Deactivate();
        return Result.Success();
    }

    public Result ActivateLoginNumber(Guid loginNumberId)
    {
        var number = _loginNumbers.Find(n => n.Id == loginNumberId);
        if (number is null)
        {
            return IdentityErrors.LoginNumberNotFound;
        }

        number.Activate();
        return Result.Success();
    }

    private static Result<string> CheckName(string? fullName)
    {
        var name = fullName?.Trim() ?? string.Empty;
        if (name.Length == 0)
        {
            return IdentityErrors.FullNameRequired;
        }

        if (name.Length > FullNameMaxLength)
        {
            return IdentityErrors.FullNameTooLong;
        }

        return name;
    }
}
