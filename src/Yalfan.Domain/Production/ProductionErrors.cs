using Yalfan.Domain.Common;

namespace Yalfan.Domain.Production;

public static class ProductionErrors
{
    public static readonly Error NameRequired = Error.Validation("production.name_required");

    public static readonly Error NameTooLong = Error.Validation("production.name_too_long");

    /// <summary>Another stage already has this Persian name.</summary>
    public static readonly Error NameTaken = Error.Conflict("production.name_taken");

    public static readonly Error StageNotFound = Error.NotFound("production.stage_not_found");

    /// <summary>The model a rate names does not exist (the catalog's own code is the catalog's).</summary>
    public static readonly Error ProductNotFound = Error.NotFound("production.product_not_found");

    /// <summary>A piece rate is more than zero; a stage that is not paid has no rate at all.</summary>
    public static readonly Error RateNotPositive = Error.Validation("production.rate_not_positive");
}
