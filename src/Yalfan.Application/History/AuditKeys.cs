using System.Globalization;

namespace Yalfan.Application.History;

/// <summary>Keys as the audit writes them: each part in its stored form, joined by commas (ADR 0013).</summary>
internal static class AuditKeys
{
    public static string Key(Guid id) => id.ToString("D", CultureInfo.InvariantCulture);

    /// <summary>A row with a composite key, such as a person's stage: <c>&lt;contractor id&gt;,&lt;stage id&gt;</c>.</summary>
    public static string Key(Guid first, Guid second) => $"{Key(first)},{Key(second)}";

    /// <summary>The Persian translation row of an item: <c>&lt;item id&gt;,fa</c>.</summary>
    public static string Persian(Guid ownerId) => $"{Key(ownerId)},fa";
}
