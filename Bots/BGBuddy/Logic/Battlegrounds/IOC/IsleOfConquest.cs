using System;
using System.Linq;
using System.Text;
using Bots.BGBuddy.Helpers;
using Bots.BGBuddy.Resources;
using CommonBehaviors.Actions;
using Styx;
using Styx.Common;
using Styx.Common.Helpers;
using Styx.CommonBot;
using Styx.CommonBot.POI;
using Styx.CommonBot.Profiles;
using Styx.Helpers;

using Styx.Pathing;
using Styx.TreeSharp;
using Styx.WoWInternals;
using Tripper.MeshMisc;
using Tripper.Navigation;
using Tripper.RecastManaged.Detour;
using Action = Styx.TreeSharp.Action;

namespace Bots.BGBuddy.Logic.Battlegrounds.IOC
{
    internal class IsleOfConquest : Battleground
    {
        #region Battleground Overrides

        public override string Name { get { return BGBuddyResources.IsleOfConquest; } }

        public override int MapId { get { return 628; } }

        public override void Dispose()
        {
            BGBuddy.Instance.WorldStatesUpdated -= HandleWorldStatesUpdated;

            var nav = Navigator.NavigationProvider as MeshNavigator;
            if (nav != null)
                nav.Nav.OnSubTileLoaded -= NavOnSubTileLoaded;

            StartOfGameTimer.Finished -= StartOfGameTimerFinished;

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
            if(nav != null)
                nav.Nav.OnSubTileLoaded += NavOnSubTileLoaded;

            StartOfGameTimer.Finished += StartOfGameTimerFinished;

            BotEvents.OnBotStarted += BotEventsOnBotStart;
        }

        #endregion

        #region Logic

        private bool _lastTeleportBuffState;
        protected override Composite CreateBeforePrepBehavior()
        {
            return
                new PrioritySelector(
                    // Teleporter stuff. When you use a teleporter you get a debuff that prevents you to use another one for 30 secs.
                    // Disabling teleporters while we have the debuff.
                    new Action(ret =>
                                   {
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
                                           ResetMeshes("Teleport Buff");
                                       }

                                       return RunStatus.Failure;
                                   })
                    );
        }

        protected override int BattlegroundRaidSize { get { return 40; } }

        public override Composite Logic
        {
            get
            {
                return
                    new Sequence(
                    // Actual logic for the bot. (Hotspot selection)
                        new Switch<LogicType>(
                            ret => BGBuddySettings.Instance.IocLogicType,
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
            ResetMeshes("Bot Start");
        }

        private void StartOfGameTimerFinished(object sender, WaitTimer.WaitTimerEventArgs e)
        {
            ResetMeshes("Start of Game Timer Finished");
        }

        private void NavOnSubTileLoaded(object sender, TileLoadedEventArgs args)
        {
            MeshTile meshTile = ((WowNavigator)sender).Mesh.GetTileAt(args.Tile.X, args.Tile.Y);
            if (meshTile != null)
                ModifyGates(meshTile);
        }

        private void ModifyGates(MeshTile tile)
        {
            var isHorde = StyxWoW.Me.IsHorde;
            var usedTeleport = StyxWoW.Me.HasAura("Teleport");
            var destroyedLandmarks = Statuses.Values.Where(l => l.Control == LandmarkControlType.Destroyed).ToList();

            var polys = tile.GetAllPolys();
            foreach (var poly in polys)
            {
                switch ((AreaType)poly.Area)
                {
                    case AreaType.HordePortal:
                        poly.Flags = !isHorde || !StartOfGameTimer.IsFinished || usedTeleport ? (ushort)AbilityFlags.Unwalkable : (ushort)AbilityFlags.Teleport;
                        Logging.WriteDiagnostic(poly.Flags == (ushort)AbilityFlags.Unwalkable ? "Disabled horde teleport" : "Enabled horde teleport");
                        break;

                    case AreaType.AlliancePortal:
                        poly.Flags = isHorde || !StartOfGameTimer.IsFinished || usedTeleport ? (ushort)AbilityFlags.Unwalkable : (ushort)AbilityFlags.Teleport;
                        Logging.WriteDiagnostic(poly.Flags == (ushort)AbilityFlags.Unwalkable ? "Disabled alliance teleport" : "Enabled horde teleport");
                        break;
                }

                switch ((IoCGate)poly.Area)
                {
                    case IoCGate.AllianceEast:
                        poly.Flags = destroyedLandmarks.Any(l => l.Type == (int)IsleOfConquestLandmarkType.AllianceGateEast)
                                         ? (ushort)AbilityFlags.Run
                                         : (ushort)AbilityFlags.Unwalkable;
                        Logging.WriteDiagnostic(poly.Flags == (ushort)AbilityFlags.Unwalkable ? "Disabled alliance east gate" : "Enabled alliance east gate");
                        break;
                    case IoCGate.AllianceNorth:
                        poly.Flags = destroyedLandmarks.Any(l => l.Type == (int)IsleOfConquestLandmarkType.AllianceGateFront) || !StartOfGameTimer.IsFinished
                                         ? (ushort)AbilityFlags.Run
                                         : (ushort)AbilityFlags.Unwalkable;
                        Logging.WriteDiagnostic(poly.Flags == (ushort)AbilityFlags.Unwalkable ? "Disabled alliance front gate" : "Enabled alliance front gate");
                        break;
                    case IoCGate.AllianceWest:
                        poly.Flags = destroyedLandmarks.Any(l => l.Type == (int)IsleOfConquestLandmarkType.AllianceGateWest)
                                         ? (ushort)AbilityFlags.Run
                                         : (ushort)AbilityFlags.Unwalkable;
                        Logging.WriteDiagnostic(poly.Flags == (ushort)AbilityFlags.Unwalkable ? "Disabled alliance west gate" : "Enabled alliance west gate");
                        break;
                    case IoCGate.HordeEast:
                        poly.Flags = destroyedLandmarks.Any(l => l.Type == (int)IsleOfConquestLandmarkType.HordeGateEast)
                                         ? (ushort)AbilityFlags.Run
                                         : (ushort)AbilityFlags.Unwalkable;
                        Logging.WriteDiagnostic(poly.Flags == (ushort)AbilityFlags.Unwalkable ? "Disabled horde east gate" : "Enabled horde east gate");
                        break;
                    case IoCGate.HordeSouth:
                        poly.Flags = destroyedLandmarks.Any(l => l.Type == (int)IsleOfConquestLandmarkType.HordeGateFront) || !StartOfGameTimer.IsFinished
                                         ? (ushort)AbilityFlags.Run
                                         : (ushort)AbilityFlags.Unwalkable;
                        Logging.WriteDiagnostic(poly.Flags == (ushort)AbilityFlags.Unwalkable ? "Disabled horde front gate" : "Enabled horde front gate");
                        break;
                    case IoCGate.HordeWest:
                        poly.Flags = destroyedLandmarks.Any(l => l.Type == (int)IsleOfConquestLandmarkType.HordeGateWest)
                                         ? (ushort)AbilityFlags.Run
                                         : (ushort)AbilityFlags.Unwalkable;
                        Logging.WriteDiagnostic(poly.Flags == (ushort)AbilityFlags.Unwalkable ? "Disabled horde west gate" : "Enabled horde west gate");
                        break;
                    default:
                        continue;
                }
            }
        }

        #endregion

        #region Common

        private readonly WaitTimer _updateTimer = new WaitTimer(TimeSpan.FromSeconds(2));
        private void HandleWorldStatesUpdated()
        {
            if (!_updateTimer.IsFinished)
                return;

            _updateTimer.Reset();
            // Fired whenever something changes (flag taken/returned, bases captured, etc)
            Styx.WoWInternals.Battlegrounds.LandMarks.Refresh();

            var needToReset = false;
            var resetReason = new StringBuilder();
            resetReason.AppendLine();
            // Iterate, and refresh our landmark shit.)
            foreach (WoWLandMark landmark in Styx.WoWInternals.Battlegrounds.LandMarks.LandmarkList)
            {
                var lm = landmark.ToIsleOfConquestLandmark();
                if (!Statuses.ContainsKey((int)lm.LandmarkType))
                {
                    Statuses.Add(
                        (int)lm.LandmarkType, new LandmarkInfo((int)lm.LandmarkType, lm.ControlType, GetLandmarkBox(lm.LandmarkType.ToString()), this));

                    if ((lm.LandmarkType & IsleOfConquestLandmarkType.Gates) != 0)
                    {
                        needToReset = true;
                        resetReason.AppendLine(string.Format("New gate added [{0}]", lm.LandmarkType));
                    }
                }
                else
                {
                    var oldStatus = Statuses[(int) lm.LandmarkType].Control;
                    var newStatus = lm.ControlType;

                    if (oldStatus != newStatus &&
                        (lm.LandmarkType & (IsleOfConquestLandmarkType.HordeGates | IsleOfConquestLandmarkType.AllianceGates)) != 0)
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
        }

        #endregion

        #region Offensive

        private IsleOfConquestLandmarkType _startingBase;
        private bool _selectedStartingBase;

        private IsleOfConquestLandmarkType StartingBase
        {
            get
            {
                if (_selectedStartingBase)
                    return _startingBase;

                var random = new Random();
                var randomLogic = random.NextDouble() * 100;

                if (randomLogic > 50)
                {
                    _startingBase = IsleOfConquestLandmarkType.Docks;
                    _selectedStartingBase = true;
                    return _startingBase;
                }

                _startingBase = StyxWoW.Me.IsHorde ? IsleOfConquestLandmarkType.Workshop : IsleOfConquestLandmarkType.Hangar;
                _selectedStartingBase = true;
                return _startingBase;
            }
        }

        private Composite CreateOffensiveLogic()
        {
            return new PrioritySelector(
                ctx => AttackToKeep,
                new Decorator(
                    ret => (IsleOfConquestLandmarkType)ret != IsleOfConquestLandmarkType.Unknown,
                    new Action(ret => SetHotspot(((IsleOfConquestLandmarkType)ret).ToString(), BGBuddyResources.EnemyKeep))
                    ),
                new Decorator(
                    ret => !_selectedStartingBase,
                    new Sequence(
                        new Action(ret => SetHotspot(StartingBase.ToString(), "Start of the game")))
                    ),
                new Decorator(
                    ret => BiggestFight != WoWPoint.Zero,
                    new Action(ret => SetHotspot(BiggestFight, "Biggest fight"))
                    ),
                new ActionAlwaysSucceed()
                );
        }

        #endregion

        #region Helpers

        private IsleOfConquestLandmarkType AttackToKeep
        {
            get
            {
                var isHorde = StyxWoW.Me.IsHorde;
                foreach (var type in from landmarkInfo in Statuses.Values
                                     where landmarkInfo.Control == LandmarkControlType.Destroyed
                                     select (IsleOfConquestLandmarkType) landmarkInfo.Type)
                {
                    if ((type & IsleOfConquestLandmarkType.AllianceGates) != 0)
                    {
                        if (isHorde)
                        {
                            return IsleOfConquestLandmarkType.AllianceKeep;
                        }
                    }

                    if ((type & IsleOfConquestLandmarkType.HordeGates) != 0)
                    {
                        if (!isHorde)
                        {
                            return IsleOfConquestLandmarkType.HordeKeep;
                        }
                    }
                }
                return IsleOfConquestLandmarkType.Unknown;
            }
        }

        #endregion
    }
}
