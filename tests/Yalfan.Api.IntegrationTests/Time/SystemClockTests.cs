using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using Yalfan.Application.Common;
using Yalfan.Infrastructure.Time;

namespace Yalfan.Api.IntegrationTests.Time;

/// <summary>
/// "Today" is the workshop's calendar day in Tehran, whatever the server's own time zone
/// (ADR 0005). CI runs these on Linux in UTC and the developer runs them on Windows in Tehran
/// time, so a clock that leaned on the machine's zone would fail in one of the two.
/// </summary>
public sealed class SystemClockTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Theory]
    // Winter: Tehran is UTC+03:30, so its day starts at 20:30 UTC.
    [InlineData("2026-11-18T20:29:59Z", "2026-11-18")]
    [InlineData("2026-11-18T20:30:00Z", "2026-11-19")]
    // Summer: still +03:30, because Iran has had no daylight saving since 2023.
    [InlineData("2026-06-21T20:29:59Z", "2026-06-21")]
    [InlineData("2026-06-21T20:30:00Z", "2026-06-22")]
    // The small hours: 02:00 in Tehran is already the next day, while UTC is still on the last one.
    [InlineData("2026-11-18T22:30:00Z", "2026-11-19")]
    // A past summer with daylight saving (+04:30): only the named zone knows this, a fixed
    // +03:30 would put 19:30 UTC on the previous day (ADR 0005, decision 6).
    [InlineData("2021-06-21T19:29:59Z", "2021-06-21")]
    [InlineData("2021-06-21T19:30:00Z", "2021-06-22")]
    public void Today_is_the_calendar_day_in_Tehran(string utc, string expectedDay)
    {
        var clock = new SystemClock(new FakeTimeProvider(Instant(utc)));

        Assert.Equal(DateOnly.ParseExact(expectedDay, "yyyy-MM-dd", CultureInfo.InvariantCulture), clock.Today);
    }

    [Fact]
    public void UtcNow_is_the_time_provider_instant_in_utc()
    {
        var instant = Instant("2026-11-19T08:15:30Z");

        var clock = new SystemClock(new FakeTimeProvider(instant));

        Assert.Equal(instant, clock.UtcNow);
        Assert.Equal(TimeSpan.Zero, clock.UtcNow.Offset);
    }

    [Fact]
    public void The_api_clock_follows_the_time_the_test_sets()
    {
        // Every later test that moves factory.Time (a punch after midnight, a cheque due
        // tomorrow) relies on the API's real clock reading that time.
        var clock = factory.Services.GetRequiredService<IClock>();
        Assert.IsType<SystemClock>(clock);

        factory.Time.SetUtcNow(Instant("2099-03-20T20:29:59Z"));
        Assert.Equal(new DateOnly(2099, 3, 20), clock.Today);

        factory.Time.Advance(TimeSpan.FromSeconds(1));
        Assert.Equal(new DateOnly(2099, 3, 21), clock.Today);
        Assert.Equal(Instant("2099-03-20T20:30:00Z"), clock.UtcNow);
    }

    private static DateTimeOffset Instant(string utc) =>
        DateTimeOffset.Parse(utc, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);
}
