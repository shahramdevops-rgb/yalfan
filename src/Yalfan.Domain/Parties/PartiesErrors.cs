using Yalfan.Domain.Common;

namespace Yalfan.Domain.Parties;

public static class PartiesErrors
{
    public static readonly Error FullNameRequired = Error.Validation("parties.full_name_required");

    public static readonly Error FullNameTooLong = Error.Validation("parties.full_name_too_long");

    public static readonly Error NotesTooLong = Error.Validation("parties.notes_too_long");

    /// <summary>A contractor does at least one stage (decided 1405/09/02). A worker may have none.</summary>
    public static readonly Error StageRequired = Error.Validation("parties.stage_required");

    /// <summary>One of the ticked stages does not exist.</summary>
    public static readonly Error StageNotFound = Error.NotFound("parties.stage_not_found");

    /// <summary>
    /// A newly ticked stage is deactivated. A stage that was already ticked before it was
    /// deactivated stays ticked, so saving the profile again never fails on it (ADR 0022).
    /// </summary>
    public static readonly Error StageInactive = Error.Rule("parties.stage_inactive");

    /// <summary>
    /// Another contractor, or another worker, already has this mobile number: it will be the
    /// login (Phase 10). The two lists are checked apart; one person may be in both (ADR 0020).
    /// </summary>
    public static readonly Error MobileTaken = Error.Conflict("parties.mobile_taken");

    public static readonly Error ContractorNotFound = Error.NotFound("parties.contractor_not_found");

    public static readonly Error WorkerNotFound = Error.NotFound("parties.worker_not_found");

    /// <summary>A worker's start date after today, almost always a mistyped year (decided 1405/09/02).</summary>
    public static readonly Error StartDateInFuture = Error.Validation("parties.start_date_in_future");

    /// <summary>The owner asked for the whole IBAN of a contractor or worker who has none.</summary>
    public static readonly Error IbanNotSet = Error.NotFound("parties.iban_not_set");
}
