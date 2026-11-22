using Yalfan.Domain.Common;

namespace Yalfan.Domain.Production;

public static class ProductionErrors
{
    public static readonly Error NameRequired = Error.Validation("production.name_required");

    public static readonly Error NameTooLong = Error.Validation("production.name_too_long");

    /// <summary>Another stage already has this Persian name.</summary>
    public static readonly Error NameTaken = Error.Conflict("production.name_taken");

    public static readonly Error StageNotFound = Error.NotFound("production.stage_not_found");
}
