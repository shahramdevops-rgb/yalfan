using Yalfan.Domain.Common;

namespace Yalfan.Domain.Parties;

public static class PartiesErrors
{
    public static readonly Error FullNameRequired = Error.Validation("parties.full_name_required");

    public static readonly Error FullNameTooLong = Error.Validation("parties.full_name_too_long");

    public static readonly Error NotesTooLong = Error.Validation("parties.notes_too_long");

    /// <summary>A contractor does at least one stage (decided 1405/09/02).</summary>
    public static readonly Error StageRequired = Error.Validation("parties.stage_required");

    /// <summary>One of the ticked stages does not exist.</summary>
    public static readonly Error StageNotFound = Error.NotFound("parties.stage_not_found");

    /// <summary>Another contractor already has this mobile number: it will be his login (Phase 10).</summary>
    public static readonly Error MobileTaken = Error.Conflict("parties.mobile_taken");

    public static readonly Error ContractorNotFound = Error.NotFound("parties.contractor_not_found");

    /// <summary>The owner asked for the whole IBAN of a contractor who has none.</summary>
    public static readonly Error IbanNotSet = Error.NotFound("parties.iban_not_set");
}
