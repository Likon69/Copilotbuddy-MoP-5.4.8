using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Styx.Common;
using Styx.Common.Helpers;
using Styx.CommonBot.AreaManagement;
using Styx.CommonBot.POI;
using Styx.CommonBot.Profiles;
using Styx.CommonBot.Profiles.Quest;
using Styx.CommonBot.Routines;
using Styx.Helpers;
using Styx.Pathing;
using Styx.Resources;
using Styx.WoWInternals;
using Styx.WoWInternals.World;
using Styx.WoWInternals.WoWCache;
using Styx.WoWInternals.WoWObjects;

namespace Styx.CommonBot
{
    public class Targeting
    {
        #region Fields

        private static readonly List<string> GuiList = new List<string>();

        private static Targeting _instance;

        private IncludeTargetsFilterDelegate _includesFilter;
        private RemoveTargetsFilterDelegate _removeTargetsFilter;
        private WeighTargetsDelegate _weighTargetsDelegate;
        private bool _temporarilyIgnoreEventSettings;

        private bool _includeWorldPlayers = true;

        private static readonly WaitTimer _mountedGracePeriod = new WaitTimer(TimeSpan.FromSeconds(4.0));

        private TimeCachedValue<List<Styx.WoWInternals.Quest.QuestObjective>> _questObjectivesCache;

        protected static StyxLog _targetingLog;

        #region Bugged Mobs

        private static readonly HashSet<uint> BuggedMobs = new HashSet<uint>
        {
            28093, // Sholazar Tickbird
            22979, // Wild Sparrowhawk
            24196, // Trapped Animal
            13358, // Stormpike Bowman
            23837, // ELM General Purpose Bunny
            13358, // Stormpike Bowman
            9157,  // Bloodpetal Pest
            17407, // Felmist          // <Mote of Shadow>
            17378, // Swamp Gas        // <Mote of Water>
            17408, // Arcane Vortex    // <Mote of Mana>
            24222, // Windy Cloud      // <Mote of Air>
            32544, // Steam Cloud      // <Crystalized Fire> and <Crystalized Water>
            32522, // Cinder Cloud     // <Crystalized Fire>
            24879, // Arctic Cloud     // <Crystalized Air> and <Crystalized Water>
            25040, // Greater Water Elemental
            23537, // Headless Horseman
            18729, // Infernal Rain (Hellfire)
            55659, // Wild Imp
            15352, // Greater Earth Elemental(Shaman)
            30230, // Risen Ally(DK)
            57783, // Ghost Iron Dragonling
        };

        #endregion

        #region Ignore TappedBy

        private static readonly HashSet<uint> IgnoreTappedBy = new HashSet<uint>
        {
            38915, // Hyjal Warden
            43427, // Furious Hyjal Warden
        };

        #endregion

        #region Citizen's of new Avalon

        private static readonly HashSet<uint> CitizensOfNewAvalon = new HashSet<uint>
        {
            28577,
            28557,
            28576,
            28560,
            28559,
            28941,
            28942
        };

        #endregion

        #endregion

        #region Constructors

        public Targeting()
        {
            AllowEvents = true;
            DisplayTargetingExceptions = true;

            ObjectList = new List<WoWObject>();
            MaxTargets = 5;

            Initialize();
        }

        #endregion

        #region Properties

        protected bool AllowEvents { get; set; }

        /// <summary>
        /// Should we print out targeting exceptions? [Default: true]
        /// </summary>
        public bool DisplayTargetingExceptions { get; set; }

        public static Targeting Instance { get { return _instance ?? (_instance = new Targeting()); } set { _instance = value; } }

        public WoWUnit FirstUnit { get { return ObjectList.Count != 0 ? ObjectList[0] as WoWUnit : null; } }

        /// <summary>
        /// The current list of viable targets.
        /// </summary>
        public List<WoWUnit> TargetList { get { return ObjectList.ConvertAll(o => o.ToUnit()); } }

        /// <summary>
        /// This is the actual list of objects held by the targeting system. Any forward-facing API should use a separate property using the ConvertAll() function.
        /// </summary>
        protected List<WoWObject> ObjectList { get; private set; }

        /// <summary>
        /// The 'limit' on the number of possible targets at any one time. [Default: 5]
        /// </summary>
        public int MaxTargets { get; set; }

        /// <summary>
        /// Include players in non-battlegrounds/arenas? [Default: true]
        /// </summary>
        public bool IncludeWorldPlayers
        {
            get { return _includeWorldPlayers; }
            set { _includeWorldPlayers = value; }
        }

        /// <summary>
        /// Include Elites? [Default: false]
        /// </summary>
        public bool IncludeElites { get; set; }

        public bool KillBetweenHotspots { get { return GlobalSettings.Instance.KillBetweenHotspots; } }

        /// <summary>
        /// Pulldistance used by hb for pulling evil npc's
        /// </summary>
        public static double PullDistance
        {
            get
            {
                if (RoutineManager.Current == null)
                    return 45.0;

                double? pullDistance = RoutineManager.Current.PullDistance;
                if (!pullDistance.HasValue)
                    return 45.0;

                return pullDistance.Value;
            }
        }

        public static double PullDistanceSqr
        {
            get
            {
                double pullDistance = PullDistance;
                return pullDistance * pullDistance;
            }
        }

        /// <summary>
        /// The distance from ourselves, where targets will be collected. [Default: 100]
        /// </summary>
        public static double CollectionRange
        {
            get
            {
                GrindArea area = StyxWoW.AreaManager != null ? StyxWoW.AreaManager.CurrentGrindArea : null;
                if (area != null && area.MaxDistance.HasValue && area.MaxDistance.Value > 10)
                    return area.MaxDistance.Value;

                Profile currentProfile = ProfileManager.CurrentProfile;
                if (currentProfile != null && currentProfile.TargetingDistance.HasValue)
                    return currentProfile.TargetingDistance.Value;

                return 100d;
            }
        }

        public static bool EnableLogging { get; set; }

        private List<Styx.WoWInternals.Quest.QuestObjective> QuestObjectives
        {
            get
            {
                if (_questObjectivesCache == null)
                {
                    _questObjectivesCache = new TimeCachedValue<List<Styx.WoWInternals.Quest.QuestObjective>>(
                        TimeSpan.FromSeconds(1.0),
                        () => StyxWoW.Me.QuestLog.GetAllQuests().SelectMany(q => q.GetObjectives()).ToList());
                }

                return _questObjectivesCache.Value;
            }
        }

        #endregion

        #region Events

        /// <summary>
        /// Occurs when the target list has been fully updated.
        /// </summary>
        public event TargetListUpdateFinishedDelegate OnTargetListUpdateFinished;

        /// <summary>
        /// Occurrs when a request is made to include targets in the targeting list.
        /// </summary>
        public event IncludeTargetsFilterDelegate IncludeTargetsFilter
        {
            add
            {
                if (AllowEvents || _temporarilyIgnoreEventSettings)
                {
                    _includesFilter += value;
                }
            }
            remove
            {
                if (AllowEvents || _temporarilyIgnoreEventSettings)
                {
                    _includesFilter -= value;
                }
            }
        }

        /// <summary>
        /// Occurs when a request is made to remove targets in the targeting list.
        /// </summary>
        public event RemoveTargetsFilterDelegate RemoveTargetsFilter
        {
            add
            {
                if (AllowEvents || _temporarilyIgnoreEventSettings)
                {
                    _removeTargetsFilter += value;
                }
            }
            remove
            {
                if (AllowEvents || _temporarilyIgnoreEventSettings)
                {
                    _removeTargetsFilter -= value;
                }
            }
        }

        /// <summary>
        /// Occurs when a request is made to weigh the targets in the targeting list.
        /// </summary>
        public event WeighTargetsDelegate WeighTargetsFilter
        {
            add
            {
                if (AllowEvents || _temporarilyIgnoreEventSettings)
                {
                    _weighTargetsDelegate += value;
                }
            }
            remove
            {
                if (AllowEvents || _temporarilyIgnoreEventSettings)
                {
                    _weighTargetsDelegate -= value;
                }
            }
        }

        #endregion

        #region Methods

        private void Initialize()
        {
            _temporarilyIgnoreEventSettings = true;
            IncludeTargetsFilter += DefaultIncludeTargetsFilter;
            RemoveTargetsFilter += DefaultRemoveTargetsFilter;
            WeighTargetsFilter += DefaultTargetWeight;
            _temporarilyIgnoreEventSettings = false;
        }

        /// <summary>
        /// Clears the list of targets
        /// </summary>
        public void Clear()
        {
            if (ObjectList != null)
            {
                ObjectList.Clear();
            }
        }

        protected virtual List<WoWObject> GetInitialObjectList()
        {
            if (Battlegrounds.IsInsideBattleground)
            {
                return ObjectManager.GetObjectsOfType<WoWObject>(true, true).Where(o => o is WoWPlayer).ToList();
            }

            return IncludeWorldPlayers
                       ? ObjectManager.GetObjectsOfType<WoWObject>(true, true).Where(o => o is WoWUnit).ToList()
                       : ObjectManager.GetObjectsOfType<WoWObject>(true, true).Where(o => o is WoWUnit && !(o is WoWPlayer)).ToList();
        }

        public virtual void Pulse()
        {
            try
            {
                using (StyxWoW.Memory.AcquireFrame())
                {
                    if (StyxWoW.Me == null)
                    {
                        return;
                    }

                    if (!StyxWoW.IsInGame)
                    {
                        return;
                    }

                    List<WoWObject> objects = GetInitialObjectList();

                    InvokeEvent(_removeTargetsFilter, objects);

                    var finalObjects = new HashSet<WoWObject>();
                    InvokeEvent(_includesFilter, objects, finalObjects);

                    List<TargetPriority> prioObjects = finalObjects.Select(unit => new TargetPriority { Object = unit, Score = 0.0d }).ToList();

                    InvokeEvent(_weighTargetsDelegate, prioObjects);

                    prioObjects = prioObjects.OrderByDescending(p => p.Score).Take(MaxTargets).ToList();
                    ObjectList = prioObjects.Select(p => p.Object).ToList();

                    GuiList.Clear();

                    GrindArea curGrindArea = StyxWoW.AreaManager.CurrentGrindArea;
                    foreach (TargetPriority prio in prioObjects.Take(MaxTargets).ToList())
                    {
                        GuiList.Add(string.Format(StyxResources._0__Dist___1_, prio.Object.Name, Math.Ceiling(prio.Object.Distance)));
                    }

                    try
                    {
                        if (OnTargetListUpdateFinished != null)
                        {
                            OnTargetListUpdateFinished(GuiList);
                        }
                    }
                    catch
                    {
                    }
                }
            }
            catch (ThreadAbortException)
            {
            }
            catch (Exception e)
            {
                if (DisplayTargetingExceptions)
                {
                    Logging.WriteException(e);
                }
            }
        }

        private void InvokeEvent(Delegate d, params object[] args)
        {
            if (d == null)
            {
                return;
            }

            foreach (Delegate d2 in d.GetInvocationList())
            {
                try
                {
                    d2.DynamicInvoke(args);
                }
                catch (Exception ex)
                {
                    if (DisplayTargetingExceptions)
                    {
                        Logging.WriteException(ex);
                    }
                }
            }
        }

        public static int GetAggroOnMeWithin(WoWPoint position, float range)
        {
            range *= range;
            return ObjectManager.GetObjectsOfType<WoWUnit>(true)
                .Count(u => u.Location.DistanceSqr(position) < range
                    && u.IsAlive
                    && u.Attackable
                    && u.IsHostile
                    && (u.IsTargetingMeOrPet || u.IsTargetingAnyMinion || u.TappedByAllThreatLists));
        }

        public static int GetAggroWithin(WoWPoint position, float range)
        {
            range *= range;
            return ObjectManager.GetObjectsOfType<WoWUnit>(true)
                .Count(u => u.Location.DistanceSqr(position) < range
                    && u.IsAlive
                    && u.Attackable
                    && u.IsHostile);
        }

        #endregion

        #region Logging

        protected static void Log(string message)
        {
            if (!EnableLogging)
            {
                return;
            }

            if (_targetingLog == null)
            {
                _targetingLog = StyxWoW.GetLog("Targeting");
            }

            _targetingLog.Write(message);
        }

        protected static void Log(string format, params object[] args)
        {
            if (!EnableLogging)
            {
                return;
            }

            Log(string.Format(format, args));
        }

        protected static void LogRemoval(WoWObject obj, string reason)
        {
            if (!EnableLogging)
            {
                return;
            }

            Log("Removed {0} - {1}", obj != null ? obj.Name : "(null)", reason);
        }

        #endregion

        #region Default Filters

        /// <summary>
        /// Default target filter for all bots.
        /// Removes all dead, flying, non-attackble mobs etc..
        /// </summary>
        protected virtual void DefaultRemoveTargetsFilter(List<WoWObject> units)
        {
            Blacklist.Flush();

            double collectionRangeSqr = CollectionRange * CollectionRange;
            Profile curProfile = ProfileManager.CurrentProfile;
            bool isInCombat = StyxWoW.Me.Combat;
            WoWPoint myLocation = StyxWoW.Me.Location;
            ulong poiGuid = BotPoi.Current.Guid;

            List<Styx.WoWInternals.Quest.QuestObjective> questObjectives = QuestObjectives;
            List<ObjectiveInfo> objectiveInfos = curProfile != null
                ? curProfile.Quests.SelectMany(q => q.Objectives).ToList()
                : new List<ObjectiveInfo>();

            HashSet<uint> killMobIds = new HashSet<uint>(
                questObjectives.Where(o => o.Type == Styx.WoWInternals.Quest.QuestObjectiveType.KillMob).Select(o => o.ID));
            killMobIds.UnionWith(
                objectiveInfos.Where(o => o is KillMobObjectiveInfo).Select(o => ((KillMobObjectiveInfo)o).MobID));

            HashSet<uint> collectItemIds = new HashSet<uint>(
                questObjectives.Where(o => o.Type == Styx.WoWInternals.Quest.QuestObjectiveType.CollectItem || o.Type == Styx.WoWInternals.Quest.QuestObjectiveType.CollectIntermediateItem).Select(o => o.ID));
            collectItemIds.UnionWith(
                objectiveInfos.Where(o => o is CollectItemObjectiveInfo).Select(o => ((CollectItemObjectiveInfo)o).ItemID));

            using (StyxWoW.Memory.AcquireFrame())
            {
                units.RemoveAll(o => ShouldRemoveTarget(o, isInCombat, myLocation, poiGuid, collectionRangeSqr, curProfile, killMobIds, collectItemIds));
            }
        }

        private bool ShouldRemoveTarget(WoWObject obj, bool isInCombat, WoWPoint myLocation, ulong poiGuid, double collectionRangeSqr, Profile curProfile, HashSet<uint> killMobIds, HashSet<uint> collectItemIds)
        {
            if (obj == null || !obj.IsValid || !(obj is WoWUnit))
            {
                LogRemoval(obj, "Null/Invalid/Not unit");
                return true;
            }

            if (Blacklist.Contains(obj, BlacklistFlags.Combat))
            {
                LogRemoval(obj, "Combat blacklisted");
                return true;
            }

            if (!isInCombat && Blacklist.Contains(obj, BlacklistFlags.Pull))
            {
                LogRemoval(obj, "Pull blacklisted");
                return true;
            }

            if (BuggedMobs.Contains(obj.Entry))
            {
                LogRemoval(obj, "Bugged mob");
                return true;
            }

            WoWUnit unit = obj as WoWUnit;

            if (unit.IsNonCombatPet)
            {
                LogRemoval(obj, "Non combat pet");
                return true;
            }

            if (unit.IsDead)
            {
                LogRemoval(obj, "Dead");
                return true;
            }

            if (unit.OnTaxi)
            {
                LogRemoval(obj, "OnTaxi");
                return true;
            }

            if (unit.IsFlightMaster)
            {
                LogRemoval(obj, "Flight master");
                return true;
            }

            if (!unit.CanSelect)
            {
                LogRemoval(obj, "Can't select");
                return true;
            }

            if (!unit.Attackable)
            {
                LogRemoval(obj, "Not attackable");
                return true;
            }

            if (curProfile != null && (curProfile.AvoidMobs.Contains(unit.Entry) || curProfile.AvoidMobs.Contains(unit.Name)) && !killMobIds.Contains(unit.Entry))
            {
                WoWCache.CreatureCacheEntry creatureCacheEntry;
                if (!unit.GetCachedInfo(out creatureCacheEntry))
                {
                    LogRemoval(obj, "Avoid mob without cached info");
                    return true;
                }

                if (!creatureCacheEntry.QuestItems.Where(id => id > 0U).Any(collectItemIds.Contains))
                {
                    if (!creatureCacheEntry.GroupID.Where(id => id > 0U).Any(killMobIds.Contains))
                    {
                        LogRemoval(obj, "Avoid mob that isn't quest mob");
                        return true;
                    }
                }
            }

            if (unit.IsFriendly)
            {
                LogRemoval(obj, "Is friendly");
                return true;
            }

            if (unit.IsEvadeRunningBack)
            {
                LogRemoval(obj, "Evade running back");
                return true;
            }

            WoWPoint location = unit.Location;

            if (!isInCombat && BlackspotManager.IsBlackspotted(location, 0f, BlackspotQueryFlags.All))
            {
                LogRemoval(obj, "In blackspot");
                return true;
            }

            if (myLocation.DistanceSqr(location) > collectionRangeSqr && unit.Guid != poiGuid && !unit.Aggro)
            {
                LogRemoval(obj, "Out of collection range and isn't poi/aggroed");
                return true;
            }

            if (Math.Abs(myLocation.Z - location.Z) > 10f && !unit.InLineOfSight && unit.Guid != poiGuid && !unit.Aggro && !unit.PetAggro)
            {
                LogRemoval(obj, "Other level");
                return true;
            }

            return false;
        }

        public static bool IsTooNearBlackspot(IEnumerable<Blackspot> blackspots, WoWPoint point)
        {
            return blackspots != null && blackspots.Any(b => point.Distance2D(b.Location) <= b.Radius);
        }

        /// <summary>
        /// Default include target's logic
        /// makes sure we have targets in combat
        /// </summary>
        /// <param name="incomingUnits"></param>
        /// <param name="outgoingUnits"></param>
        protected virtual void DefaultIncludeTargetsFilter(List<WoWObject> incomingUnits, HashSet<WoWObject> outgoingUnits)
        {
            List<WoWUnit> minions = StyxWoW.Me.Minions;
            bool isBeingAttacked = StyxWoW.Me.IsBeingAttacked;
            bool mounted = StyxWoW.Me.Mounted;
            bool isFlying = StyxWoW.Me.IsFlying;
            WoWUnit activeMover = WoWMovement.ActiveMover;
            bool isMoving = activeMover != null && activeMover.IsMoving;
            if (mounted && isMoving)
                _mountedGracePeriod.Reset();
            bool skipNonPriority = (mounted && isMoving) || !_mountedGracePeriod.IsFinished;
            Profile currentProfile = ProfileManager.CurrentProfile;

            HashSet<uint> factionIds = new HashSet<uint>();
            List<int> mobIds = new List<int>();

            if (StyxWoW.AreaManager.CurrentGrindArea != null)
            {
                mobIds = StyxWoW.AreaManager.CurrentGrindArea.MobIDs;

                if (StyxWoW.AreaManager.CurrentGrindArea.Factions.Count > 0)
                {
                    foreach (int faction in StyxWoW.AreaManager.CurrentGrindArea.Factions)
                    {
                        factionIds.Add((uint)faction);
                    }
                }
            }
            else if (currentProfile != null)
            {
                foreach (uint faction in currentProfile.Factions)
                {
                    factionIds.Add(faction);
                }
            }

            using (StyxWoW.Memory.AcquireFrame())
            {
                foreach (WoWObject o in incomingUnits)
                {
                    bool isInsideBattleground = Battlegrounds.IsInsideBattleground;
                    WoWUnit unit = o.ToUnit();

                    if (BotPoi.Current.Type == PoiType.Kill && unit.Guid == BotPoi.Current.Guid && !unit.TaggedByOther)
                    {
                        outgoingUnits.Add(unit);
                        continue;
                    }

                    if (!isBeingAttacked || (isFlying && !mobIds.Contains((int)unit.Entry) && !factionIds.Contains(unit.FactionId)))
                    {
                        continue;
                    }

                    WoWPlayer player = o as WoWPlayer;

                    if (player != null && !isInsideBattleground && player.Combat)
                    {
                        bool isAggroingUs = player.Aggro || player.PetAggro || player.IsTargetingMeOrPet
                            || (player.GotTarget && minions.Any(m => m.Guid == player.CurrentTargetGuid));

                        if (isAggroingUs)
                        {
                            outgoingUnits.Add(player);
                            continue;
                        }
                    }

                    if (player != null)
                    {
                        continue;
                    }

                    if ((mobIds.Contains((int)unit.Entry) || factionIds.Contains(unit.FactionId) || !skipNonPriority
                            || (GlobalSettings.Instance.KillBetweenHotspots && unit.Difficulty != DifficultyColor.Gray))
                        && !unit.IsCritter)
                    {
                        if (!unit.Aggro && !unit.PetAggro && (!unit.Fleeing || !unit.TaggedByMe)
                            && (!unit.GotTarget || !minions.Any(m => m.Guid == unit.CurrentTargetGuid)))
                        {
                            if (IsSpecialQuestMob(unit))
                            {
                                outgoingUnits.Add(unit);
                            }
                        }
                        else
                        {
                            WoWUnit ownedByRoot = unit.OwnedByRoot;
                            if (ownedByRoot != null && ownedByRoot.IsPlayer && !ownedByRoot.IsFriendly)
                            {
                                outgoingUnits.Add(ownedByRoot);
                            }

                            outgoingUnits.Add(unit);
                        }
                    }
                }
            }
        }

        private bool IsSpecialQuestMob(WoWUnit unit)
        {
            return CitizensOfNewAvalon.Contains(unit.Entry)
                && ((unit.TaggedByMe && unit.HealthPercent < 99.0)
                    || ((StyxWoW.Me.QuestLog.ContainsQuest(12678U) || StyxWoW.Me.QuestLog.ContainsQuest(12722U)) && unit.DistanceSqr < 400.0));
        }

        /// <summary>
        /// The default target weight used by all bots.
        /// Sorts players by distance, (removed the weight from the PvPBot.. seems kinda redundant)
        /// </summary>
        /// <param name="units"></param>
        protected virtual void DefaultTargetWeight(List<TargetPriority> units)
        {
            bool inCombat = StyxWoW.Me.PetInCombat || StyxWoW.Me.Combat;
            LocalPlayer me = StyxWoW.Me;

            HashSet<ulong> friendlyGuids = new HashSet<ulong> { me.Guid };
            foreach (WoWUnit minion in me.Minions)
            {
                friendlyGuids.Add(minion.Guid);
            }

            ulong poiGuid = BotPoi.Current.Guid;
            WoWPoint myLocation = me.Location;
            Profile curProfile = ProfileManager.CurrentProfile;

            bool[] hitResults = null;
            if (!Battlegrounds.IsInsideBattleground)
            {
                var lines = new WorldLine[units.Count];
                WoWPoint tracelineLoc = me.GetTraceLinePos();

                for (int i = units.Count - 1; i >= 0; i--)
                {
                    lines[i] = new WorldLine(tracelineLoc, units[i].Object.ToUnit().GetTraceLinePos());
                }

                GameWorld.MassTraceLine(lines, TraceLineHitFlags.Collision, out hitResults);
            }

            using (StyxWoW.Memory.AcquireFrame())
            {
                for (int i = units.Count - 1; i >= 0; i--)
                {
                    WoWUnit u = units[i].Object.ToUnit();
                    WoWPoint location = u.Location;
                    double dist = myLocation.Distance(location);
                    double score = 200.0 - 2.0 * dist;
                    ulong guid = u.Guid;

                    if (inCombat)
                    {
                        if (u.MaxMana > 1U && u.ManaPercent > 5.0)
                        {
                            score += 60.0;
                        }

                        score -= u.HealthPercent;

                        if (poiGuid == guid)
                        {
                            score += 50.0;
                        }

                        if (friendlyGuids.Contains(u.CurrentTargetGuid))
                        {
                            score += 110.0;
                        }

                        if (u.Fleeing)
                        {
                            score -= 1000.0;
                        }
                    }
                    else
                    {
                        if (curProfile != null && !curProfile.TargetElites && u.Elite)
                        {
                            if (dist > 20.0)
                            {
                                units.RemoveAt(i);
                                continue;
                            }

                            score -= 1000.0;
                        }

                        if (Blacklist.Contains(guid, BlacklistFlags.Combat | BlacklistFlags.Pull))
                        {
                            if (dist > 20.0)
                            {
                                units.RemoveAt(i);
                                continue;
                            }

                            score -= 1000.0;
                        }

                        if (u.MyReaction <= WoWUnitReaction.Neutral && dist < 30.0)
                        {
                            score += (30.0 - dist) * (WoWUnitReaction.Friendly - u.MyReaction);
                        }

                        if (poiGuid == guid)
                        {
                            score += 1000.0;
                        }

                        if (u.IsPet)
                        {
                            score -= 50.0;
                        }

                        float aggroRadius = u.MyAggroRange;
                        if (aggroRadius != 0f && dist < aggroRadius + 5f)
                        {
                            score += 100.0;
                        }

                        if (hitResults != null && !hitResults[i])
                        {
                            score += 25.0;
                        }
                        else
                        {
                            score -= 25.0;
                        }

                        if (u.Entry == 61245U)
                        {
                            score += 50000.0;
                        }
                    }

                    WoWUnit ownedByRoot = u.OwnedByRoot;
                    if (ownedByRoot != null && ownedByRoot.IsPlayer && !ownedByRoot.IsFriendly)
                    {
                        score -= 6000.0;
                    }

                    units[i].Score += score;
                }
            }
        }

        #endregion

        #region Nested type: TargetPriority

        public sealed class TargetPriority
        {
            public WoWObject Object;
            public double Score;
        }

        #endregion
    }
}
