using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ExileCore;
using ExileCore.PoEMemory;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared;
using ExileCore.Shared.Abstract;
using ExileCore.Shared.Cache;
using ExileCore.Shared.Enums;
using ExileCore.Shared.Helpers;
using GameOffsets.Native;
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

            if (ingameStateIngameUi.AtlasPanel.IsVisibleLocal || ingameStateIngameUi.DelveWindow.IsVisibleLocal ||
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

                if (icon.Entity.Type == EntityType.WorldItem)
                    continue;

                if (!Settings.DrawMonsters && icon.Entity.Type == EntityType.Monster)
                    continue;

                if (!icon.Show())
                    continue;

                var component = icon?.Entity?.GetComponent<Render>();
                if (component == null) continue;
                var iconZ = component.Pos.Z;

                var shouldDraw = false;
                var color = Color.White;
                switch (icon.Rarity)
                {
                    case MonsterRarity.White:
                        color = Settings.WhiteMonsterPathColor;
                        shouldDraw = icon.Entity.IsHostile && Settings.ShowWhiteMonsterPath;
                        break;
                    case MonsterRarity.Magic:
                        color = Settings.MagicMonsterPathColor;
                        shouldDraw = icon.Entity.IsHostile && Settings.ShowMagicMonsterPath;
                        break;
                    case MonsterRarity.Rare:
                        color = Settings.RareMonsterPathColor;
                        shouldDraw = icon.Entity.IsHostile && Settings.ShowRareMonsterPath;
                        break;
                    case MonsterRarity.Unique:
                        color = Settings.UniqueMonsterPathColor;
                        shouldDraw = icon.Entity.IsHostile && Settings.ShowUniqueMonsterPath;
                        break;
                }

                if (!shouldDraw) continue;

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
                gridToWorldToScreenList = GridDestinationToWorldToScreen(pathingNodesWorld, icon.Entity);

                if (Settings.DrawOnWorld)
                    gridToWorldList = GridDestinationToWorld(pathingNodesWorld, icon.Entity);

                switch (actionFlag)
                {
                    case ActionFlags.None:
                        break;
                    case ActionFlags.UsingAbility:
                        var castPosConvert = new Vector2();
                        var castDestination = actorComp.CurrentAction.Destination;


                        var castGridToWorld = WorldPositionExtensions.GridToWorld(new Vector2(actorComp.CurrentAction.Destination.X, actorComp.CurrentAction.Destination.Y));
                        var gridToWorld2 = WorldPositionExtensions.GridToWorld(icon.Entity.GridPos);
                        var worldToSreen = GameController.Game.IngameState.Camera.WorldToScreen(new Vector3(castGridToWorld.X, castGridToWorld.Y, iconZ + component.Bounds.Z));
                        var worldToSreen2 = GameController.Game.IngameState.Camera.WorldToScreen(new Vector3(gridToWorld2.X, gridToWorld2.Y, iconZ + component.Bounds.Z));

                        if (Settings.DrawOnMap)
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
                            Graphics.DrawLine(position, castPosConvert, 2, Settings.AttackPathColor);
                        }

                        // world
                        if (Settings.DrawOnWorld)
                        {
                            DrawEllipseToWorld(icon.Entity.Pos, (int)component.Bounds.X, 15, 3, Settings.AttackPathColor);
                            Graphics.DrawLine(worldToSreen2, worldToSreen, 5, Settings.AttackPathColor);
                            DrawEllipseToWorld(new Vector3(castGridToWorld.X, castGridToWorld.Y, icon.Entity.Pos.Z), (int)component.Bounds.X / 3, 10, 3, Settings.AttackPathColor);
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
                        if (Settings.DrawOnMap)
                        {
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
                                    Graphics.DrawLine(mapPathNodes[i], mapPathNodes[i + 1], 2, color);
                                }
                            }
                        }

                        // world
                        if (Settings.DrawOnWorld)
                        {
                            if (gridToWorldToScreenList.Count > 0)
                            {
                                for (int i = 0; i < gridToWorldToScreenList.Count - 1; i++)
                                {
                                    Graphics.DrawLine(gridToWorldToScreenList[i], gridToWorldToScreenList[i + 1], 5, color);
                                }
                            }

                            // entity pos circle
                            DrawEllipseToWorld(icon.Entity.Pos, (int) component.Bounds.X, 15, 3, color);
                            // destination circle
                            DrawEllipseToWorld( new Vector3(gridToWorldList.Last().X, gridToWorldList.Last().Y, icon.Entity.Pos.Z), (int) component.Bounds.X / 3, 10, 3, color);
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
}
