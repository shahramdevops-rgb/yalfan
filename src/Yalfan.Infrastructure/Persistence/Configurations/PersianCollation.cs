namespace Yalfan.Infrastructure.Persistence.Configurations;

/// <summary>
/// PostgreSQL's ICU collation for Persian. A column with it sorts «پ چ ژ گ» in their alphabet
/// places, which code-point order (the <c>C</c> locale) does not, and does not depend on the
/// operating system's locale data the way a glibc collation does. It is deterministic, so equality,
/// unique indexes and substring search behave as usual.
/// </summary>
internal static class PersianCollation
{
    public const string Name = "fa-x-icu";
}
