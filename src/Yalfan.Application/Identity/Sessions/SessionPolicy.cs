namespace Yalfan.Application.Identity.Sessions;

/// <summary>The numbers of the login rules (ADR 0012). Changing one is a decision, not a setting.</summary>
public static class SessionPolicy
{
    /// <summary>
    /// Short, so that a deactivated user, a changed number or role, or a stolen phone's number
    /// switched off takes effect within minutes; the panel renews it silently. Ten minutes since
    /// 1405/09/05 (ADR 0026): the open page on a stolen phone works at most this long. Nothing is
    /// checked in the database on each request, to keep that load off the future store.
    /// </summary>
    public static readonly TimeSpan AccessTokenLifetime = TimeSpan.FromMinutes(10);

    /// <summary>Renewed on every use: a daily user almost never asks for a new code.</summary>
    public static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(7);

    /// <summary>Wait between two codes for the same number.</summary>
    public static readonly TimeSpan CodeResendInterval = TimeSpan.FromMinutes(2);

    /// <summary>Codes per number per hour; stops a script from sending SMS at the workshop's cost.</summary>
    public const int MaxCodesPerHour = 5;
}
