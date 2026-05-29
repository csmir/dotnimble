using System.ComponentModel;

namespace System;

/// <summary>
///     Provides extensions for the <see cref="Enum"/> primitive type.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class EnumExtensions
{
    private static class GenericAttributeCache<TEnum, TAttribute>
        where TEnum : Enum
        where TAttribute : Attribute
    {
        public static readonly IReadOnlyDictionary<TEnum, IEnumerable<TAttribute>> Attributes = GetAttributes();

        private static Dictionary<TEnum, IEnumerable<TAttribute>> GetAttributes()
            => typeof(TEnum)
                .GetFields(BindingFlags.Public | BindingFlags.Static)
                .ToDictionary(
                    f => (TEnum)f.GetValue(null)!,
                    f => f.GetCustomAttributes(false).OfType<TAttribute>()
                );
    }

    extension(Enum enm)
    {
        /// <summary>
        ///     Gets the attribute of type <typeparamref name="TAttribute"/> applied to the enum value. If no such attribute is found, returns null.
        /// </summary>
        /// <typeparam name="TAttribute">The type of attribute to retrieve from the enum value.</typeparam>
        /// <returns>The reference of <typeparamref name="TAttribute"/> found on the enum value if it exists. Otherwise <see langword="null"/>.</returns>
        /// <exception cref="KeyNotFoundException">Thrown when the field does not exist explicitly on the provided enum.</exception>
        public TAttribute? GetAttribute<TAttribute>()
            where TAttribute : Attribute
        {
            var targetField = enm.GetType().GetField(enm.ToString() ?? "")
                ?? throw new KeyNotFoundException($"Field '{enm}' not found in enum type '{enm.GetType().FullName}'.");

            return targetField.GetCustomAttributes(false)
                .OfType<TAttribute>()
                .FirstOrDefault();
        }

        /// <summary>
        ///     Gets whether the enum value has an attribute of type <typeparamref name="TAttribute"/> applied to it.
        /// </summary>
        /// <typeparam name="TAttribute">The type of attribute to check the existence of on the enum value.</typeparam>
        /// <returns><see langword="true"/> if the attribute of <typeparamref name="TAttribute"/> exists on the field; otherwise <see langword="false"/>.</returns>
        /// <exception cref="KeyNotFoundException">Thrown when the field does not exist explicitly on the provided enum.</exception>
        public bool HasAttribute<TAttribute>()
            where TAttribute : Attribute
            => GetAttribute<TAttribute>(enm) is not null;

        /// <summary>
        ///     Creates a dictionary mapping each enum value of type <typeparamref name="TEnum"/> to its associated attributes.
        /// </summary>
        /// <typeparam name="TEnum">The type of the enum to access the values of.</typeparam>
        /// <returns>An <see cref="IReadOnlyDictionary{TKey, TValue}"/> containing all named fields in the enum and any attributes on that field.</returns>
        public static IReadOnlyDictionary<TEnum, IEnumerable<Attribute>> GetAttributes<TEnum>()
            where TEnum : Enum
            => GetAttributes<TEnum, Attribute>();

        /// <summary>
        ///     Creates a dictionary mapping each enum value of type <typeparamref name="TEnum"/> to its associated attributes. 
        /// </summary>
        /// <returns>An <see cref="IReadOnlyDictionary{TKey, TValue}"/> containing all named fields in the enum and any attributes on that field.</returns>
        public static IReadOnlyDictionary<TEnum, IEnumerable<TAttribute>> GetAttributes<TEnum, TAttribute>()
            where TEnum : Enum
            where TAttribute : Attribute
            => GenericAttributeCache<TEnum, TAttribute>.Attributes;
    }
}
