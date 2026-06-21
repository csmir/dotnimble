using Microsoft.Extensions.DependencyInjection;

namespace Nimble.Extensions.DependencyInjection;

/// <summary>
///     A lazily evaluated service that may or may not be resolved from the dependency injection container.
/// </summary>
/// <remarks>
///     When accessed, this implementation will throw a <see cref="InvalidOperationException"/> if the service is not registered in the dependency injection container.
/// </remarks>
/// <typeparam name="T">The service type to resolve optionally.</typeparam>
public interface IServiceLazy<T>
    where T : notnull
{
    /// <summary>
    ///     Gets the value of the service, resolving it from the dependency injection container if it has not been resolved yet.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the service of type <typeparamref name="T"/> is not registered in the dependency injection container.</exception>
    public T Value { get; }
}

internal sealed class ServiceLazy<T>(IServiceProvider serviceProvider) : IServiceLazy<T>
    where T : notnull
{
    public T Value 
        => field ??= serviceProvider.GetRequiredService<T>();
}