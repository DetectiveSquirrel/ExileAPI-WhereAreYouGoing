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
        private Map mapWindow => GameController.Game.IngameState.IngameUi.Map;
        private RectangleF MapRect => _mapRect?.Value ?? (_mapRect = new TimeCache<RectangleF>(() => mapWindow.GetClientRect(), 100)).Value;

        private Vector2 screenCenter =>
            new Vector2(MapRect.Width / 2, (MapRect.Height / 2) - 20) + new Vector2(MapRect.X, MapRect.Y) +
            new Vector2(mapWindow.LargeMapShiftX, mapWindow.LargeMapShiftY);

        private IngameUIElements ingameStateIngameUi;
        private Vector2 screentCenterCache;
        private bool largeMap;
        private float scale;
        private float k;

        private float diag =>
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

            if (ingameStateIngameUi.Map.SmallMiniMap.IsVisibleLocal)
            {
                var mapRect = ingameStateIngameUi.Map.SmallMiniMap.GetClientRectCache;
                screentCenterCache = new Vector2(mapRect.X + (mapRect.Width / 2), mapRect.Y + (mapRect.Height / 2));
                largeMap = false;
            }
            else if (ingameStateIngameUi.Map.LargeMap.IsVisibleLocal)
            {
                screentCenterCache = screenCenter;
                largeMap = true;
            }

            k = Camera.Width < 1024f ? 1120f : 1024f;
            scale = k / Camera.Height * Camera.Width * 3f / 4f / mapWindow.LargeMapZoom;
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

                switch (actionFlag)
                {
                    case (ActionFlags)512:
                    case ActionFlags.None:

                        if (drawSettings.World.AlwaysRenderCircle)
                            if (icon.Entity.DistancePlayer < Settings.MaxDrawDistance)
                                DrawPosNumCircleInWorld(icon.Entity.PosNum, component.BoundsNum.X, drawSettings.World.RenderCircleThickness, drawSettings.Colors.WorldColor);

                        break;

                    case ActionFlags.UsingAbility:
                        if (drawSettings.World.DrawAttack)
                        {
                            var entityGridPosNum = GetWorldScreenPosition(icon.Entity.GridPosNum);
                            var castDestination = actorComp.CurrentAction.Destination;
                            var entityGridCastPosNum = GetWorldScreenPosition(castDestination);
                            var castPosConvert = new Vector2();

                            if (drawSettings.Map.Enable)
                            {
                                if (drawSettings.Map.DrawAttack)
                                {
                                    Vector2 position;
                                    var entityTerrainHeight = ExpandWithTerrainHeight(icon.Entity.GridPosNum);
                                    var entityCastTerrainHeight = ExpandWithTerrainHeight(actorComp.CurrentAction.Destination);

                                    if (largeMap)
                                    {
                                        position = screentCenterCache + Helper.DeltaInWorldToMinimapDelta(icon.GridPositionNum() - playerPos, diag, scale, (entityTerrainHeight.Z - posZ) / (9f / mapWindowLargeMapZoom));

                                        castPosConvert = screentCenterCache + Helper.DeltaInWorldToMinimapDelta(new Vector2(castDestination.X, castDestination.Y) - playerPos, diag, scale, (entityCastTerrainHeight.Z - posZ) / (9f / mapWindowLargeMapZoom));
                                    }
                                    else
                                    {
                                        position = screentCenterCache + Helper.DeltaInWorldToMinimapDelta(icon.GridPositionNum() - playerPos, diag, 240f, (entityTerrainHeight.Z - posZ) / 20);
                                        castPosConvert = screentCenterCache + Helper.DeltaInWorldToMinimapDelta(new Vector2(castDestination.X, castDestination.Y) - playerPos, diag, 240f, (entityCastTerrainHeight.Z - posZ) / 20);
                                    }

                                    // map
                                    Graphics.DrawLine(position, castPosConvert, drawSettings.Map.LineThickness, drawSettings.Colors.MapAttackColor);
                                }
                            }

                            if (drawSettings.World.Enable)
                            {

                                if (icon.Entity.DistancePlayer < Settings.MaxDrawDistance)
                                    DrawPosNumCircleInWorld(icon.Entity.PosNum, component.BoundsNum.X, drawSettings.World.RenderCircleThickness, drawSettings.Colors.WorldAttackColor);

                                if (drawSettings.World.DrawLine)
                                {
                                    Graphics.DrawLine(entityGridPosNum, entityGridCastPosNum, drawSettings.World.LineThickness, drawSettings.Colors.WorldAttackColor);
                                }

                                if (drawSettings.World.DrawAttackEndPoint)
                                {
                                    var worldPosFromGrid = new Vector3(castDestination.GridToWorld().X, castDestination.GridToWorld().Y, 0);
                                    if (icon.Entity.DistancePlayer < Settings.MaxDrawDistance)
                                        DrawPosNumCircleInWorld(new Vector3(worldPosFromGrid.Xy(), GameController.IngameState.Data.GetTerrainHeightAt(worldPosFromGrid.WorldToGrid())), component.BoundsNum.X / 3, drawSettings.World.LineThickness, drawSettings.Colors.WorldAttackColor);
                                }
                            }
                        }
                        else
                        {
                            if (drawSettings.World.AlwaysRenderCircle)
                                if (icon.Entity.DistancePlayer < Settings.MaxDrawDistance)
                                    DrawPosNumCircleInWorld(icon.Entity.PosNum, component.BoundsNum.X, drawSettings.World.RenderCircleThickness, drawSettings.Colors.WorldColor);
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
                                    var expandedHeight = ExpandWithTerrainHeight(pathNode);
                                    var terrainHeight = expandedHeight.Z;
                                    mapPathNodes.Add(screentCenterCache + Helper.DeltaInWorldToMinimapDelta(new Vector2(pathNode.X, pathNode.Y) - playerPos, diag, largeMap ? scale : 240f, (terrainHeight - posZ) / (largeMap ? (9f / mapWindowLargeMapZoom) : 20)));
                                }
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
                            if (drawSettings.World.DrawLine && pathComp.PathingNodes.Any())
                            {
                                var pathingNodesToWorld = GetWorldScreenPositions(pathComp.PathingNodes.ConvertToVector2List());

                                var previousPoint = pathingNodesToWorld.First();
                                foreach (var currentPoint in pathingNodesToWorld.Skip(1))
                                {
                                    Graphics.DrawLine(previousPoint, currentPoint, drawSettings.World.LineThickness, drawSettings.Colors.WorldColor);
                                    previousPoint = currentPoint;
                                }
                            }

                            if (drawSettings.World.DrawDestinationEndPoint && pathComp.PathingNodes.Any())
                            {
                                var pathingNodesToWorld = GetWorldScreenPositions(pathComp.PathingNodes.ConvertToVector2List());
                                var worldPosFromGrid = new Vector3(pathingNodesToWorld.Last().X, pathingNodesToWorld.Last().Y, 0);
                                //DrawPosNumCircleInWorld(new Vector3(worldPosFromGrid.Xy(), GameController.IngameState.Data.GetTerrainHeightAt(worldPosFromGrid.WorldToGrid())), component.BoundsNum.X / 3, drawSettings.World.RenderCircleThickness, drawSettings.Colors.WorldColor);
                            }

                            if (drawSettings.World.AlwaysRenderCircle)
                                DrawPosNumCircleInWorld(icon.Entity.PosNum, component.BoundsNum.X, drawSettings.World.RenderCircleThickness, drawSettings.Colors.WorldColor);
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

        private Vector2 GetWorldScreenPosition(Vector2 gridPos)
        {
            return Camera.WorldToScreen(ExpandWithTerrainHeight(gridPos));
        }

        private List<Vector2> GetWorldScreenPositions(List<Vector2> gridPositions)
        {
            return gridPositions.Select(gridPos => Camera.WorldToScreen(ExpandWithTerrainHeight(gridPos))).ToList();
        }

        private Vector3 ExpandWithTerrainHeight(Vector2 gridPosition)
        {
            return new Vector3(gridPosition.GridToWorld(), GameController.IngameState.Data.GetTerrainHeightAt(gridPosition));
        }

        private void DrawPosNumCircleInWorld(Vector3 position, float radius, int thickness, Color color)
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