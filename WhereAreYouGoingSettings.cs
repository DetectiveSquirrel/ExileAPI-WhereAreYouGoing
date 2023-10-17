using ExileCore.Shared.Interfaces;
using ExileCore.Shared.Nodes;
using SharpDX;

namespace WhereAreYouGoing
{
    public class WhereAreYouGoingSettings : ISettings
    {
        //Mandatory setting to allow enabling/disabling your plugin
        public ToggleNode Enable { get; set; } = new ToggleNode(false);

        public ToggleNode MultiThreading { get; set; } = new ToggleNode(false);
        public RangeNode<int> MaxCircleDrawDistance { get; set; } = new RangeNode<int>(120, 0, 200);
        public WAYGConfig NormalMonster { get; set; } = new WAYGConfig()
        {
            Enable = true,
            Colors = new WAYGConfig.WAYGColors
            {
                MapColor = new Color(255, 255, 255, 94),
                MapAttackColor = new Color(255, 0, 0, 255),
                WorldColor = new Color(255, 255, 255, 94),
                WorldAttackColor = new Color(255, 0, 0, 255),
            },
            World = new WAYGConfig.WAYGWorld
            {
                Enable = true,
                DrawAttack = false,
                DrawAttackEndPoint = true,
                DrawDestinationEndPoint = false,
                DrawLine = false,
                AlwaysRenderWorldUnit = true,
                DrawFilledCircle = false,
                DrawBoundingBox = false,
                RenderCircleThickness = 3,
                LineThickness = 5
            },
            Map = new WAYGConfig.WAYGMap
            {
                Enable = false,
                DrawAttack = true,
                DrawDestination = true,
                LineThickness = 1
            }
        };
        public WAYGConfig MagicMonster { get; set; } = new WAYGConfig()
        {
            Enable = true,
            Colors = new WAYGConfig.WAYGColors
            {
                MapColor = new Color(43, 120, 255, 176),
                MapAttackColor = new Color(255, 0, 0, 144),
                WorldColor = new Color(43, 120, 255, 176),
                WorldAttackColor = new Color(255, 0, 0, 144),
            },
            World = new WAYGConfig.WAYGWorld
            {
                Enable = true,
                DrawAttack = false,
                DrawAttackEndPoint = true,
                DrawDestinationEndPoint = false,
                DrawLine = false,
                AlwaysRenderWorldUnit = true,
                DrawFilledCircle = false,
                DrawBoundingBox = false,
                RenderCircleThickness = 3,
                LineThickness = 5
            },
            Map = new WAYGConfig.WAYGMap
            {
                Enable = false,
                DrawAttack = true,
                DrawDestination = true,
                LineThickness = 1
            }
        };
        public WAYGConfig RareMonster { get; set; } = new WAYGConfig()
        {
            Enable = true,
            Colors = new WAYGConfig.WAYGColors
            {
                MapColor = new Color(225, 210, 19, 255),
                MapAttackColor = new Color(255, 0, 0, 140),
                WorldColor = new Color(225, 210, 19, 255),
                WorldAttackColor = new Color(255, 0, 0, 140),
            },
            World = new WAYGConfig.WAYGWorld
            {
                Enable = true,
                DrawAttack = true,
                DrawAttackEndPoint = true,
                DrawDestinationEndPoint = true,
                DrawLine = true,
                AlwaysRenderWorldUnit = true,
                DrawFilledCircle = true,
                DrawBoundingBox = false,
                RenderCircleThickness = 5,
                LineThickness = 5
            },
            Map = new WAYGConfig.WAYGMap
            {
                Enable = false,
                DrawAttack = true,
                DrawDestination = true,
                LineThickness = 5
            }
        };
        public WAYGConfig UniqueMonster { get; set; } = new WAYGConfig()
        {
            Enable = true,
            Colors = new WAYGConfig.WAYGColors
            {
                MapColor = new Color(226, 122, 33, 255),
                MapAttackColor = new Color(255, 0, 0, 255),
                WorldColor = new Color(226, 122, 33, 255),
                WorldAttackColor = new Color(255, 0, 0, 255),
            },
            World = new WAYGConfig.WAYGWorld
            {
                Enable = true,
                DrawAttack = true,
                DrawAttackEndPoint = true,
                DrawDestinationEndPoint = true,
                DrawLine = true,
                AlwaysRenderWorldUnit = true,
                DrawFilledCircle = true,
                DrawBoundingBox = false,
                RenderCircleThickness = 5,
                LineThickness = 5
            },
            Map = new WAYGConfig.WAYGMap
            {
                Enable = true,
                DrawAttack = true,
                DrawDestination = true,
                LineThickness = 3
            }
        };
        public WAYGConfig Self { get; set; } = new WAYGConfig()
        {
            Enable = true,
            Colors = new WAYGConfig.WAYGColors
            {
                MapColor = new Color(35, 194, 47, 193),
                MapAttackColor = new Color(255, 0, 0, 255),
                WorldColor = new Color(35, 194, 47, 193),
                WorldAttackColor = new Color(255, 0, 0, 255),
            },
            World = new WAYGConfig.WAYGWorld
            {
                Enable = true,
                DrawAttack = true,
                DrawAttackEndPoint = true,
                DrawDestinationEndPoint = true,
                DrawLine = true,
                AlwaysRenderWorldUnit = true,
                DrawBoundingBox = false,
                RenderCircleThickness = 3,
                LineThickness = 6
            },
            Map = new WAYGConfig.WAYGMap
            {
                Enable = true,
                DrawAttack = true,
                DrawDestination = true,
                LineThickness = 5
            }
        };
        public WAYGConfig Players { get; set; } = new WAYGConfig()
        {
            Enable = true,
            Colors = new WAYGConfig.WAYGColors
            {
                MapColor = new Color(35, 194, 47, 193),
                MapAttackColor = new Color(255, 0, 0, 255),
                WorldColor = new Color(35, 194, 47, 193),
                WorldAttackColor = new Color(255, 0, 0, 255),
            },
            World = new WAYGConfig.WAYGWorld
            {
                Enable = true,
                DrawAttack = true,
                DrawAttackEndPoint = true,
                DrawDestinationEndPoint = true,
                DrawLine = true,
                AlwaysRenderWorldUnit = true,
                DrawBoundingBox = false,
                RenderCircleThickness = 3,
                LineThickness = 6
            },
            Map = new WAYGConfig.WAYGMap
            {
                Enable = true,
                DrawAttack = true,
                DrawDestination = true,
                LineThickness = 5
            }
        };
        public WAYGConfig Minions { get; set; } = new WAYGConfig()
        {
            Enable = true,
            Colors = new WAYGConfig.WAYGColors
            {
                MapColor = new Color(218, 73, 255, 255),
                MapAttackColor = new Color(255, 73, 115, 121),
                WorldColor = new Color(218, 73, 255, 255),
                WorldAttackColor = new Color(255, 73, 115, 121),
            },
            World = new WAYGConfig.WAYGWorld
            {
                Enable = true,
                DrawAttack = true,
                DrawAttackEndPoint = true,
                DrawDestinationEndPoint = true,
                DrawLine = true,
                AlwaysRenderWorldUnit = true,
                DrawBoundingBox = false,
                RenderCircleThickness = 5,
                LineThickness = 5
            },
            Map = new WAYGConfig.WAYGMap
            {
                Enable = false,
                DrawAttack = true,
                DrawDestination = true,
                LineThickness = 5
            }
        };
    }
}