using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Bots.BGBuddy.HeatMap;
using Bots.BGBuddy.Resources;
using Bots.BGBuddy.Helpers;
using Bots.BGBuddy.Logic.Battlegrounds;

using CommonBehaviors.Actions;
using Styx;
using Styx.Common;
using Styx.Common.Helpers;
using Styx.CommonBot;
using Styx.CommonBot.Frames;
using Styx.CommonBot.POI;
using Styx.CommonBot.Routines;
using Styx.Helpers;


using Styx.Pathing;
using Styx.TreeSharp;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;
using Action = Styx.TreeSharp.Action;
using Vector3 = Tripper.Tools.Math.Vector3;

namespace Bots.BGBuddy
{
    public enum LogicType
    {
        Attack,
        Defend
    }

    public abstract class Battleground : IDisposable
    {
        private WoWPoint _targetedAtLocation;
        private ulong _targetedGuid;

        public Battleground()
        {
            Statuses = new Dictionary<int, LandmarkInfo>();

        }

        internal static Dictionary<int, LandmarkInfo> Statuses { get; set; }

        public abstract string Name { get; }
        public abstract int MapId { get; }
        public BgBotProfile Profile { get; set; }
        public WoWPoint Hotspot { get; set; }
        public WoWPoint ActualHotspot { get; set; }
        public WoWPoint StartingLocation { get; set; }
        public WoWPoint StartingFaceLocation { get; set; }
        public MapBox StartingLocationBox { get; set; }
        public abstract Composite Logic { get; }

        public LogicType CurrentLogicType = LogicType.Attack;

        #region Heatmap Stuff

        protected virtual float EnemyPlayerHeat { get { return 0.10f; } }
        protected virtual float FriendlyPlayerHeat { get { return -0.13f; } }

        protected virtual float ObjectiveHeat { get { return 0.6f; } }
        protected virtual float ObjectiveAssaultHeat { get { return CurrentLogicType == LogicType.Attack ? ObjectiveHeat : ObjectiveHeat * 0.5f; } }
        protected virtual float ObjectiveConflictedHeat { get { return ObjectiveHeat * 1.2f; } }
        protected virtual float ObjectiveDefendHeat { get { return CurrentLogicType == LogicType.Attack ? ObjectiveHeat * 0.5f : ObjectiveHeat; } }

        protected virtual float EnemyPlayerRadius {get { return 30f; }}
        protected virtual float FriendlyPlayerRadius { get { return 30f; } }
        protected virtual float ObjectiveRadius { get { return 60f; } }

        internal void UpdateHeatmap()
        {
            Heatmap.Clear();

            foreach (var li in Statuses.Values)
            {
                if (li.ControlledByEnemy)
                {
                    Heatmap.Add(li, ObjectiveRadius, ObjectiveAssaultHeat);
                }
                else if (li.ControlledByUs)
                {
                    Heatmap.Add(li, ObjectiveRadius, ObjectiveDefendHeat);
                }
                else if (li.Control != LandmarkControlType.Destroyed)
                {
                    Heatmap.Add(li, ObjectiveRadius, ObjectiveConflictedHeat);
                }
            }


            foreach (WoWObject o in ObjectManager.GetObjectsOfType<WoWObject>(true, true))
            {
                try
                {
                    if (o is WoWPlayer)
                    {
                        if (o.IsMe)
                            continue;

                        var p = o as WoWPlayer;
                        if (p.IsFriendly)
                            Heatmap.Add(p, FriendlyPlayerRadius, FriendlyPlayerHeat);
                        else
                            Heatmap.Add(p, EnemyPlayerRadius, EnemyPlayerHeat);
                    }
                }
                catch
                {
                    // If there's any error adding a heatpoint, then just eat it. Thanks!
                }
            }
        }

        private IEnumerable<ObjectiveHeatpoint> GetHotpoints()
        {
            const float heatMin = -1.0f;

            var points = (from hp in Heatmap.HeatPoints.Where(o => o is ObjectiveHeatpoint)
                          let heat = hp.GetHeatAverage()
                          where heat >= heatMin
                          select hp as ObjectiveHeatpoint).ToList();

            var myLoc = StyxWoW.Me.Location;
            var myLoc2D = new Vector2(myLoc.X, myLoc.Y);
            return points.OrderByDescending(p => p.GetHeatAverage()).ThenBy(p => myLoc2D.DistanceSqr(new Vector2(p.X, p.Y))).ToList();
        }

        protected LandmarkInfo GetHotBox()
        {
            IEnumerable<ObjectiveHeatpoint> points = GetHotpoints();

            Dictionary<int, LandmarkInfo>.ValueCollection boxes = Statuses.Values;

            var ret = points.Select(hp => boxes.FirstOrDefault(b => b.Box.Contains(new Tripper.Tools.Math.Vector2(hp.X, hp.Y)))).FirstOrDefault(box => box != null);
            return ret;
        }

        #endregion

        public BattlegroundSide Side
        {
            get
            {
                if (StyxWoW.Me.CurrentMap.MapId == 607)
                {
                    return IsAttacking ? BattlegroundSide.Attack : BattlegroundSide.Defend;
                }

                return StyxWoW.Me.IsHorde ? BattlegroundSide.Horde : BattlegroundSide.Alliance;
            }
        }
        public bool IsAtHotspot { get { return StyxWoW.Me.Location.DistanceSqr(Hotspot) < 30 * 30; } }

        protected bool IsAttacking
        {
            get
            {
                if (StyxWoW.Me.IsHorde)
                    return
                        Statuses.Values.All(s => (StrandOfTheAncientsLandmarkType) s.Type != StrandOfTheAncientsLandmarkType.HordeDefense);

                return Statuses.Values.All(s => (StrandOfTheAncientsLandmarkType) s.Type != StrandOfTheAncientsLandmarkType.AllianceDefense);
            }
        }

        protected bool NeedToDropCurrentTarget { get { return _targetedAtLocation != WoWPoint.Zero && StyxWoW.Me.CurrentTarget != null && StyxWoW.Me.CurrentTarget.Entry != 28781 && StyxWoW.Me.CurrentTargetGuid == _targetedGuid && StyxWoW.Me.CurrentTarget.Location.DistanceSqr(_targetedAtLocation) > 50 * 50; } }

        #region IDisposable Members

        public abstract void Dispose();

        #endregion

        public abstract void Start();

        public string NonLocalizedName
        {
            get
            {
                switch (MapId)
                {
                    case 529:
                        return "Arathi Basin";
                    case 30:
                        return "Alterac Valley";
                    case 761:
                        return "Battle For Gilneas";
                    case 566:
                        return "Eye of the Storm";
                    case 628:
                        return "Isle of Conquest";
                    case 607:
                        return "Strand of the Ancients";
                    case 726:
                        return "Twin Peaks";
                    case 489:
                        return "Warsong Gulch";
                    case 727:
                        return "Silvershard Mines";
                    case 998:
                        return "Temple of Kotmogu";
                    case 1105:
                        return "Deepwind Gorge";
                    default:
                        return "";
                }
            }
        }

        public void LoadProfile()
        {
            Profile = BgBotProfile.Load(Utilities.AssemblyDirectory + "\\Default Profiles\\BGBuddy\\" + NonLocalizedName + ".xml");

            if (Profile.Boxes.Any(kvp => kvp.Value.Any(box => box.Center == Vector3.Zero)))
            {
                throw new Exception(string.Format("Invalid profile loaded for battleground {0}!", NonLocalizedName));
            }

            if (Profile.StartLocations.ContainsKey(Side))
                StartingLocation = Profile.StartLocations[Side];
            else
                StartingLocation = WoWPoint.Zero;

            if (Profile.StartFaceLocations.ContainsKey(Side))
                StartingFaceLocation = Profile.StartFaceLocations[Side];
            else
                StartingFaceLocation = WoWPoint.Zero;

            StartingLocationBox = GetLandmarkBox("StartLocation");

            if (StartingLocationBox.TopLeft != (Vector3)WoWPoint.Zero && StartingLocationBox.BottomRight != (Vector3)WoWPoint.Zero)
            {
                var random = new Random();
                var randomX = 0;
                var randomY = 0;

                if((int)StartingLocationBox.TopLeft.X > (int)StartingLocationBox.BottomRight.X)
                    randomX = random.Next((int)StartingLocationBox.BottomRight.X, (int)StartingLocationBox.TopLeft.X);
                else
                    randomX = random.Next((int)StartingLocationBox.TopLeft.X, (int)StartingLocationBox.BottomRight.X);

                if ((int)StartingLocationBox.TopLeft.Y > (int)StartingLocationBox.BottomRight.Y)
                    randomY = random.Next((int)StartingLocationBox.BottomRight.Y, (int)StartingLocationBox.TopLeft.Y);
                else
                    randomY = random.Next((int)StartingLocationBox.TopLeft.Y, (int)StartingLocationBox.BottomRight.Y);


                Vector3 newStartLocation = new Vector3(randomX, randomY, 0);
                Navigator.FindHeight(ref newStartLocation);

                StartingLocation = newStartLocation;

                // Rectangle newStartingBox = new Rectangle((int)StartingLocationBox.TopLeft.X, (int)StartingLocationBox.TopLeft.Y, (int)StartingLocationBox.BottomRight.X, (int)StartingLocationBox.BottomRight.Y);

                // newStartingBox.
            }

        }

        private readonly WaitTimer _randomizationTimer = new WaitTimer(TimeSpan.FromSeconds(1));

        public WoWPoint RandomizeLocation(WoWPoint pt)
        {
            return RandomizeLocation(pt, 10);
        }

        public WoWPoint RandomizeLocation(WoWPoint pt, int maxDistance)
        {
            if (pt == WoWPoint.Zero)
                return pt;

            _randomizationTimer.Reset();
            var rand = new Random();
            for (;;)
            {
                if (_randomizationTimer.IsFinished)
                    return pt;

                double degrees = rand.NextDouble() * 360;
                int distance = rand.Next(0, maxDistance);
                WoWPoint endLocation = pt.RayCast(WoWMathHelper.DegreesToRadians((float)degrees), distance);

                var zValues = Navigator.FindHeights(endLocation.X, endLocation.Y);

                if (zValues.Count == 0)
                    continue;

                var newZ = zValues.OrderBy(v => Math.Abs(v - endLocation.Z)).First();

                endLocation.Z = newZ;

                if (!Navigator.CanNavigateFully(StyxWoW.Me.Location, endLocation))
                    continue;

                return endLocation;
            }
        }

        protected void TargetFirstTarget()
        {
            WoWUnit unit = Targeting.Instance.FirstUnit;

            _targetedGuid = unit.Guid;
            _targetedAtLocation = unit.Location;
            if (StyxWoW.Me.CurrentTargetGuid != _targetedGuid)
                unit.Target();

            BotPoi.Current = new BotPoi(unit, PoiType.Kill);
        }

        protected void ClearTarget()
        {
            BotPoi.Clear(BGBuddyResources.Battleground_ClearTarget_Target_being_cleared_);
            StyxWoW.Me.ClearTarget();
            _targetedGuid = 0;
            _targetedAtLocation = WoWPoint.Zero;
        }

        protected virtual Composite CreateBeforePrepBehavior()
        {
            return new PrioritySelector();
        }

        internal bool ReachedLocation;

        public Composite CreateCommonLogic
        {
            get
            {
                return
                    new PrioritySelector(
                        CreateBeforePrepBehavior(),
                        new Decorator(
                            ret => StyxWoW.Me.HasAura("Preparation"),
                            new Sequence(
                                new PrioritySelector(
                                   // new Decorator(
                                   //     ret => StyxWoW.Me.MapId != 607,
                                   //     BGBuddy.Instance.CreateTakeConsumablesBehavior()),
                                    RoutineManager.Current.RestBehavior,
                                    RoutineManager.Current.PreCombatBuffBehavior,
                                    new Decorator(
                                        ret => !StyxWoW.Me.Mounted && Mount.MountUp(() => true, () => StartingLocation),
                                        new ActionAlwaysSucceed()),
                                    new Decorator(
                                        ret => StartingLocation != WoWPoint.Zero && StartingFaceLocation != WoWPoint.Zero && !StyxWoW.Me.IsMoving && !StyxWoW.Me.IsOnTransport && StartingLocation.Distance(StyxWoW.Me.Location) <= 3 && !WoWMathHelper.IsFacing(StyxWoW.Me.Location, StyxWoW.Me.Rotation, StartingFaceLocation, 0.5f),
                                          new Sequence(
                                             new Action(ret => StyxWoW.Me.SetFacing(StartingFaceLocation)),
                                             new Action(ret => ReachedLocation = true))),
                                    new Decorator(
                                        ret => StartingLocation != WoWPoint.Zero && !StyxWoW.Me.IsOnTransport && !ReachedLocation,
                                        new Sequence(
                                            new Action(ret => Logger.Write("Moving to start location {0}", StartingLocation)),
                                            BGBuddy.CreateMoveToLocationBehavior(ret => StartingLocation, true, 3))),
                                    new ActionAlwaysSucceed()
                                    ),
                                new Action(ret => StartOfGameTimer.Reset()))),
                        CreateTargetingSanityChecks(),
                        CreateCombatBehavior(),
                        //CreateLootInsigniaBehavior(),
                        // We don't want to be flag carriers in ctf bgs right ? WSG - 489 / TP - 726
                        new Decorator(
                            ret => MapId != 489 && MapId != 726,
                            CreateInteractFlagsBehavior()),

                        new Action(ret => { UpdateHeatmap(); return RunStatus.Failure; }),
                        new Action(ret =>
                                       {
                                           foreach (var landmark in Statuses.Values.Where(
                                               s => DateTime.Now.Subtract(s.LastProcessed) >= TimeSpan.FromSeconds(5)))
                                           {
                                               landmark.Process();
                                           }
                                           return RunStatus.Failure;
                                       }));
            }
        }

        protected Composite CreateLootInsigniaBehavior()
        {
            return
                new PrioritySelector(
                    ctx => (from o in ObjectManager.GetObjectsOfType<WoWObject>(true)
                                             let player = o.ToPlayer()
                                             let corpse = o as WoWCorpse
                                             where ((player != null && player.IsHostile && player.CanLoot) ||
                                                    (corpse != null && corpse.Lootable && !corpse.IsOnlyBones)) &&
                                                   o.DistanceSqr < 20*20
                                             orderby o.DistanceSqr
                                             select o).FirstOrDefault(),
                    new Decorator(
                        ret => ret != null && BGBuddySettings.Instance.LootCorpses && Targeting.Instance.FirstUnit == null,
                        new PrioritySelector(
                            new Decorator(
                                ret => ((WoWCorpse)ret).DistanceSqr < 3*3,
                                new Sequence(
                                    new DecoratorContinue(
                                        ret => StyxWoW.Me.IsMoving,
                                        new Action(ret => Navigator.PlayerMover.MoveStop())),
                                    new WaitContinue(
                                        1, ret => !StyxWoW.Me.IsMoving, new ActionAlwaysSucceed()),
                                    new Action(ret => ((WoWCorpse)ret).Interact()),
                                    new WaitContinue(
                                        2, ret => LootFrame.Instance.IsVisible,
                                        new Action(ret => LootFrame.Instance.LootAll())),
                                    new Action(ret => Navigator.Clear()))),
                            new Sequence(
                                new Action(ret => Logger.Write(BGBuddyResources.Battleground_CreateLootInsigniaBehavior_Moving_to_loot_the_player_corpse)),
                                BGBuddy.CreateMoveToLocationBehavior(ret => ((WoWCorpse)ret).Location, true, 2f))
                            ))
                    );
        }

        private static readonly HashSet<uint> PicnicBasketIds = new HashSet<uint> { 187267 };
        protected Composite CreateInteractFlagsBehavior()
        {
            return
                new PrioritySelector(
                    ctx =>
                    ObjectManager.GetObjectsOfType<WoWGameObject>(false, false).FirstOrDefault(
                        o => (o.SubType == WoWGameObjectType.Button || o.SubType == WoWGameObjectType.Goober) &&
                             !PicnicBasketIds.Contains(o.Entry) && o.CanUse() && o.DistanceSqr < 35 * 35),
                    new Decorator(
                        ret => ret != null && Targeting.Instance.FirstUnit == null && ObjectManager.GetObjectsOfType<WoWUnit>().Count(o => (o.CastingSpellId == 98322 && o.Distance < 30)) == 0,
                        new PrioritySelector(
                            new Decorator(
                                ret => ((WoWGameObject)ret).WithinInteractRange,
                                new Sequence(
                                    new DecoratorContinue(
                                        ret => StyxWoW.Me.IsMoving,
                                        new Action(ret => Navigator.PlayerMover.MoveStop())),
                                    new WaitContinue(
                                        1, ret => !StyxWoW.Me.IsMoving, new ActionAlwaysSucceed()),
                                    new Action(ret => ((WoWGameObject)ret).Interact()),
                                    new WaitContinue(
                                        1, ret => StyxWoW.Me.IsCasting, new ActionAlwaysSucceed()),
                                    new Action(ret => Navigator.Clear()),
                                    new WaitContinue(
                                        10, ret => !StyxWoW.Me.IsCasting, new ActionAlwaysSucceed()))),
                            new Sequence(
                                new Action(ret => Logger.Write(BGBuddyResources.Battleground_CreateInteractFlagsBehavior_Moving_to_interact_with_flag)),
                                BGBuddy.CreateMoveToLocationBehavior(ret => ((WoWGameObject)ret).Location, true, 3f))
                            )));
        }

        protected Composite CreateTargetingSanityChecks()
        {
            return new PrioritySelector(
                // Sanity checks

                // #1 Make sure the target isn't dead.
                new Decorator(
                    ret => StyxWoW.Me.CurrentTarget != null && StyxWoW.Me.CurrentTarget.IsDead,
                    new Sequence(
                        new Action(ret => ClearTarget()),
                        new WaitContinue(
                            2, ret => StyxWoW.Me.CurrentTargetGuid == 0, new ActionAlwaysSucceed()))),
                // #2 make sure we're not being kited far away
                new Decorator(
                    ret => NeedToDropCurrentTarget,
                    new Sequence(
                        new Action(
                            ret =>
                                {
                                    Logger.Write(BGBuddyResources.Battleground_CreateTargetingSanityChecks_We_ve_gotten_to_far_away_from_the_original_target_spot__Blacklisting_target_for_5s_and_clearing_);
                                    Blacklist.Add(StyxWoW.Me.CurrentTarget, BlacklistFlags.Combat, TimeSpan.FromSeconds(5), "We are too far from the original target spot.");
                                    ClearTarget();
                                }),
                        new WaitContinue(
                            2, ret => StyxWoW.Me.CurrentTargetGuid == 0, new ActionAlwaysSucceed()))),
                // #3 make sure we actually have a target...
                new Decorator(
                    ret => (StyxWoW.Me.SpecType != SpecType.Healer || StyxWoW.Me.Combat) &&
                           Targeting.Instance.FirstUnit != null &&
                           (_targetedGuid != Targeting.Instance.FirstUnit.Guid || BotPoi.Current.Type != PoiType.Kill) &&
                           Navigator.CanNavigateFully(StyxWoW.Me.Location, Targeting.Instance.FirstUnit.Location),
                    new Sequence(
                        new Action(ret => TargetFirstTarget()),
                        new WaitContinue(
                            2, ret => StyxWoW.Me.CurrentTargetGuid == _targetedGuid, new ActionAlwaysSucceed()))),
                // #4 Make sure we clear the "Kill" POI if there is no target.
                new Decorator(
                    ret => Targeting.Instance.FirstUnit == null && BotPoi.Current.Type == PoiType.Kill,
                    new Sequence(
                        new Action(ret => ClearTarget()),
                        new ActionClearPoi("BGBuddy Sanity Check #4 [No Target+Type=Kill]"),
                        new WaitContinue(
                            2, ret => StyxWoW.Me.CurrentTargetGuid == 0, new ActionAlwaysSucceed())))
                );
        }

        protected Composite CreateCombatBehavior()
        {
            return new PrioritySelector(
                // Custom combat handler. Don't do more than we actually need to.
                new Decorator(
                    ret => !StyxWoW.Me.Combat,
                    new PrioritySelector(
                        RoutineManager.Current.RestBehavior,
                        RoutineManager.Current.PreCombatBuffBehavior
                        )),

                // Should call HealBehavior no matter what while we are in combat.
                new Decorator(
                    ret => StyxWoW.Me.Combat,
                    new PrioritySelector(
                        RoutineManager.Current.HealBehavior,
                        RoutineManager.Current.CombatBuffBehavior)),

                new Decorator(
                    ret => BotPoi.Current.Type == PoiType.Kill,
                    new PrioritySelector(
                        new Decorator(
                            ret => !StyxWoW.Me.Combat && StyxWoW.Me.GotTarget,
                            new PrioritySelector(
                                new Decorator(
                                    ret => StyxWoW.Me.CurrentTarget.Distance <= BGBuddySettings.Instance.PullDistance,
                                    new PrioritySelector(
                                        RoutineManager.Current.PullBuffBehavior,
                                        RoutineManager.Current.PullBehavior)),
                                BGBuddy.CreateMoveToLocationBehavior(ret => StyxWoW.Me.CurrentTarget.Location, true, 2f)
                                )),
                        new Decorator(
                            ret => StyxWoW.Me.GotTarget && StyxWoW.Me.Combat,
                            new PrioritySelector(
                                RoutineManager.Current.CombatBehavior
                                )),
                        new ActionAlwaysSucceed()))
                );
        }

        protected RunStatus SetHotspot(string type, string reason)
        {
            return SetHotspot(type, true, reason);
        }

        protected RunStatus SetHotspot(string type, bool randomize, string reason)
        {
            Logging.WriteDiagnostic("Hotspot: " + type + ", " + reason);
            //return RunStatus.Success;

            // Reset the actual hotspot if the poi is changed/cleared so we can set it again.
            if (BotPoi.Current.Type != PoiType.Hotspot)
            {
                ActualHotspot = WoWPoint.Zero;
            }

            if (!Profile.Boxes.ContainsKey(Side))
            {
                return RunStatus.Failure;
            }

            MapBox box = Profile.Boxes[Side].FirstOrDefault(b => b.Name == type);

            if (box.Center == (Vector3)WoWPoint.Zero)
            {
                return RunStatus.Failure;
            }

            if (BotPoi.Current.Type == PoiType.Hotspot)
            {
                // Don't override previous hotspot until we reach to it
                if (StyxWoW.Me.Location.DistanceSqr(ActualHotspot) > 15 * 15)
                {
                    return RunStatus.Success;
                }

                if (ActualHotspot.Equals(box.Center))
                {
                    return RunStatus.Success;
                }

                //// Don't switch too often!
                //if (!FightTimer.IsFinished)
                //{
                //    return RunStatus.Success;
                //}
            }

            FightTimer.Reset();
            ActualHotspot = box.Center;
            Hotspot = randomize ? RandomizeLocation(box.Center) : (WoWPoint)box.Center;
            BotPoi.Current = new BotPoi(Hotspot, PoiType.Hotspot);

            Logger.Write(string.Format(BGBuddyResources.Battleground_SetHotspot_Moving_to__0___Reason___1__, box.Name, reason));
            return RunStatus.Success;
        }

        internal static readonly WaitTimer StartOfGameTimer = new WaitTimer(TimeSpan.FromSeconds(10));
        internal static readonly WaitTimer FightTimer = new WaitTimer(TimeSpan.FromSeconds(10));

        protected RunStatus SetHotspot(WoWPoint location)
        {
            return SetHotspot(location, BGBuddyResources.Battleground_SetHotspot_biggest_fight);
        }

        protected RunStatus SetHotspot(WoWPoint location, string reason)
        {
            return SetHotspot(location, false, reason);
        }

        protected RunStatus SetHotspot(WoWPoint location, float overrideDistance, string reason)
        {
            return SetHotspot(location, overrideDistance, true, reason);
        }

        protected RunStatus SetHotspot(WoWPoint location, bool ignoreTimer, string reason)
        {
            return SetHotspot(location, 30f, ignoreTimer, reason);
        }

        protected RunStatus SetHotspot(WoWPoint location, float overrideDistance, bool ignoreTimer, string reason)
        {
            // sanity check.
            if (location == WoWPoint.Zero)
            {
                return RunStatus.Failure;
            }

            if (BotPoi.Current.Type == PoiType.Hotspot)
            {
                // Don't override previous hotspot until we reach to it
                if (StyxWoW.Me.Location.Distance2DSqr(ActualHotspot) > 20 * 20)
                {
                    return RunStatus.Success;
                }

                //// Don't switch too often!
                //if (!ignoreTimer && !FightTimer.IsFinished)
                //{
                //    return RunStatus.Success;
                //}

                // Don't keep changing the location if the last battle was close.
                if (ActualHotspot.DistanceSqr(location) < overrideDistance * overrideDistance)
                {
                    return RunStatus.Success;
                }
            }

            FightTimer.Reset();
            ActualHotspot = location;
            // We don't want to randomize closest friendly pack at start of the game to move with the pack.
            Hotspot = StartOfGameTimer.IsFinished ? RandomizeLocation(location) : location;
            BotPoi.Current = new BotPoi(Hotspot, PoiType.Hotspot);
            Logger.Write(string.Format(BGBuddyResources.Battleground_SetHotspot_Moving_to__0_, reason));
            return RunStatus.Success;
        }

        protected abstract int BattlegroundRaidSize { get; }

        public int MaxPlayersInGroup
        {
            get
            {
                switch (BattlegroundRaidSize)
                {
                    // These are CTF maps (except BFG).
                        // BFG will have a smaller group size to ensure we have some people roaming around.
                    case 10:
                        if (NonLocalizedName == "Battle for Gilneas")
                            return 4;

                        // Having a "bigger" group in these is fine, since we're ferrying flags around.
                        return 6;

                    // SotA, EotS, AB, BFG
                    case 15:
                        // Should never have more than 4 in a group at any time. The rest should be running around capturing
                        // or defending.
                        return 4;

                    case 20:
                        // Are there any 20 player maps?
                        return 6;

                    case 40:
                        // 1/4th of the BG can be in any given spot. This is only AV/IoC, so this really shouldn't need to be used unless there's really nothing else to do.
                        // This is mainly for capture points, not PVE objectives!
                        return 10;

                        // Should never be hit.
                    default:
                        return 0;
                }
            }
        }

        private static int GCD(int a, int b)
        {
            return b == 0 ? a : GCD(b, a % b);
        }

        protected void GetPlayerRatios(LandmarkInfo info, out double enemy, out double friendly, int enemyMod = 0,
            int friendlyMod = 0)
        {
            var enemyCount = info.EnemyPlayersAround + enemyMod;
            var friendlyCount = info.FriendlyPlayersAround + friendlyMod;

            var gcd = (double) GCD(enemyCount, friendlyCount);
            enemy = enemyCount / gcd;
            friendly = friendlyCount / gcd;
        }


        protected MapBox GetLandmarkBox(string type)
        {
            if (!Profile.Boxes.ContainsKey(Side))
                return new MapBox();

            return Profile.Boxes[Side].FirstOrDefault(b => b.Name == type.ToString());
        }

        protected bool IsBoxAvailable(string type)
        {
            MapBox box = Profile.Boxes[Side].FirstOrDefault(b => b.Name == type);

            var friendlyCount = RaidHelper.GetCountWithin(box) - 3;
            var enemyCount = RaidHelper.GetEnemyCountWithin(box);

            if (enemyCount > friendlyCount)
                return true;

            return false;
        }


        protected LandmarkInfo ClosestConflicted
        {
            get
            {
                return
                    Statuses.Values.OrderBy(s => s.Box.Center.Distance(StyxWoW.Me.Location)).FirstOrDefault(
                        s => s.Control == LandmarkControlType.InConflict && s.FriendlyPlayersAround >= 2);
            }
        }

        protected LandmarkInfo ClosestInBattle
        {
            get
            {
                return
                    Statuses.Values.OrderBy(s => s.Box.Center.DistanceSqr(StyxWoW.Me.Location)).FirstOrDefault(
                        s => (!s.ControlledByUs && s.FriendlyPlayersAround >= 2) || (s.ControlledByUs && s.EnemyPlayersAround >= 2));
            }
        }

        protected LandmarkInfo ClosestToCapture
        {
            get
            {
                return
                    Statuses.Values.OrderBy(s => s.Box.Center.DistanceSqr(StyxWoW.Me.Location)).FirstOrDefault(
                        s =>
                            {
                                if (s.ControlledByEnemy)
                                {
                                    double enemyRatio, friendlyRatio;
                                    GetPlayerRatios(s, out enemyRatio, out friendlyRatio, friendlyMod:1);

                                    return friendlyRatio >= enemyRatio;
                                }
                                return false;
                            });
            }
        }

        protected LandmarkInfo ClosestToDefend
        {
            get
            {
                return
                    Statuses.Values.OrderBy(s => s.FriendlyPlayersAround)
                            .FirstOrDefault(s =>
                                {
                                    if (s.ControlledByUs)
                                    {

                                    }

                                    return s.ControlledByUs && s.FriendlyPlayersAround < 5;
                                });
            }
        }

        protected WoWPoint BiggestFight
        {
            get
            {
                var meIsHorde = StyxWoW.Me.IsHorde;
                var allPlayers = ObjectManager.GetObjectsOfType<WoWPlayer>().Where(p => p.IsAlive);
                var friendlyPlayers = allPlayers.Where(p => p.IsHorde == meIsHorde);
                var enemyPlayers = allPlayers.Where(p => p.IsHorde != meIsHorde);

                var result = WoWPoint.Zero;

                int minNearby = 1;
                WoWPlayer bestPlayer = null;
                foreach (var enemy in enemyPlayers)
                {
                    int nearby = friendlyPlayers.Count(p => p.Location.DistanceSqr(enemy.Location) <= 30*30);

                    if (nearby <= minNearby)
                        continue;

                    if (!Navigator.CanNavigateFully(StyxWoW.Me.Location, enemy.Location))
                        continue;

                    minNearby = nearby;
                    bestPlayer = enemy;
                }

                if (bestPlayer != null)
                    result = bestPlayer.Location;

                return result;
            }
        }

        protected WoWPoint BiggestFriendlyPack
        {
            get
            {
                var result = WoWPoint.Zero;

                var meIsHorde = StyxWoW.Me.IsHorde;
                var friendlyPlayers = ObjectManager.GetObjectsOfType<WoWPlayer>().Where(p => p.IsHorde == meIsHorde && p.IsAlive).ToList();
                var orderedPlayers = friendlyPlayers.OrderByDescending(p => friendlyPlayers.Count(r => r.Location.DistanceSqr(p.Location) <= 60 * 60)).ThenBy(p => p.DistanceSqr);

                foreach (var bestPlayer in orderedPlayers)
                {
                    if (!Navigator.CanNavigateFully(StyxWoW.Me.Location, bestPlayer.Location))
                        continue;

                    if (!bestPlayer.IsMoving)
                        continue;

                    result = bestPlayer.Location;
                    break;
                }

                return result;
            }
        }

        protected WoWPlayer EnemyFlagCarrier
        {
            get
            {
                var meIsHorde = StyxWoW.Me.IsHorde;
                return
                    ObjectManager.GetObjectsOfType<WoWPlayer>(false, false).FirstOrDefault(
                        p => p.IsHorde != meIsHorde && p.Auras.Keys.Any(a => a.ToLowerInvariant().Contains("flag")));
            }
        }

        protected WoWPlayer FriendlyFlagCarrier
        {
            get
            {
                var meIsHorde = StyxWoW.Me.IsHorde;
                return
                    ObjectManager.GetObjectsOfType<WoWPlayer>(false, false).FirstOrDefault(
                        p => p.IsHorde == meIsHorde && p.Auras.Keys.Any(a => a.ToLowerInvariant().Contains("flag")));
            }
        }

        protected WoWPlayer EnemyCartCarrier
        {
            get
            {
                var meIsHorde = StyxWoW.Me.IsHorde;
                return
                    ObjectManager.GetObjectsOfType<WoWPlayer>(false, false).FirstOrDefault(
                        p => p.IsHorde != meIsHorde && p.Auras.Keys.Any(a => a.ToLowerInvariant().Contains("cart")));
            }
        }

        protected WoWPlayer FriendlyCartCarrier
        {
            get
            {
                var meIsHorde = StyxWoW.Me.IsHorde;
                return
                    ObjectManager.GetObjectsOfType<WoWPlayer>(false, false).FirstOrDefault(
                        p => p.IsHorde == meIsHorde && p.Auras.Keys.Any(a => a.ToLowerInvariant().Contains("cart")));
            }
        }

        protected WoWPlayer EnemyOrbCarrier
        {
            get
            {
                var meIsHorde = StyxWoW.Me.IsHorde;
                return
                    ObjectManager.GetObjectsOfType<WoWPlayer>(false, false).FirstOrDefault(
                        p => p.IsHorde != meIsHorde && p.Auras.Keys.Any(a => a.Equals("Orb of Power")));
            }
        }

        protected WoWPlayer FriendlyOrbCarrier
        {
            get
            {
                var meIsHorde = StyxWoW.Me.IsHorde;
                return
                    ObjectManager.GetObjectsOfType<WoWPlayer>(false, false).FirstOrDefault(
                        p => p.IsHorde == meIsHorde && p.Auras.Keys.Any(a => a.Equals("Orb of Power")));
            }
        }
    }
}
