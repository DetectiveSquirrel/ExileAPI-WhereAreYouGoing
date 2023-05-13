using GameOffsets.Native;
using System.Collections.Generic;
using System.Linq;
using Vector2 = System.Numerics.Vector2;

namespace WhereAreYouGoing
{
    /// <summary>
    /// Provides extension methods for <see cref="System.Numerics.Vector2"/> objects.
    /// </summary>
    public static class Vector2Extensions
    {
        /// <summary>
        /// Converts a list of <see cref="Vector2i"/> objects to a list of <see cref="System.Numerics.Vector2"/> objects.
        /// </summary>
        /// <param name="vector2iList">The list of <see cref="Vector2i"/> objects to convert.</param>
        /// <returns>A list of <see cref="System.Numerics.Vector2"/> objects.</returns>
        public static List<Vector2> ConvertToVector2List(this IList<Vector2i> vector2iList)
        {
            return vector2iList.Select(v => new Vector2(v.X, v.Y)).ToList();
        }
    }
}