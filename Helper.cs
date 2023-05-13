using SharpDX;
using System;
using System.Collections.Generic;
using Vector2 = System.Numerics.Vector2;

namespace WhereAreYouGoing
{
    public class Helper
    {
        /// <summary>
        /// Converts the delta in world coordinates to the corresponding delta in minimap coordinates.
        /// </summary>
        /// <param name="delta">The delta in world coordinates to convert.</param>
        /// <param name="diag">The diagonal distance in world units.</param>
        /// <param name="scale">The scale factor for converting world units to minimap units.</param>
        /// <param name="deltaZ">The delta in the Z-axis (vertical) coordinate.</param>
        /// <returns>The delta in minimap coordinates.</returns>
        public static Vector2 DeltaInWorldToMinimapDelta(Vector2 delta, double diag, float scale, float deltaZ = 0)
        {
            const float CAMERA_ANGLE = 38 * MathUtil.Pi / 180;

            // Values according to 40 degree rotation of cartesian coordiantes, still doesn't seem right but closer
            var cos = (float)(diag * Math.Cos(CAMERA_ANGLE) / scale);
            var sin = (float)(diag * Math.Sin(CAMERA_ANGLE) / scale); // possible to use cos so angle = nearly 45 degrees

            // 2D rotation formulas not correct, but it's what appears to work?
            return new Vector2((delta.X - delta.Y) * cos, deltaZ - ((delta.X + delta.Y) * sin));
        }
    }
}