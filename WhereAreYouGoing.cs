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
using static WhereAreYouGoing.WhereAreYouGoingSettings;
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
                    settings.World.AlwaysRenderWorldUnit = ImGuiExtension.Checkbox("Always Render Entity Circle", settings.World.AlwaysRenderWorldUnit);
                    settings.World.DrawFilledCircle = ImGuiExtension.Checkbox("Draw Filled Circle", settings.World.DrawFilledCircle);
                    settings.World.DrawBoundingBox = ImGuiExtension.Checkbox("Draw Bounding Box Instead of Circle Around Unit", settings.World.DrawBoundingBox);
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

            Settings.Players = SettingMenu(Settings.Players, "Players");
            Settings.Self = SettingMenu(Settings.Self, "Self");
            Settings.Minions = SettingMenu(Settings.Minions, "All Friendlys");
            Settings.NormalMonster = SettingMenu(Settings.NormalMonster, "Normal Monster");
            Settings.MagicMonster = SettingMenu(Settings.MagicMonster, "Magic Monster");
            Settings.RareMonster = SettingMenu(Settings.RareMonster, "Rare Monster");
            Settings.UniqueMonster = SettingMenu(Settings.UniqueMonster, "Unique Monster");
            Settings.TestingUnits = SettingMenu(Settings.TestingUnits, "Testing Units");
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

            var player = GameController?.Player;

            var playerPositioned = player?.GetComponent<Positioned>();
            if (playerPositioned == null) return;
            var playerPos = playerPositioned.GridPosNum;

            var playerRender = player?.GetComponent<Render>();
            if (playerRender == null) return;
            var posZ = GameController.Player.PosNum.Z;

            if (MapWindow == null) return;
            var mapWindowLargeMapZoom = MapWindow.LargeMapZoom;

            var entityLists = new List<IEnumerable<Entity>>
            {
                //GameController?.EntityListWrapper?.ValidEntitiesByType[EntityType.Error] ?? Enumerable.Empty<Entity>(),
                //GameController?.EntityListWrapper?.ValidEntitiesByType[EntityType.None] ?? Enumerable.Empty<Entity>(),
                //GameController?.EntityListWrapper?.ValidEntitiesByType[EntityType.ServerObject] ?? Enumerable.Empty<Entity>(),
                //GameController?.EntityListWrapper?.ValidEntitiesByType[EntityType.Effect] ?? Enumerable.Empty<Entity>(),
                //GameController?.EntityListWrapper?.ValidEntitiesByType[EntityType.Light] ?? Enumerable.Empty<Entity>(),
                GameController?.EntityListWrapper?.ValidEntitiesByType[EntityType.Monster] ?? Enumerable.Empty<Entity>(),
                //GameController?.EntityListWrapper?.ValidEntitiesByType[EntityType.Chest] ?? Enumerable.Empty<Entity>(),
                //GameController?.EntityListWrapper?.ValidEntitiesByType[EntityType.SmallChest] ?? Enumerable.Empty<Entity>(),
                //GameController?.EntityListWrapper?.ValidEntitiesByType[EntityType.Npc] ?? Enumerable.Empty<Entity>(),
                //GameController?.EntityListWrapper?.ValidEntitiesByType[EntityType.Shrine] ?? Enumerable.Empty<Entity>(),
                //GameController?.EntityListWrapper?.ValidEntitiesByType[EntityType.AreaTransition] ?? Enumerable.Empty<Entity>(),
                //GameController?.EntityListWrapper?.ValidEntitiesByType[EntityType.Portal] ?? Enumerable.Empty<Entity>(),
                //GameController?.EntityListWrapper?.ValidEntitiesByType[EntityType.QuestObject] ?? Enumerable.Empty<Entity>(),
                //GameController?.EntityListWrapper?.ValidEntitiesByType[EntityType.Stash] ?? Enumerable.Empty<Entity>(),
                //GameController?.EntityListWrapper?.ValidEntitiesByType[EntityType.Waypoint] ?? Enumerable.Empty<Entity>(),
                GameController?.EntityListWrapper?.ValidEntitiesByType[EntityType.Player] ?? Enumerable.Empty<Entity>(),
                //GameController?.EntityListWrapper?.ValidEntitiesByType[EntityType.Pet] ?? Enumerable.Empty<Entity>(),
                //GameController?.EntityListWrapper?.ValidEntitiesByType[EntityType.WorldItem] ?? Enumerable.Empty<Entity>(),
                //GameController?.EntityListWrapper?.ValidEntitiesByType[EntityType.Resource] ?? Enumerable.Empty<Entity>(),
                //GameController?.EntityListWrapper?.ValidEntitiesByType[EntityType.Breach] ?? Enumerable.Empty<Entity>(),
                //GameController?.EntityListWrapper?.ValidEntitiesByType[EntityType.ControlObjects] ?? Enumerable.Empty<Entity>(),
                //GameController?.EntityListWrapper?.ValidEntitiesByType[EntityType.HideoutDecoration] ?? Enumerable.Empty<Entity>(),
                //GameController?.EntityListWrapper?.ValidEntitiesByType[EntityType.CraftUnlock] ?? Enumerable.Empty<Entity>(),
                //GameController?.EntityListWrapper?.ValidEntitiesByType[EntityType.Daemon] ?? Enumerable.Empty<Entity>(),
                //GameController?.EntityListWrapper?.ValidEntitiesByType[EntityType.TownPortal] ?? Enumerable.Empty<Entity>(),
                //GameController?.EntityListWrapper?.ValidEntitiesByType[EntityType.Monolith] ?? Enumerable.Empty<Entity>(),
                //GameController?.EntityListWrapper?.ValidEntitiesByType[EntityType.MiniMonolith] ?? Enumerable.Empty<Entity>(),
                //GameController?.EntityListWrapper?.ValidEntitiesByType[EntityType.BetrayalChoice] ?? Enumerable.Empty<Entity>(),
                //GameController?.EntityListWrapper?.ValidEntitiesByType[EntityType.IngameIcon] ?? Enumerable.Empty<Entity>(),
                //GameController?.EntityListWrapper?.ValidEntitiesByType[EntityType.LegionMonolith] ?? Enumerable.Empty<Entity>(),
                //GameController?.EntityListWrapper?.ValidEntitiesByType[EntityType.Item] ?? Enumerable.Empty<Entity>(),
                //GameController?.EntityListWrapper?.ValidEntitiesByType[EntityType.Terrain] ?? Enumerable.Empty<Entity>(),
                //GameController?.EntityListWrapper?.ValidEntitiesByType[EntityType.DelveCraftingBench] ?? Enumerable.Empty<Entity>(),
                //GameController?.EntityListWrapper?.ValidEntitiesByType[EntityType.GuildStash] ?? Enumerable.Empty<Entity>(),
                //GameController?.EntityListWrapper?.ValidEntitiesByType[EntityType.MiscellaneousObjects] ?? Enumerable.Empty<Entity>(),
                //GameController?.EntityListWrapper?.ValidEntitiesByType[EntityType.Door] ?? Enumerable.Empty<Entity>(),
                //GameController?.EntityListWrapper?.ValidEntitiesByType[EntityType.DoorSwitch] ?? Enumerable.Empty<Entity>(),
                //GameController?.EntityListWrapper?.ValidEntitiesByType[EntityType.ExpeditionRelic] ?? Enumerable.Empty<Entity>(),
                //GameController?.EntityListWrapper?.ValidEntitiesByType[EntityType.ExpeditionRune] ?? Enumerable.Empty<Entity>(),
                //GameController?.EntityListWrapper?.ValidEntitiesByType[EntityType.TriggerableBlockage] ?? Enumerable.Empty<Entity>()
            };

            var entityList = entityLists.SelectMany(list => list).ToList();

            if (entityList == null) return;

            foreach (var entity in entityList)
            {
                if (entity == null) continue;

                var drawSettings = new WAYGConfig();

                switch (entity.Type)
                {
                    case EntityType.Monster:
                        switch (entity.IsHostile)
                        {
                            case true:
                                switch (entity.Rarity)
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
                        if (entity.Address != player?.Address)
                            drawSettings = Settings.Players;
                        else
                            drawSettings = Settings.Self;

                        break;

                    case EntityType.None:
                        if (entity.Metadata == "Metadata/Projectiles/Fireball")
                            drawSettings = Settings.TestingUnits;
                        if (entity.Metadata.Contains("LightningArrow"))
                            drawSettings = Settings.TestingUnits;

                        break;
                }

                if (!drawSettings.Enable) continue;

                var component = entity?.GetComponent<Render>();
                if (component == null) continue;

#region UnitTesting Drawing
                if (drawSettings.UnitType == UnitType.UnitTesting)
                {
                    if (drawSettings.World.AlwaysRenderWorldUnit)
                        switch (drawSettings.World.DrawBoundingBox)
                        {
                            case true:
                                DrawBoundingBoxInWorld(entity.PosNum, drawSettings.Colors.WorldColor, component.BoundsNum, component.RotationNum);
                                break;
                            case false:
                                DrawCircleInWorldPos(drawSettings.World.DrawFilledCircle, entity.PosNum, component.BoundsNum.X, drawSettings.World.RenderCircleThickness, drawSettings.Colors.WorldColor);
                                break;
                        }
                }
#endregion

                var pathComp = entity?.GetComponent<Pathfinding>();
                if (pathComp == null) continue;

                var actorComp = entity?.GetComponent<Actor>();
                if (actorComp == null) continue;

                var shouldDrawCircle = entity.IsAlive && entity.DistancePlayer < Settings.MaxCircleDrawDistance;

                var actionFlag = actorComp.Action;

                switch (actionFlag)
                {
                    case (ActionFlags)512:
                    case var flags when (flags & ActionFlags.None) == 0:
                        if (drawSettings.World.AlwaysRenderWorldUnit)
                        {
                            if (shouldDrawCircle)
                                switch (drawSettings.World.DrawBoundingBox)
                                {
                                    case true:
                                        DrawBoundingBoxInWorld(entity.PosNum, drawSettings.Colors.WorldColor, component.BoundsNum, component.RotationNum);
                                        break;
                                    case false:
                                        DrawCircleInWorldPos(drawSettings.World.DrawFilledCircle, entity.PosNum, component.BoundsNum.X, drawSettings.World.RenderCircleThickness, drawSettings.Colors.WorldColor);
                                        break;
                                }
                        }

                        break;

                    case var flags when (flags & ActionFlags.UsingAbility) != 0:
                        var castGridDestination = actorComp.CurrentAction.Destination;

                        if (drawSettings.Map.Enable && drawSettings.Map.DrawAttack)
                        {
                            var entityTerrainHeight = QueryGridPositionToWorldWithTerrainHeight(entity.GridPosNum);
                            var entityCastTerrainHeight = QueryGridPositionToWorldWithTerrainHeight(castGridDestination);

                            Vector2 position;
                            var castPosConvert = new Vector2();
                            if (largeMap)
                            {
                                position = ScreenCenterCache + Helper.DeltaInWorldToMinimapDelta(entity.GridPosNum - playerPos, Diagonal, scale, (entityTerrainHeight.Z - posZ) / (9f / mapWindowLargeMapZoom));
                                castPosConvert = ScreenCenterCache + Helper.DeltaInWorldToMinimapDelta(new Vector2(castGridDestination.X, castGridDestination.Y) - playerPos, Diagonal, scale, (entityCastTerrainHeight.Z - posZ) / (9f / mapWindowLargeMapZoom));
                            }
                            else
                            {
                                position = ScreenCenterCache + Helper.DeltaInWorldToMinimapDelta(entity.GridPosNum - playerPos, Diagonal, 240f, (entityTerrainHeight.Z - posZ) / 20);
                                castPosConvert = ScreenCenterCache + Helper.DeltaInWorldToMinimapDelta(new Vector2(castGridDestination.X, castGridDestination.Y) - playerPos, Diagonal, 240f, (entityCastTerrainHeight.Z - posZ) / 20);
                            }

                            Graphics.DrawLine(position, castPosConvert, drawSettings.Map.LineThickness, drawSettings.Colors.MapAttackColor);
                        }

                        if (drawSettings.World.Enable && drawSettings.World.DrawAttack)
                        {
                            if (shouldDrawCircle)
                                switch (drawSettings.World.DrawBoundingBox)
                                {
                                    case true:
                                        DrawBoundingBoxInWorld(entity.PosNum, drawSettings.Colors.WorldAttackColor, component.BoundsNum, component.RotationNum);
                                        break;
                                    case false:
                                        DrawCircleInWorldPos(drawSettings.World.DrawFilledCircle, entity.PosNum, component.BoundsNum.X, drawSettings.World.RenderCircleThickness, drawSettings.Colors.WorldAttackColor);
                                        break;
                                }

                            if (drawSettings.World.DrawLine)
                            {
                                var entityScreenCastPosition = QueryWorldScreenPositionWithTerrainHeight(castGridDestination.ToVector2Num());
                                var entityWorldPosition = QueryWorldScreenPositionWithTerrainHeight(entity.GridPosNum);
                                Graphics.DrawLine(entityWorldPosition, entityScreenCastPosition, drawSettings.World.LineThickness, drawSettings.Colors.WorldAttackColor);
                            }

                            if (drawSettings.World.DrawAttackEndPoint && shouldDrawCircle)
                            {
                                var worldPosFromGrid = new Vector3(castGridDestination.GridToWorld().X, castGridDestination.GridToWorld().Y, 0);
                                DrawCircleInWorldPos(drawSettings.World.DrawFilledCircle, new Vector3(worldPosFromGrid.Xy(), GameController.IngameState.Data.GetTerrainHeightAt(worldPosFromGrid.WorldToGrid())), component.BoundsNum.X / 3, drawSettings.World.LineThickness, drawSettings.Colors.WorldAttackColor);
                            }
                        }
                        else
                        {
                            if (drawSettings.World.AlwaysRenderWorldUnit && shouldDrawCircle)
                                switch (drawSettings.World.DrawBoundingBox)
                                {
                                    case true:
                                        DrawBoundingBoxInWorld(entity.PosNum, drawSettings.Colors.WorldColor, component.BoundsNum, component.RotationNum);
                                        break;
                                    case false:
                                        DrawCircleInWorldPos(drawSettings.World.DrawFilledCircle, entity.PosNum, component.BoundsNum.X, drawSettings.World.RenderCircleThickness, drawSettings.Colors.WorldColor);
                                        break;
                                }
                        }
                        break;

                    case ActionFlags.AbilityCooldownActive:
                        break;

                    case ActionFlags.UsingAbilityAbilityCooldown:
                        break;

                    case ActionFlags.Dead:
                        break;

                    case var flags when (flags & ActionFlags.Moving) != 0:
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
                                var queriedWorldPos = new Vector3(pathingNodes.Last().GridToWorld().X, pathingNodes.Last().GridToWorld().Y, 0);
                                DrawCircleInWorldPos(drawSettings.World.DrawFilledCircle, new Vector3(queriedWorldPos.Xy(), GameController.IngameState.Data.GetTerrainHeightAt(queriedWorldPos.WorldToGrid())), component.BoundsNum.X / 3, drawSettings.World.RenderCircleThickness, drawSettings.Colors.WorldColor);
                            }

                            if (drawSettings.World.AlwaysRenderWorldUnit && shouldDrawCircle)
                                switch (drawSettings.World.DrawBoundingBox)
                                {
                                    case true:
                                        DrawBoundingBoxInWorld(entity.PosNum, drawSettings.Colors.WorldColor, component.BoundsNum, component.RotationNum);
                                        break;
                                    case false:
                                        DrawCircleInWorldPos(drawSettings.World.DrawFilledCircle, entity.PosNum, component.BoundsNum.X, drawSettings.World.RenderCircleThickness, drawSettings.Colors.WorldColor);
                                        break;
                                }
                        }
                        break;

                    case ActionFlags.WashedUpState:
                        // Handle WashedUpState
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

        /// <summary>
        /// Draws a filled circle at the specified world position with the given radius and color.
        /// </summary>
        /// <param name="position">The world position to draw the circle at.</param>
        /// <param name="radius">The radius of the circle.</param>
        /// <param name="color">The color of the circle.</param>
        private void DrawFilledCircleInWorldPosition(Vector3 position, float radius, int thickness, Color color)
        {
            var circlePoints = new List<Vector2>();
            const int segments = 15;
            const float segmentAngle = 2f * MathF.PI / segments;

            for (var i = 0; i < segments; i++)
            {
                var angle = i * segmentAngle;
                var currentOffset = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius;
                var nextOffset = new Vector2(MathF.Cos(angle + segmentAngle), MathF.Sin(angle + segmentAngle)) * radius;

                var currentWorldPos = position + new Vector3(currentOffset, 0);
                var nextWorldPos = position + new Vector3(nextOffset, 0);

                circlePoints.Add(Camera.WorldToScreen(currentWorldPos));
                circlePoints.Add(Camera.WorldToScreen(nextWorldPos));
            }

            Graphics.DrawConvexPolyFilled(circlePoints.ToArray(), color);
        }

        /// <summary>
        /// Draws a circle at the specified world position with the given radius, thickness, and color.
        /// </summary>
        /// <param name="position">The world position to draw the circle at.</param>
        /// <param name="radius">The radius of the circle.</param>
        /// <param name="thickness">The thickness of the circle's outline.</param>
        /// <param name="color">The color of the circle.</param>
        private void DrawBoundingBoxInWorld(Vector3 position, Color color, Vector3 bounds, Vector3 meshRotation)
        {

            var _color = color;
            //_color.A = 50;

            var points = new List<Vector3>
            {
                new Vector3(position.X - bounds.X, position.Y - bounds.Y, position.Z), // Bottom left = Pos.X-Bounds.X/2, Pos.Y-Bounds.Y/2
                new Vector3(position.X - bounds.X, position.Y + bounds.Y, position.Z), // Top left = Pos.X-Bounds.X/2, Pos.Y+Bounds.Y/2
                new Vector3(position.X + bounds.X, position.Y + bounds.Y, position.Z), // Top Right = Pos.X+Bounds.X/2, Pos.Y+Bounds.Y/2
                new Vector3(position.X + bounds.X, position.Y - bounds.Y, position.Z) // Bottom Right = Pos.X+Bounds.X/2, Pos.Y-Bounds.Y/2
            };



            var points2 = new List<Vector3>
            {
                new Vector3(position.X - bounds.X, position.Y - bounds.Y, position.Z-bounds.Z*2), // Bottom left = Pos.X-Bounds.X/2, Pos.Y-Bounds.Y/2
                new Vector3(position.X - bounds.X, position.Y + bounds.Y, position.Z-bounds.Z*2), // Top left = Pos.X-Bounds.X/2, Pos.Y+Bounds.Y/2
                new Vector3(position.X + bounds.X, position.Y + bounds.Y, position.Z-bounds.Z*2), // Top Right = Pos.X+Bounds.X/2, Pos.Y+Bounds.Y/2
                new Vector3(position.X + bounds.X, position.Y - bounds.Y, position.Z-bounds.Z*2) // Bottom Right = Pos.X+Bounds.X/2, Pos.Y-Bounds.Y/2
            };



            points = RotatePointsAroundCenter(points, position, meshRotation.X);
            points2 = RotatePointsAroundCenter(points2, position, meshRotation.X);

            var pointsOnScreen = new List<Vector2>
            {
                Camera.WorldToScreen(points[0]), // Bottom left = Pos.X-Bounds.X/2, Pos.Y-Bounds.Y/2
                Camera.WorldToScreen(points[1]), // Top left = Pos.X-Bounds.X/2, Pos.Y+Bounds.Y/2
                Camera.WorldToScreen(points[2]), // Top Right = Pos.X+Bounds.X/2, Pos.Y+Bounds.Y/2
                Camera.WorldToScreen(points[3]) // Bottom Right = Pos.X+Bounds.X/2, Pos.Y-Bounds.Y/2
            };

            var points2OnScreen = new List<Vector2>
            {
                Camera.WorldToScreen(points2[0]), // Bottom left = Pos.X-Bounds.X/2, Pos.Y-Bounds.Y/2
                Camera.WorldToScreen(points2[1]), // Top left = Pos.X-Bounds.X/2, Pos.Y+Bounds.Y/2
                Camera.WorldToScreen(points2[2]), // Top Right = Pos.X+Bounds.X/2, Pos.Y+Bounds.Y/2
                Camera.WorldToScreen(points2[3]) // Bottom Right = Pos.X+Bounds.X/2, Pos.Y-Bounds.Y/2
            };

            Graphics.DrawConvexPolyFilled(pointsOnScreen.ToArray(), _color); // Bottom Bounds
            Graphics.DrawConvexPolyFilled(points2OnScreen.ToArray(), _color); // Top Bounds

            //_color.A = 100;

            Graphics.DrawLine(Camera.WorldToScreen(points[0]), Camera.WorldToScreen(points2[0]), 2f, _color);
            Graphics.DrawLine(Camera.WorldToScreen(points[1]), Camera.WorldToScreen(points2[1]), 2f, _color);
            Graphics.DrawLine(Camera.WorldToScreen(points[2]), Camera.WorldToScreen(points2[2]), 2f, _color);
            Graphics.DrawLine(Camera.WorldToScreen(points[3]), Camera.WorldToScreen(points2[3]), 2f, _color);

        }

        static List<Vector3> RotatePointsAroundCenter(List<Vector3> points, Vector3 position, float angleInRadians)
        {
            var rotatedPoints = new List<Vector3>();

            // Find the center point
            var center = new Vector3(position.X, position.Y, position.Z);

            // Iterate over each point
            foreach (var point in points)
            {
                // Translate to origin
                var translatedPoint = point - center;

                // Rotate the translated point
                var rotatedPoint = RotatePoint(translatedPoint, angleInRadians);

                // Translate back to the original position
                var finalPoint = rotatedPoint + center;

                rotatedPoints.Add(finalPoint);
            }

            return rotatedPoints;
        }

        static Vector3 RotatePoint(Vector3 point, float angle)
        {
            // Your rotation logic here
            // Implement your rotation logic for a 3D point around the origin

            // For example, for rotating around the Z-axis (assuming a 2D rotation), you can use:
            float newX = (float)(point.X * Math.Cos(angle) - point.Y * Math.Sin(angle));
            float newY = (float)(point.X * Math.Sin(angle) + point.Y * Math.Cos(angle));
            float newZ = point.Z; // Z remains the same for a 2D rotation

            return new Vector3(newX, newY, newZ);
        }

        private void DrawCircleInWorldPos(bool drawFilledCircle, Vector3 position, float radius, int thickness, Color color)
        {
            if (drawFilledCircle)
            {
                DrawFilledCircleInWorldPosition(position, radius, thickness, color);
            }
            else
            {
                DrawCircleInWorldPosition(position, radius, thickness, color);
            }
        }
    }
}