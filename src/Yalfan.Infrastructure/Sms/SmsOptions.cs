using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Yalfan.Infrastructure.Sms;

/// <summary>Which SMS provider sends messages (<c>Sms:Provider</c>). It must be set; there is no default.</summary>
public sealed class SmsOptions
{
    public const string SectionName = "Sms";

    public string? Provider { get; set; }
}

public static class SmsProviders
{
    /// <summary>Sends nothing and writes the message, login code included, to the log.</summary>
    public const string Log = "Log";
}

/// <summary>
/// Stops the start when no provider is set, and when the log provider is chosen outside
/// Development and Testing: a login code in a production log lets anyone who reads the log
/// log in. The real provider adapter is added before go-live (ADR 0012).
/// </summary>
internal sealed class SmsOptionsValidator(IHostEnvironment environment) : IValidateOptions<SmsOptions>
{
    public ValidateOptionsResult Validate(string? name, SmsOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Provider))
        {
            return ValidateOptionsResult.Fail("Sms:Provider is not set. Use \"Log\" in Development.");
        }

        if (!string.Equals(options.Provider, SmsProviders.Log, StringComparison.Ordinal))
        {
            return ValidateOptionsResult.Fail($"Sms:Provider \"{options.Provider}\" is not known.");
        }

        if (!environment.IsDevelopment() && !environment.IsEnvironment("Testing"))
        {
            return ValidateOptionsResult.Fail(
                "The Log SMS provider writes login codes to the log; it is allowed only in Development and Testing.");
        }

        return ValidateOptionsResult.Success;
    }
}
