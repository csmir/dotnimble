using System.ComponentModel;
using Nimble.Drawing;

namespace System.Drawing;

/// <summary>
///     Provides extension methods for <see cref="Color"/>.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class ColorExtensions
{
    extension(Color color)
    { 
        /// <summary>
        ///     Converts this <see cref="Color"/> to a <see cref="Composite"/> instance.
        /// </summary>
        /// <returns>A <see cref="Composite"/> instance that encapsulates the value of this <see cref="Color"/>.</returns>
        public Composite ToComposite() => Composite.FromColor(color);
    }
}
