using System.ComponentModel;

namespace System;

/// <summary>
///     Extensions for the <see cref="Type"/> class.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class TypeExtensions
{
    extension (Type t)
    {
        /// <summary>
        ///     Gets whether the current type is an implementation of <see cref="Nullable{T}"/>.
        /// </summary>
        /// <returns><see langword="true"/> if the type is an implementation of <see cref="Nullable{T}"/>, otherwise <see langword="false"/>.</returns>
        public bool IsNullable => Nullable.GetUnderlyingType(t) is not null;

        /// <summary>
        ///     Attempts to retrieve the underlying type of a nullable type.
        /// </summary>
        /// <remarks>
        ///     This method checks if the provided <paramref name="type"/> is an implementation of <see cref="Nullable{T}"/> and, if so, retrieves the underlying type of the nullable type.
        /// </remarks>
        /// <param name="type">The type to check for.</param>
        /// <param name="underlyingType">When this method returns, contains the underlying type of the nullable type if the type is nullable; otherwise, <see langword="null"/>.</param>
        /// <returns><see langword="true"/> if the type is an implementation of <see cref="Nullable{T}"/>, otherwise <see langword="false"/>.</returns>
        public static bool TryGetNullableUnderlyingType(Type type,
#if NET6_0_OR_GREATER
            [NotNullWhen(true)]
#endif
            out Type? underlyingType)
        {
            underlyingType = Nullable.GetUnderlyingType(type);

            return underlyingType is not null;
        }

        /// <summary>
        ///     Checks whether the provided types have weak equality. It returns <see langword="true"/> when:
        /// </summary>
        /// <remarks>
        ///     <list type="number">
        ///         <item>The types are equal.</item>
        ///         <item>The types are arrays with the same* element type.</item>
        ///         <item>The types are generic types with the same* generic arguments.</item>
        ///         <item>One of the types is nullable but has the same underlying type as the other.</item>
        ///         <item>One of the types is assignable from the other.</item>
        ///     </list>
        ///     <br/>
        ///     "Same" in this context means that the element type or generic arguments are also compared using this method. 
        ///     This means that the underlying types are matched using the same logical order as listed above.
        /// </remarks>
        /// <param name="a">The first type to compare.</param>
        /// <param name="b">The second type to compare.</param>
        /// <returns><see langword="true"/> if the types have weak equality, otherwise <see langword="false"/>.</returns>
        public static bool WeakEquals(Type? a, Type? b)
        {
            // 1: The types are equal
            if (a == b)
                return true;

            if (a is null || b is null)
                return false;

            // 2: The types are arrays with the same* element type
            if (a.IsArray && b.IsArray)
                return Equals(a.GetElementType(), b.GetElementType());

            // 3: The types are generic types with the same* generic arguments
            if (a.IsGenericType && b.IsGenericType)
            {
                if (a.GetGenericTypeDefinition() != b.GetGenericTypeDefinition())
                    return false;

                var argsA = a.GetGenericArguments();
                var argsB = b.GetGenericArguments();

                if (argsA.Length != argsB.Length)
                    return false;

                for (int i = 0; i < argsA.Length; i++)
                    if (!Equals(argsA[i], argsB[i]))
                        return false;

                return true;
            }

            // 4: One of the types is nullable but has the same underlying type as the other.
            if (TryGetNullableUnderlyingType(a, out var underlyingA))
                return Equals(underlyingA, b);

            if (TryGetNullableUnderlyingType(b, out var underlyingB))
                return Equals(underlyingB, a);

            // 5: One of the types is assignable from the other.
            if (a.IsAssignableFrom(b) || b.IsAssignableFrom(a))
                return true;

            return false;
        }
    }
}
