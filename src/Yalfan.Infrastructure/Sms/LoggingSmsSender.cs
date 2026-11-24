using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Yalfan.Application.Common;
using Yalfan.Application.Common.Sms;
using Yalfan.Domain.Common;

namespace Yalfan.Infrastructure.Sms;

/// <summary>
/// Development stand-in for an SMS provider: the message, login code included, goes to the log
/// (console and Seq) instead of a phone, and the last one per number and template is kept, so
/// the panel can show the login code on its page in Development (ADR 0012).
/// <see cref="SmsOptionsValidator"/> keeps it out of production.
/// </summary>
internal sealed partial class LoggingSmsSender(ILogger<LoggingSmsSender> logger, IClock clock) : ISmsSender, ISmsOutbox
{
    private readonly ConcurrentDictionary<(string Mobile, string Template), KeptSms> _kept = new();

    public Task SendAsync(SmsMessage message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);

        LogMessage(logger, message.To.Value, message.Template, message.Values);
        _kept[(message.To.Value, message.Template)] = new KeptSms(message, clock.UtcNow);
        return Task.CompletedTask;
    }

    public KeptSms? Last(MobileNumber to, string template) =>
        _kept.TryGetValue((to.Value, template), out var kept) ? kept : null;

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "SMS not sent (development log provider) to {Mobile}: {Template} {@Values}")]
    private static partial void LogMessage(
        ILogger logger, string mobile, string template, IReadOnlyDictionary<string, string> values);
}
