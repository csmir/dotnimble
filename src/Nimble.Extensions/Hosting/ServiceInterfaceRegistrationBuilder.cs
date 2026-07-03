using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nimble.Extensions.DependencyInjection;
using Nimble.Extensions.DependencyInjection.Extensions;
using System.Reflection;

namespace Nimble.Extensions.Hosting;

/// <summary>
///     An implementation of the <see cref="IServiceInterfaceRegistrationBuilder"/> interface that provides methods for configuring the registration of service interfaces in the dependency injection container.
/// </summary>
public class ServiceInterfaceRegistrationBuilder : IServiceInterfaceRegistrationBuilder
{
    /// <summary>
    ///     Gets the list of service types that have been registered in the dependency injection container.
    /// </summary>
    /// <remarks>
    ///     Types added to this list will be registered with the dependency injection container when the <see cref="ServiceCollectionExtensions.AddServiceInterfaces(IServiceCollection, IEnumerable{Type})"/> method is called.
    /// </remarks>
    public IList<Type> ServiceTypes { get; } = [];

    /// <inheritdoc />
    public void AddServiceType<T>()
        where T : class, IService 
        => ServiceTypes.Add(typeof(T));

    /// <inheritdoc />
    public void AddAssemblyTypes(Assembly assembly)
    {
        foreach (var type in assembly.GetExportedTypes())
            ServiceTypes.Add(type);
    }
}
