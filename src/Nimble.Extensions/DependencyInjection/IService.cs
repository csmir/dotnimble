using Nimble.Extensions.DependencyInjection.Extensions;

namespace Nimble.Extensions.DependencyInjection;

/// <summary>
///     A marker interface used to identify services that can be registered with the dependency injection container.
/// </summary>
/// <remarks>
///     Interface-driven service registration is not enabled by default. Consider calling <see cref="ServiceCollectionExtensions.AddServiceInterfaces(Microsoft.Extensions.DependencyInjection.IServiceCollection)"/> to enable automatic registration of services that implement the <see cref="IService"/> interface and its derived interfaces.
/// </remarks>
public interface IService { }

/// <summary>
///     A marker interface that indicates a service should be registered with a transient lifetime in the dependency injection container.
/// </summary>
public interface ITransientService : IService { }

/// <summary>
///     A marker interface that indicates a service should be registered with a scoped lifetime in the dependency injection container.
/// </summary>
public interface IScopedService : IService { }

/// <summary>
///     A marker interface that indicates a service should be registered with a singleton lifetime in the dependency injection container.
/// </summary>
public interface ISingletonService : IService { }