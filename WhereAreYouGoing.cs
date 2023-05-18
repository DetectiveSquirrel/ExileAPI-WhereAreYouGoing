using ExileCore;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared.Abstract;
using ExileCore.Shared.Cache;
using ExileCore.Shared.Enums;
using ExileCore.Shared.Helpers;
using ImGuiNET;
using JM.LinqFaster;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using Map = ExileCore.PoEMemory.Elements.Map;
using Vector2 = System.Numerics.Vector2;
using Vector3 = System.Numerics.Vector3;

namespace WhereAreYouGoing
{
    public class WhereAreYouGoing : BaseSettingsPlugin<WhereAreYouGoingSettings>
    {
        private CachedValue<RectangleF> _mapRect;
        private CachedValue<float> _diag;
        private Camera Camera => GameController.Game.IngameState.Camera;
        private Map MapWindow => GameController.Game.IngameState.IngameUi.Map;
        private RectangleF CurrentMapRect => _mapRect?.Value ?? (_mapRect = new TimeCache<RectangleF>(() => MapWindow.GetClientRect(), 100)).Value;

        private Vector2 ScreenCenter =>
            new Vector2(CurrentMapRect.Width / 2, (CurrentMapRect.Height / 2) - 20) + new Vector2(CurrentMapRect.X, CurrentMapRect.Y) +
            new Vector2(MapWindow.LargeMapShiftX, MapWindow.LargeMapShiftY);

        private IngameUIElements ingameStateIngameUi;
        private Vector2 ScreenCenterCache;
        private bool largeMap;
        private float scale;
        private float k;

        private float Diagonal =>
            _diag?.Value ?? (_diag = new TimeCache<float>(() =>
            {
                if (ingameStateIngameUi.Map.SmallMiniMap.IsVisibleLocal)
                {
                    var mapRect = ingameStateIngameUi.Map.SmallMiniMap.GetClientRect();
                    return (float)(Math.Sqrt((mapRect.Width * mapRect.Width) + (mapRect.Height * mapRect.Height)) / 2f);
                }

                return (float)Math.Sqrt((Camera.Width * Camera.Width) + (Camera.Height * Camera.Height));
            }, 100)).Value;

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

            //Settings.Self = SettingMenu(Settings.Players, "Self");
            Settings.Players = SettingMenu(Settings.Players, "Players");
            Settings.Minions = SettingMenu(Settings.Minions, "All Minion");
            Settings.NormalMonster = SettingMenu(Settings.NormalMonster, "Normal Monster");
            Settings.MagicMonster = SettingMenu(Settings.MagicMonster, "Magic Monster");
            Settings.RareMonster = SettingMenu(Settings.RareMonster, "Rare Monster");
            Settings.UniqueMonster = SettingMenu(Settings.UniqueMonster, "Unique Monster");
        }

        public override Job Tick()
        {
            ingameStateIngameUi = GameController.Game.IngameState.IngameUi;
            k = Camera.Width < 1024f ? 1120f : 1024f;

            if (ingameStateIngameUi.Map.SmallMiniMap.IsVisibleLocal)
            {
                var mapRect = ingameStateIngameUi.Map.SmallMiniMap.GetClientRectCache;
                ScreenCenterCache = new Vector2(mapRect.X + (mapRect.Width / 2), mapRect.Y + (mapRect.Height / 2));
                largeMap = false;
            }
            else if (ingameStateIngameUi.Map.LargeMap.IsVisibleLocal)
            {
                ScreenCenterCache = ScreenCenter;
                largeMap = true;
            }

            scale = k / Camera.Height * Camera.Width * 3f / 4f / MapWindow.LargeMapZoom;
            return null;
        }

        public override void Render()
        {
            //Any Imgui or Graphics calls go here. This is called after Tick
            if (!Settings.Enable.Value || !GameController.InGame) return;

            var playerPositioned = GameController?.Player?.GetComponent<Positioned>();
            if (playerPositioned == null) return;
            var playerPos = playerPositioned.GridPosNum;

            var playerRender = GameController?.Player?.GetComponent<Render>();
            if (playerRender == null) return;
            var posZ = GameController.Player.PosNum.Z;

            if (MapWindow == null) return;
            var mapWindowLargeMapZoom = MapWindow.LargeMapZoom;

            var baseIcons = GameController?.EntityListWrapper?.OnlyValidEntities
                .SelectWhereF(x => x.GetHudComponent<BaseIcon>(), icon => icon != null).OrderByF(x => x.Priority)
                .ToList();

            if (baseIcons == null) return;

            foreach (var icon in baseIcons)
            {
                if (icon == null) continue;
                if (icon.Entity == null) continue;

                var drawSettings = new WAYGConfig();

                switch (icon.Entity.Type)
                {
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

                    case EntityType.Player:
                        drawSettings = Settings.Players;
                        break;
                }

                if (!drawSettings.Enable) continue;

                var component = icon?.Entity?.GetComponent<Render>();
                if (component == null) continue;

                var pathComp = icon.Entity?.GetComponent<Pathfinding>();
                if (pathComp == null) continue;

                var actorComp = icon.Entity?.GetComponent<Actor>();
                if (actorComp == null) continue;

                var actionFlag = actorComp.Action;

                var shouldDrawCircle = icon.Entity.DistancePlayer < Settings.MaxCircleDrawDistance;

                switch (actionFlag)
                {
                    case (ActionFlags)512:
                    case ActionFlags.None:
                        if (drawSettings.World.AlwaysRenderCircle)
                        {
                            if (shouldDrawCircle)
                                DrawCircleInWorldPosition(icon.Entity.PosNum, component.BoundsNum.X, drawSettings.World.RenderCircleThickness, drawSettings.Colors.WorldColor);
                        }

                        break;

                    case ActionFlags.UsingAbility:
                        var castGridDestination = actorComp.CurrentAction.Destination;

                        if (drawSettings.Map.Enable && drawSettings.Map.DrawAttack)
                        {
                            var entityTerrainHeight = QueryGridPositionToWorldWithTerrainHeight(icon.Entity.GridPosNum);
                            var entityCastTerrainHeight = QueryGridPositionToWorldWithTerrainHeight(castGridDestination);

                            Vector2 position;
                            var castPosConvert = new Vector2();
                            if (largeMap)
                            {
                                position = ScreenCenterCache + Helper.DeltaInWorldToMinimapDelta(icon.GridPositionNum() - playerPos, Diagonal, scale, (entityTerrainHeight.Z - posZ) / (9f / mapWindowLargeMapZoom));
                                castPosConvert = ScreenCenterCache + Helper.DeltaInWorldToMinimapDelta(new Vector2(castGridDestination.X, castGridDestination.Y) - playerPos, Diagonal, scale, (entityCastTerrainHeight.Z - posZ) / (9f / mapWindowLargeMapZoom));
                            }
                            else
                            {
                                position = ScreenCenterCache + Helper.DeltaInWorldToMinimapDelta(icon.GridPositionNum() - playerPos, Diagonal, 240f, (entityTerrainHeight.Z - posZ) / 20);
                                castPosConvert = ScreenCenterCache + Helper.DeltaInWorldToMinimapDelta(new Vector2(castGridDestination.X, castGridDestination.Y) - playerPos, Diagonal, 240f, (entityCastTerrainHeight.Z - posZ) / 20);
                            }

                            Graphics.DrawLine(position, castPosConvert, drawSettings.Map.LineThickness, drawSettings.Colors.MapAttackColor);
                        }

                        if (drawSettings.World.Enable && drawSettings.World.DrawAttack)
                        {
                            if (shouldDrawCircle)
                                DrawCircleInWorldPosition(icon.Entity.PosNum, component.BoundsNum.X, drawSettings.World.RenderCircleThickness, drawSettings.Colors.WorldAttackColor);

                            if (drawSettings.World.DrawLine)
                            {
                                var entityScreenCastPosition = QueryWorldScreenPositionWithTerrainHeight(castGridDestination.ToVector2Num());
                                var entityWorldPosition = QueryWorldScreenPositionWithTerrainHeight(icon.Entity.GridPosNum);
                                Graphics.DrawLine(entityWorldPosition, entityScreenCastPosition, drawSettings.World.LineThickness, drawSettings.Colors.WorldAttackColor);
                            }

                            if (drawSettings.World.DrawAttackEndPoint && shouldDrawCircle)
                            {
                                var worldPosFromGrid = new Vector3(castGridDestination.GridToWorld().X, castGridDestination.GridToWorld().Y, 0);
                                DrawCircleInWorldPosition(new Vector3(worldPosFromGrid.Xy(), GameController.IngameState.Data.GetTerrainHeightAt(worldPosFromGrid.WorldToGrid())), component.BoundsNum.X / 3, drawSettings.World.LineThickness, drawSettings.Colors.WorldAttackColor);
                            }
                        }
                        else
                        {
                            if (drawSettings.World.AlwaysRenderCircle && shouldDrawCircle)
                                DrawCircleInWorldPosition(icon.Entity.PosNum, component.BoundsNum.X, drawSettings.World.RenderCircleThickness, drawSettings.Colors.WorldColor);
                        }
                        break;

                    case ActionFlags.AbilityCooldownActive:
                        break;

                    case ActionFlags.UsingAbilityAbilityCooldown:
                        break;

                    case ActionFlags.Dead:
                        break;

                    case ActionFlags.Moving:
                        if (drawSettings.Map.Enable)
                        {
                            var mapPathNodes = new List<Vector2>();

                            if (drawSettings.Map.DrawDestination && pathComp.PathingNodes.Any())
                            {
                                foreach (var pathNode in pathComp.PathingNodes)
                                {
                                    var queriedHeight = QueryGridPositionToWorldWithTerrainHeight(pathNode);
                                    mapPathNodes.Add(ScreenCenterCache + Helper.DeltaInWorldToMinimapDelta(new Vector2(pathNode.X, pathNode.Y) - playerPos, Diagonal, largeMap ? scale : 240f, (queriedHeight.Z - posZ) / (largeMap ? (9f / mapWindowLargeMapZoom) : 20)));
                                }
                            }

                            if (mapPathNodes.AddOffset(drawSettings.Map.LineThickness).Count > 0)
                            {
                                for (var i = 0; i < mapPathNodes.Count - 1; i++)
                                {
                                    Graphics.DrawLine(mapPathNodes[i], mapPathNodes[i + 1], drawSettings.Map.LineThickness, drawSettings.Colors.MapColor);
                                }
                            }
                        }

                        if (drawSettings.World.Enable)
                        {
                            var pathingNodes = pathComp.PathingNodes.ConvertToVector2List();

                            if (drawSettings.World.DrawLine && pathingNodes.Any())
                            {
                                var pathingNodesToWorld = QueryWorldScreenPositionsWithTerrainHeight(pathingNodes);

                                var previousPoint = pathingNodesToWorld.First();
                                foreach (var currentPoint in pathingNodesToWorld.Skip(1))
                                {
                                    Graphics.DrawLine(previousPoint, currentPoint, drawSettings.World.LineThickness, drawSettings.Colors.WorldColor);
                                    previousPoint = currentPoint;
                                }
                            }

                            if (drawSettings.World.DrawDestinationEndPoint && pathingNodes.Any() && shouldDrawCircle)
                            {
                                var pathingNodesToWorld = QueryWorldScreenPositionsWithTerrainHeight(pathingNodes);
                                var queriedWorldPos = new Vector3(pathingNodes.Last().GridToWorld().X, pathingNodes.Last().GridToWorld().Y, GameController.IngameState.Data.GetTerrainHeightAt(pathingNodes.Last().WorldToGrid()));
                                DrawCircleInWorldPosition(queriedWorldPos, component.BoundsNum.X / 3, drawSettings.World.RenderCircleThickness, drawSettings.Colors.WorldColor);
                            }

                            if (drawSettings.World.AlwaysRenderCircle && shouldDrawCircle)
                                DrawCircleInWorldPosition(icon.Entity.PosNum, component.BoundsNum.X, drawSettings.World.RenderCircleThickness, drawSettings.Colors.WorldColor);
                        }
                        break;

                    case ActionFlags.WashedUpState:
                        // Handle WashedUpState
                        break;

                    case ActionFlags.HasMines:
                        // Handle HasMines
                        break;
                }
            }
        }

        /// <summary>
        /// Queries the world screen position with terrain height for the given grid position.
        /// </summary>
        /// <param name="gridPosition">The grid position to query.</param>
        /// <returns>The world screen position with terrain height.</returns>
        private Vector2 QueryWorldScreenPositionWithTerrainHeight(Vector2 gridPosition)
        {
            // Query the world screen position with terrain height for the given grid position
            return Camera.WorldToScreen(QueryGridPositionToWorldWithTerrainHeight(gridPosition));
        }

        /// <summary>
        /// Queries the world screen positions with terrain height for the given grid positions.
        /// </summary>
        /// <param name="gridPositions">The grid positions to query.</param>
        /// <returns>The world screen positions with terrain height.</returns>
        private List<Vector2> QueryWorldScreenPositionsWithTerrainHeight(List<Vector2> gridPositions)
        {
            // Query the world screen positions with terrain height for the given grid positions
            return gridPositions.Select(gridPos => Camera.WorldToScreen(QueryGridPositionToWorldWithTerrainHeight(gridPos))).ToList();
        }

        /// <summary>
        /// Queries the grid position and extracts the corresponding terrain height.
        /// </summary>
        /// <param name="gridPosition">The grid position to query.</param>
        /// <returns>The world position with the extracted terrain height.</returns>
        private Vector3 QueryGridPositionToWorldWithTerrainHeight(Vector2 gridPosition)
        {
            // Query the grid position and extract the corresponding world position with terrain height
            return new Vector3(gridPosition.GridToWorld(), (float)GameController.IngameState.Data.GetTerrainHeightAt(gridPosition));
        }

        /// <summary>
        /// Draws a circle at the specified world position with the given radius, thickness, and color.
        /// </summary>
        /// <param name="position">The world position to draw the circle at.</param>
        /// <param name="radius">The radius of the circle.</param>
        /// <param name="thickness">The thickness of the circle's outline.</param>
        /// <param name="color">The color of the circle.</param>
        private void DrawCircleInWorldPosition(Vector3 position, float radius, int thickness, Color color)
        {
            const int segments = 15;
            const float segmentAngle = 2f * MathF.PI / segments;

            for (var i = 0; i < segments; i++)
            {
                var angle = i * segmentAngle;
                var currentOffset = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius;
                var nextOffset = new Vector2(MathF.Cos(angle + segmentAngle), MathF.Sin(angle + segmentAngle)) * radius;

                var currentWorldPos = position + new Vector3(currentOffset, 0);
                var nextWorldPos = position + new Vector3(nextOffset, 0);

                Graphics.DrawLine(
                    Camera.WorldToScreen(currentWorldPos),
                    Camera.WorldToScreen(nextWorldPos),
                    thickness,
                    color
                );
            }
        }
    }
}