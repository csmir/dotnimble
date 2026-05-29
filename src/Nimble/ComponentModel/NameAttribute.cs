namespace Nimble.ComponentModel;

/// <summary>
///     Defines an attribute that can be used to specify the name of a member, type, or other code element. This attribute can be used for documentation purposes, code generation, or to provide additional metadata about the element it is applied to.
/// </summary>
[AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = false)]
public class NameAttribute(string name) : Attribute
{
    /// <summary>
    ///     Gets the name associated with the attributed element.
    /// </summary>
    public string Name { get; } = name;
}
