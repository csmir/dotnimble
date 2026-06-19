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
        ///     Gets an empty service provider instance that can be used when no services are available.
        /// </summary>
        public static IServiceProvider Empty => new EmptyServiceProvider();
    }
}