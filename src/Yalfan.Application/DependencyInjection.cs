using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Yalfan.Application.Common.Handlers;
using Yalfan.Application.Identity.Sessions;

namespace Yalfan.Application;

public static class DependencyInjection
{
    /// <summary>Registers the Application layer: use-case handlers, validators and their helpers.</summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // A rule stops at its first failure, so each field reports one problem: "required" and
        // "too short" for the same empty box is noise for the operator.
        ValidatorOptions.Global.DefaultRuleLevelCascadeMode = CascadeMode.Stop;

        services.AddScoped<SessionIssuer>();

        return services.AddUseCases(typeof(DependencyInjection).Assembly);
    }
}
