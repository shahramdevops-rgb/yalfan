using System.Security.Cryptography;
using System.Text;

namespace Yalfan.Infrastructure.Persistence.Encryption;

/// <summary>
/// Encrypts IBANs before they are stored and decrypts them when they are read (ADR 0019), with
/// AES-256-GCM and the key from <see cref="EncryptionOptions.IbanKey"/>. The key lives outside
/// the database, like the JWT key, so a database dump or backup alone does not reveal an IBAN.
/// <para>
/// Stored form: <c>v1:</c> and base64 of nonce (12 bytes), tag (16 bytes) and ciphertext. Every
/// write uses a new random nonce, so the same IBAN is stored differently each time and two rows
/// cannot be matched by their stored values. The <c>v1</c> names the key, so a key can be rotated
/// later by adding <c>v2</c> while old rows stay readable.
/// </para>
/// </summary>
/// <remarks>
/// This instance lives inside the IBAN columns' value converter, in a model EF Core may build once
/// and reuse for the whole process, so it holds only the key bytes and nothing disposable, and one
/// process uses one key. The key is checked when
/// the API starts (<see cref="EncryptionOptionsValidator"/>); here it is checked only when used, so
/// design-time tools such as <c>dotnet ef migrations add</c> can build the model without it.
/// </remarks>
public sealed class IbanCipher
{
    /// <summary>The longest stored value: an IBAN is 26 ASCII characters.</summary>
    public const int StoredMaxLength = 80;

    private const string Version = "v1:";
    private const int NonceSize = 12;
    private const int TagSize = 16;

    private readonly byte[]? _key;

    public IbanCipher(EncryptionOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _key = EncryptionOptions.TryReadKey(options.IbanKey);
    }

    public string Encrypt(string iban)
    {
        ArgumentNullException.ThrowIfNull(iban);

        var plaintext = Encoding.ASCII.GetBytes(iban);
        var stored = new byte[NonceSize + TagSize + plaintext.Length];
        var nonce = stored.AsSpan(0, NonceSize);
        var tag = stored.AsSpan(NonceSize, TagSize);
        var ciphertext = stored.AsSpan(NonceSize + TagSize);

        RandomNumberGenerator.Fill(nonce);
        using (var aes = new AesGcm(Key(), TagSize))
        {
            aes.Encrypt(nonce, plaintext, ciphertext, tag);
        }

        return Version + Convert.ToBase64String(stored);
    }

    /// <summary>Throws when the value was not written with this key or was changed in the database.</summary>
    public string Decrypt(string stored)
    {
        ArgumentNullException.ThrowIfNull(stored);

        if (!stored.StartsWith(Version, StringComparison.Ordinal))
        {
            throw new CryptographicException("The stored IBAN has an unknown format or key version.");
        }

        var bytes = Convert.FromBase64String(stored[Version.Length..]);
        if (bytes.Length <= NonceSize + TagSize)
        {
            throw new CryptographicException("The stored IBAN is too short.");
        }

        var plaintext = new byte[bytes.Length - NonceSize - TagSize];
        using (var aes = new AesGcm(Key(), TagSize))
        {
            aes.Decrypt(
                nonce: bytes.AsSpan(0, NonceSize),
                ciphertext: bytes.AsSpan(NonceSize + TagSize),
                tag: bytes.AsSpan(NonceSize, TagSize),
                plaintext: plaintext);
        }

        return Encoding.ASCII.GetString(plaintext);
    }

    private byte[] Key() =>
        _key ?? throw new InvalidOperationException(EncryptionOptionsValidator.MissingKeyMessage);
}
