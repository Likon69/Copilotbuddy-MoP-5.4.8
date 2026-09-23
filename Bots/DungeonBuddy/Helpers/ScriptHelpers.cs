using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Media;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Threading;
using CommonBehaviors.Actions;
using Buddy.Coroutines;
using Styx;
using JetBrains.Annotations;
using Styx.Common.Helpers;
using Styx.CommonBot.Frames;
using Styx.CommonBot.Coroutines;
using Styx.CommonBot.POI;
using Styx.CommonBot.Routines;
using Styx.CommonBot;
using Styx.CommonBot.AreaManagement;
using Styx.Helpers;
using Styx.Patchables;
using Styx.Pathing;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;
using Styx.TreeSharp;
using Tripper.Tools.Math;
using Action = Styx.TreeSharp.Action;
#if USE_DUNGEONBUDDY_DLL
using Bots.DungeonBuddy.Helpers;
using Bots.DungeonBuddy.Profiles;
using Bots.DungeonBuddy.Enums;
using Application = System.Windows.Application;
using Timer = System.Windows.Forms.Timer;

namespace Bots.DungeonBuddy.Helpers
#else
using Bots.DungeonBuddy.Helpers;
using Bots.DungeonBuddy.Profiles;
using Bots.DungeonBuddy.Enums;
using Application = System.Windows.Application;
using Timer = System.Windows.Forms.Timer;

namespace Bots.DungeonBuddy.Helpers
#endif
{
    public static partial class ScriptHelpers
    {
        #region EnemyDispelType enum

        public enum EnemyDispelType
        {
            Magic,
            Enrage
        }

        #endregion

        #region GroupRoleFlags enum

        [Flags]
        public enum GroupRoleFlags
        {
            None = 0,
            Tank = 1,
            Healer = 2,
            Dps = 4,
            Ranged = 8,
            Melee = 16,
            Leader = 32,
            Follower = 64
        }

        #endregion

        #region PartyDispelType enum

        public enum PartyDispelType
        {
            Magic,
            Poison,
            Disease,
            Curse,
        }

        #endregion

        private const string BuyItemFormat = @"
            local amount={1} 
            local id={0} 
            local stackSize 
            local index = -1 
            local quantity 
            for i=1,GetMerchantNumItems() do 
            local link=GetMerchantItemLink(i) 
            if link then if link:find(id) then 
            index=i 
            stackSize=GetMerchantItemMaxStack(i) 
            end 
            end 
            end 
            if index == -1 then return -1 end 
            while amount>0 do 
            if amount>=stackSize then 
            quantity=stackSize 
            else 
            quantity=amount 
            end 
            BuyMerchantItem(index, quantity) 
            amount=amount-quantity 
            end 
            return 1 ";

        //private static readonly PartyInspection PartyInspection = new PartyInspection();

        public static readonly Random Rnd = new Random();
        private static bool _shouldWaitForTankToPull;
        private static readonly Stopwatch TankOutOfRangeSw = new Stopwatch();
        private static readonly string[] DkRangedSpells = new[] { "Death Grip", "Death Coil", "Icy Touch", "Dark Command" };

        private static readonly string[] DruidRangedSpells = new[] { "Wrath", "MoonFire" };
        private static readonly string[] DruidTankRangedSpells = new[] { "Faerie Fire (Feral)", "Growl" };
        private static readonly string[] HunterSpells = new[] { "Arcane Shot", "Steady Shot" };
        private static readonly string[] MageSpells = new[] { "Fireball", "Frostbolt" };
        private static readonly string[] MonkSpells = new[] { "Dizzying Haze", "Provoke" };
        private static readonly string[] RogueRangedSpells = new[] { "Shoot", "Throw" };

        private static readonly string[] PaladinRangedSpells = new[] { "Avenger's Shield", "Judgment", "Exorcism", "Hand of Reckoning" };

        private static readonly string[] PriestRangedSpells = new[] { "Mind Blast", "Smite" };
        private static readonly string[] ShamanRangedSpells = new[] { "Lightning Bolt" };
        private static readonly string[] WarlockRangedSpells = new[] { "Shadow Bolt" };
        private static readonly string[] WarriorRangedSpells = new[] { "Heroic Throw", "Throw", "Taunt" };
        private static ScenarioInfo _cachedScenarioInfo;
        private static readonly WaitTimer RefreshScenarioInfoTimer = new WaitTimer(TimeSpan.FromMilliseconds(250));

        /// <summary>
        ///     Gets the tank.
        /// </summary>
        public static WoWPlayer Tank
        {
            get { return Leader; }
        }

        public static WoWPlayer Leader
        {
            get
            {
                return GroupMembers.Where(member => member.IsLeader() && member.Player != null)
                    .Select(member => member.Player)
                    .OrderByDescending(player => player.MaxHealth)
                    .FirstOrDefault();
            }
        }

        /// <summary>
        ///     Gets the healer.
        /// </summary>
        public static WoWPlayer Healer
        {
            get { return GroupMembers.Where(g => g.IsHealer && g.Player != null).Select(g => g.Player).FirstOrDefault(); }
        }

        /// <summary>
        ///     Returns the info for the current Scenario.
        /// </summary>
        public static ScenarioInfo CurrentScenarioInfo
        {
            get
            {
                if (RefreshScenarioInfoTimer.IsFinished || _cachedScenarioInfo == null)
                {
                    using (StyxWoW.Memory.AcquireFrame())
                    {
                        _cachedScenarioInfo = new ScenarioInfo();
                    }
                    RefreshScenarioInfoTimer.Reset();
                }
                return _cachedScenarioInfo;
            }
        }

        /// <summary>
        ///     Gets the group members.
        /// </summary>
        /// <value>
        ///     The group members.
        /// </value>
        public static List<GroupMember> GroupMembers
        {
            get { return GroupMember.GroupMembers; }
        }

        private static bool ShouldWaitForTankToPull
        {
            get
            {
                var tank = Tank;
                if (tank == null || !tank.IsAlive || !StyxWoW.Me.IsAlive || StyxWoW.Me == tank)
                    return false;

                if (StyxWoW.Me.IsHealer && StyxWoW.Me.PartyMembers.Any(player => player.HealthPercent <= 50))
                    // make sure the nobody in grp dies...
                    return false;
                if (tank.DistanceSqr > 35 * 35)
                {
                    if (!TankOutOfRangeSw.IsRunning)
                        TankOutOfRangeSw.Start();
                    if (TankOutOfRangeSw.Elapsed > TimeSpan.FromSeconds(15))
                        return false;
                }
                else if (TankOutOfRangeSw.IsRunning)
                    TankOutOfRangeSw.Reset();

                if (!tank.Combat)
                {
                    if (!_shouldWaitForTankToPull)
                        _shouldWaitForTankToPull = true;
                    return true;
                }


                if (_shouldWaitForTankToPull)
                    _shouldWaitForTankToPull =
                        !(tank.CurrentTargetGuid != 0 && tank.IsSafelyFacing(tank.CurrentTarget) && !tank.IsMoving &&
                          ObjectManager.GetObjectsOfType<WoWUnit>().Any(u => u.CurrentTarget == tank && u.Location.Distance(tank.Location) <= u.MeleeRange()));

                return _shouldWaitForTankToPull;
            }
        }

        public static bool InRoleBasedContent
        {
            get
            {
                if (DungeonManager.SelectedDungeon == null)
                    return false;
                if (DungeonManager.SelectedDungeon.LfgDungeon.SubType == LfgDungeons.LfgType.Dungeon && DungeonBuddySettings.Instance.DungeonType == DungeonType.Farm)
                    return false;
                if (DungeonManager.SelectedDungeon.LfgDungeon.SubType == LfgDungeons.LfgType.Raid && DungeonBuddySettings.Instance.RaidType == RaidType.Farm)
                    return false;
                if (DungeonManager.SelectedDungeon.LfgDungeon.SubType == LfgDungeons.LfgType.Scenario)
                    return false;
                return true;
            }
        }

        private static TimeCachedValue<bool> _inCinematic;

        public static bool InCinematic
        {
            get
            {
                return _inCinematic ?? (_inCinematic = new TimeCachedValue<bool>(TimeSpan.FromSeconds(1.0),
                    () => Lua.GetReturnVal<bool>("return CinematicFrame:IsVisible() or MovieFrame:IsVisible()", 0)));
            }
        }

        public static bool IsFarming
        {
            get
            {
                if (DungeonBuddySettings.Instance.DungeonType != DungeonType.Farm)
                    return DungeonBuddySettings.Instance.RaidType == RaidType.Farm;
                return true;
            }
        }

        private static IEnumerable<PlayerQuest> LowLevelQuests
        {
            get
            {
                int maximumLevel = StyxWoW.Me.Level - 10;
                if (maximumLevel < 0)
                    return new List<PlayerQuest>();
                return StyxWoW.Me.QuestLog.GetAllQuests().Where(quest => quest.Level <= maximumLevel);
            }
        }

        public static bool SupportsQuesting
        {
            get { return true; }
        }

        [ContractAnnotation("wowObject:null => false")]
        public static bool IsViable(WoWObject wowObject)
        {
            return wowObject != null && wowObject.IsValid;
        }


        /// <summary>
        ///     Gets the party members by role including player.
        /// </summary>
        /// <param name="role"> The role. Can 'Or'ed multiple roles together e.g. dps and ranged </param>
        /// <returns> </returns>
        public static IEnumerable<WoWPlayer> GetGroupMembersByRole(GroupRoleFlags role)
        {
            return GroupMembers.Where(member => member.Player != null && member.HasRole(role)).Select(member => member.Player);
        }

        public static WoWPoint GetRandomPointAtLocation(WoWPoint location, float maxRadius)
        {
            float maxRadiusSquared = maxRadius * maxRadius;
            WoWPoint offset = WoWPoint.Zero;
            do
            {
                offset.X = (float)((Rnd.NextDouble() - 0.5) * 2.0 * maxRadius);
                offset.Y = (float)((Rnd.NextDouble() - 0.5) * 2.0 * maxRadius);
            }
            while (offset.DistanceSqr(WoWPoint.Center) > maxRadiusSquared);
            return location + offset;
        }


        /// <summary>
        ///     Moves to an object and interacts with it
        /// </summary>
        /// <param name="id"> Object id </param>
        /// <returns> </returns>
        public static Composite CreateInteractWithObject(uint id)
        {
            return CreateInteractWithObject(id, 0);
        }

        /// <summary>
        ///     Moves to an object and interacts with it
        /// </summary>
        /// <param name="objectSelector"> The object selector. </param>
        public static Composite CreateInteractWithObject(Func<object, WoWObject> objectSelector)
        {
            return new ActionRunCoroutine(context => InteractWithObject(objectSelector(context)));
        }

        /// <summary>
        ///     Moves to an object and interacts with it
        /// </summary>
        /// <param name="id"> Object id </param>
        /// <param name="channelTime"> The time in seconds to keep channeling for </param>
        /// <param name="ignoreCombat"> keep trying to internact with object if in combat </param>
        /// <returns> </returns>
        public static Composite CreateInteractWithObject(uint id, int channelTime, bool ignoreCombat = false)
        {
            return new ActionRunCoroutine(context => InteractWithObject(
                ObjectManager.ObjectList.Where(obj => obj.Entry == id).OrderBy(obj => obj.DistanceSqr).FirstOrDefault(),
                channelTime * 1000,
                ignoreCombat));
        }

        /// <summary>
        ///     Moves to an object and interacts with it
        /// </summary>
        /// <param name="objectSelector"> The object selector. </param>
        /// <param name="channelTime"> The time in seconds to keep channeling for </param>
        /// <param name="ignoreCombat"> keep trying to internact with object if in combat </param>
        /// <returns> </returns>
        public static Composite CreateInteractWithObject(Func<object, WoWObject> objectSelector, int channelTime, bool ignoreCombat = false)
        {
            return new ActionRunCoroutine(context => InteractWithObject(objectSelector(context), channelTime * 1000, ignoreCombat));
        }

        /// <summary>
        ///     Moves to an object and interacts with it. For use with Sequences.
        /// </summary>
        /// <param name="id"> The id. </param>
        /// <returns> </returns>
        public static Composite CreateInteractWithObjectContinue(uint id)
        {
            return CreateInteractWithObjectContinue(id, 0, false);
        }

        /// <summary>
        ///     Moves to an object and interacts with it. For use with Sequences.
        /// </summary>
        /// <param name="objectSelector"> The object selector. </param>
        /// <returns> </returns>
        public static Composite CreateInteractWithObjectContinue(Func<object, WoWObject> objectSelector)
        {
            return CreateInteractWithObjectContinue(objectSelector, 0, false);
        }

        /// <summary>
        ///     Moves to an object and interacts with it. For use with Sequences.
        /// </summary>
        /// <param name="id"> The object id. </param>
        /// <param name="channelTime"> The time in seconds to keep channeling for </param>
        /// <param name="ignoreCombat"> keep trying to internact with object if in combat. </param>
        /// <returns> </returns>
        public static Composite CreateInteractWithObjectContinue(uint id, int channelTime, bool ignoreCombat = false)
        {
            WoWObject obj = null;
            return new Sequence(
                ctx => obj = ObjectManager.ObjectList.Where(o => o.Entry == id).OrderBy(o => o.DistanceSqr).FirstOrDefault(),
                CreateInteractWithObjectContinue(context => obj, channelTime, ignoreCombat));
        }

        /// <summary>
        ///     Moves to an object and interacts with it. For use with Sequences.
        /// </summary>
        /// <param name="objectSelector"> The object selector. </param>
        /// <param name="channelTime"> The time in seconds to keep channeling for </param>
        /// <param name="ignoreCombat"> keep trying to internact with object if in combat. </param>
        /// <returns> </returns>
        public static Composite CreateInteractWithObjectContinue(Func<object, WoWObject> objectSelector, int channelTime, bool ignoreCombat = false)
        {
            WoWObject obj = null;
            return new PrioritySelector(
                ctx => obj = objectSelector(ctx),
                new Decorator(
                    ctx => Navigator.CanNavigateFully(StyxWoW.Me.Location, obj.Location),
                    new Sequence(
                        new ActionSetActivity("Interacting with Object"),
                        new DecoratorContinue(ctx => obj != null && obj.DistanceSqr > 4 * 4, CreateMoveToContinue(context => true, context => objectSelector(context), ignoreCombat)),
                        new DecoratorContinue(
                            ctx => obj != null && obj.DistanceSqr <= 4 * 4,
                            new Sequence(
                                new DecoratorContinue(
                                    ctx => StyxWoW.Me.IsMoving,
                                    new Sequence(new Action(ctx => WoWMovement.MoveStop()), new WaitContinue(2, ctx => !StyxWoW.Me.IsMoving, new ActionAlwaysSucceed()))),
                                new Action(ctx => obj.Interact()),
                                new WaitContinue(2, ctx => StyxWoW.Me.IsCasting || (!ignoreCombat && StyxWoW.Me.IsActuallyInCombat), new ActionAlwaysSucceed()),
                                new WaitContinue(channelTime, ctx => !StyxWoW.Me.IsCasting || (!ignoreCombat && StyxWoW.Me.IsActuallyInCombat), new ActionAlwaysSucceed()))))));
        }

        /// <summary>
        ///     Checks if moving to location will draw aggro.
        /// </summary>
        /// <param name="loc"> The loc. </param>
        /// <returns> </returns>
        public static bool WillPullAggroAtLocation(WoWPoint loc)
        {
            //var loc = unit.Location; loc.Z = lootLocation.Z; Avoidance.Helpers.SetToNearestZ(loc); if (Math.Abs(unit.Z - loc.Z) > 2) return false

            return (from unit in ObjectManager.GetObjectsOfType<WoWUnit>()
                    let aggroRange = unit.MyAggroRange
                    where
                        unit.IsHostile && unit.Attackable && unit.CanSelect && unit.IsAlive && Math.Abs(unit.Z - loc.Z) < 12 && !unit.Combat &&
                        aggroRange * aggroRange + 16f > unit.Location.DistanceSqr(loc)
                    select unit).Any();
        }

        /// <summary>
        ///     tank faces current target away from group.
        /// </summary>
        /// <param name="maxDistance"> The max distance </param>
        /// <returns> </returns>
        public static Composite CreateTankFaceAwayGroupUnit(int maxDistance)
        {
            return new ActionRunCoroutine(context => TankFaceUnitAwayFromGroup(maxDistance));
        }

        /// <summary>
        ///     Tank faces unit away from group.
        /// </summary>
        /// <param name="unitSelector"> The unit. </param>
        /// <param name="maxDistance"> The max distance. </param>
        /// <returns> </returns>
        public static Composite CreateTankFaceAwayGroupUnit(Func<object, WoWUnit> unitSelector, int maxDistance)
        {
            return new ActionRunCoroutine(context => TankFaceUnitAwayFromGroup(unitSelector(context), maxDistance));
        }

        /// <summary>
        ///     Clears an area centered around a location of all the specified NPCs or all non-friendly NPCs if no none are specified.
        /// </summary>
        /// <param name="centerLocationSelector"> Center of the location to clear NPCs. </param>
        /// <param name="radius"> Radius of the area to clear </param>
        /// <param name="unitSelector"> The unit selector. </param>
        /// <returns> </returns>
        public static Composite CreateClearArea(Func<WoWPoint> centerLocationSelector, float radius, Predicate<WoWUnit> unitSelector = null)
        {
            return new ActionRunCoroutine(context => ClearArea(centerLocationSelector(), radius, unitSelector));
        }

        /// <summary>
        ///     Returns a list of unfriendly Nps at location.
        /// </summary>
        /// <param name="centerLocationSelector"> The center location selector. </param>
        /// <param name="radius"> The radius. </param>
        /// <param name="unitSelector"> The unit selector. </param>
        /// <returns> </returns>
        [Obsolete("This overload should no-longer be used.")]
        public static List<WoWUnit> GetUnfriendlyNpsAtLocation(Func<WoWPoint> centerLocationSelector, float radius, Predicate<WoWUnit> unitSelector = null, bool checkNavigation = false)
        {
            return GetUnfriendlyNpsAtLocation(centerLocationSelector(), radius, unitSelector, checkNavigation);
        }

        public static List<WoWUnit> GetUnfriendlyNpsAtLocation(WoWPoint location, float radius, Predicate<WoWUnit> unitSelector = null, bool checkPathDistance = false)
        {
            float radiusSquared = radius * radius;
            WoWPoint myLocation = StyxWoW.Me.Location;
            return ObjectManager.GetObjectsOfType<WoWUnit>()
                .Where(unit => unit.IsValid && unit.IsAlive && unit.Attackable && unit.CanSelect &&
                    (unitSelector == null || unitSelector(unit)) && !unit.IsCritter && !unit.IsFriendly)
                .Where(unit => location.DistanceSqr(unit.Location) < radiusSquared &&
                    (!checkPathDistance || DungeonBot.DungeonNavigator.CanNavigateDistanceFully(location, unit.Location, radius)))
                .OrderBy(unit => unit.Location.DistanceSqr(myLocation))
                .ToList();
        }

        /// <summary>
        ///     Gets the unfriendly NPS at area.
        /// </summary>
        /// <param name="area"> The area. </param>
        /// <param name="unitSelector"> The unit selector. </param>
        /// <returns> </returns>
        public static List<WoWUnit> GetUnfriendlyNpcsAtArea(DungeonArea area, Predicate<WoWUnit> unitSelector = null)
        {
            return
                ObjectManager.GetObjectsOfType<WoWUnit>()
                             .Where(
                                 u =>
                                 u.IsValid && u.IsAlive && !u.IsFriendly && !u.IsCritter && u.Attackable && u.CanSelect && area.IsPointInPoly(u.Location) &&
                                 (unitSelector == null || unitSelector(u)) && Navigator.CanNavigateFully(StyxWoW.Me.Location, u.Location))
                             .OrderBy(u => u.DistanceSqr)
                             .ToList();
        }

        /// <summary>
        ///     Finds the best target with ids range.
        /// </summary>
        /// <param name="maxRange"> The max range. </param>
        /// <param name="unitIds"> The unit ids. </param>
        /// <returns> </returns>
        public static WoWUnit FindBestTargetWithIdsRange(int maxRange, params uint[] unitIds)
        {
            return
                ObjectManager.GetObjectsOfType<WoWUnit>()
                             .Where(u => u.IsAlive && unitIds.Contains(u.Entry) && u.Location.DistanceSqr(StyxWoW.Me.Location) < maxRange * maxRange)
                             .OrderByDescending(u => u.DistanceSqr)
                             .FirstOrDefault();
        }

        /// <summary>
        ///     Returns 'true' if specified boss is alive
        /// </summary>
        /// <param name="id"> Id of the boss </param>
        /// <returns> is it dead? </returns>
        public static bool IsBossAlive(uint id)
        {
            var boss = BossManager.BossEncounters.FirstOrDefault(b => b.Entry == id);
            return boss != null && boss.IsAlive;
        }

        /// <summary>
        ///     Returns 'true' if specified boss is alive
        /// </summary>
        /// <param name="name"> name of the boss </param>
        /// <returns> is it dead? </returns>
        public static bool IsBossAlive(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new InvalidOperationException("Name can not be empty or null");
            var boss = BossManager.BossEncounters.FirstOrDefault(b => b.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
            return boss != null && boss.IsAlive;
        }

        /// <summary>
        ///     Returns true if the boss is not optional or if optional and 'Kill Optional Bosses' is setting is true.
        /// </summary>
        /// <param name="name"> The name. </param>
        /// <returns> true if boss should be killed </returns>
        public static bool ShouldKillBoss(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new InvalidOperationException("Name can not be empty or null");
            var boss = BossManager.BossEncounters.FirstOrDefault(b => b.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
            return boss != null && ((boss.Optional && DungeonBuddySettings.Instance.KillOptionalBosses) || !boss.Optional);
        }

        /// <summary>
        ///     Marks the boss as dead.
        /// </summary>
        /// <param name="name"> The name. </param>
        /// <param name="reason"> </param>
        /// <returns> true if boss should be killed </returns>
        public static void MarkBossAsDead(string name, string reason = null)
        {
            if (string.IsNullOrEmpty(name))
                throw new InvalidOperationException("Name can not be empty or null");
            var boss = BossManager.BossEncounters.FirstOrDefault(b => b.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
            if (boss != null && boss.IsAlive)
                BossManager.OnKill(boss, reason);
        }

        /// <summary>
        ///     Telports outside current LFG instance
        /// </summary>
        public static void TelportOutsideLfgInstance()
        {
            Lua.DoString("LFGTeleport(true)");
        }

        // called when merchantFrame is visible. {0} = itemId, {1} = amount
        //private const string BuyItemFormat = "local amount={1} local id={0} local stackSize local index = -1 local quantity for i=1,GetMerchantNumItems() do local link=GetMerchantItemLink(i) if link then if link:find(id) then index=i stackSize=GetMerchantItemMaxStack(i) end end end if index == -1 then return -1 end while amount>0 do if amount>=stackSize then quantity=stackSize else quantity=amount end BuyMerchantItem(index, quantity) amount=amount-quantity end return 1";

        // called when merchantFrame is visible. 

        /// <summary>
        ///     Buys item from open merchant frame
        /// </summary>
        /// <param name="itemId"> item id to buy </param>
        /// <param name="amount"> Amount to buy </param>
        public static void BuyItem(uint itemId, int amount)
        {
            Lua.DoString(BuyItemFormat, itemId, amount);
        }

        /// <summary>
        ///     wait at location while condition is true
        /// </summary>
        /// <param name="canrun"> The canrun. </param>
        /// <param name="locationSelector"> The location selector. </param>
        /// <param name="radius"> The radius. </param>
        /// <param name="roles"> The roles. </param>
        /// <returns> </returns>
        public static Composite CreateWaitAtLocationWhile(CanRunDecoratorDelegate canRun, Func<object, WoWPoint> locationSelector, string locationDescription = null, float precision = 8f, params GroupRoleFlags[] roles)
        {
            return new Decorator(canRun,
                new ActionRunCoroutine(context => StayAtLocationWhile(() => canRun(context), locationSelector(context), locationDescription, precision, roles)));
        }

        /// <summary>
        ///     waits at location until tank pulled.
        /// </summary>
        /// <param name="canrun"> The canrun. </param>
        /// <param name="locationSelector"> The location selector. if null then current locaton is used. </param>
        /// <returns> </returns>
        public static Composite CreateWaitAtLocationUntilTankPulled(CanRunDecoratorDelegate canrun, Func<object, WoWPoint> locationSelector)
        {
            return new Decorator(
                ctx => StyxWoW.Me.IsFollower() && ShouldWaitForTankToPull && canrun(ctx),
                new PrioritySelector(
                    ctx => locationSelector != null ? locationSelector(ctx) : WoWPoint.Zero,
                    new Action(ctx =>
                    {
                        TreeRoot.StatusText = string.Format("Waiting for tank to pull");
                        Navigator.NavigationProvider.StuckHandler.Reset();
                        return RunStatus.Failure;
                    }),
                    new Decorator(
                        ctx => (WoWPoint)ctx == WoWPoint.Zero || StyxWoW.Me.Location.DistanceSqr((WoWPoint)ctx) <= 4 * 4,
                        new PrioritySelector(new Decorator(ctx => StyxWoW.Me.IsMoving, new Action(ctx => WoWMovement.MoveStop())), new ActionAlwaysSucceed())),
                    new Decorator(
                        ctx => (WoWPoint)ctx != WoWPoint.Zero && StyxWoW.Me.Location.DistanceSqr((WoWPoint)ctx) > 4 * 4,
                        new Action(ctx => Navigator.GetRunStatusFromMoveResult(Navigator.MoveTo(locationSelector(ctx)))))));
        }


        /// <summary>
        ///     Casts a ranged ability on current target or first unit in targeting list.
        /// </summary>
        /// <returns> </returns>
        public static Composite CreateCastRangedAbility()
        {
            return new ActionRunCoroutine(context => CastRangedAbility());
        }


        /// <summary>
        ///     Returns a ranged spell that class can use on target.
        /// </summary>
        /// <param name="target"> The target. </param>
        /// <returns> </returns>
        public static WoWSpell GetRangedSpell(WoWUnit target)
        {
            if (!IsViable(target) || !target.InLineOfSpellSight)
                return null;
            switch (StyxWoW.Me.Class)
            {
                case WoWClass.DeathKnight:
                    return DkRangedSpells.Where(s => SpellManager.CanCast(s, target, true, false)).Select(s => SpellManager.Spells[s]).FirstOrDefault();
                case WoWClass.Druid:
                    if (StyxWoW.Me.IsTank())
                        return DruidTankRangedSpells.Where(s => SpellManager.CanCast(s, target, true, false)).Select(s => SpellManager.Spells[s]).FirstOrDefault();
                    return DruidRangedSpells.Where(s => SpellManager.CanCast(s, target, true, false)).Select(s => SpellManager.Spells[s]).FirstOrDefault();
                case WoWClass.Hunter:
                    return HunterSpells.Where(s => SpellManager.CanCast(s, target, true, false)).Select(s => SpellManager.Spells[s]).FirstOrDefault();
                case WoWClass.Mage:
                    return MageSpells.Where(s => SpellManager.CanCast(s, target, true, false)).Select(s => SpellManager.Spells[s]).FirstOrDefault();
                case WoWClass.Monk:
                    return MonkSpells.Where(
                        s =>
                        {
                            if (s == "Dizzying Haze")
                                return target.Distance < 38 && SpellManager.CanCast(s, target, false, false);
                            return SpellManager.CanCast(s, target, true, false);
                        }).Select(s => SpellManager.Spells[s]).FirstOrDefault();
                case WoWClass.Paladin:
                    return PaladinRangedSpells.Where(s => SpellManager.CanCast(s, target, true, false)).Select(s => SpellManager.Spells[s]).FirstOrDefault();
                case WoWClass.Priest:
                    return PriestRangedSpells.Where(s => SpellManager.CanCast(s, target, true, false)).Select(s => SpellManager.Spells[s]).FirstOrDefault();
                case WoWClass.Rogue:
                    return RogueRangedSpells.Where(s => SpellManager.CanCast(s, target, true, false)).Select(s => SpellManager.Spells[s]).FirstOrDefault();
                case WoWClass.Shaman:
                    return ShamanRangedSpells.Where(s => SpellManager.CanCast(s, target, true, false)).Select(s => SpellManager.Spells[s]).FirstOrDefault();
                case WoWClass.Warlock:
                    return WarlockRangedSpells.Where(s => SpellManager.CanCast(s, target, true, false)).Select(s => SpellManager.Spells[s]).FirstOrDefault();
                case WoWClass.Warrior:
                    return WarriorRangedSpells.Where(s => SpellManager.CanCast(s, target, true, false)).Select(s => SpellManager.Spells[s]).FirstOrDefault();
            }
            return null;
        }

        /// <summary>
        ///     dispells magic from select NPC.
        /// </summary>
        /// <param name="spellName"> Name of the spell. </param>
        /// <param name="dispellType"> The dispell type. </param>
        /// <param name="targetSelector"> The target selector. </param>
        /// <returns> </returns>
        public static Composite CreateDispelEnemy(string spellName, EnemyDispelType dispelType, Func<object, WoWUnit> targetSelector)
        {
            return new ActionRunCoroutine(context => DispelEnemy(spellName, dispelType, targetSelector(context)));
        }

        /// <summary>
        ///     dispells magic from party members.
        /// </summary>
        /// <param name="spellName"> The spell name. </param>
        /// <param name="dispellType"> The dispell type. </param>
        /// <returns> </returns>
        public static Composite CreateDispelGroup(string spellName, PartyDispelType dispelType)
        {
            return new ActionRunCoroutine(context => DispelGroup(spellName, dispelType));
        }

        public static Composite CreateDispelFriendlyUnit(string spellName, PartyDispelType dispelType, Func<object, WoWUnit> targetSelector)
        {
            return new ActionRunCoroutine(context => DispelFriendlyUnit(spellName, dispelType, targetSelector(context)));
        }


        public static Composite CreateInterruptCast(Func<object, WoWUnit> targetSelector, params int[] spellIds)
        {
            return new ActionRunCoroutine(context => InterruptCast(targetSelector(context), spellIds));
        }

        public static WoWSpell GetOffensiveMagicDispel()
        {
            switch (StyxWoW.Me.Class)
            {
                case WoWClass.Hunter:
                    return SpellManager.HasSpell("Tranquilizing Shot") ? SpellManager.Spells["Tranquilizing Shot"] : null;
                case WoWClass.Mage:
                    return SpellManager.HasSpell("Spellsteal") ? SpellManager.Spells["Spellsteal"] : null;
                case WoWClass.Priest:
                    return SpellManager.HasSpell("Dispel Magic") ? SpellManager.Spells["Dispel Magic"] : null;
                case WoWClass.Shaman:
                    return SpellManager.HasSpell("Purge") ? SpellManager.Spells["Purge"] : null;
                case WoWClass.Warrior:
                    return SpellManager.HasSpell("Shield Slam") ? SpellManager.Spells["Shield Slam"] : null;
                default:
                    return null;
            }
        }

        public static WoWSpell GetDefensiveMagicDispel()
        {
            SpellFindResults result;
            switch (StyxWoW.Me.Class)
            {
                case WoWClass.Druid:
                    return SpellManager.FindSpell("Nature's Cure", out result) ? result.Override : null;
                case WoWClass.Monk:
                    return StyxWoW.Me.Specialization == WoWSpec.MonkMistweaver && SpellManager.HasSpell("Detox") ? SpellManager.Spells["Detox"] : null;
                case WoWClass.Paladin:
                    return StyxWoW.Me.Specialization == WoWSpec.PaladinHoly && SpellManager.HasSpell("Cleanse") ? SpellManager.Spells["Cleanse"] : null;
                case WoWClass.Priest:
                    return SpellManager.HasSpell("Purify") ? SpellManager.Spells["Purify"] : null;
                case WoWClass.Shaman:
                    return SpellManager.FindSpell("Purify Spirit", out result) ? result.Override : null;
                default:
                    return null;
            }
        }

        public static WoWSpell GetCurseDispel()
        {
            SpellFindResults result;
            switch (StyxWoW.Me.Class)
            {
                case WoWClass.Druid:
                    if (SpellManager.FindSpell("Nature's Cure", out result))
                        return result.Override;

                    return StyxWoW.Me.Specialization == WoWSpec.DruidBalance && SpellManager.HasSpell("Remove Corruption") ? SpellManager.Spells["Remove Corruption"] : null;
                case WoWClass.Mage:
                    return SpellManager.HasSpell("Remove Curse") ? SpellManager.Spells["Remove Curse"] : null;
                case WoWClass.Shaman:
                    if (SpellManager.FindSpell("Purify Spirit", out result))
                        return result.Override;
                    return SpellManager.HasSpell("Cleanse Spirit") ? SpellManager.Spells["Cleanse Spirit"] : null;
                default:
                    return null;
            }
        }

        public static WoWSpell GetPoisonDispel()
        {
            switch (StyxWoW.Me.Class)
            {
                case WoWClass.Druid:
                    SpellFindResults result;
                    if (SpellManager.FindSpell("Nature's Cure", out result))
                        return result.Override;

                    return StyxWoW.Me.Specialization == WoWSpec.DruidBalance && SpellManager.HasSpell("Remove Corruption") ? SpellManager.Spells["Remove Corruption"] : null;
                case WoWClass.Monk:
                    return SpellManager.HasSpell("Detox") ? SpellManager.Spells["Detox"] : null;

                case WoWClass.Paladin:
                    return SpellManager.HasSpell("Cleanse") ? SpellManager.Spells["Cleanse"] : null;
                default:
                    return null;
            }
        }

        public static WoWSpell GetDiseaseDispel()
        {
            switch (StyxWoW.Me.Class)
            {
                case WoWClass.Monk:
                    return SpellManager.HasSpell("Detox") ? SpellManager.Spells["Detox"] : null;
                case WoWClass.Paladin:
                    return SpellManager.HasSpell("Cleanse") ? SpellManager.Spells["Cleanse"] : null;
                case WoWClass.Priest:
                    return SpellManager.HasSpell("Purify") ? SpellManager.Spells["Purify"] : null;
                default:
                    return null;
            }
        }

        public static WoWSpell GetEnrageDispel()
        {
            switch (StyxWoW.Me.Class)
            {
                case WoWClass.Druid:
                    return (StyxWoW.Me.Specialization == WoWSpec.DruidBalance || StyxWoW.Me.Specialization == WoWSpec.DruidRestoration) && SpellManager.HasSpell("Soothe")
                               ? SpellManager.Spells["Soothe"]
                               : null;
                case WoWClass.Hunter:
                    return SpellManager.HasSpell("Tranquilizing Shot") ? SpellManager.Spells["Tranquilizing Shot"] : null;
                case WoWClass.Rogue:
                    return SpellManager.HasSpell("Shiv") ? SpellManager.Spells["Shiv"] : null;
                default:
                    return null;
            }
        }

        public static WoWSpell GetInterruptSpell()
        {
            switch (StyxWoW.Me.Class)
            {
                case WoWClass.DeathKnight:
                    return SpellManager.HasSpell("Mind Freeze") ? SpellManager.Spells["Mind Freeze"] : null;
                case WoWClass.Druid:
                    return SpellManager.HasSpell("Skull Bash") ? SpellManager.Spells["Skull Bash"] : null;
                case WoWClass.Mage:
                    return SpellManager.HasSpell("Mind Freeze") ? SpellManager.Spells["Counterspell"] : null;
                case WoWClass.Monk:
                    return SpellManager.HasSpell("Spear Hand Strike") ? SpellManager.Spells["Spear Hand Strike"] : null;
                case WoWClass.Paladin:
                    return SpellManager.HasSpell("Rebuke") ? SpellManager.Spells["Rebuke"] : null;
                case WoWClass.Priest:
                    return SpellManager.HasSpell("Silence") ? SpellManager.Spells["Silence"] : null;
                case WoWClass.Rogue:
                    return SpellManager.HasSpell("Kick") ? SpellManager.Spells["Kick"] : null;
                case WoWClass.Shaman:
                    return SpellManager.HasSpell("Wind Shear") ? SpellManager.Spells["Wind Shear"] : null;
                case WoWClass.Warrior:
                    return SpellManager.HasSpell("Pummel") ? SpellManager.Spells["Pummel"] : null;
                default:
                    return null;
            }
        }

        /// <summary>
        ///     Returns the center location of group. First the algorithm finds group member who has the highest number of others within <c>radius</c> distance then the arithmetic mean of thier locations is returned.
        ///     Excludes self and the dead
        /// </summary>
        /// <param name="predicate"></param>
        /// <param name="radius"></param>
        /// <returns></returns>
        public static WoWPoint GetGroupCenterLocation(Predicate<GroupMember> predicate = null, float radius = 40f)
        {
            float radiusSqr = radius * radius;
            var myGuid = StyxWoW.Me.Guid;

            var raidLocations = GroupMembers.Where(g => g.Guid != myGuid && g.IsAlive && (predicate == null || predicate(g))).Select(g => g.Location).ToArray();

            if (!raidLocations.Any())
                return WoWPoint.Zero;

            var densestClusterOfLocations =
                (from point in raidLocations 
                let points = (from p in raidLocations
                              let distSqr = point.Distance2DSqr(p)
                              let zDiff = Math.Abs(point.Z - p.Z)
                              where distSqr <= radiusSqr && (zDiff * zDiff) / distSqr <= 1.420278f
                              select p).ToList()
                 orderby points.Count descending select points)
                    .FirstOrDefault();

            if (densestClusterOfLocations == null || !densestClusterOfLocations.Any())
                return WoWPoint.Zero;
            WoWPoint raidCenterLoc = WoWPoint.Zero;
            raidCenterLoc.X = densestClusterOfLocations.Average(l => l.X);
            raidCenterLoc.Y = densestClusterOfLocations.Average(l => l.Y);
            raidCenterLoc.Z = densestClusterOfLocations.Average(l => l.Z);
            return raidCenterLoc;
        }

        /// <summary>
        ///     Creates a behavior that moves to chest and loots it
        /// </summary>
        /// <param name="chestSelector">The chest selector.</param>
        /// <param name="encounterReward">Specifies if this chest is a reqard for an encounter.</param>
        /// <returns></returns>
        public static Composite CreateLootChest(Func<object, WoWGameObject> chestSelector, bool encounterReward = true)
        {
            return new ActionRunCoroutine(context => LootChest(chestSelector(context), encounterReward));
        }

        public static Composite CreateCancelCinematicIfPlaying()
        {
            return new ActionRunCoroutine(context => CancelCinematicIfPlaying());
        }

        public static Task SleepForRandomReactionTime()
        {
            return Coroutine.Sleep(Rnd.Next(200, 700));
        }

        public static Task SleepForRandomUiInteractionTime()
        {
            return Coroutine.Sleep(Rnd.Next(900, 2000));
        }

        public static async Task<bool> PortOutsideAndBackIn()
        {
            if (StyxWoW.Me.GroupInfo.LfgDungeonId == 0 || StyxWoW.Me.Combat || StyxWoW.Me.IsFalling)
                return false;

            uint mapId = StyxWoW.Me.MapId;
            int attempts = 0;
            while (attempts < 10 && StyxWoW.IsInWorld && StyxWoW.Me.MapId == mapId)
            {
                Lua.DoString("LFGTeleport(true)");
                await SleepForRandomUiInteractionTime();
                attempts++;
            }
            if (StyxWoW.IsInWorld && StyxWoW.Me.MapId == mapId)
            {
                Logger.WriteDebug("Teleport failed. Aborting teleport");
                return false;
            }

            await Coroutine.Sleep(2000);
            attempts = 0;
            while (attempts < 10 && StyxWoW.IsInWorld && StyxWoW.Me.MapId != mapId)
            {
                Lua.DoString("LFGTeleport()");
                await SleepForRandomUiInteractionTime();
                attempts++;
            }
            if (StyxWoW.IsInWorld && StyxWoW.Me.MapId != mapId)
            {
                Logger.WriteDebug("Teleport failed. Aborting teleport");
                return false;
            }
            return true;
        }

        public static async Task<bool> TankFaceUnitAwayFromGroup(int maxDistance)
        {
            return await TankFaceUnitAwayFromGroup(StyxWoW.Me.CurrentTarget, maxDistance);
        }

        public static async Task<bool> TankFaceUnitAwayFromGroup(WoWUnit unit, int maxDistance)
        {
            int maxDistanceSquared = maxDistance * maxDistance;
            if (unit == null || !unit.IsValid || !StyxWoW.Me.IsTank() || !unit.Aggro)
                return false;

            WoWPoint unitLocation = unit.Location;
            var partyMembers = StyxWoW.Me.RaidMembers.Where(player => !player.IsMe)
                .Select(player => new { Player = player, DistanceSquared = player.Location.DistanceSqr(unitLocation) })
                .ToList();
            if (!partyMembers.Any(member => unit.IsFacing(member.Player) && member.DistanceSquared < maxDistanceSquared))
                return false;

            float direction = WoWMathHelper.NormalizeRadian(partyMembers
                .Where(member => member.DistanceSquared <= maxDistanceSquared)
                .Select(member => unitLocation.GetDirectionTo(member.Player.Location))
                .Select(point => (float)Math.Atan2(point.Y, point.X))
                .Average() + (float)Math.PI);
            float meleeRange = unit.MeleeRange();
            float distance = unit.Distance < meleeRange ? 3f : meleeRange;
            WoWPoint destination = unit.Location.RayCast(direction, distance);
            WoWPoint myLocation = StyxWoW.Me.Location;
            if (myLocation.Distance2DSqr(destination) > 0.25 && Bots.DungeonBuddy.Avoidance.Helpers.MeshTraceline(myLocation, destination) == false)
            {
                Logger.Write("Facing {0} away from party", unit.SafeName);
                MoveResult result = await CommonCoroutines.MoveTo(destination);
                return result.IsSuccessful();
            }
            return false;
        }

        public static async Task<bool> DispelEnemy(string spellName, EnemyDispelType dispelType, WoWUnit target)
        {
            if (!IsViable(target) || !target.HasAura(spellName))
                return false;
            WoWSpell dispelSpell = dispelType == EnemyDispelType.Magic ? GetOffensiveMagicDispel() : GetEnrageDispel();
            if (dispelSpell == null || !SpellManager.CanCast(dispelSpell, false))
                return false;
            if (target.Distance > dispelSpell.ActualMaxRange(target))
            {
                MoveResult result = await CommonCoroutines.MoveTo(target.Location, string.Format("{0} to dispel {1}", target.SafeName, dispelSpell.Name));
                return result.IsSuccessful();
            }
            Logger.Write("Dispeling {0} effect {1} on {2}", dispelType, spellName, target.SafeName);
            SpellManager.Cast(dispelSpell, target);
            await CommonCoroutines.SleepForLagDuration();
            return true;
        }

        public static async Task<bool> DispelGroup(string spellName, PartyDispelType dispelType)
        {
            WoWUnit target = StyxWoW.Me.RaidMembers.FirstOrDefault(unit => unit.HasAura(spellName));
            return await DispelFriendlyUnit(spellName, dispelType, target);
        }

        public static async Task<bool> DispelFriendlyUnit(string spellName, PartyDispelType dispelType, WoWUnit target)
        {
            if (!IsViable(target) || !target.HasAura(spellName))
                return false;
            WoWSpell dispelSpell = null;
            switch (dispelType)
            {
                case PartyDispelType.Magic:
                    dispelSpell = GetDefensiveMagicDispel();
                    break;
                case PartyDispelType.Poison:
                    dispelSpell = GetPoisonDispel();
                    break;
                case PartyDispelType.Disease:
                    dispelSpell = GetDiseaseDispel();
                    break;
                case PartyDispelType.Curse:
                    dispelSpell = GetCurseDispel();
                    break;
            }
            if (dispelSpell == null || !SpellManager.CanCast(dispelSpell, false))
                return false;
            if (target.Distance > dispelSpell.ActualMaxRange(target))
            {
                MoveResult result = await CommonCoroutines.MoveTo(target.Location, string.Format("{0} to dispel {1}", target.SafeName, dispelSpell.Name));
                return result.IsSuccessful();
            }
            Logger.Write("Dispeling {0} effect {1} on {2}", dispelType, spellName, target.SafeName);
            SpellManager.Cast(dispelSpell, target);
            await CommonCoroutines.SleepForLagDuration();
            return true;
        }

        public static async Task<bool> InterruptCast(WoWUnit target, params int[] spellIds)
        {
            int castingSpellId;
            if (!IsViable(target) || !spellIds.Contains(castingSpellId = target.CastingSpellId))
                return false;
            WoWSpell interruptSpell = GetInterruptSpell();
            if (interruptSpell == null || !SpellManager.CanCast(interruptSpell, target, true, true, true))
                return false;
            if (!StyxWoW.Me.IsSafelyFacing(target))
            {
                target.Face();
                if (!await Coroutine.Wait(1000, () => StyxWoW.Me.IsSafelyFacing(target)) || !target.IsValid)
                    return false;
            }
            Logger.Write("Interrupted spell {0} casted by {1}", WoWSpell.FromId(castingSpellId).Name, target.SafeName);
            SpellManager.Cast(interruptSpell, target);
            await CommonCoroutines.SleepForLagDuration();
            return true;
        }

        public static async Task<bool> LootChest(WoWGameObject chest, bool encounterReward = true)
        {
            LootMode lootMode = DungeonBuddySettings.Instance.LootMode;
            if (lootMode == LootMode.Off || lootMode == LootMode.BossesOnly && !encounterReward)
                return false;
            if (StyxWoW.Me.Combat || !IsViable(chest) || chest.InUse || !chest.CanUse())
                return false;
            if (Blacklist.Contains(chest, BlacklistFlags.Loot))
                return false;
            if (!chest.WithinInteractRange)
            {
                MoveResult result = await CommonCoroutines.MoveTo(chest.Location, chest.SafeName);
                return result.IsSuccessful();
            }
            await StopMovingIfMoving("looting chest");
            int attempts = 0;
            while (attempts < 4 && !LootFrame.Instance.IsVisible && IsViable(chest))
            {
                chest.Interact();
                await Coroutine.Wait(2000, () => StyxWoW.Me.IsCasting);
                await Coroutine.Wait(10000, () => !StyxWoW.Me.IsCasting);
                attempts++;
            }
            if (LootFrame.Instance.IsVisible)
                LootFrame.Instance.LootAll();
            Blacklist.Add(chest, BlacklistFlags.Loot, TimeSpan.FromMinutes(3.0), "Chest looted");
            return true;
        }

        public static async Task<bool> CancelCinematicIfPlaying()
        {
            if (!InCinematic)
                return false;
            await SleepForRandomUiInteractionTime();
            Logger.Write("Canceling Cinematic");
            Lua.DoString("if MovieFrame:IsVisible() then MovieFrame:StopMovie() else CinematicFrame_CancelCinematic() end");
            await SleepForRandomUiInteractionTime();
            return true;
        }

        public static async Task<bool> ClearArea(WoWPoint location, float radius, Predicate<WoWUnit> unitSelector = null, bool checkPathDistance = true)
        {
            if (StyxWoW.Me.IsTank() && BotPoi.Current.Type == PoiType.None)
            {
                List<WoWUnit> targets = GetUnfriendlyNpsAtLocation(location, radius, unitSelector, checkPathDistance);
                return targets.Any() && Targeting.Instance.IsEmpty() && SetLeaderMoveToPoi(targets.First().Location);
            }
            return false;
        }

        public static async Task<bool> InteractWithObject(WoWObject obj, int channelTimeMs = 0, bool ignoreCombat = false)
        {
            if (!IsViable(obj) || !ignoreCombat && StyxWoW.Me.IsActuallyInCombat)
                return false;
            WoWGameObject gameObject = obj as WoWGameObject;
            if (gameObject != null && !gameObject.CanUse() || !Navigator.CanNavigateFully(StyxWoW.Me.Location, obj.Location))
                return false;
            if (!obj.WithinInteractRange)
            {
                MoveResult result = await CommonCoroutines.MoveTo(obj.Location, obj.SafeName);
                return result.IsSuccessful();
            }
            await StopMovingIfMoving(string.Format("interacting with {0}", obj.SafeName));
            obj.Interact();
            await CommonCoroutines.SleepForLagDuration();
            if (channelTimeMs > 0)
            {
                await Coroutine.Wait(1000, () => StyxWoW.Me.IsCasting || !ignoreCombat && StyxWoW.Me.IsActuallyInCombat);
                await Coroutine.Wait(channelTimeMs, () => !StyxWoW.Me.IsCasting || !ignoreCombat && StyxWoW.Me.IsActuallyInCombat);
            }
            if (LootFrame.Instance.IsVisible)
                LootFrame.Instance.LootAll();
            return true;
        }

        public static async Task<bool> CastRangedAbility(WoWUnit target = null)
        {
            WoWUnit activeMover = WoWMovement.ActiveMover;
            if (activeMover == null)
                return false;
            target = target ?? activeMover.CurrentTarget ?? Targeting.Instance.FirstUnit;
            if (!IsViable(target))
                return false;
            WoWSpell rangedSpell = GetRangedSpell(target);
            if (rangedSpell == null || target.HasAura(90954) || activeMover.Location.Distance(target.Location) >= rangedSpell.ActualMaxRange(target))
                return false;
            await FaceIfNotFacing(target);
            await StopMovingIfMoving();
            Logger.Write("Casting ranged ability {0} on {1}", rangedSpell.Name, target.SafeName);
            rangedSpell.Cast();
            if (StyxWoW.Me.CurrentPendingCursorSpell != null)
                SpellManager.ClickRemoteLocation(target.Location);
            await CommonCoroutines.SleepForLagDuration();
            if (rangedSpell.CastTime > 0)
                await Coroutine.Sleep((int)rangedSpell.CastTime);
            return await Coroutine.Wait(10000, () => !activeMover.IsCasting);
        }
    }

}
