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

        /// <summary>
        /// Adds an offset to each Vector2 in the list.
        /// </summary>
        /// <param name="vectorList">The list of Vector2 objects to modify.</param>
        /// <param name="offsetX">The offset value to add to the X component of each vector.</param>
        /// <param name="offsetY">The offset value to add to the Y component of each vector.</param>
        public static List<Vector2> AddOffset(this List<Vector2> vectorList, float offsetX, float offsetY)
        {
            List<Vector2> modifiedList = new List<Vector2>(vectorList.Count);

            for (int i = 0; i < vectorList.Count; i++)
            {
                Vector2 vector = vectorList[i];
                vector.X += offsetX;
                vector.Y += offsetY;
                modifiedList.Add(vector);
            }

            return modifiedList;
        }

        /// <summary>
        /// Adds an offset to each Vector2 in the list.
        /// </summary>
        /// <param name="vectorList">The list of Vector2 objects to modify.</param>
        /// <param name="offset">The offset value to add to each vector.</param>
        public static List<Vector2> AddOffset(this List<Vector2> vectorList, float offset)
        {
            List<Vector2> modifiedList = new List<Vector2>(vectorList.Count);

            for (int i = 0; i < vectorList.Count; i++)
            {
                Vector2 vector = vectorList[i];
                vector.X += offset;
                vector.Y += offset;
                modifiedList.Add(vector);
            }

            return modifiedList;
        }

        /// <summary>
        /// Adds an offset to the X and Y components of a Vector2.
        /// </summary>
        /// <param name="vector">The Vector2 to modify.</param>
        /// <param name="offsetX">The offset value to add to the X component.</param>
        /// <param name="offsetY">The offset value to add to the Y component.</param>
        /// <returns>The modified Vector2 with the added offset.</returns>
        public static Vector2 AddOffset(this Vector2 vector, float offsetX, float offsetY)
        {
            vector.X += offsetX;
            vector.Y += offsetY;
            return vector;
        }

        /// <summary>
        /// Adds an offset to the X and Y components of a Vector2.
        /// </summary>
        /// <param name="vector">The Vector2 to modify.</param>
        /// <param name="offset">The offset value to add to each vector.</param>
        /// <returns>The modified Vector2 with the added offset.</returns>
        public static Vector2 AddOffset(this Vector2 vector, float offset)
        {
            vector.X += offset;
            vector.Y += offset;
            return vector;
        }
    }
}