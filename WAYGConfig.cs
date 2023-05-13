using SharpDX;

namespace WhereAreYouGoing
{
    /// <summary>
    /// Represents the WAYG (Where Are You Going) configuration.
    /// </summary>
    public class WAYGConfig
    {
        /// <summary>
        /// Gets or sets a value indicating whether the WAYG feature is enabled.
        /// </summary>
        public bool Enable { get; set; } = true;

        /// <summary>
        /// Gets or sets the colors used for the WAYG feature.
        /// </summary>
        public WAYGColors Colors { get; set; } = new WAYGColors();

        /// <summary>
        /// Gets or sets the settings for the WAYG feature in the world.
        /// </summary>
        public WAYGWorld World { get; set; } = new WAYGWorld();

        /// <summary>
        /// Gets or sets the settings for the WAYG feature on the map.
        /// </summary>
        public WAYGMap Map { get; set; } = new WAYGMap();

        /// <summary>
        /// Represents the settings for the WAYG feature on the map.
        /// </summary>
        public class WAYGMap
        {
            /// <summary>
            /// Gets or sets a value indicating whether the WAYG feature on the map is enabled.
            /// </summary>
            public bool Enable { get; set; } = true;

            /// <summary>
            /// Gets or sets a value indicating whether to draw the attack on the map.
            /// </summary>
            public bool DrawAttack { get; set; } = true;

            /// <summary>
            /// Gets or sets a value indicating whether to draw the destination on the map.
            /// </summary>
            public bool DrawDestination { get; set; } = true;

            /// <summary>
            /// Gets or sets the line thickness for drawing on the map.
            /// </summary>
            public int LineThickness { get; set; } = 5;
        }

        /// <summary>
        /// Represents the settings for the WAYG feature in the world.
        /// </summary>
        public class WAYGWorld
        {
            /// <summary>
            /// Gets or sets a value indicating whether the WAYG feature in the world is enabled.
            /// </summary>
            public bool Enable { get; set; } = true;

            /// <summary>
            /// Gets or sets a value indicating whether to draw the attack in the world.
            /// </summary>
            public bool DrawAttack { get; set; } = true;

            /// <summary>
            /// Gets or sets a value indicating whether to draw the endpoint of the attack in the world.
            /// </summary>
            public bool DrawAttackEndPoint { get; set; } = true;

            /// <summary>
            /// Gets or sets a value indicating whether to draw the endpoint of the destination in the world.
            /// </summary>
            public bool DrawDestinationEndPoint { get; set; } = true;

            /// <summary>
            /// Gets or sets a value indicating whether to draw a line in the world.
            /// </summary>
            public bool DrawLine { get; set; } = true;

            /// <summary>
            /// Gets or sets a value indicating whether to always render a circle in the world.
            /// </summary>
            public bool AlwaysRenderCircle { get; set; } = true;

            /// <summary>
            /// Gets or sets the thickness of the rendered circle in the world.
            /// </summary>
            public int RenderCircleThickness { get; set; } = 5;

            /// <summary>
            /// Gets or sets the line thickness for drawing in the world.
            /// </summary        
            public int LineThickness { get; set; } = 5;
        }

        /// <summary>
        /// Represents the colors used for the WAYG feature.
        /// </summary>
        public class WAYGColors
        {
            /// <summary>
            /// Gets or sets the color for the map.
            /// </summary>
            public Color MapColor { get; set; } = Color.White;

            /// <summary>
            /// Gets or sets the color for the map attack.
            /// </summary>
            public Color MapAttackColor { get; set; } = Color.Red;

            /// <summary>
            /// Gets or sets the color for the world.
            /// </summary>
            public Color WorldColor { get; set; } = Color.White;

            /// <summary>
            /// Gets or sets the color for the world attack.
            /// </summary>
            public Color WorldAttackColor { get; set; } = Color.Red;
        }
    }
}