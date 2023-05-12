using GameOffsets.Native;
using System.Collections.Generic;
using System.Linq;
using Vector2 = System.Numerics.Vector2;

namespace WhereAreYouGoing
{
    public static class Vector2Extensions
    {
        public static List<Vector2> ConvertToVector2List(this IList<Vector2i> vector2iList)
        {
            return vector2iList.Select(v => new Vector2(v.X, v.Y)).ToList();
        }
    }
}