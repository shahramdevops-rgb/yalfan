using Yalfan.Domain.Common;

namespace Yalfan.Domain.Identity;

/// <summary>
/// A mobile number a user logs in with. A manager or operator has exactly one; an owner account
/// may have several, set in the server's configuration, so that a stolen phone can be switched
/// off while the owner logs in with another (ADR 0026). Unique among all users' numbers.
/// </summary>
public sealed class LoginNumber : Entity
{
    private LoginNumber()
    {
    }

    internal LoginNumber(MobileNumber mobileNumber)
    {
        MobileNumber = mobileNumber;
        IsActive = true;
    }

    public MobileNumber MobileNumber { get; private set; } = null!;

    /// <summary>A switched-off number gets no login code and cannot log in.</summary>
    public bool IsActive { get; private set; }

    internal void ChangeTo(MobileNumber mobileNumber) => MobileNumber = mobileNumber;

    internal void Deactivate() => IsActive = false;

    internal void Activate() => IsActive = true;
}
