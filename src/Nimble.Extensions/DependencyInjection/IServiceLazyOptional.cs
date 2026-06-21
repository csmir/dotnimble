using Microsoft.Extensions.DependencyInjection;

namespace Nimble.Extensions.DependencyInjection;

/// <summary>
///     A lazily evaluated service that may or may not be resolved from the dependency injection container.
/// </summary>
/// <remarks>
///     When accessed, this implementation may return <see langword="null"/> if the service cannot be resolved.
/// </remarks>
/// <typeparam name="T">The service type to resolve.</typeparam>
public interface IServiceLazyOptional<T>
{
    /// <summary>
    ///     Gets the value of the service, resolving it from the dependency injection container if it has not been resolved yet.
    /// </summary>
    /// <remarks>
    ///     If the service cannot be resolved, this property will return <see langword="null"/>. If lazily evaluated required services are preferred, use <see cref="IServiceLazy{T}"/>.
    /// </remarks>
    public T? Value { get; }
}

internal sealed class ServiceLazyOptional<T>(IServiceProvider serviceProvider) : IServiceLazyOptional<T>
    where T : class
{
    public T? Value 
        => field ??= serviceProvider.GetService<T>();
}