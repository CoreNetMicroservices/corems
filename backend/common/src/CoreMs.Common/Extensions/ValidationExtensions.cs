using System.Reflection;
using CoreMs.Common.Middleware;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace CoreMs.Common.Extensions;

/// <summary>
/// Extension methods for setting up FluentValidation with the CoreMS validation pipeline.
/// </summary>
public static class ValidationExtensions
{
    /// <summary>
    /// Registers the ValidationFilter globally and scans the given assemblies for FluentValidation validators.
    /// Replaces the need for AddValidatorsFromAssemblyContaining and manual filter registration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="assemblies">Assemblies to scan for AbstractValidator implementations.</param>
    public static IServiceCollection AddCoreMsValidation(this IServiceCollection services, params Assembly[] assemblies)
    {
        // Register ValidationFilter as a global action filter
        services.Configure<MvcOptions>(options =>
        {
            options.Filters.Add<ValidationFilter>();
        });

        // Scan assemblies for validators and register them
        foreach (var assembly in assemblies)
        {
            var validatorTypes = assembly.GetTypes()
                .Where(t => t is { IsClass: true, IsAbstract: false, IsGenericType: false })
                .Where(t => t.BaseType is { IsGenericType: true }
                         && t.BaseType.GetGenericTypeDefinition() == typeof(AbstractValidator<>));

            foreach (var validatorType in validatorTypes)
            {
                var modelType = validatorType.BaseType!.GetGenericArguments()[0];
                var serviceType = typeof(IValidator<>).MakeGenericType(modelType);
                services.AddScoped(serviceType, validatorType);
            }
        }

        return services;
    }
}
