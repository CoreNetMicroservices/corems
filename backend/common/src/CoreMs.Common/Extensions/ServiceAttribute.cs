using Microsoft.Extensions.DependencyInjection;

namespace CoreMs.Common.Extensions;

/// <summary>
/// Marks a class for automatic DI registration as a scoped service.
/// If the class implements an interface matching IClassName, it registers as that interface.
/// Otherwise it registers as itself (concrete type).
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class ServiceAttribute(ServiceLifetime lifetime = ServiceLifetime.Scoped) : Attribute
{
    public ServiceLifetime Lifetime { get; } = lifetime;
}
