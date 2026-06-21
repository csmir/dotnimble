using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Nimble.Extensions.DependencyInjection.Extensions;

/// <inheritdoc cref="ServiceCollectionServiceExtensions"/>
public static class ServiceCollectionExtensions
{
    /// <summary>
    ///     Adds the <see cref="IServiceLazy{T}"/> and <see cref="IServiceLazyOptional{T}"/> services to the service collection.
    /// </summary>
    /// <remarks>
    ///     Services injected as <see cref="IServiceLazy{T}"/> or <see cref="IServiceLazyOptional{T}"/> will be resolved lazily, 
    ///     meaning that the service will not be created until it is actually needed. This can help improve performance and reduce memory usage in certain scenarios.
    /// </remarks>
    /// <param name="services">The service collection to which the lazy services will be added.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddLazyServices(this IServiceCollection services)
    {
        services.AddTransient(typeof(IServiceLazy<>), typeof(ServiceLazy<>));
        services.AddTransient(typeof(IServiceLazyOptional<>), typeof(ServiceLazyOptional<>));

        return services;
    }

    /// <inheritdoc cref="AddServiceInterfaces(IServiceCollection, IEnumerable{Type})"/>
    /// <remarks>
    ///     This method scans the <see cref="Assembly.GetEntryAssembly"/> for implementations of <see cref="IService"/> and registers them with the service collection.
    /// </remarks>
    public static IServiceCollection AddServiceInterfaces(this IServiceCollection services)
        => AddServiceInterfaces(services, Assembly.GetEntryAssembly()?.GetTypes() ?? []);

    /// <inheritdoc cref="AddServiceInterfaces(IServiceCollection, IEnumerable{Type})"/>
    /// <remarks>
    ///     This method scans the provided <paramref name="assembly"/> for implementations of <see cref="IService"/> and registers them with the service collection.
    /// </remarks>
    /// <param name="services">The service collection to which the interface-driven services will be added.</param>
    /// <param name="assembly">The assembly to scan for interface implementations.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddServiceInterfaces(this IServiceCollection services, Assembly assembly)
        => AddServiceInterfaces(services, assembly.GetTypes());

    /// <summary>
    ///     Adds interface-driven service registrations to the service collection using the following lifetimes:
    ///     <list type="bullet">
    ///         <item><see cref="ITransientService"/> as <see cref="ServiceLifetime.Transient"/>.</item>
    ///         <item><see cref="IScopedService"/> as <see cref="ServiceLifetime.Scoped"/>.</item>
    ///         <item><see cref="ISingletonService"/> and <see cref="IService"/> as <see cref="ServiceLifetime.Singleton"/>.</item>
    ///     </list>
    ///     When a service implements multiple <see cref="IService"/> implementations, the smallest lifetime will be used for registration.<br />
    ///     <br />
    ///     When a service implements a matching interface (e.g. <c>MyService : IMyService</c>), the service will be registered as the interface type. Otherwise, the service will be registered as itself.<br />
    ///     <b>IMPORTANT:</b> It is recommended to use this method with caution, as it may register unintended services if the scanned assembly contains multiple implementations of the same interface.
    /// </summary>
    /// <param name="services">The service collection to which the interface-driven services will be added.</param>
    /// <param name="types">The types to scan for interface implementations.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddServiceInterfaces(this IServiceCollection services, params IEnumerable<Type> types)
    {
        foreach (var type in types) 
            TryAddServiceInterface(services, type);
        return services;
    }

    private static void TryAddServiceInterface(IServiceCollection services, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type type)
    {
        if (!type.IsClass || type.IsAbstract || !typeof(IService).IsAssignableFrom(type))
            return;

        services.Add(new ServiceDescriptor(GetServiceType(type), type, GetServiceLifetime(type)));
    }

    private static Type GetServiceType([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type type)
    {
        var matchingName = type.GetInterfaces().FirstOrDefault(i => i.Name == $"I{type.Name}");

        if (matchingName != null)
            return matchingName;

        return type;
    }

    private static ServiceLifetime GetServiceLifetime([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type type)
    {
        if (typeof(ITransientService).IsAssignableFrom(type))
            return ServiceLifetime.Transient;

        if (typeof(IScopedService).IsAssignableFrom(type))
            return ServiceLifetime.Scoped;

        return ServiceLifetime.Singleton;
    }
}
