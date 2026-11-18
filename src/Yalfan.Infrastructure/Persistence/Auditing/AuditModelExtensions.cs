using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Yalfan.Infrastructure.Persistence.Auditing;

/// <summary>
/// Marks in the EF model what the audit leaves out or hides. The marks live in the entity
/// configurations, not in the Domain, which knows nothing about auditing (ADR 0013).
/// </summary>
public static class AuditModelExtensions
{
    private const string ExcludedAnnotation = "Yalfan:Audit:Excluded";
    private const string RedactedAnnotation = "Yalfan:Audit:Redacted";

    /// <summary>
    /// No audit entries for this entity. Only for rows that are themselves a security record
    /// (login codes, session tokens), never for business data.
    /// </summary>
    public static EntityTypeBuilder<T> ExcludeFromAudit<T>(this EntityTypeBuilder<T> builder)
        where T : class =>
        builder.HasAnnotation(ExcludedAnnotation, true);

    /// <summary>
    /// The audit records that this field changed, but not its values. For secrets that must be
    /// noticed when they change but must not be copied, such as an IBAN.
    /// </summary>
    public static PropertyBuilder<T> IsAuditRedacted<T>(this PropertyBuilder<T> builder) =>
        builder.HasAnnotation(RedactedAnnotation, true);

    internal static bool IsExcludedFromAudit(this IReadOnlyEntityType entityType) =>
        entityType.FindAnnotation(ExcludedAnnotation)?.Value is true;

    internal static bool IsAuditRedacted(this IReadOnlyProperty property) =>
        property.FindAnnotation(RedactedAnnotation)?.Value is true;
}
