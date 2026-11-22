using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Yalfan.Domain.Common;
using Yalfan.Infrastructure.Persistence.Auditing;

namespace Yalfan.Infrastructure.Persistence.Encryption;

internal static class IbanMapping
{
    /// <summary>
    /// Every <see cref="Iban"/> property of every entity is stored encrypted and audited as
    /// "changed" without its values (ADR 0019, ADR 0013). Applied to the whole model by type
    /// rather than in each entity's configuration, so an entity that gets an IBAN later (workers,
    /// payees) cannot forget either. The configuration only names the property.
    /// </summary>
    public static void EncryptIbans(this ModelBuilder modelBuilder, IbanCipher cipher)
    {
        var converter = new ValueConverter<Iban, string>(
            iban => cipher.Encrypt(iban.Value),
            stored => Iban.Create(cipher.Decrypt(stored)).Value);

        var properties = modelBuilder.Model.GetEntityTypes()
            .SelectMany(entityType => entityType.GetDeclaredProperties())
            .Where(property => property.ClrType == typeof(Iban));

        foreach (var property in properties)
        {
            property.SetValueConverter(converter);
            property.SetMaxLength(IbanCipher.StoredMaxLength);
            property.SetAuditRedacted();
        }
    }
}
