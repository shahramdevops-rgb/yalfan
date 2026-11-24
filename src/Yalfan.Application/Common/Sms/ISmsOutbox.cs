using Yalfan.Domain.Common;

namespace Yalfan.Application.Common.Sms;

/// <summary>
/// The messages a stand-in provider kept instead of sending them. Until the SMS provider is
/// connected (task 3.0), the panel reads the login code from here and shows it on its login page
/// in Development (ADR 0012). Only the log provider has an outbox; a real provider registers none.
/// </summary>
public interface ISmsOutbox
{
    /// <summary>The last message kept for this number with this template, or null when there is none.</summary>
    KeptSms? Last(MobileNumber to, string template);
}

/// <summary>A message the stand-in provider kept, and when it would have been sent.</summary>
public sealed record KeptSms(SmsMessage Message, DateTimeOffset SentAt);
