using Microsoft.Extensions.DependencyInjection;

namespace CoreMs.Common.Extensions;

/// <summary>
/// Marks a class for automatic DI registration as a scoped repository.
/// Registers against the first interface matching IClassName convention.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class RepositoryAttribute(ServiceLifetime lifetime = ServiceLifetime.Scoped) : Attribute
{
    public ServiceLifetime Lifetime { get; } = lifetime;
}
