namespace Nimble.ComponentModel;

/// <inheritdoc />
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
public class ModelBindingAttribute<T>() : ModelBindingAttribute(typeof(T)) { }

/// <summary>
///     Defines an attribute that can be used to specify the type of model that a member is bound to. This attribute can be used for documentation purposes, code generation, or to provide additional metadata about the element it is applied to.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
public class ModelBindingAttribute(Type modelType) : Attribute
{
    /// <summary>
    ///     Gets the type of the model that the attributed member is bound to.
    /// </summary>
    public Type ModelType { get; } = modelType;
}