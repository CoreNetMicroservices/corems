using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace CoreMs.Common.Extensions;

/// <summary>
/// Extension methods for auto-registering services by convention.
/// Scans assemblies for interface/implementation pairs and registers them in DI.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Auto-registers all services from the given assemblies by convention.
    /// Matches IFooService → FooService and IFooRepository → FooRepository.
    /// All registrations are Scoped (one instance per HTTP request).
    /// </summary>
    public static IServiceCollection AddCoreMsServices(this IServiceCollection services, params Assembly[] assemblies)
    {
        foreach (var assembly in assemblies)
        {
            var types = assembly.GetTypes()
                .Where(t => t is { IsClass: true, IsAbstract: false, IsGenericType: false })
                .ToList();

            foreach (var implementationType in types)
            {
                var interfaces = implementationType.GetInterfaces()
                    .Where(i => !i.IsGenericType && i.Name.StartsWith('I') && MatchesByConvention(i, implementationType))
                    .ToList();

                foreach (var interfaceType in interfaces)
                {
                    services.AddScoped(interfaceType, implementationType);
                }
            }
        }

        return services;
    }

    /// <summary>
    /// Convention: IFooService matches FooService, IUserRepository matches UserRepository.
    /// The implementation name must equal the interface name without the leading 'I'.
    /// </summary>
    private static bool MatchesByConvention(Type interfaceType, Type implementationType)
    {
        var expectedName = interfaceType.Name[1..]; // Remove leading 'I'
        return implementationType.Name == expectedName;
    }
}
