namespace Yalfan.Application.History;

/// <summary>
/// The tables whose audit entries make up a change history. An audit entry names its row by
/// table (ADR 0013), so these names are what the history reads by; the EF configurations map
/// the tables with the same constants, so the two can never drift apart (ADR 0027).
/// </summary>
public static class AuditedTables
{
    public const string Products = "products";

    public const string ProductTranslations = "product_translations";

    public const string ProductSizes = "product_sizes";

    public const string ProductSizeTranslations = "product_size_translations";

    public const string ProductVariants = "product_variants";

    public const string PieceRates = "piece_rates";

    public const string Contractors = "contractors";

    public const string ContractorStages = "contractor_stages";

    public const string Workers = "workers";

    public const string WorkerStages = "worker_stages";
}
