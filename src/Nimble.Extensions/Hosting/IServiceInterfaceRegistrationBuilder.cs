using Microsoft.Extensions.Configuration;
using Nimble.Extensions.DependencyInjection;
using System.Reflection;

namespace Nimble.Extensions.Hosting;

/// <summary>
///     A builder interface that defines methods for configuring the registration of service interfaces in the dependency injection container.
/// </summary>
public interface IServiceInterfaceRegistrationBuilder
{
    /// <summary>
    ///     Adds a service type to the list of service types that will be registered in the dependency injection container.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public void AddServiceType<T>()
        where T : class, IService;

    /// <summary>
    ///     Adds all exported types from the specified assembly to the list of service types that will be registered in the dependency injection container.
    /// </summary>
    /// <param name="assembly"></param>
    public void AddAssemblyTypes(Assembly assembly);
}
