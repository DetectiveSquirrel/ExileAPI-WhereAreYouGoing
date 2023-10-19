using ExileCore;
using ExileCore.PoEMemory;
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

            player.TryGetComponent<Positioned>(out var playerPositioned);
            if (playerPositioned == null) return;
            var playerPos = playerPositioned.GridPosNum;


            player.TryGetComponent<Render>(out var playerRender);
            if (playerRender == null) return;

            var posZ = GameController.Player.PosNum.Z;

            if (MapWindow == null) return;
            var mapWindowLargeMapZoom = MapWindow.LargeMapZoom;

            // To reduce going over pointless amounts of valid entities, just enable the lists youre currently reading from.
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
                GameController?.EntityListWrapper?.ValidEntitiesByType[EntityType.Shrine] ?? Enumerable.Empty<Entity>(),
                GameController?.EntityListWrapper?.ValidEntitiesByType[EntityType.AreaTransition] ?? Enumerable.Empty<Entity>(),
                GameController?.EntityListWrapper?.ValidEntitiesByType[EntityType.Portal] ?? Enumerable.Empty<Entity>(),
                //GameController?.EntityListWrapper?.ValidEntitiesByType[EntityType.QuestObject] ?? Enumerable.Empty<Entity>(),
                GameController?.EntityListWrapper?.ValidEntitiesByType[EntityType.Stash] ?? Enumerable.Empty<Entity>(),
                //GameController?.EntityListWrapper?.ValidEntitiesByType[EntityType.Waypoint] ?? Enumerable.Empty<Entity>(),
                GameController?.EntityListWrapper?.ValidEntitiesByType[EntityType.Player] ?? Enumerable.Empty<Entity>(),
                //GameController?.EntityListWrapper?.ValidEntitiesByType[EntityType.Pet] ?? Enumerable.Empty<Entity>(),
                //GameController?.EntityListWrapper?.ValidEntitiesByType[EntityType.WorldItem] ?? Enumerable.Empty<Entity>(),
                //GameController?.EntityListWrapper?.ValidEntitiesByType[EntityType.Resource] ?? Enumerable.Empty<Entity>(),
                GameController?.EntityListWrapper?.ValidEntitiesByType[EntityType.Breach] ?? Enumerable.Empty<Entity>(),
                //GameController?.EntityListWrapper?.ValidEntitiesByType[EntityType.ControlObjects] ?? Enumerable.Empty<Entity>(),
                //GameController?.EntityListWrapper?.ValidEntitiesByType[EntityType.HideoutDecoration] ?? Enumerable.Empty<Entity>(),
                //GameController?.EntityListWrapper?.ValidEntitiesByType[EntityType.CraftUnlock] ?? Enumerable.Empty<Entity>(),
                //GameController?.EntityListWrapper?.ValidEntitiesByType[EntityType.Daemon] ?? Enumerable.Empty<Entity>(),
                GameController?.EntityListWrapper?.ValidEntitiesByType[EntityType.TownPortal] ?? Enumerable.Empty<Entity>(),
                //GameController?.EntityListWrapper?.ValidEntitiesByType[EntityType.Monolith] ?? Enumerable.Empty<Entity>(),
                //GameController?.EntityListWrapper?.ValidEntitiesByType[EntityType.MiniMonolith] ?? Enumerable.Empty<Entity>(),
                //GameController?.EntityListWrapper?.ValidEntitiesByType[EntityType.BetrayalChoice] ?? Enumerable.Empty<Entity>(),
                GameController?.EntityListWrapper?.ValidEntitiesByType[EntityType.IngameIcon] ?? Enumerable.Empty<Entity>(),
                //GameController?.EntityListWrapper?.ValidEntitiesByType[EntityType.LegionMonolith] ?? Enumerable.Empty<Entity>(),
                //GameController?.EntityListWrapper?.ValidEntitiesByType[EntityType.Item] ?? Enumerable.Empty<Entity>(),
                //GameController?.EntityListWrapper?.ValidEntitiesByType[EntityType.Terrain] ?? Enumerable.Empty<Entity>(),
                //GameController?.EntityListWrapper?.ValidEntitiesByType[EntityType.DelveCraftingBench] ?? Enumerable.Empty<Entity>(),
                //GameController?.EntityListWrapper?.ValidEntitiesByType[EntityType.GuildStash] ?? Enumerable.Empty<Entity>(),
                //GameController?.EntityListWrapper?.ValidEntitiesByType[EntityType.MiscellaneousObjects] ?? Enumerable.Empty<Entity>(),
                GameController?.EntityListWrapper?.ValidEntitiesByType[EntityType.Door] ?? Enumerable.Empty<Entity>()
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

                #region UnitTesting Entity Add
                if (entity.Type != EntityType.Monster && entity.Type != EntityType.Player)
                    drawSettings = Settings.TestingUnits;
                #endregion


                if (!drawSettings.Enable) continue;

                entity.TryGetComponent<Render>(out var renderComp);
                if (renderComp == null) continue;

                #region UnitTesting Drawing
                if (drawSettings.UnitType == UnitType.UnitTesting)
                {
                    if (drawSettings.World.AlwaysRenderWorldUnit)
                        switch (drawSettings.World.DrawBoundingBox)
                        {
                            case true:
                                DrawBoundingBoxInWorld(entity.PosNum, drawSettings.Colors.WorldColor, renderComp.BoundsNum, renderComp.RotationNum.X);
                                break;
                            case false:
                                DrawCircleInWorldPos(drawSettings.World.DrawFilledCircle, entity.PosNum, renderComp.BoundsNum.X, drawSettings.World.RenderCircleThickness, drawSettings.Colors.WorldColor);
                                break;
                        }
                }
                #endregion

                entity.TryGetComponent<Pathfinding>(out var pathComp);
                if (pathComp == null) continue;

                entity.TryGetComponent<Actor>(out var actorComp);
                if (actorComp == null) continue;

                var shouldDrawCircle = entity.IsAlive && entity.DistancePlayer < Settings.MaxCircleDrawDistance;

                var actionFlag = actorComp.Action;

                switch (actionFlag)
                {
                    case (ActionFlags)512:
                    case ActionFlags.None:
                    case ActionFlags.None | ActionFlags.HasMines:
                        if (drawSettings.World.AlwaysRenderWorldUnit)
                        {
                            if (shouldDrawCircle)
                                switch (drawSettings.World.DrawBoundingBox)
                                {
                                    case true:
                                        DrawBoundingBoxInWorld(entity.PosNum, drawSettings.Colors.WorldColor, renderComp.BoundsNum, renderComp.RotationNum.X);
                                        break;
                                    case false:
                                        DrawCircleInWorldPos(drawSettings.World.DrawFilledCircle, entity.PosNum, renderComp.BoundsNum.X, drawSettings.World.RenderCircleThickness, drawSettings.Colors.WorldColor);
                                        break;
                                }
                        }

                        break;

                    case ActionFlags.UsingAbility:
                    case ActionFlags.UsingAbility | ActionFlags.HasMines:
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
                                        DrawBoundingBoxInWorld(entity.PosNum, drawSettings.Colors.WorldAttackColor, renderComp.BoundsNum, renderComp.RotationNum.X);
                                        break;
                                    case false:
                                        DrawCircleInWorldPos(drawSettings.World.DrawFilledCircle, entity.PosNum, renderComp.BoundsNum.X, drawSettings.World.RenderCircleThickness, drawSettings.Colors.WorldAttackColor);
                                        break;
                                }

                            if (drawSettings.World.DrawLine)
                            {
                                var entityScreenCastPosition = GameController.IngameState.Data.GetGridScreenPosition(castGridDestination.ToVector2Num());
                                var entityWorldPosition = GameController.IngameState.Data.GetGridScreenPosition(entity.GridPosNum);
                                Graphics.DrawLine(entityWorldPosition, entityScreenCastPosition, drawSettings.World.LineThickness, drawSettings.Colors.WorldAttackColor);
                            }

                            if (drawSettings.World.DrawAttackEndPoint && shouldDrawCircle)
                            {
                                var worldPosFromGrid = new Vector3(castGridDestination.GridToWorld().X, castGridDestination.GridToWorld().Y, 0);
                                DrawCircleInWorldPos(drawSettings.World.DrawFilledCircle, new Vector3(worldPosFromGrid.Xy(), GameController.IngameState.Data.GetTerrainHeightAt(worldPosFromGrid.WorldToGrid())), renderComp.BoundsNum.X / 3, drawSettings.World.LineThickness, drawSettings.Colors.WorldAttackColor);
                            }
                        }
                        else
                        {
                            if (drawSettings.World.AlwaysRenderWorldUnit && shouldDrawCircle)
                                switch (drawSettings.World.DrawBoundingBox)
                                {
                                    case true:
                                        DrawBoundingBoxInWorld(entity.PosNum, drawSettings.Colors.WorldColor, renderComp.BoundsNum, renderComp.RotationNum.X);
                                        break;
                                    case false:
                                        DrawCircleInWorldPos(drawSettings.World.DrawFilledCircle, entity.PosNum, renderComp.BoundsNum.X, drawSettings.World.RenderCircleThickness, drawSettings.Colors.WorldColor);
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

                    case ActionFlags.Moving:
                    case ActionFlags.Moving | ActionFlags.HasMines:
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
                                DrawCircleInWorldPos(drawSettings.World.DrawFilledCircle, new Vector3(queriedWorldPos.Xy(), GameController.IngameState.Data.GetTerrainHeightAt(queriedWorldPos.WorldToGrid())), renderComp.BoundsNum.X / 3, drawSettings.World.RenderCircleThickness, drawSettings.Colors.WorldColor);
                            }

                            if (drawSettings.World.AlwaysRenderWorldUnit && shouldDrawCircle)
                                switch (drawSettings.World.DrawBoundingBox)
                                {
                                    case true:
                                        DrawBoundingBoxInWorld(entity.PosNum, drawSettings.Colors.WorldColor, renderComp.BoundsNum, renderComp.RotationNum.X);
                                        break;
                                    case false:
                                        DrawCircleInWorldPos(drawSettings.World.DrawFilledCircle, entity.PosNum, renderComp.BoundsNum.X, drawSettings.World.RenderCircleThickness, drawSettings.Colors.WorldColor);
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

        private void DrawCircleInWorldPos(bool drawFilledCircle, Vector3 position, float radius, int thickness, Color color)
        {
            RectangleF screensize = GameController.Window.GetWindowRectangleReal();
            Vector2 entityPos = RemoteMemoryObject.pTheGame.IngameState.Camera.WorldToScreen(position);
            if (IsEntityWithinScreen(entityPos, screensize, 50))
            {
                if (drawFilledCircle)
                {
                    Graphics.DrawFilledCircleInWorld(position, radius, color);
                }
                else
                {
                    Graphics.DrawCircleInWorld(position, radius, color, thickness);
                }
            }
        }

        private void DrawBoundingBoxInWorld(Vector3 position, Color color, Vector3 bounds, float rotationRadians)
        {
            RectangleF screensize = GameController.Window.GetWindowRectangleReal();
            Vector2 entityPos = RemoteMemoryObject.pTheGame.IngameState.Camera.WorldToScreen(position);
            if (IsEntityWithinScreen(entityPos, screensize, 50))
            {
                Graphics.DrawBoundingBoxInWorld(position, color, bounds, rotationRadians);
            }
        }
        private bool IsEntityWithinScreen(Vector2 entityPos, RectangleF screensize, float allowancePX)
        {
            // Check if the entity position is within the screen bounds with allowance
            float leftBound = screensize.Left - allowancePX;
            float rightBound = screensize.Right + allowancePX;
            float topBound = screensize.Top - allowancePX;
            float bottomBound = screensize.Bottom + allowancePX;

            return entityPos.X >= leftBound && entityPos.X <= rightBound &&
                   entityPos.Y >= topBound && entityPos.Y <= bottomBound;
        }
    }
}