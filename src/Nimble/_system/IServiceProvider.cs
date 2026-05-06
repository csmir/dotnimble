using System.ComponentModel;

namespace System;

/// <summary>
///     Provides extension methods for the <see cref="IServiceProvider"/> interface.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class ServiceProviderExtensions
{
    internal sealed class EmptyServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType) => null;
    }

    extension(IServiceProvider provider)
    {
        /// <summary>
        ///     Gets a service of type <typeparamref name="T"/> from the service provider. If the service is not found, returns null.
        /// </summary>
        /// <typeparam name="T">The type of service to retrieve from the service provider.</typeparam>
        /// <returns>The reference of the service of type <typeparamref name="T"/> if it exists. Otherwise <see langword="null"/>.</returns>
        public T? GetService<T>()
            where T : class
            => provider.GetService(typeof(T)) as T;

        /// <summary>
        ///     Gets an empty service provider instance that can be used when no services are available.
        /// </summary>
        public static IServiceProvider Empty => new EmptyServiceProvider();
    }
}