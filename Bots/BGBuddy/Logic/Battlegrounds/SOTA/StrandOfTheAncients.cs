using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bots.BGBuddy.Helpers;
using Bots.BGBuddy.Resources;
using CommonBehaviors.Actions;
using Styx;
using Styx.Common.Helpers;
using Styx.CommonBot;
using Styx.CommonBot.POI;
using Styx.CommonBot.Profiles;
using Styx.Helpers;

using Styx.Pathing;
using Styx.TreeSharp;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;
using Tripper.MeshMisc;
using Tripper.Navigation;
using Tripper.RecastManaged.Detour;
using Action = Styx.TreeSharp.Action;

namespace Bots.BGBuddy.Logic.Battlegrounds.SOTA
{
    internal class StrandOfTheAncients : Battleground
    {
        #region Battleground Overrides

        public override string Name { get { return BGBuddyResources.StrandOfTheAncients; } }

        public override int MapId { get { return 607; } }

        public override void Dispose()
        {
            BGBuddy.Instance.WorldStatesUpdated -= HandleWorldStatesUpdated;

            var nav = Navigator.NavigationProvider as MeshNavigator;
            if (nav != null)
            {
                nav.Nav.OnSubTileLoaded -= NavOnSubTileLoaded;
            }

            BotEvents.OnBotStarted -= BotEventsOnBotStart;
            Statuses.Clear();
        }

        public override void Start()
        {
            LoadProfile();

            StartingLocation = RandomizeLocation(StartingLocation, 10);

            HandleWorldStatesUpdated();
            BGBuddy.Instance.WorldStatesUpdated += HandleWorldStatesUpdated;

            var nav = Navigator.NavigationProvider as MeshNavigator;
            if (nav != null)
            {
                nav.Nav.OnSubTileLoaded += NavOnSubTileLoaded;
            }

            BotEvents.OnBotStarted += BotEventsOnBotStart;

            if (IsAttacking)
                _wasAttacking = true;
        }

        #endregion

        #region Logic

        private bool _lastTeleportBuffState;
        private bool _wasAttacking;
        private WaitTimer _boatTimer = new WaitTimer(TimeSpan.FromSeconds(5));
        private readonly HashSet<WoWPoint> _getOfPoints = new HashSet<WoWPoint>
                                                 {
                                                    new WoWPoint(1610.332f, 50.70834f, 7.579856f),
                                                    new WoWPoint(1599.641f, -102.9774f, 8.8739f),
                                                 };

        protected override Composite CreateBeforePrepBehavior()
        {
            return
                new PrioritySelector(
                    new Decorator(
                        ret => StyxWoW.Me.HasAura("Preparation"),
                        new Action(ret => { _boatTimer.Reset(); return RunStatus.Failure; })),
                    new Decorator(
                        ret => !StyxWoW.Me.HasAura("Preparation") &&
                                ObjectManager.GetObjectsOfType<WoWGameObject>().Any(o =>
                                    o.SubType ==WoWGameObjectType.Transport && o.DistanceSqr < 20 * 20),
                        new PrioritySelector(
                            new Decorator(
                                ret => !_boatTimer.IsFinished,
                                new Action(ret => Logger.Write(BGBuddyResources.WaitingForBoat))),
                            new Sequence(
                                new Action(ret => Logger.Write(BGBuddyResources.GettingOfTheBoat)),
                                new Action(ret => WoWMovement.ClickToMove(_getOfPoints.OrderBy(p => p.DistanceSqr(StyxWoW.Me.Location)).FirstOrDefault()))))),

                    // Teleporter stuff. When you use a teleporter you get a debuff that prevents you to use another one for 30 secs.
                    // Disabling teleporters while we have the debuff.
                    new Action(ret =>
                    {
                        if (_wasAttacking != IsAttacking)
                        {
                            LoadProfile();
                            StartingLocation = RandomizeLocation(StartingLocation, 10);
                            _wasAttacking = !_wasAttacking;
                        }

                        var changed = false;
                        if (_lastTeleportBuffState && !StyxWoW.Me.HasAura("Teleport"))
                        {
                            _lastTeleportBuffState = false;
                            changed = true;
                        }

                        if (!_lastTeleportBuffState && StyxWoW.Me.HasAura("Teleport"))
                        {
                            _lastTeleportBuffState = true;
                            changed = true;
                        }

                        if (changed)
                        {
                            ResetMeshes("Teleport buff");
                        }

                        return RunStatus.Failure;
                    }));
        }

        protected override int BattlegroundRaidSize { get { return 15; } }

        public override Composite Logic
        {
            get
            {
                return
                    new Sequence(
                    // Actual logic for the bot. (Hotspot selection)
                        new Switch<LogicType>(
                            ret => BGBuddySettings.Instance.SotaLogicType,
                            new SwitchArgument<LogicType>(LogicType.Attack, CreateOffensiveLogic())),

                        new Decorator(
                            ret => BotPoi.Current.Type == PoiType.Hotspot,
                            BGBuddy.CreateMoveToLocationBehavior(ret => Hotspot, true, 5))
                        );
            }
        }

        #endregion

        #region Gate Stuff

        private static void ResetMeshes(string reason)
        {
            var nav = Navigator.NavigationProvider as MeshNavigator;
            if (nav != null)
                nav.Nav.UnloadAllTiles();

            Logger.WriteDebug("Unloading all tiles. Reason(s): {0}", reason);
            Navigator.Clear();
        }

        private void BotEventsOnBotStart(EventArgs args)
        {
            ResetMeshes("Bot start");
        }

        private void NavOnSubTileLoaded(object sender, TileLoadedEventArgs args)
        {
            MeshTile meshTile = ((WowNavigator)sender).Mesh.GetTileAt(args.Tile.X, args.Tile.Y);
            if (meshTile != null)
            {
                ModifyGates(meshTile);
            }
        }

        private void ModifyGates(MeshTile tile)
        {
            var usedTeleport = StyxWoW.Me.HasAura("Teleport");
            var destroyedLandmarks = Statuses.Values.Where(l => l.Control == LandmarkControlType.Destroyed);
            var polys = tile.GetAllPolys();
            foreach (var poly in polys)
            {
                switch ((AreaType)poly.Area)
                {
                    case AreaType.DefendersPortal:
                        poly.Flags = IsAttacking || usedTeleport ? (ushort)AbilityFlags.Unwalkable : (ushort)AbilityFlags.Teleport;
                        break;
                }

                switch ((SotAGate)poly.Area)
                {
                    case SotAGate.Blue:
                        poly.Flags = destroyedLandmarks.Any(l => l.Type == (int)StrandOfTheAncientsLandmarkType.GateOfTheBlueSapphire)
                                         ? (ushort)AbilityFlags.Run
                                         : (ushort)AbilityFlags.Unwalkable;
                        break;
                    case SotAGate.Green:
                        poly.Flags = destroyedLandmarks.Any(l => l.Type == (int)StrandOfTheAncientsLandmarkType.GateOfTheGreenEmerald)
                                         ? (ushort)AbilityFlags.Run
                                         : (ushort)AbilityFlags.Unwalkable;
                        break;
                    case SotAGate.Purple:
                        poly.Flags = destroyedLandmarks.Any(l => l.Type == (int)StrandOfTheAncientsLandmarkType.GateOfThePurpleAmethyst)
                                         ? (ushort)AbilityFlags.Run
                                         : (ushort)AbilityFlags.Unwalkable;
                        break;
                    case SotAGate.Red:
                        poly.Flags = destroyedLandmarks.Any(l => l.Type == (int)StrandOfTheAncientsLandmarkType.GateOfTheRedSun)
                                         ? (ushort)AbilityFlags.Run
                                         : (ushort)AbilityFlags.Unwalkable;
                        break;
                    case SotAGate.Yellow:
                        poly.Flags = destroyedLandmarks.Any(l => l.Type == (int)StrandOfTheAncientsLandmarkType.GateOfTheYellowMoon)
                                         ? (ushort)AbilityFlags.Run
                                         : (ushort)AbilityFlags.Unwalkable;
                        break;
                    default:
                        continue;
                }
            }
        }

        #endregion

        #region Common

        private void HandleWorldStatesUpdated()
        {
            // Fired whenever something changes (flag taken/returned, bases captured, etc)
            Styx.WoWInternals.Battlegrounds.LandMarks.Refresh();

            var sotaLandmarks =
                Styx.WoWInternals.Battlegrounds.LandMarks.LandmarkList.Select(l => l.ToStrandOfTheAncientsLandmark());

            var needToReset = false;
            var resetReason = new StringBuilder();
            resetReason.AppendLine();
            // Iterate, and refresh our landmark shit.
            foreach (var lm in sotaLandmarks)
            {
                if (!Statuses.ContainsKey((int)lm.LandmarkType))
                {
                    Statuses.Add(
                        (int)lm.LandmarkType, new LandmarkInfo((int)lm.LandmarkType, lm.ControlType, GetLandmarkBox(lm.LandmarkType.ToString()), this));

                    if ((lm.LandmarkType & StrandOfTheAncientsLandmarkType.Gates) != 0)
                    {
                        needToReset = true;
                        resetReason.AppendLine(string.Format("{0} has been added", lm.LandmarkType));
                    }
                }
                else
                {
                    var oldStatus = Statuses[(int)lm.LandmarkType].Control;
                    var newStatus = lm.ControlType;

                    if (oldStatus != newStatus &&
                        (lm.LandmarkType & StrandOfTheAncientsLandmarkType.Gates) != 0)
                    {
                        needToReset = true;
                        resetReason.AppendLine(string.Format("State of {0} changed. Old Value: {1} New Value: {2}",
                                                             lm.LandmarkType, oldStatus, newStatus));
                    }

                    Statuses[(int)lm.LandmarkType] = new LandmarkInfo(
                        (int)lm.LandmarkType, lm.ControlType, GetLandmarkBox(lm.LandmarkType.ToString()), this);
                }

                Statuses[(int)lm.LandmarkType].Process();
            }

            if (needToReset)
            {
                ResetMeshes(resetReason.ToString());
            }

            Statuses.RemoveAll(v =>
                                   {
                                       if (!sotaLandmarks.Any(l => (int)l.LandmarkType == v.Type))
                                       {
                                           return true;
                                       }

                                       return false;
                                   });
        }

        #endregion

        #region Offensive

        private Composite CreateOffensiveLogic()
        {
            return new PrioritySelector(
                new Decorator(
                    ret => Statuses.Values.Any(
                                s => s.Type == (int)StrandOfTheAncientsLandmarkType.GateOfTheYellowMoon && s.Control == LandmarkControlType.Destroyed),
                    new Action(ret => SetHotspot(StrandOfTheAncientsLandmarkType.ChamberOfAncientRelics.ToString(), true, Side.ToString()))),
                new Decorator(
                    ret => Statuses.Values.Any(
                                s => (s.Type == (int)StrandOfTheAncientsLandmarkType.GateOfTheRedSun || s.Type == (int)StrandOfTheAncientsLandmarkType.GateOfThePurpleAmethyst) &&
                                     s.Control == LandmarkControlType.Destroyed),
                    new Action(ret => SetHotspot(StrandOfTheAncientsLandmarkType.GateOfTheYellowMoon.ToString(), true, Side.ToString()))),
                new Decorator(
                    ret => Statuses.Values.Any(
                                s => s.Type == (int)StrandOfTheAncientsLandmarkType.GateOfTheGreenEmerald && s.Control == LandmarkControlType.Destroyed),
                    new Action(ret => SetHotspot(StrandOfTheAncientsLandmarkType.GateOfThePurpleAmethyst.ToString(), true, Side.ToString()))),
                new Decorator(
                    ret => Statuses.Values.Any(
                                s => s.Type == (int)StrandOfTheAncientsLandmarkType.GateOfTheBlueSapphire && s.Control == LandmarkControlType.Destroyed),
                    new Action(ret => SetHotspot(StrandOfTheAncientsLandmarkType.GateOfTheRedSun.ToString(), true, Side.ToString()))),
                new PrioritySelector(
                    ctx => BiggestFight,
                    new Decorator(
                        ret => (WoWPoint)ret != WoWPoint.Zero,
                        new Action(ret => SetHotspot((WoWPoint)ret))
                        )),
                new PrioritySelector(
                    ctx => BiggestFriendlyPack,
                    new Decorator(
                        ret => (WoWPoint)ret != WoWPoint.Zero,
                        new Action(ret => SetHotspot((WoWPoint)ret, 10f, BGBuddyResources.BiggestFriendlyPack))
                        )),
                new ActionAlwaysSucceed()
                );
        }

        #endregion
    }
}
