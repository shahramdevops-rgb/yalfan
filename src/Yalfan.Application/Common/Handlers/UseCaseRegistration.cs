using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Yalfan.Application.Common.Validation;

namespace Yalfan.Application.Common.Handlers;

public static class UseCaseRegistration
{
    /// <summary>
    /// Registers every command and query handler and every validator found in
    /// <paramref name="assembly"/>. Callers resolve the handler interface and get the handler
    /// wrapped in the pipeline (validation first).
    /// </summary>
    /// <remarks>
    /// Written by hand instead of with Scrutor: it is a few lines, and it wraps only the handlers
    /// of this assembly, so calling it for a second assembly (the tests do) never wraps a handler
    /// twice. Two handlers for the same request are a bug and stop the start.
    /// </remarks>
    public static IServiceCollection AddUseCases(this IServiceCollection services, Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        var handlerTypes = assembly.GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false, IsGenericTypeDefinition: false });

        foreach (var handlerType in handlerTypes)
        {
            foreach (var handlerInterface in handlerType.GetInterfaces().Where(type => type.IsGenericType))
            {
                var pipelineType = PipelineFor(handlerInterface.GetGenericTypeDefinition());
                if (pipelineType is null)
                {
                    continue;
                }

                if (services.Any(descriptor => descriptor.ServiceType == handlerInterface))
                {
                    throw new InvalidOperationException(
                        $"{handlerInterface} has more than one handler; {handlerType} is the second.");
                }

                services.AddScoped(handlerType);

                var createPipeline = ActivatorUtilities.CreateFactory(
                    pipelineType.MakeGenericType(handlerInterface.GetGenericArguments()),
                    [handlerInterface]);

                services.AddScoped(handlerInterface, provider =>
                    createPipeline(provider, [provider.GetRequiredService(handlerType)]));
            }
        }

        services.AddValidatorsFromAssembly(assembly, ServiceLifetime.Scoped, includeInternalTypes: true);

        return services;
    }

    private static Type? PipelineFor(Type handlerDefinition)
    {
        if (handlerDefinition == typeof(ICommandHandler<,>))
        {
            return typeof(ValidatingCommandHandler<,>);
        }

        if (handlerDefinition == typeof(IQueryHandler<,>))
        {
            return typeof(ValidatingQueryHandler<,>);
        }

        return null;
    }
}
