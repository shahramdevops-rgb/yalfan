using System.Collections.Concurrent;
using Yalfan.Application.Common.Sms;

namespace Yalfan.Api.IntegrationTests.Infrastructure;

/// <summary>Keeps every SMS instead of sending it, so a test can read the login code.</summary>
public sealed class CapturingSmsSender : ISmsSender
{
    private readonly ConcurrentQueue<SmsMessage> _sent = new();

    public Task SendAsync(SmsMessage message, CancellationToken cancellationToken)
    {
        _sent.Enqueue(message);
        return Task.CompletedTask;
    }

    public int CountFor(string mobile) => LoginCodes(mobile).Count();

    public string LastCodeFor(string mobile) =>
        LoginCodes(mobile).LastOrDefault() ?? throw new InvalidOperationException($"No login code was sent to {mobile}.");

    private IEnumerable<string> LoginCodes(string mobile) => _sent
        .Where(message => message.To.Value == mobile && message.Template == SmsTemplates.LoginCode)
        .Select(message => message.Values["code"]);
}
