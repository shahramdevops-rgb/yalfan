using Microsoft.Extensions.Options;

namespace Yalfan.Infrastructure.Persistence.Encryption;

/// <summary>
/// Keys for data stored encrypted (ADR 0019). Secrets: they come from user-secrets or the
/// environment (<c>Encryption__IbanKey</c>), never from the repository.
/// <para>
/// Losing the IBAN key loses every stored IBAN, so it is kept outside the server as well, apart
/// from the database backups (ARCHITECTURE.md 12).
/// </para>
/// </summary>
public sealed class EncryptionOptions
{
    public const string SectionName = "Encryption";

    /// <summary>AES-256 needs exactly 32 bytes.</summary>
    public const int KeyBytes = 32;

    /// <summary>32 random bytes in base64.</summary>
    public string IbanKey { get; set; } = string.Empty;

    /// <summary>The key's bytes, or null when it is missing, not base64, or not 32 bytes long.</summary>
    internal static byte[]? TryReadKey(string? base64)
    {
        if (string.IsNullOrWhiteSpace(base64))
        {
            return null;
        }

        var buffer = new byte[base64.Length];
        return Convert.TryFromBase64String(base64.Trim(), buffer, out var written) && written == KeyBytes
            ? buffer[..written]
            : null;
    }
}

/// <summary>The API does not start without a usable IBAN key: it could neither store nor read an IBAN.</summary>
internal sealed class EncryptionOptionsValidator : IValidateOptions<EncryptionOptions>
{
    public const string MissingKeyMessage =
        "Encryption:IbanKey must be 32 random bytes in base64. Set it with user-secrets or Encryption__IbanKey.";

    public ValidateOptionsResult Validate(string? name, EncryptionOptions options) =>
        EncryptionOptions.TryReadKey(options.IbanKey) is null
            ? ValidateOptionsResult.Fail(MissingKeyMessage)
            : ValidateOptionsResult.Success;
}
