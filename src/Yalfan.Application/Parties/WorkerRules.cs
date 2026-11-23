using FluentValidation;
using Yalfan.Application.Common;
using Yalfan.Domain.Parties;

namespace Yalfan.Application.Parties;

internal static class WorkerRules
{
    /// <summary>
    /// Required, and not after the workshop's today (decided 1405/09/02, ADR 0020). Checked here as
    /// well as in the Domain, so the form shows the problem on the date field.
    /// </summary>
    public static IRuleBuilderOptions<T, DateOnly?> MustBeStartDate<T>(this IRuleBuilder<T, DateOnly?> rule, IClock clock) =>
        rule.NotNull()
            .Must(date => date <= clock.Today)
            .WithErrorCode(PartiesErrors.StartDateInFuture.Code);
}
