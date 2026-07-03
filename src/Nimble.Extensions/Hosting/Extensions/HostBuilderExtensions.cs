using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Nimble.Extensions.DependencyInjection;
using Nimble.Extensions.DependencyInjection.Extensions;
using System.Reflection;

namespace Nimble.Extensions.Hosting.Extensions;

/// <inheritdoc cref="HostingHostBuilderExtensions"/>
public static class HostBuilderExtensions
{
    /// <inheritdoc cref="ConfigureServiceInterfaces(IHostBuilder, Action{HostBuilderContext, IServiceInterfaceRegistrationBuilder})"/>
    /// <param name="hostBuilder">The host builder to configure.</param>
    /// <returns>The configured host builder.</returns>
    public static IHostBuilder ConfigureServiceInterfaces(this IHostBuilder hostBuilder)
        => hostBuilder.ConfigureServiceInterfaces((_, builder) => builder.AddAssemblyTypes(Assembly.GetCallingAssembly()));

    /// <inheritdoc cref="ConfigureServiceInterfaces(IHostBuilder, Action{HostBuilderContext, IServiceInterfaceRegistrationBuilder})"/>
    /// <param name="hostBuilder">The host builder to configure.</param>
    /// <param name="configure">A delegate to configure the service interface registration builder.</param>
    /// <returns>The configured host builder.</returns>
    public static IHostBuilder ConfigureServiceInterfaces(this IHostBuilder hostBuilder, Action<IServiceInterfaceRegistrationBuilder> configure)
        => hostBuilder.ConfigureServiceInterfaces((_, builder) => configure(builder));

    /// <summary>
    ///     Configures the host with automated service registration.
    /// </summary>
    /// <remarks>
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
    /// </remarks>
    /// <param name="hostBuilder">The host builder to configure.</param>
    /// <param name="configure">A delegate to configure the service interface registration builder.</param>
    /// <returns>The configured host builder.</returns>
    public static IHostBuilder ConfigureServiceInterfaces(this IHostBuilder hostBuilder, Action<HostBuilderContext, IServiceInterfaceRegistrationBuilder> configure)
    {
        hostBuilder.ConfigureServices((context, services) =>
        {
            var builder = new ServiceInterfaceRegistrationBuilder();

            configure(context, builder);

            services.AddServiceInterfaces(builder.ServiceTypes);
        });

        return hostBuilder;
    }
}
