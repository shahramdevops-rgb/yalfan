namespace Yalfan.Application.Common;

/// <summary>
/// The only source of the current time (ADR 0005). Nothing calls <c>DateTime.Now</c> or
/// <c>DateTime.UtcNow</c>; the build forbids it. Use cases read the clock and pass the time
/// into the Domain, which never asks for it.
/// </summary>
public interface IClock
{
    /// <summary>The current instant, in UTC. This is what gets stored.</summary>
    DateTimeOffset UtcNow { get; }

    /// <summary>
    /// The workshop's calendar day right now, in Asia/Tehran. Use it for anything a person
    /// would call "today": due dates, attendance days, the date on a work order.
    /// </summary>
    DateOnly Today { get; }
}
