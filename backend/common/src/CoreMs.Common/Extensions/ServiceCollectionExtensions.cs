using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace CoreMs.Common.Extensions;

/// <summary>
/// Extension methods for auto-registering services and repositories by attribute.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Scans the given assemblies for classes marked with [Service] or [Repository]
    /// and registers them in the DI container.
    /// If the class implements an interface matching IClassName, it registers as that interface.
    /// Otherwise it registers as itself (concrete type).
    /// </summary>
    public static IServiceCollection AddCoreMsServices(this IServiceCollection services, params Assembly[] assemblies)
    {
        foreach (var assembly in assemblies)
        {
            var types = assembly.GetTypes()
                .Where(t => t is { IsClass: true, IsAbstract: false, IsGenericType: false });

            foreach (var type in types)
            {
                var lifetime = GetLifetime(type);
                if (lifetime is null) continue;

                var matchingInterface = type.GetInterfaces()
                    .FirstOrDefault(i => !i.IsGenericType && i.Name == $"I{type.Name}");

                if (matchingInterface != null)
                    services.Add(new ServiceDescriptor(matchingInterface, type, lifetime.Value));
                else
                    services.Add(new ServiceDescriptor(type, type, lifetime.Value));
            }
        }

        return services;
    }

    private static ServiceLifetime? GetLifetime(Type type)
    {
        var serviceAttr = type.GetCustomAttribute<ServiceAttribute>();
        if (serviceAttr != null) return serviceAttr.Lifetime;

        var repoAttr = type.GetCustomAttribute<RepositoryAttribute>();
        if (repoAttr != null) return repoAttr.Lifetime;

        return null;
    }
}
