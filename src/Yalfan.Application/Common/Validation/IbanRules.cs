using FluentValidation;
using Yalfan.Domain.Common;

namespace Yalfan.Application.Common.Validation;

public static class IbanRules
{
    /// <summary>Required, and an Iranian IBAN in any form <see cref="Iban.Create"/> accepts.</summary>
    public static IRuleBuilderOptions<T, string?> MustBeIban<T>(this IRuleBuilder<T, string?> rule) =>
        rule.NotEmpty()
            .Must(value => Iban.Create(value).IsSuccess)
            .WithErrorCode(Iban.InvalidCode);
}
