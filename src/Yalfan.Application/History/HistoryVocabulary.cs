namespace Yalfan.Application.History;

/// <summary>Which part of an audit key names the part a row belongs to.</summary>
internal enum OwnerKey
{
    /// <summary>The whole key: the row is the part (a model, a size, a bag, a rate).</summary>
    Whole,

    /// <summary>The first part: a translation row belongs to the row it translates (<c>&lt;size id&gt;,fa</c>).</summary>
    First,

    /// <summary>The last part: a person's stage row is about the stage (<c>&lt;contractor id&gt;,&lt;stage id&gt;</c>).</summary>
    Last,
}

/// <summary>
/// How the history reads the audited tables (ADR 0027): the part each table's rows are about,
/// and the field of the history each recorded field becomes, or null where it means nothing to
/// the owner (keys of the parent, the moment of entry, the currency, which is always Rial).
/// Every field the audit records must be listed, so a column added later is a decision, not a
/// silent gap; a test checks it against what the audit writes.
/// </summary>
internal sealed record HistoryVocabulary(HistoryPart Part, OwnerKey Owner, IReadOnlyDictionary<string, HistoryFieldName?> Fields)
{
    public static readonly IReadOnlyDictionary<string, HistoryVocabulary> Tables = new Dictionary<string, HistoryVocabulary>
    {
        [AuditedTables.Products] = new(HistoryPart.Model, OwnerKey.Whole, new Dictionary<string, HistoryFieldName?>
        {
            // The number is the model's for good and shown on its page.
            ["number"] = null,
            ["categoryId"] = HistoryFieldName.Category,
            ["isActive"] = HistoryFieldName.Active,
            ["createdAt"] = null,
        }),
        [AuditedTables.ProductTranslations] = new(HistoryPart.Model, OwnerKey.First, new Dictionary<string, HistoryFieldName?>
        {
            ["name"] = HistoryFieldName.Name,
            ["description"] = HistoryFieldName.Description,
        }),
        [AuditedTables.ProductSizes] = new(HistoryPart.Size, OwnerKey.Whole, new Dictionary<string, HistoryFieldName?>
        {
            ["position"] = HistoryFieldName.Position,
            ["productId"] = null,
        }),
        [AuditedTables.ProductSizeTranslations] = new(HistoryPart.Size, OwnerKey.First, new Dictionary<string, HistoryFieldName?>
        {
            ["name"] = HistoryFieldName.Name,
        }),
        [AuditedTables.ProductVariants] = new(HistoryPart.Bag, OwnerKey.Whole, new Dictionary<string, HistoryFieldName?>
        {
            // The code is the bag's label, and its number is part of the code.
            ["number"] = null,
            ["sku"] = null,
            ["fabricId"] = HistoryFieldName.Fabric,
            ["colourId"] = HistoryFieldName.Colour,
            ["sizeId"] = HistoryFieldName.Size,
            ["isActive"] = HistoryFieldName.Active,
            ["productId"] = null,
        }),
        [AuditedTables.PieceRates] = new(HistoryPart.Rate, OwnerKey.Whole, new Dictionary<string, HistoryFieldName?>
        {
            // The stage is the rate's label.
            ["productId"] = null,
            ["stageId"] = null,
            ["amount"] = HistoryFieldName.Amount,
            ["currency"] = null,
            ["startsOn"] = HistoryFieldName.StartsOn,
            ["enteredAt"] = null,
        }),
        [AuditedTables.Contractors] = new(HistoryPart.Contractor, OwnerKey.Whole, Person(hasStartDate: false)),
        [AuditedTables.ContractorStages] = new(HistoryPart.Stage, OwnerKey.Last, new Dictionary<string, HistoryFieldName?>
        {
            ["contractorId"] = null,
            ["stageId"] = null,
        }),
        [AuditedTables.Workers] = new(HistoryPart.Worker, OwnerKey.Whole, Person(hasStartDate: true)),
        [AuditedTables.WorkerStages] = new(HistoryPart.Stage, OwnerKey.Last, new Dictionary<string, HistoryFieldName?>
        {
            ["workerId"] = null,
            ["stageId"] = null,
        }),
    };

    public string OwnerOf(string key) => Owner switch
    {
        OwnerKey.First => key[..key.IndexOf(',', StringComparison.Ordinal)],
        OwnerKey.Last => key[(key.LastIndexOf(',') + 1)..],
        _ => key,
    };

    private static Dictionary<string, HistoryFieldName?> Person(bool hasStartDate)
    {
        var fields = new Dictionary<string, HistoryFieldName?>
        {
            ["fullName"] = HistoryFieldName.FullName,
            ["mobileNumber"] = HistoryFieldName.MobileNumber,
            ["notes"] = HistoryFieldName.Notes,
            ["iban"] = HistoryFieldName.Iban,
            ["isActive"] = HistoryFieldName.Active,
            ["createdAt"] = null,
        };

        if (hasStartDate)
        {
            fields["startDate"] = HistoryFieldName.StartDate;
        }

        return fields;
    }
}
