using Yalfan.Domain.Common;

namespace Yalfan.Domain.Production;

public static class ProductionErrors
{
    public static readonly Error NameRequired = Error.Validation("production.name_required");

    public static readonly Error NameTooLong = Error.Validation("production.name_too_long");

    /// <summary>Another stage already has this Persian name.</summary>
    public static readonly Error NameTaken = Error.Conflict("production.name_taken");

    public static readonly Error StageNotFound = Error.NotFound("production.stage_not_found");

    /// <summary>A new rate for a deactivated stage; its old rates stay (ADR 0022).</summary>
    public static readonly Error StageInactive = Error.Rule("production.stage_inactive");

    /// <summary>
    /// The new order does not name every stage exactly once: it was made from an old copy of the
    /// list, for example while another user added a stage. The panel reloads the list.
    /// </summary>
    public static readonly Error StageOrderOutdated = Error.Conflict("production.stage_order_outdated");

    /// <summary>The model a rate names does not exist (the catalog's own code is the catalog's).</summary>
    public static readonly Error ProductNotFound = Error.NotFound("production.product_not_found");

    /// <summary>A piece rate is more than zero; a stage that is not paid has no rate at all.</summary>
    public static readonly Error RateNotPositive = Error.Validation("production.rate_not_positive");
}
