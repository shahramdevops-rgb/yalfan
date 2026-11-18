using Yalfan.Application.Common;

namespace Yalfan.Infrastructure.Time;

internal sealed class SystemClock(TimeProvider timeProvider) : IClock
{
    private static readonly TimeZoneInfo BusinessTimeZone =
        TimeZoneInfo.FindSystemTimeZoneById("Asia/Tehran");

    public DateTimeOffset UtcNow => timeProvider.GetUtcNow();

    public DateOnly Today =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(UtcNow, BusinessTimeZone).DateTime);
}
