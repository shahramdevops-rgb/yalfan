using Yalfan.Domain.Common;

namespace Yalfan.Domain.Identity;

public static class IdentityErrors
{
    /// <summary>The code is wrong, or the number has no active account (the two look the same).</summary>
    public static readonly Error OtpInvalid = Error.Unauthenticated("identity.otp_invalid");

    /// <summary>No usable code: expired, already used, too many wrong tries, or never sent.</summary>
    public static readonly Error OtpExpired = Error.Unauthenticated("identity.otp_expired");

    /// <summary>The session cannot be renewed; the user must log in again.</summary>
    public static readonly Error RefreshInvalid = Error.Unauthenticated("identity.refresh_invalid");

    /// <summary>
    /// Another tab renewed the session a moment ago. Not a theft: the frontend retries once with
    /// the new cookie.
    /// </summary>
    public static readonly Error RefreshSuperseded = Error.Unauthenticated("identity.refresh_superseded");

    public static readonly Error MobileTaken = Error.Conflict("identity.mobile_taken");

    public static readonly Error UserNotFound = Error.NotFound("identity.user_not_found");

    public static readonly Error FullNameRequired = Error.Validation("identity.full_name_required");

    public static readonly Error FullNameTooLong = Error.Validation("identity.full_name_too_long");

    /// <summary>The panel gives a user only the manager or operator role (ADR 0012).</summary>
    public static readonly Error RoleNotAllowed = Error.Validation("identity.role_not_allowed");

    public static readonly Error LoginNumberNotFound = Error.NotFound("identity.login_number_not_found");

    /// <summary>
    /// An owner account's role, active flag and numbers come from the server's configuration, not
    /// from the panel (ADR 0026).
    /// </summary>
    public static readonly Error OwnerLocked = Error.Rule("identity.owner_locked");

    /// <summary>An account keeps at least one number switched on, so it can always log in (ADR 0026).</summary>
    public static readonly Error LastLoginNumber = Error.Rule("identity.last_login_number");

    /// <summary>
    /// A number of one's own account is switched back on by another owner account, never by the
    /// account itself: a thief whose page on the stolen phone still works for a few minutes must
    /// not switch the stolen number back on (ADR 0026).
    /// </summary>
    public static readonly Error OwnNumberSwitchOn = Error.Rule("identity.own_number_switch_on");

    /// <summary>Only an owner account has more than one number; a manager's or operator's is changed instead.</summary>
    public static readonly Error SingleLoginNumber = Error.Rule("identity.single_login_number");
}
