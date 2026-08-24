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
    /// and registers each as its concrete type.
    /// To register a class behind an interface (e.g. multiple implementations), register it
    /// explicitly in Program.cs instead.
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
