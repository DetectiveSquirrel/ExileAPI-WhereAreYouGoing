using System;
using System.Collections.Generic;
using System.Linq;
using ExileCore;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared.Abstract;
using ExileCore.Shared.Cache;
using ExileCore.Shared.Enums;
using GameOffsets.Native;
using ImGuiNET;
using JM.LinqFaster;
using SharpDX;
using Map = ExileCore.PoEMemory.Elements.Map;

namespace WhereAreYouGoing
{
    public class WhereAreYouGoing : BaseSettingsPlugin<WhereAreYouGoingSettings>
    {
        private const string ALERT_CONFIG = "config\\new_mod_alerts.txt";
        private readonly Dictionary<string, Size2> modIcons = new Dictionary<string, Size2>();
        private CachedValue<float> _diag;
        private CachedValue<RectangleF> _mapRect;

        private List<string> ignoreEntites = new List<string>
        {
            "Metadata/Monsters/Frog/FrogGod/SilverPool",
            "Metadata/MiscellaneousObjects/WorldItem",
            "Metadata/Pet/Weta/Basic",
            "Metadata/Monsters/Daemon/SilverPoolChillDaemon",
            "Metadata/Monsters/Daemon",
            "Metadata/Monsters/Frog/FrogGod/SilverOrbFromMonsters"
        };

        private IngameUIElements ingameStateIngameUi;
        private float k;
        private bool largeMap;
        private float scale;
        private Vector2 screentCenterCache;
        private RectangleF MapRect => _mapRect?.Value ?? (_mapRect = new TimeCache<RectangleF>(() => mapWindow.GetClientRect(), 100)).Value;
        private Map mapWindow => GameController.Game.IngameState.IngameUi.Map;
        private Camera camera => GameController.Game.IngameState.Camera;
        private float diag =>
            _diag?.Value ?? (_diag = new TimeCache<float>(() =>
            {
                if (ingameStateIngameUi.Map.SmallMiniMap.IsVisibleLocal)
                {
                    var mapRect = ingameStateIngameUi.Map.SmallMiniMap.GetClientRect();
                    return (float) (Math.Sqrt(mapRect.Width * mapRect.Width + mapRect.Height * mapRect.Height) / 2f);
                }

                return (float) Math.Sqrt(camera.Width * camera.Width + camera.Height * camera.Height);
            }, 100)).Value;
        private Vector2 screenCenter =>
            new Vector2(MapRect.Width / 2, MapRect.Height / 2 - 20) + new Vector2(MapRect.X, MapRect.Y) +
            new Vector2(mapWindow.LargeMapShiftX, mapWindow.LargeMapShiftY);

        public override void OnLoad()
        {
            CanUseMultiThreading = true;
        }

        public override bool Initialise()
        {
            return true;
        }

        public WAYGConfig SettingMenu(WAYGConfig setting, string prefix)
        {
            var settings = setting;
            if (ImGui.CollapsingHeader($@"{prefix} Entities##{prefix}", ImGuiTreeNodeFlags.Framed | ImGuiTreeNodeFlags.DefaultOpen))
            {
                // Start Indent
                ImGui.Indent();

                settings.Enable = ImGuiExtension.Checkbox($@"{prefix}(s) Enabled", settings.Enable);
                if (ImGui.TreeNode($@"Colors##{prefix}"))
                {

                    settings.Colors.MapColor = ImGuiExtension.ColorPicker("Map Color", settings.Colors.MapColor);
                    settings.Colors.MapAttackColor = ImGuiExtension.ColorPicker("Map Attack Color", settings.Colors.MapAttackColor);
                    settings.Colors.WorldColor = ImGuiExtension.ColorPicker("World Color", settings.Colors.WorldColor);
                    settings.Colors.WorldAttackColor = ImGuiExtension.ColorPicker("World Attack Color", settings.Colors.WorldAttackColor);
                    ImGui.Spacing();
                    ImGui.TreePop();
                }

                if (ImGui.TreeNode($@"Map##{prefix}"))
                {
                    settings.Map.Enable = ImGuiExtension.Checkbox("Map Drawing Enabled", settings.Map.Enable);
                    settings.Map.DrawAttack = ImGuiExtension.Checkbox("Draw Attacks", settings.Map.DrawAttack);
                    settings.Map.DrawDestination = ImGuiExtension.Checkbox("Draw Destinations", settings.Map.DrawDestination);
                    settings.Map.LineThickness = ImGuiExtension.IntDrag("Line Thickness", settings.Map.LineThickness, 1, 100, 0.1f);
                    ImGui.Spacing();
                    ImGui.TreePop();
                }

                if (ImGui.TreeNode($@"World##{prefix}"))
                {
                    settings.World.Enable = ImGuiExtension.Checkbox("World Drawing Enabled", settings.World.Enable);
                    settings.World.DrawAttack = ImGuiExtension.Checkbox("World Attacks", settings.World.DrawAttack);
                    settings.World.DrawAttackEndPoint = ImGuiExtension.Checkbox("World Attack Endpoint", settings.World.DrawAttackEndPoint);
                    settings.World.DrawDestinationEndPoint = ImGuiExtension.Checkbox("World Destination Endpoint", settings.World.DrawDestinationEndPoint);
                    settings.World.DrawLine = ImGuiExtension.Checkbox("Draw Line", settings.World.DrawLine);
                    settings.World.AlwaysRenderCircle = ImGuiExtension.Checkbox("Always Render Entity Circle", settings.World.AlwaysRenderCircle);
                    settings.World.RenderCircleThickness = ImGuiExtension.IntDrag("Entity Circle Thickness", settings.World.RenderCircleThickness, 1, 100, 0.1f);
                    settings.World.LineThickness = ImGuiExtension.IntDrag("Line Thickness", settings.World.LineThickness, 1, 100, 0.1f);
                    ImGui.Spacing();
                    ImGui.TreePop();
                }

                // End Indent
                ImGui.Unindent();
            }

            // Reapply new settings
            return settings;
        }

        public override void DrawSettings()
        {
            base.DrawSettings();

            Settings.Players = SettingMenu(Settings.Players, "Player");
            Settings.Minions = SettingMenu(Settings.Minions, "All Minion");
            Settings.NormalMonster = SettingMenu(Settings.NormalMonster, "Normal Monster");
            Settings.MagicMonster = SettingMenu(Settings.MagicMonster, "Magic Monster");
            Settings.RareMonster = SettingMenu(Settings.RareMonster, "Rare Monster");
            Settings.UniqueMonster = SettingMenu(Settings.UniqueMonster, "Unique Monster");
        }

        public override Job Tick()
        {
            if (Settings.MultiThreading)
                return GameController.MultiThreadManager.AddJob(TickLogic, nameof(WhereAreYouGoing));

            TickLogic();
            return null;
        }

        private void TickLogic()
        {
            ingameStateIngameUi = GameController.Game.IngameState.IngameUi;

            if (ingameStateIngameUi.Map.SmallMiniMap.IsVisibleLocal)
            {
                var mapRect = ingameStateIngameUi.Map.SmallMiniMap.GetClientRectCache;
                screentCenterCache = new Vector2(mapRect.X + mapRect.Width / 2, mapRect.Y + mapRect.Height / 2);
                largeMap = false;
            }
            else if (ingameStateIngameUi.Map.LargeMap.IsVisibleLocal)
            {
                screentCenterCache = screenCenter;
                largeMap = true;
            }

            k = camera.Width < 1024f ? 1120f : 1024f;
            scale = k / camera.Height * camera.Width * 3f / 4f / mapWindow.LargeMapZoom;
        }

        public override void Render()
        {
            if (!Settings.Enable.Value || !GameController.InGame) return;

            if (ingameStateIngameUi.Atlas.IsVisibleLocal || ingameStateIngameUi.DelveWindow.IsVisibleLocal ||
                ingameStateIngameUi.TreePanel.IsVisibleLocal)
                return;

            Positioned playerPositioned = GameController?.Player?.GetComponent<Positioned>();
            if (playerPositioned == null) return;
            Vector2 playerPos = playerPositioned.GridPos;
            Render playerRender = GameController?.Player?.GetComponent<Render>();
            if (playerRender == null) return;
            float posZ = playerRender.Pos.Z;

            if (mapWindow == null) return;
            var mapWindowLargeMapZoom = mapWindow.LargeMapZoom;

            var baseIcons = GameController?.EntityListWrapper?.OnlyValidEntities
                .SelectWhereF(x => x.GetHudComponent<BaseIcon>(), icon => icon != null).OrderByF(x => x.Priority)
                .ToList();
            if (baseIcons == null) return;

            foreach (var icon in baseIcons)
            {
                if (icon == null) continue;
                if (icon.Entity == null) continue;
                if (icon.Entity.DistancePlayer > Settings.MaxDrawDistance) continue;

                var component = icon?.Entity?.GetComponent<Render>();
                if (component == null) continue;
                var iconZ = component.Pos.Z;

                WAYGConfig drawSettings = new WAYGConfig();

                switch (icon.Entity.Type)
                {
                    case EntityType.Error:
                        break;
                    case EntityType.None:
                        break;
                    case EntityType.ServerObject:
                        break;
                    case EntityType.Effect:
                        break;
                    case EntityType.Light:
                        break;
                    case EntityType.Monster:
                        switch (icon.Entity.IsHostile)
                        {
                            case true:
                                switch (icon.Rarity)
                                {
                                    case MonsterRarity.White:
                                        drawSettings = Settings.NormalMonster;
                                        break;
                                    case MonsterRarity.Magic:
                                        drawSettings = Settings.MagicMonster;
                                        break;
                                    case MonsterRarity.Rare:
                                        drawSettings = Settings.RareMonster;
                                        break;
                                    case MonsterRarity.Unique:
                                        drawSettings = Settings.UniqueMonster;
                                        break;
                                }
                                break;
                            case false:
                                drawSettings = Settings.Minions;
                                break;
                                
                        }
                        break;
                    case EntityType.Chest:
                        break;
                    case EntityType.SmallChest:
                        break;
                    case EntityType.Npc:
                        break;
                    case EntityType.Shrine:
                        break;
                    case EntityType.AreaTransition:
                        break;
                    case EntityType.Portal:
                        break;
                    case EntityType.QuestObject:
                        break;
                    case EntityType.Stash:
                        break;
                    case EntityType.Waypoint:
                        break;
                    case EntityType.Player:
                        drawSettings = Settings.Players;
                        break;
                    case EntityType.Pet:
                        break;
                    case EntityType.WorldItem:
                        break;
                    case EntityType.Resource:
                        break;
                    case EntityType.Breach:
                        break;
                    case EntityType.ControlObjects:
                        break;
                    case EntityType.HideoutDecoration:
                        break;
                    case EntityType.CraftUnlock:
                        break;
                    case EntityType.Daemon:
                        break;
                    case EntityType.TownPortal:
                        break;
                    case EntityType.Monolith:
                        break;
                    case EntityType.MiniMonolith:
                        break;
                    case EntityType.BetrayalChoice:
                        break;
                    case EntityType.IngameIcon:
                        break;
                    case EntityType.LegionMonolith:
                        break;
                    case EntityType.Item:
                        break;
                    case EntityType.Terrain:
                        break;
                    case EntityType.DelveCraftingBench:
                        break;
                    case EntityType.GuildStash:
                        break;
                    case EntityType.MiscellaneousObjects:
                        break;
                    case EntityType.Door:
                        break;
                    case EntityType.DoorSwitch:
                        break;
                }

                if (!drawSettings.Enable) continue;

                var pathComp = icon.Entity?.GetComponent<Pathfinding>();
                if (pathComp == null) continue;

                var actorComp = icon.Entity?.GetComponent<Actor>();
                if (actorComp == null) continue;

                var actionFlag = actorComp.Action;

                var mapPathNodes = new List<Vector2>();
                var pathingNodes = pathComp.PathingNodes;

                var gridToWorldToScreenList = new List<Vector2>();
                var gridToWorldList = new List<Vector2>();
                var pathingNodesWorld = pathComp.PathingNodes;

                switch (actionFlag)
                {
                    case (ActionFlags)512:
                    case ActionFlags.None:
                        if (drawSettings.World.AlwaysRenderCircle)
                            DrawEllipseToWorld(icon.Entity.Pos, (int)component.Bounds.X, 15, drawSettings.World.RenderCircleThickness, drawSettings.Colors.WorldColor);
                        break;
                    case ActionFlags.UsingAbility:
                        if (drawSettings.World.DrawAttack)
                        {
                            var castPosConvert = new Vector2();
                            var castDestination = actorComp.CurrentAction.Destination;


                            var castGridToWorld = WorldPositionExtensions.GridToWorld(new Vector2(actorComp.CurrentAction.Destination.X, actorComp.CurrentAction.Destination.Y));
                            var gridToWorld2 = WorldPositionExtensions.GridToWorld(icon.Entity.GridPos);
                            var worldToSreen = GameController.Game.IngameState.Camera.WorldToScreen(new Vector3(castGridToWorld.X, castGridToWorld.Y, iconZ + component.Bounds.Z));
                            var worldToSreen2 = GameController.Game.IngameState.Camera.WorldToScreen(new Vector3(gridToWorld2.X, gridToWorld2.Y, iconZ + component.Bounds.Z));

                            if (drawSettings.Map.Enable)
                            {
                                if (drawSettings.Map.DrawAttack)
                                {

                                    Vector2 position;

                                    if (largeMap)
                                    {
                                        position = screentCenterCache + Helper.DeltaInWorldToMinimapDelta(
                                                       icon.GridPosition() - playerPos, diag, scale, (iconZ - posZ) / (9f / mapWindowLargeMapZoom));
                                        castPosConvert = screentCenterCache +
                                                         Helper.DeltaInWorldToMinimapDelta(
                                                             new Vector2(castDestination.X, castDestination.Y) - playerPos,
                                                             diag, scale,
                                                             (iconZ - posZ) / (9f / mapWindowLargeMapZoom));
                                    }
                                    else
                                    {
                                        position = screentCenterCache +
                                                   Helper.DeltaInWorldToMinimapDelta(icon.GridPosition() - playerPos, diag, 240f, (iconZ - posZ) / 20);
                                        castPosConvert = screentCenterCache +
                                                         Helper.DeltaInWorldToMinimapDelta(
                                                             new Vector2(castDestination.X, castDestination.Y) - playerPos,
                                                             diag, 240f,
                                                             (iconZ - posZ) / 20);
                                    }

                                    // map
                                    Graphics.DrawLine(position, castPosConvert, drawSettings.Map.LineThickness, drawSettings.Colors.MapAttackColor);
                                }
                            }

                            // world
                            if (drawSettings.World.Enable)
                            {
                                DrawEllipseToWorld(icon.Entity.Pos, (int)component.Bounds.X, 15, drawSettings.World.RenderCircleThickness, drawSettings.Colors.WorldAttackColor);

                                if (drawSettings.World.DrawLine)
                                    Graphics.DrawLine(worldToSreen2, worldToSreen, drawSettings.World.LineThickness, drawSettings.Colors.WorldAttackColor);

                                DrawEllipseToWorld(new Vector3(castGridToWorld.X, castGridToWorld.Y, icon.Entity.Pos.Z), (int)component.Bounds.X / 3, 10, drawSettings.World.LineThickness, drawSettings.Colors.WorldAttackColor);
                            }
                        }
                        else
                        {

                            if (drawSettings.World.AlwaysRenderCircle)
                                DrawEllipseToWorld(icon.Entity.Pos, (int)component.Bounds.X, 15, drawSettings.World.RenderCircleThickness, drawSettings.Colors.WorldColor);
                        }
                        break;
                    case ActionFlags.AbilityCooldownActive:
                        break;
                    case ActionFlags.UsingAbilityAbilityCooldown:
                        break;
                    case ActionFlags.Dead:
                        break;
                    case ActionFlags.Moving:
                    {
                        if (drawSettings.Map.Enable)
                        {
                            if (drawSettings.Map.DrawDestination)
                                foreach (var pathNode in pathingNodes)
                                {
                                    if (largeMap)
                                        mapPathNodes.Add(screentCenterCache +
                                                         Helper.DeltaInWorldToMinimapDelta(
                                                             new Vector2(pathNode.X, pathNode.Y) - playerPos, diag, scale,
                                                             (iconZ - posZ) / (9f / mapWindowLargeMapZoom)));
                                    else
                                        mapPathNodes.Add(screentCenterCache +
                                                         Helper.DeltaInWorldToMinimapDelta(
                                                             new Vector2(pathNode.X, pathNode.Y) - playerPos, diag, 240f,
                                                             (iconZ - posZ) / 20));
                                }

                            if (mapPathNodes.Count > 0)
                            {
                                for (int i = 0; i < mapPathNodes.Count - 1; i++)
                                {
                                    Graphics.DrawLine(mapPathNodes[i], mapPathNodes[i + 1], drawSettings.Map.LineThickness, drawSettings.Colors.MapColor);
                                }
                            }
                        }

                        // world
                        if (drawSettings.World.Enable)
                        {
                            if (drawSettings.World.DrawLine)
                             {
                                 gridToWorldToScreenList = GridDestinationToWorldToScreen(pathingNodesWorld, icon.Entity);

                                 if (gridToWorldToScreenList.Count > 0)
                                 {
                                     for (int i = 0; i < gridToWorldToScreenList.Count - 1; i++)
                                     {
                                         Graphics.DrawLine(gridToWorldToScreenList[i], gridToWorldToScreenList[i + 1], drawSettings.World.LineThickness, drawSettings.Colors.WorldColor);
                                     }
                                 }
                             }

                            // entity pos circle
                            DrawEllipseToWorld(icon.Entity.Pos, (int) component.Bounds.X, 15, drawSettings.World.RenderCircleThickness, drawSettings.Colors.WorldColor);
                            // destination circle
                            if (drawSettings.World.DrawDestinationEndPoint)
                            {
                                gridToWorldList = GridDestinationToWorld(pathingNodesWorld, icon.Entity);
                                DrawEllipseToWorld(new Vector3(gridToWorldList.Last().X, gridToWorldList.Last().Y, icon.Entity.Pos.Z), (int)component.Bounds.X / 3, 10, drawSettings.World.RenderCircleThickness, drawSettings.Colors.WorldColor);
                            }
                        }

                        break;
                    }
                    case ActionFlags.WashedUpState:
                        break;
                    case ActionFlags.HasMines:
                        break;
                }
            }
        }

        public List<Vector2> GridDestinationToWorldToScreen(IList<Vector2i> points, Entity entity)
        {
            var convertedNodes = new List<Vector2>();
            var entPosZ = entity?.Pos.Z;

            if (entPosZ == null)
                return convertedNodes;

            foreach (var pathNode in points)
            {
                var gridToWorld = WorldPositionExtensions.GridToWorld(new Vector2(pathNode.X, pathNode.Y));
                var worldToScreen = camera.WorldToScreen(new Vector3(gridToWorld.X, gridToWorld.Y, (float)entPosZ));

                convertedNodes.Add(worldToScreen);
            }

            return convertedNodes;
        }

        public List<Vector2> GridDestinationToWorld(IList<Vector2i> points, Entity entity)
        {
            var convertedNodes = new List<Vector2>();
            var entPosZ = entity?.Pos.Z;

            if (entPosZ == null)
                return convertedNodes;

            foreach (var pathNode in points)
            {
                var gridToWorld = WorldPositionExtensions.GridToWorld(new Vector2(pathNode.X, pathNode.Y));

                convertedNodes.Add(gridToWorld);
            }

            return convertedNodes;
        }

        public void DrawEllipseToWorld(Vector3 vector3Pos, int radius, int points, int lineWidth, Color color)
        {
            var camera = GameController.Game.IngameState.Camera;

            var plottedCirclePoints = new List<Vector3>();
            var slice = 2 * Math.PI / points;
            for (var i = 0; i < points; i++)
            {
                var angle = slice * i;
                var x = (decimal)vector3Pos.X + decimal.Multiply((decimal)radius, (decimal)Math.Cos(angle));
                var y = (decimal)vector3Pos.Y + decimal.Multiply((decimal)radius, (decimal)Math.Sin(angle));
                plottedCirclePoints.Add(new Vector3((float)x, (float)y, vector3Pos.Z));
            }

            for (var i = 0; i < plottedCirclePoints.Count; i++)
            {
                if (i >= plottedCirclePoints.Count - 1)
                {
                    var pointEnd1 = camera.WorldToScreen(plottedCirclePoints.Last());
                    var pointEnd2 = camera.WorldToScreen(plottedCirclePoints[0]);
                    Graphics.DrawLine(pointEnd1, pointEnd2, lineWidth, color);
                    return;
                }

                var point1 = camera.WorldToScreen(plottedCirclePoints[i]);
                var point2 = camera.WorldToScreen(plottedCirclePoints[i + 1]);
                Graphics.DrawLine(point1, point2, lineWidth, color);
            }
        }
    }

    public class WAYGConfig
    {
        public bool Enable { get; set; } = true;
        public WAYGColors Colors { get; set; } = new WAYGColors();
        public WAYGWorld World { get; set; } = new WAYGWorld();
        public WAYGMap Map { get; set; } = new WAYGMap();
        public class WAYGMap
        {
            public bool Enable { get; set; } = true;
            public bool DrawAttack { get; set; } = true;
            public bool DrawDestination { get; set; } = true;
            public int LineThickness { get; set; } = 5;
        }
        public class WAYGWorld
        {
            public bool Enable { get; set; } = true;
            public bool DrawAttack { get; set; } = true;
            public bool DrawAttackEndPoint { get; set; } = true;
            public bool DrawDestinationEndPoint { get; set; } = true;
            public bool DrawLine { get; set; } = true;
            public bool AlwaysRenderCircle { get; set; } = true;
            public int RenderCircleThickness { get; set; } = 5;
            public int LineThickness { get; set; } = 5;
        }
        public class WAYGColors
        {
            public Color MapColor { get; set; } = Color.White;
            public Color MapAttackColor { get; set; } = Color.Red;
            public Color WorldColor { get; set; } = Color.White;
            public Color WorldAttackColor { get; set; } = Color.Red;
        }
    }
}
