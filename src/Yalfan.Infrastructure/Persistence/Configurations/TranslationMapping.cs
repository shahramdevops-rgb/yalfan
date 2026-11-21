using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Yalfan.Domain.Catalog;
using Yalfan.Domain.Common;

namespace Yalfan.Infrastructure.Persistence.Configurations;

/// <summary>
/// How translation rows are stored (ADR 0006): one table per kind of item, keyed by the item and
/// the locale, so an item has at most one row per language.
/// </summary>
internal static class TranslationMapping
{
    public const int LocaleMaxLength = 2;

    /// <summary>Stored as "fa", "ar", "en": the codes a hand-written query or report expects.</summary>
    public static readonly ValueConverter<Locale, string> LocaleConverter = new(
        locale => ToCode(locale),
        code => FromCode(code));

    /// <summary>
    /// Maps a list of <see cref="NameTranslation"/> to <paramref name="table"/>. With
    /// <paramref name="uniqueNames"/>, no two items share a name in the same language.
    /// </summary>
    public static void OwnsNames<TOwner>(
        this EntityTypeBuilder<TOwner> builder,
        Expression<Func<TOwner, IEnumerable<NameTranslation>?>> names,
        string table,
        int nameMaxLength,
        bool uniqueNames)
        where TOwner : class
    {
        var ownerKey = typeof(TOwner).Name + "Id";

        builder.OwnsMany(names, translation =>
        {
            translation.ToTable(table);
            translation.WithOwner().HasForeignKey(ownerKey);
            translation.Property(t => t.Locale).HasConversion(LocaleConverter).HasMaxLength(LocaleMaxLength);
            translation.HasKey(ownerKey, nameof(NameTranslation.Locale));
            translation.Property(t => t.Name).HasMaxLength(nameMaxLength).IsRequired();

            if (uniqueNames)
            {
                translation.HasIndex(t => new { t.Locale, t.Name }).IsUnique();
            }
        });
    }

    private static string ToCode(Locale locale) => locale switch
    {
        Locale.Fa => "fa",
        Locale.Ar => "ar",
        Locale.En => "en",
        _ => throw new ArgumentOutOfRangeException(nameof(locale), locale, "Unknown locale."),
    };

    private static Locale FromCode(string code) => code switch
    {
        "fa" => Locale.Fa,
        "ar" => Locale.Ar,
        "en" => Locale.En,
        _ => throw new ArgumentOutOfRangeException(nameof(code), code, "Unknown locale code."),
    };
}
