using System.Reflection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Yalfan.Api.Common.Endpoints;

public static class EndpointModuleExtensions
{
    /// <summary>
    /// Registers every <see cref="IEndpointModule"/> in <paramref name="assembly"/>. Modules are
    /// services, so a test can register one more and have it mapped by the real pipeline.
    /// </summary>
    public static IServiceCollection AddEndpointModules(this IServiceCollection services, Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        var moduleTypes = assembly.GetTypes().Where(type =>
            type is { IsClass: true, IsAbstract: false } && type.IsAssignableTo(typeof(IEndpointModule)));

        foreach (var moduleType in moduleTypes)
        {
            services.TryAddEnumerable(ServiceDescriptor.Singleton(typeof(IEndpointModule), moduleType));
        }

        return services;
    }

    public static IEndpointRouteBuilder MapEndpointModules(this IEndpointRouteBuilder endpoints)
    {
        foreach (var module in endpoints.ServiceProvider.GetServices<IEndpointModule>())
        {
            module.MapEndpoints(endpoints);
        }

        return endpoints;
    }
}
