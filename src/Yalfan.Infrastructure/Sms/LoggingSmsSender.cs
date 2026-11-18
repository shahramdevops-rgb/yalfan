using Microsoft.Extensions.Logging;
using Yalfan.Application.Common.Sms;

namespace Yalfan.Infrastructure.Sms;

/// <summary>
/// Development stand-in for an SMS provider: the message, login code included, goes to the log
/// (console and Seq) instead of a phone. <see cref="SmsOptionsValidator"/> keeps it out of
/// production.
/// </summary>
internal sealed partial class LoggingSmsSender(ILogger<LoggingSmsSender> logger) : ISmsSender
{
    public Task SendAsync(SmsMessage message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);

        LogMessage(logger, message.To.Value, message.Template, message.Values);
        return Task.CompletedTask;
    }

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "SMS not sent (development log provider) to {Mobile}: {Template} {@Values}")]
    private static partial void LogMessage(
        ILogger logger, string mobile, string template, IReadOnlyDictionary<string, string> values);
}
