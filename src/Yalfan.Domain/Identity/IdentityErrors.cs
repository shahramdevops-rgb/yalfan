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
}
