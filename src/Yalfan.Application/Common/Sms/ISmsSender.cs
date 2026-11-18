using Yalfan.Domain.Common;

namespace Yalfan.Application.Common.Sms;

/// <summary>
/// Sends an SMS through the configured provider. Iranian providers send service messages
/// (login codes) from a template registered with them, filled with values, so a message is a
/// template name plus values, not free text.
/// </summary>
public interface ISmsSender
{
    /// <summary>Throws when the provider cannot take the message; that is an outside failure, not a business one.</summary>
    Task SendAsync(SmsMessage message, CancellationToken cancellationToken);
}

public sealed record SmsMessage(MobileNumber To, string Template, IReadOnlyDictionary<string, string> Values);

/// <summary>Template names; each provider adapter maps them to the templates registered with it.</summary>
public static class SmsTemplates
{
    /// <summary>The login code. Value: <c>code</c>.</summary>
    public const string LoginCode = "login_code";
}
