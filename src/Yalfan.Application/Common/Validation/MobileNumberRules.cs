using FluentValidation;
using Yalfan.Domain.Common;

namespace Yalfan.Application.Common.Validation;

public static class MobileNumberRules
{
    /// <summary>Required, and an Iranian mobile in any form <see cref="MobileNumber.Create"/> accepts.</summary>
    public static IRuleBuilderOptions<T, string?> MustBeMobileNumber<T>(this IRuleBuilder<T, string?> rule) =>
        rule.NotEmpty()
            .Must(value => MobileNumber.Create(value).IsSuccess)
            .WithErrorCode(MobileNumber.InvalidCode);
}
