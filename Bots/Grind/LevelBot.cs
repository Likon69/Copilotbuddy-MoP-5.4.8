using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Media;
using Buddy.Coroutines;
using CommonBehaviors;
using CommonBehaviors.Actions;
using CommonBehaviors.Decorators;
using Levelbot;
using Levelbot.Actions.Death;
using Levelbot.Decorators.Combat;
using Levelbot.Resources;
using Styx;
using Styx.Common;
using Styx.Common.Helpers;
using Styx.CommonBot;
using Styx.CommonBot.AreaManagement;
using Styx.CommonBot.Coroutines;
using Styx.CommonBot.Frames;
using Styx.CommonBot.Inventory;
using Styx.CommonBot.POI;
using Styx.CommonBot.ObjectDatabase;
using Styx.CommonBot.Profiles;
using Styx.CommonBot.Routines;
using Styx.Helpers;
using Styx.Localization;
using Styx.Pathing;
using Styx.TreeSharp;
using Styx.WoWInternals;
using Styx.WoWInternals.DBC;
using Styx.WoWInternals.WoWObjects;
using Tripper.Navigation;
using Tripper.Tools.Math;
using Action = Styx.TreeSharp.Action;

namespace Bots.Grind
{
    public class LevelBot : BotBase
    {
        public LevelBot()
        {
            BotEvents.OnBotStopped += OnBotStopped;
        }

        private void OnBotStopped(EventArgs eventArgs)
        {
            BehaviorFlags = BehaviorFlags.All;
        }

        private static void ProcessLootFrame()
        {
            using (StyxWoW.Memory.AcquireFrame())
            {
                List<WoWItem> carriedItems = StyxWoW.Me.CarriedItems;
                for (int i = 0; i < LootFrame.Instance.LootItems; i++)
                {
                    uint itemId = LootFrame.Instance.GetItemId(i);
                    foreach (WoWItem item in carriedItems)
                    {
                        if (item.Entry == itemId)
                        {
                            ItemInfo itemInfo = item.ItemInfo;
                            if (itemInfo != null && (itemInfo.UniqueCount == 1 || itemInfo.BeginQuestId != 0))
                            {
                                Logging.WriteDiagnostic("We already have {0} (unique) in our bags. Will blacklist the lootable.", new object[] { item.Name });
                                Blacklist.Add(BotPoi.Current.Guid, BlacklistFlags.Loot | BlacklistFlags.Node, TimeSpan.FromHours(3.0), "Don't want duplicate unique item");
                                BotPoi.Clear("Unique item on mob.");
                                break;
                            }
                        }
                    }
                    LootFrame.Instance.Loot(i);
                    Lua.DoString("ConfirmLootSlot({0})", new object[] { i + 1 });
                }
                Lua.DoString("CloseLoot();", "WoW.lua");
            }
        }

        private static void OnLootChatMessage(object sender, LuaEventArgs e)
        {
            _lootRetryCount = 0;
            _lootBlacklistCount = 0;
        }

        private static bool CanPickGenericLootable()
        {
            return BotPoi.Current.Type != PoiType.Skin && BotPoi.Current.Type != PoiType.Loot && LootTargeting.Instance.FirstObject != null &&
                   !Blacklist.Contains(LootTargeting.Instance.FirstObject, BlacklistFlags.Loot) &&
                   (LootTargeting.Instance.FirstObject.ToUnit() == null || !LootTargeting.Instance.FirstObject.ToUnit().Skinnable) &&
                   !IsLootableUnreachable(LootTargeting.Instance.FirstObject);
        }

        private static bool IsLootableUnreachable(WoWObject lootable)
        {
            WoWPoint myLocation = StyxWoW.Me.Location;
            WoWPoint[] path = Navigator.GeneratePath(myLocation, lootable.Location);
            if (path.Length > 0)
            {
                if (path.Any(point => point.Distance(myLocation) > 80f))
                    return true;

                if (path[path.Length - 1].Distance(lootable.Location) > 5f)
                {
                    Blacklist.Add(lootable.Guid, BlacklistFlags.Loot, new TimeSpan(1, 1, 1), "Object is outside of loot range");
                    return true;
                }
            }
            return false;
        }

        private static bool HasFreeBagSpaceForLoot()
        {
            if (ProfileManager.CurrentProfile != null)
            {
                uint freeSlots = LevelbotSettings.Instance.GroundMountFarmingMode ? StyxWoW.Me.FreeBagSlots : StyxWoW.Me.FreeNormalBagSlots;
                if (freeSlots <= 1U || (ulong)freeSlots < (ulong)ProfileManager.CurrentProfile.MinFreeBagSlots)
                    return false;
            }
            return true;
        }

        public static Composite CreateLootBehavior()
        {
            if (!_lootEventAttached)
            {
                Lua.Events.AttachEvent("CHAT_MSG_LOOT", new LuaEventHandlerDelegate(OnLootChatMessage));
                _lootEventAttached = true;
            }

            return new Decorator(
                context => !StyxWoW.Me.IsOnTransport && HasFreeBagSpaceForLoot() && !StyxWoW.Me.IsActuallyInCombat,
                new HookExecutor("Loot_Main",
                    "Loot: Loot Main Hook Location - This will call the Loots's Functions such as finding Mines/Herbs/Mobs to loot/Gameobject as well as dealing with looting blacklisting.",
                    new Decorator(context => BehaviorFlags.HasFlag(BehaviorFlags.Loot),
                        new PrioritySelector(
                            new DecoratorIsPoiType(new[] { PoiType.Loot, PoiType.Skin, PoiType.Harvest },
                                new PrioritySelector(
                                    new DecoratorNeedToFindTarget(
                                        new PrioritySelector(
                                            (ContextChangeHandler)(original => Targeting.Instance.FirstUnit),
                                            new Decorator(
                                                context => ((WoWUnit)context).IsHostile && ((WoWUnit)context).Distance < (double)(((WoWUnit)context).MyAggroRange + 2f),
                                                new PrioritySelector(
                                                    new ActionDebugString("[LB] DNTFT -> S"),
                                                    new Sequence(
                                                        new Action((ActionSucceedDelegate)(context => Targeting.Instance.FirstUnit.Target())),
                                                        new ActionDebugString("[LB] SetTarget Finished. Waiting."),
                                                        new WaitContinue(5, context => StyxWoW.Me.GotTarget, new ActionIdle()),
                                                        new ActionDebugString("[LB] Finished waiting, we got a target."),
                                                        new ActionSetPoi(true, context => new BotPoi(StyxWoW.Me.CurrentTarget, PoiType.Kill, NavType.Run))))))),
                                    new Decorator(
                                        context => _lastPoiType == BotPoi.Current.Type && _lastPoiGuid == BotPoi.Current.Guid,
                                        new Action((ActionSucceedDelegate)(context =>
                                        {
                                            if (_lootRetryCount++ < 5)
                                            {
                                                BotPoi.Clear("Already looted");
                                                return;
                                            }
                                            if (_lootBlacklistCount++ >= 2)
                                            {
                                                Logging.Write(LevelbotResources.LevelBot_CreateLootBehavior_Blacklisting_lootable_to_avoid_useless_POI_spam__tried_looting_twice_but_we_still_can_t_loot_);
                                                Blacklist.Add(BotPoi.Current.Guid, BlacklistFlags.Loot | BlacklistFlags.Node, TimeSpan.FromMinutes(15.0), "Avoiding Poi spam; looted twice - no dice.");
                                                _lootBlacklistCount = 0;
                                                BotPoi.Clear("Tried to loot more than 2 times");
                                                return;
                                            }
                                            _lastPoiGuid = 0UL;
                                            _lastPoiType = default(PoiType);
                                        }))),
                                    new Decorator(
                                        context => BotPoi.Current.Location.DistanceSqr(StyxWoW.Me.Location) > 16f && !StyxWoW.Me.IsFlying && !Navigator.CanNavigateFully(StyxWoW.Me.Location, BotPoi.Current.Location),
                                        new Action((ActionSucceedDelegate)(context =>
                                        {
                                            Logging.Write(LevelbotResources.LevelBot_CreateLootBehavior_Can_t_generate_a_path_to_the_lootable__Blacklisting_it_for_15_minutes_);
                                            Blacklist.Add(BotPoi.Current.Guid, BlacklistFlags.Loot | BlacklistFlags.Node, new TimeSpan(0, 15, 0), "Couldn't generate a path to the target.");
                                            BotPoi.Clear("Can't generate a path to lootable");
                                        }))),
                                    new Decorator(context => BotPoi.Current.AsObject == null,
                                        new Action((ActionSucceedDelegate)(context => BotPoi.Clear("Object is not valid.")))),
                                    new Decorator(
                                        context => BotPoi.Current.AsObject.ToUnit() != null && !LootPredictor.CanLoot(BotPoi.Current.AsObject.ToUnit()) && BotPoi.Current.Type == PoiType.Loot,
                                        new Action((ActionSucceedDelegate)(context => BotPoi.Clear("Unit no longer has loot.")))),
                                    new Decorator(context => !BotPoi.Current.AsObject.WithinInteractRange, new ActionMoveToPoi()),
                                    new Mount.ActionLandAndDismount("Loot/Harvest", true, null),
                                    new ActionRunCoroutine(new Func<object, Task<bool>>(WaitForLootableCoroutine)),
                                    new PrioritySelector(
                                        new Sequence(
                                            new DecoratorContinue(context => StyxWoW.Me.IsMoving,
                                                new Sequence(
                                                    new Action((ActionSucceedDelegate)(context => WoWMovement.MoveStop())),
                                                    new Sleep(100))),
                                            new Action((ActionSucceedDelegate)(context => BotPoi.Current.AsObject.Interact(true))),
                                            new WaitLuaEvent("LOOT_OPENED",
                                                (WaitGetTimeoutDelegate)(() => BotPoi.Current.Type != PoiType.Loot && BotPoi.Current.Type != PoiType.Skin ? 10 : 3),
                                                new Action((ActionSucceedDelegate)(context =>
                                                {
                                                    ulong lootingObjectGuid = LootFrame.Instance.LootingObjectGuid;
                                                    WoWObject lootingObject = ObjectManager.GetObjectByGuid<WoWObject>(lootingObjectGuid);
                                                    Logging.Write(LevelbotResources.LevelBot_CreateLootBehavior_Looting__0__Guid_0x_1_X016_, new object[]
                                                    {
                                                        lootingObject != null ? lootingObject.Name : LevelbotResources.LevelBot_CreateLootBehavior_Unkown_Object,
                                                        lootingObjectGuid
                                                    });
                                                    Blacklist.Add(BotPoi.Current.Guid, BotPoi.Current.Type == PoiType.Loot ? BlacklistFlags.Loot : BlacklistFlags.Node, TimeSpan.FromMinutes(15.0), "Looting done!");
                                                    if (BotPoi.Current.Type == PoiType.Loot)
                                                    {
                                                        LootTargeting.Instance.LootingList
                                                            .Where(lootable => lootable.Guid != BotPoi.Current.Guid && lootable.Location.DistanceSqr(BotPoi.Current.Location) <= 400f)
                                                            .ForEach(lootable => Blacklist.Add(lootable, BlacklistFlags.Loot, TimeSpan.FromSeconds(2.0), "AoE looted!"));
                                                    }
                                                    ProcessLootFrame();
                                                }))),
                                            new DecoratorContinue(
                                                context => (CharacterSettings.Instance.SkinMobs || CharacterSettings.Instance.NinjaSkin) && BotPoi.Current.Type == PoiType.Loot &&
                                                           BotPoi.Current.AsObject is WoWUnit && BotPoi.Current.AsObject.ToUnit() != null &&
                                                           BotPoi.Current.AsObject.ToUnit().CreatureType == WoWCreatureType.Beast &&
                                                           BotPoi.Current.AsObject.ToUnit().SkinType == WoWCreatureSkinType.Leather &&
                                                           BotPoi.Current.AsObject.ToUnit().Level <= StyxWoW.Me.CanSkinLevel,
                                                new WaitContinue(2, context => BotPoi.Current.AsObject != null && BotPoi.Current.AsObject.ToUnit().Skinnable, new ActionAlwaysSucceed())),
                                            new DecoratorContinue(context => BotPoi.Current.Type == PoiType.Loot,
                                                new Action((ActionSucceedDelegate)(context =>
                                                {
                                                    var unit = BotPoi.Current.AsObject as WoWUnit;
                                                    if (unit != null && unit.GetType() == typeof(WoWUnit))
                                                        BotEvents.Player.FireMobLooted(unit);
                                                    GameStats.LootedMob();
                                                }))),
                                            new DecoratorContinue(context => BotPoi.Current.Type == PoiType.Harvest,
                                                new Action((ActionSucceedDelegate)(context =>
                                                {
                                                    var gameObject = BotPoi.Current.AsObject as WoWGameObject;
                                                    if (gameObject != null)
                                                        BotEvents.Player.FireObjectHarvested(gameObject);
                                                }))),
                                            new Action((ActionSucceedDelegate)(context =>
                                            {
                                                _lastPoiType = BotPoi.Current.Type;
                                                _lastPoiGuid = BotPoi.Current.Guid;
                                            })),
                                            new ActionRunCoroutine(new Func<object, Task<bool>>(WaitForLootWindowCoroutine)),
                                            new ActionClearPoi("Finished looting")),
                                        new Sequence(new SleepForLagDuration(), new Action((ActionSucceedDelegate)(context =>
                                        {
                                            bool couldLoot;
                                            if (BotPoi.Current.Type == PoiType.Harvest)
                                                couldLoot = BotPoi.Current.AsObject.ToGameObject().CanLoot;
                                            else if (BotPoi.Current.Type == PoiType.Skin)
                                                couldLoot = BotPoi.Current.AsObject.ToUnit().CanSkin;
                                            else
                                                couldLoot = BotPoi.Current.AsObject.ToUnit().CanLoot;

                                            if (couldLoot)
                                            {
                                                Logging.Write(LevelbotResources.LevelBot_CreateLootBehavior_I_can_t_tell_if_we_looted__blacklisting_it_just_to_be_safe_);
                                                Blacklist.Add(BotPoi.Current.Guid, BlacklistFlags.Loot, new TimeSpan(0, 10, 0), "Can't tell if we looted, blacklisting to prevent getting stuck on it.");
                                            }
                                            BotPoi.Clear("Done looting");
                                        })))))),
                            new Decorator(
                                context => BotPoi.Current.Type != PoiType.Skin && LootTargeting.Instance.FirstObject != null && LootTargeting.SkinMobs &&
                                           !Blacklist.Contains(LootTargeting.Instance.FirstObject, BlacklistFlags.Node) &&
                                           LootTargeting.Instance.FirstObject is WoWUnit &&
                                           LootTargeting.Instance.FirstObject.ToUnit().SkinType == WoWCreatureSkinType.Leather &&
                                           LootTargeting.Instance.FirstObject.ToUnit().Skinnable && LootTargeting.Instance.FirstObject.ToUnit().CanSkin &&
                                           !IsLootableUnreachable(LootTargeting.Instance.FirstObject),
                                new ActionSetPoi(context => new BotPoi(LootTargeting.Instance.FirstObject, PoiType.Skin, NavType.Run))),
                            new Decorator(
                                context => BotPoi.Current.Type != PoiType.Harvest && LootTargeting.Instance.FirstObject != null &&
                                           !Blacklist.Contains(LootTargeting.Instance.FirstObject, BlacklistFlags.Node) &&
                                           LootTargeting.Instance.FirstObject is WoWGameObject && ((WoWGameObject)LootTargeting.Instance.FirstObject).CanLoot &&
                                           !IsLootableUnreachable(LootTargeting.Instance.FirstObject),
                                new ActionSetPoi(context => new BotPoi(LootTargeting.Instance.FirstObject, PoiType.Harvest, NavType.Run))),
                            new Decorator(context => CanPickGenericLootable(),
                                new ActionSetPoi(context => new BotPoi(LootTargeting.Instance.FirstObject, PoiType.Loot, NavType.Run)))))));
        }

        private static async Task<bool> WaitForLootableCoroutine(object context)
        {
            if (BotPoi.Current.Type != PoiType.Loot)
                return false;

            WoWObject lootObject = BotPoi.Current.AsObject;
            if (lootObject == null)
                return true;

            WoWUnit unit = BotPoi.Current.AsObject as WoWUnit;
            if (unit == null)
                return false;

            Stopwatch stopwatch = Stopwatch.StartNew();
            while (true)
            {
                if (unit.CanLoot)
                    return false;

                if (!LootPredictor.CanLoot(unit))
                    return true;

                await Coroutine.Yield();

                if (!unit.IsValid)
                    return true;

                if (stopwatch.ElapsedMilliseconds >= 4000L)
                    return true;
            }
        }

        private static async Task<bool> WaitForLootWindowCoroutine(object context)
        {
            if (BotPoi.Current.Type == PoiType.Loot)
            {
                await Coroutine.Wait(3000, () => BotPoi.Current.AsObject is WoWUnit && !((WoWUnit)BotPoi.Current.AsObject).Lootable);
            }
            else
            {
                await CommonCoroutines.SleepForLagDuration();
                await Coroutine.Sleep(250);
            }
            return true;
        }

        public override string Name
        {
            get { return Globalization.LevelBot_Name; }
        }

        public override bool IsPrimaryType
        {
            get { return true; }
        }

        public override bool RequirementsMet
        {
            get { return true; }
        }

        public override bool RequiresProfile
        {
            get { return true; }
        }

        public override bool RequiresProfileScope
        {
            get { return true; }
        }

        public static BehaviorFlags BehaviorFlags
        {
            get { return _behaviorFlags; }
            set
            {
                if (_behaviorFlags != value)
                {
                    Logging.WriteDiagnostic("Behavior flags changed! {0} -> {1}", new object[] { _behaviorFlags, value });
                    _behaviorFlags = value;
                }
            }
        }

        public override Composite Root
        {
            get
            {
                return _root ?? (_root = new PrioritySelector(
                    new HookExecutor("Levelbot_Main", "The main Level root behavior location. Any Leveling logic is done within this hook, or child hooks.",
                        new PrioritySelector(
                            CreateDeathBehavior(),
                            CreateCombatBehavior(),
                            CreateLootBehavior(),
                            CreateVendorBehavior(),
                            CreateRoamBehavior(NavType.Run),
                            new ActionIdle()))));
            }
        }

        public override PulseFlags PulseFlags
        {
            get { return PulseFlags.All; }
        }

        public override void Start()
        {
            if (ProfileManager.CurrentOuterProfile == null)
                throw new HonorbuddyUnableToStartException(LevelbotResources.LevelBot_Start_You_haven_t_loaded_a_profile_);

            if (StyxWoW.AreaManager.CurrentGrindArea != null)
                StyxWoW.AreaManager.CurrentGrindArea.CycleToNearest();

            Targeting.Instance.IncludeTargetsFilter += LevelBotIncludeTargetsFilter;
            LootTargeting.Instance.IncludeTargetsFilter += LevelbotIncludeLootsFilter;
        }

        public override void Stop()
        {
            Targeting.Instance.IncludeTargetsFilter -= LevelBotIncludeTargetsFilter;
            LootTargeting.Instance.IncludeTargetsFilter -= LevelbotIncludeLootsFilter;
        }

        private static ICombatRoutine Routine
        {
            get { return RoutineManager.Current; }
        }

        private float GetPathPrecision()
        {
            float currentSpeed = StyxWoW.Me.MovementInfo.CurrentSpeed;
            float precision = currentSpeed * 0.15f;
            return Tripper.Tools.Math.MathEx.Clamp(precision, 1.5f, 10f);
        }

        public override void Pulse()
        {
            Navigator.PathPrecision = GetPathPrecision();
        }

        public override Form ConfigurationForm
        {
            get { return new FormLevelbotSettings(); }
        }

        public static void SetDefaultQueryFilter()
        {
            var meshNavigator = Navigator.NavigationProvider as MeshNavigator;
            if (meshNavigator == null)
                return;

            meshNavigator.Nav.SetFactionQueryFilter(StyxWoW.Me.IsHorde);
        }

        public static bool ShouldUseSpiritHealer { get; set; }

        private static ActionMoveToPoi MoveToPoi
        {
            get { return _moveToPoi ?? (_moveToPoi = new ActionMoveToPoi()); }
        }

        public static bool NeedToFindTarget
        {
            get
            {
                if (!BehaviorFlags.HasFlag(BehaviorFlags.Combat))
                    return false;

                WoWUnit firstUnit = Targeting.Instance.FirstUnit;
                return firstUnit != null && BehaviorFlags.HasFlag(BehaviorFlags.Combat) && BehaviorFlags.HasFlag(BehaviorFlags.Pull) &&
                       (!LevelbotSettings.Instance.GroundMountFarmingMode || !StyxWoW.Me.Mounted);
            }
        }

        private static bool NeedToMoveToHotspot
        {
            get { return StyxWoW.AreaManager.CurrentGrindArea != null && StyxWoW.AreaManager.CurrentGrindArea.HotspotChanged; }
        }

        [Obsolete("Use the Roam coroutine")]
        public static PrioritySelector CreateRoamBehavior(NavType navType = NavType.Run)
        {
            return new PrioritySelector(new ActionRunCoroutine(new Func<object, Task<bool>>(context => Roam(navType))));
        }

        [Obsolete("Use the FindTarget coroutine")]
        public static Composite CreateFindTargetBehavior(NavType navType)
        {
            return new ActionRunCoroutine(new Func<object, Task<bool>>(context => FindTarget(navType)));
        }

        [Obsolete("Use the MoveToKillPoi coroutine")]
        public static Composite CreateMoveToKillPoiBehavior()
        {
            return new ActionRunCoroutine(new Func<object, Task<bool>>(context => MoveToKillPoi()));
        }

        [Obsolete("Use the MoveToHotspot coroutine")]
        public static Composite CreateMoveToHotspotBehavior(NavType navType)
        {
            return new ActionRunCoroutine(new Func<object, Task<bool>>(context => MoveToHotspot(navType)));
        }

        public static void LevelbotIncludeLootsFilter(List<WoWObject> incomingObjects, HashSet<WoWObject> outgoingObjects)
        {
            WoWPoint location = StyxWoW.Me.Location;
            double lootRadiusSqr = LootTargeting.LootRadius * LootTargeting.LootRadius;
            for (int i = 0; i < incomingObjects.Count; i++)
            {
                var unit = incomingObjects[i] as WoWUnit;
                if (unit != null)
                {
                    if (LootTargeting.LootMobs && location.DistanceSqr(unit.Location) <= lootRadiusSqr && unit.IsDead)
                    {
                        bool canLootIt = !Blacklist.Contains(unit, BlacklistFlags.Loot) && LootPredictor.WasKilledByMe(unit) && LootPredictor.CanLoot(unit);
                        bool canSkinIt = LootTargeting.SkinMobs && unit.CanSkin && !Blacklist.Contains(unit, BlacklistFlags.Node) &&
                                         (CharacterSettings.Instance.NinjaSkin || unit.KilledByMe);
                        if (canLootIt || canSkinIt)
                            outgoingObjects.Add(unit);
                    }
                }
                else
                {
                    var gameObject = incomingObjects[i] as WoWGameObject;
                    if (gameObject != null &&
                        ((LootTargeting.HarvestHerbs && gameObject.IsHerb) || (LootTargeting.HarvestMinerals && gameObject.IsMineral) || (LootTargeting.LootChests && gameObject.IsChest)) &&
                        gameObject.CanLoot && !Blacklist.Contains(gameObject.Guid, BlacklistFlags.Node))
                    {
                        WoWPoint objectLocation = gameObject.Location;
                        if (location.DistanceSqr(objectLocation) <= lootRadiusSqr)
                        {
                            if (IsTooNearBlackspot(ProfileManager.CurrentProfile.Blackspots, objectLocation))
                                Blacklist.Add(gameObject.Guid, BlacklistFlags.Loot, TimeSpan.FromDays(3.0), "Too close to blackspot");
                            else
                                outgoingObjects.Add(gameObject);
                        }
                    }
                }
            }
        }

        public static void LevelBotIncludeTargetsFilter(List<WoWObject> incomingUnits, HashSet<WoWObject> outgoingUnits)
        {
            Profile currentProfile = ProfileManager.CurrentProfile;
            GrindArea currentGrindArea = StyxWoW.AreaManager.CurrentGrindArea;
            if (StyxWoW.Me.IsBeingAttacked)
                return;

            var factions = new HashSet<uint>();
            List<int> mobIds = new List<int>();
            if (currentGrindArea != null)
            {
                mobIds = currentGrindArea.MobIDs;
                if (currentGrindArea.Factions.Count > 0)
                {
                    foreach (int faction in currentGrindArea.Factions)
                        factions.Add((uint)faction);
                }
                else
                {
                    foreach (uint faction in currentProfile.Factions)
                        factions.Add(faction);
                }
            }
            else
            {
                foreach (uint faction in currentProfile.Factions)
                    factions.Add(faction);
            }

            int minLevel = currentGrindArea != null ? currentGrindArea.TargetMinLevel : (currentProfile != null ? currentProfile.TargetMinLevel : int.MinValue);
            int maxLevel = currentGrindArea != null ? currentGrindArea.TargetMaxLevel : (currentProfile != null ? currentProfile.TargetMaxLevel : int.MaxValue);

            var pathPoints = new List<WoWPoint>();
            var meshNavigator = Navigator.NavigationProvider as MeshNavigator;
            if (meshNavigator != null && meshNavigator.CurrentMovePath != null)
            {
                PathFindResult path = meshNavigator.CurrentMovePath.Path;
                int fromIndex = meshNavigator.CurrentMovePath.Index - 1;
                pathPoints = path.Points.Skip(fromIndex).Select(v => new WoWPoint(v.X, v.Y, v.Z)).ToList();
            }

            WoWPoint myLocation = StyxWoW.Me.Location;
            bool mounted = StyxWoW.Me.Mounted;
            foreach (WoWObject woWObject in incomingUnits)
            {
                WoWUnit unit = woWObject.ToUnit();
                if (unit.IsPlayer)
                    continue;

                WoWUnit ownedByRoot = unit.OwnedByRoot;
                if (ownedByRoot != null && (ownedByRoot.IsPlayer || ownedByRoot.IsFlightMaster))
                    continue;

                if (unit.IsCritter)
                    continue;

                if (unit.TaggedByOther && unit.CurrentTargetGuid != 0UL)
                    continue;

                WoWPoint unitLocation = unit.Location;
                if (currentProfile != null && IsTooNearBlackspot(currentProfile.Blackspots, unitLocation))
                    continue;

                int level = unit.Level;
                bool inLevelRange = level >= minLevel && level <= maxLevel;
                PoiType currentPoiType = BotPoi.Current.Type;
                bool poiIsInteractionType = currentPoiType == PoiType.Buy || currentPoiType == PoiType.Mail || currentPoiType == PoiType.Repair || currentPoiType == PoiType.Sell;

                if (inLevelRange && !poiIsInteractionType && (factions.Contains(unit.FactionId) || mobIds.Contains((int)unit.Entry)))
                {
                    outgoingUnits.Add(woWObject);
                }
                else if ((!mounted || (GlobalSettings.Instance.KillBetweenHotspots && unit.Difficulty > DifficultyColor.Gray)) &&
                         unit.CurrentTargetGuid == 0UL && unit.MyReaction < WoWUnitReaction.Neutral && pathPoints.Count > 0 &&
                         WoWMathHelper.IsInPath(unitLocation, unit.MyAggroRange, myLocation, pathPoints) &&
                         (Math.Abs(myLocation.Z - unitLocation.Z) <= 10f || unit.InLineOfSpellSight))
                {
                    outgoingUnits.Add(woWObject);
                }
            }
        }

        public static bool IsTooNearBlackspot(IEnumerable<Blackspot> blackspots, WoWPoint point)
        {
            return blackspots != null && blackspots.Any(blackspot => point.Distance2DSqr(blackspot.Location) <= blackspot.Radius * blackspot.Radius);
        }

        public static Composite CreateCombatBehavior()
        {
            var combatCoroutine = new CombatCoroutine();
            return new ActionRunCoroutine(new Func<object, CoroutineTask<bool>>(context => combatCoroutine));
        }

        private static bool CanPathTo(WoWPoint start, WoWPoint end, float maxDistance)
        {
            var meshNavigator = Navigator.NavigationProvider as MeshNavigator;
            if (meshNavigator == null)
                return true;

            PathFindResult pathFindResult = meshNavigator.FindPath(start, end);
            uint mapId = StyxWoW.Me.MapId;
            if (pathFindResult.FailStep == PathFindStep.FindStartPoly)
            {
                Logging.Write("Current location seems to be not meshed. Please report this on forums. MapId: {0} Location: {1}", new object[] { mapId, start });
                return false;
            }
            if (pathFindResult.FailStep == PathFindStep.FindEndPoly)
            {
                Logging.WriteDiagnostic("Corpse point has no mesh. MapId: {0} Location: {1}", new object[] { mapId, end });
                return false;
            }
            if (pathFindResult.IsPartialPath && pathFindResult.Points.Last().DistanceSqr(end) > maxDistance * maxDistance)
            {
                Logging.WriteDiagnostic("Path to corpse is partial and last point of the path is not within 40 yard distance. MapId: {0} Start: {1} End: {2}", new object[] { mapId, start, end });
                return false;
            }
            return pathFindResult.Succeeded;
        }

        private static bool CanGrabCorpseNow(object context)
        {
            if (BotPoi.Current != null && BotPoi.Current.Location != WoWPoint.Empty)
            {
                float distanceSqr = StyxWoW.Me.Location.DistanceSqr(BotPoi.Current.Location);
                if (distanceSqr < 25f)
                    return true;
            }
            return false;
        }

        private static RunStatus GrabCorpse(object context)
        {
            if (Lua.GetReturnVal<int>("return GetCorpseRecoveryDelay()", 0U) != 0)
            {
                Logging.Write(LevelbotResources.LevelBot_GrabCorpse_Waiting_for_corpse_recovery_delay_to_expire_);
                return RunStatus.Running;
            }
            using (new ActivitySetter(LevelbotResources.LevelBot_GrabCorpse_Grabbing_corpse))
            {
                Logging.Write(LevelbotResources.LevelBot_GrabCorpse_Clicking_corpse_popup___);
                Lua.DoString("RetrieveCorpse()", "WoW.lua");
            }
            return RunStatus.Success;
        }

        private static void ReleaseFromCorpse()
        {
            if (!_releaseTimer.IsFinished)
                return;

            using (new ActivitySetter(LevelbotResources.LevelBot_Release_Releasing_corpse))
            {
                _releaseTimer.Reset();
                GameStats.Died();
                Navigator.Clear();
                Logging.Write(LevelbotResources.LevelBot_Release_I_died_);
                _wasIndoorsOnDeath = StyxWoW.Me.IsIndoors;
                Lua.DoString("RepopMe()", "WoW.lua");
            }
        }

        internal static async Task<bool> HandleDeathCoroutine()
        {
            if (StyxWoW.Me.IsDead)
            {
                ReleaseFromCorpse();
                if (!await Coroutine.Wait(5000, () => StyxWoW.Me.IsGhost))
                {
                    Logging.WriteDiagnostic("Ghost was not released after 5 seconds of waiting! Trying again..");
                    await Coroutine.Yield();
                }
                return true;
            }
            if (StyxWoW.Me.IsGhost)
            {
                WoWPoint corpsePoint = StyxWoW.Me.CorpsePoint;
                if (StyxWoW.Me.Location.DistanceSqr(corpsePoint) > 1600f)
                {
                    if (!_wasIndoorsOnDeath && (StyxWoW.Me.Mounted || StyxWoW.Me.MovementInfo.CanFly))
                        Flightor.MoveTo(corpsePoint);
                    else
                        Navigator.MoveTo(corpsePoint);
                    await Coroutine.Yield();
                    return true;
                }
                float corpseRecoveryDelayMs = Lua.GetReturnVal<float>("return GetCorpseRecoveryDelay()", 0U) * 1000f;
                if (corpseRecoveryDelayMs > 0f)
                {
                    Logging.Write("Waiting for corpse recovery delay..");
                    await Coroutine.Sleep((int)corpseRecoveryDelayMs + 1000);
                    return true;
                }
                Logging.Write("Retrieving corpse");
                Lua.DoString("RetrieveCorpse()");
                await Coroutine.Sleep(2000);
                return true;
            }
            return false;
        }

        public static PrioritySelector CreateDeathBehavior()
        {
            return new PrioritySelector(
                new Decorator(context => BehaviorFlags.HasFlag(BehaviorFlags.Death),
                    new HookExecutor("Death_Main", "Death: Death Main Hook Location - This will call the Death Handling Functions such as Spirit Healer Res, Moving to body, and Moving to instance.",
                        new PrioritySelector(
                            new Decorator(context => ShouldUseSpiritHealer && StyxWoW.Me.IsGhost,
                                new HookExecutor("Death_SpiritHealer", "Death: Death Spirit Healer Hook Location - This will call the Death Spirit Healer Handling Functions that will talk to the Resser and accept. Does not control the waiting after.",
                                    new PrioritySelector(
                                        (ContextChangeHandler)(original => ObjectManager.GetObjectsOfType<WoWUnit>().FirstOrDefault(unit => unit.IsSpiritHealer)),
                                        new Decorator(context => context == null,
                                            new Sequence(
                                                new Action((ActionSucceedDelegate)(context => Logging.WriteDiagnostic("No spirit healer around and we want to use spirit res. Repopping to the graveyard."))),
                                                new Action((ActionSucceedDelegate)(context => Navigator.PlayerMover.MoveStop())),
                                                new Action((ActionSucceedDelegate)(context => Lua.DoString("PortGraveyard()", "WoW.lua"))),
                                                new Sleep(2000))),
                                        new Decorator(context => context != null,
                                            new PrioritySelector(
                                                new Decorator(context => ((WoWObject)context).WithinInteractRange,
                                                    new Sequence(
                                                        new ActionSetActivity("Using spirit healer"),
                                                        new Action((ActionSucceedDelegate)(context => Navigator.PlayerMover.MoveStop())),
                                                        new Action((ActionSucceedDelegate)(context => ((WoWUnit)context).Interact())),
                                                        new Wait(5, context => Lua.GetReturnVal<bool>("return StaticPopup1:IsVisible() or GossipFrame:IsVisible()", 0U),
                                                            new Sequence(
                                                                new DecoratorContinue(context => GossipFrame.Instance.IsVisible,
                                                                    new Action((ActionSucceedDelegate)(context =>
                                                                    {
                                                                        GossipFrame instance = GossipFrame.Instance;
                                                                        instance.SelectGossipOption(GossipFrame.Instance.GossipOptionEntries.First(gossipEntry => gossipEntry.Type == GossipEntry.GossipEntryType.Healer).Index);
                                                                    }))),
                                                                new Action((ActionSucceedDelegate)(context => Lua.DoString("AcceptXPLoss()", "WoW.lua"))),
                                                                new SleepForLagDuration(),
                                                                new Action((ActionSucceedDelegate)(context =>
                                                                {
                                                                    ShouldUseSpiritHealer = false;
                                                                    _corpseCampCount = 0;
                                                                })))))),
                                                new Decorator(
                                                    context => ((WoWObject)context).DistanceSqr > 1600.0 || !Navigator.CanNavigateFully(StyxWoW.Me.Location, ((WoWObject)context).Location),
                                                    new Sequence(
                                                        new ActionSetActivity("We are too far away from the spirit healer. Repoping back to graveyard to use spirit healer."),
                                                        new Action((ActionSucceedDelegate)(context => Navigator.PlayerMover.MoveStop())),
                                                        new Action((ActionSucceedDelegate)(context => Lua.DoString("PortGraveyard()", "WoW.lua"))))),
                                                new NavigationAction((GetPointDelegate)(context => ((WoWUnit)context).Location))))))),
                            new Decorator(context => StyxWoW.Me.IsDead,
                                new PrioritySelector(
                                    new Decorator(context => RoutineManager.Current.DeathBehavior != null, RoutineManager.Current.DeathBehavior),
                                    new Sequence(
                                        new ActionSetActivity(LevelbotResources.LevelBot_CreateDeathBehavior_Releasing_from_corpse),
                                        new DecoratorContinue(context => CharacterSettings.Instance.RessAtSpiritHealers,
                                            new Action((ActionSucceedDelegate)(context =>
                                            {
                                                if (StyxWoW.Me.CurrentMap.MapType != MapType.Common)
                                                    return;

                                                if (!_corpseCampTimer.IsFinished)
                                                {
                                                    _corpseCampCount++;
                                                    Logging.Write(LevelbotResources.LevelBot_GrabCorpse_Corpse_possibly_being_camped__Camp_count__ + _corpseCampCount + "/10");
                                                }
                                                else
                                                {
                                                    _corpseCampCount = 0;
                                                }
                                                _corpseCampTimer.Reset();
                                                if (_corpseCampCount >= 10)
                                                {
                                                    Logging.Write(LevelbotResources.LevelBot_GrabCorpse_Corpse_camp_protection_tripped__Attempting_to_resurrect_at_a_spirit_healer_);
                                                    ShouldUseSpiritHealer = true;
                                                    _corpseCampCount = 0;
                                                    return;
                                                }
                                                if (_corpseCampCount >= 2 && StyxWoW.Me.LowestDurabilityPercent <= (double)ProfileManager.CurrentProfile.MinDurability)
                                                {
                                                    Logging.Write("Died more then 2 times in 3 minutes period and current durability is lower then the MinDurability provided by current profile. Using spirit rez to avoid getting camped.");
                                                    ShouldUseSpiritHealer = true;
                                                    _corpseCampCount = 0;
                                                }
                                            }))),
                                        new Sleep((Func<object, TimeSpan>)(context => TimeSpan.FromMilliseconds(new Random().Next(1500, 2500)))),
                                        new Action((ActionSucceedDelegate)(context => ReleaseFromCorpse())),
                                        new WaitContinue(5, context => StyxWoW.Me.IsGhost, new SleepForLagDuration()),
                                        new DecoratorContinue(context => StyxWoW.Me.ZoneId == 6757U,
                                            new Sequence(
                                                new Action((ActionSucceedDelegate)(context => TreeRoot.StatusText = "Waiting for spirit healer resurrection")),
                                                new WaitContinue(40, context => StyxWoW.Me.IsAlive, new ActionAlwaysSucceed())))))),
                            new DecoratorIsNotPoiType(PoiType.Corpse,
                                new Decorator(
                                    context => StyxWoW.Me.IsGhost && StyxWoW.Me.CorpsePoint != WoWPoint.Empty && !StyxWoW.Me.IsFalling &&
                                               StyxWoW.Me.Location.DistanceSqr(StyxWoW.Me.CorpsePoint) > 1600f &&
                                               (_wasIndoorsOnDeath || (!StyxWoW.Me.Mounted && !StyxWoW.Me.MovementInfo.CanFly)) &&
                                               !CanPathTo(StyxWoW.Me.Location, StyxWoW.Me.CorpsePoint, 40f),
                                    new Sequence(
                                        new Action((ActionSucceedDelegate)(context => Logging.Write("Can't navigate to our corpse. Trying the spirit healer instead! Corpse Point: {0}", new object[] { StyxWoW.Me.CorpsePoint }))),
                                        new Action((ActionSucceedDelegate)(context => ShouldUseSpiritHealer = true))))),
                            new DecoratorIsNotPoiType(PoiType.Corpse,
                                new HookExecutor("Death_CorpseRun", "Death: Death Corpse Run Hook Location - This will call the Death Corpse Run Handling Functions that will run or fly to your corpse including the death gryphons.",
                                    new Decorator(context => StyxWoW.Me.IsGhost && StyxWoW.Me.Location.DistanceSqr(StyxWoW.Me.CorpsePoint) >= 1600f,
                                        new Sequence(
                                            new Wait(10, context => StyxWoW.Me.CorpsePoint != WoWPoint.Empty, new ActionAlwaysSucceed()),
                                            new ActionSetActivity(LevelbotResources.LevelBot_CreateDeathBehavior_Moving_to_corpse),
                                            new PrioritySelector(
                                                new Decorator(context => !_wasIndoorsOnDeath && Flightor.MountHelper.Mounted,
                                                    new Action((ActionSucceedDelegate)(context => Flightor.MoveTo(StyxWoW.Me.CorpsePoint, 40f, false)))),
                                                new Action((ActionSucceedDelegate)(context => Navigator.MoveTo(StyxWoW.Me.CorpsePoint)))))))),
                            new HookExecutor("Death_CorpseRetrieval", "Death: Death Corpse Retrieval Hook Location - This will call the Death Corpse Retrieval Handling Functions that will find a safe spot, wait for the countdown, move inside instance portal, and accept corpse resurrection.",
                                new PrioritySelector(
                                    new Decorator(
                                        context => (StyxWoW.Me.IsGhost && StyxWoW.Me.Location.DistanceSqr(StyxWoW.Me.CorpsePoint) < 1600f) || BotPoi.Current.Type == PoiType.Corpse,
                                        new PrioritySelector(
                                            new Decorator(context => StyxWoW.Me.IsAlive,
                                                new Sequence(
                                                    new Action((ActionSucceedDelegate)(context => _corpseRetrievalStopwatch.Reset())),
                                                    new ActionClearPoi("Resurrected"))),
                                            new DecoratorIsNotPoiType(PoiType.Corpse,
                                                new Sequence(
                                                    new WaitContinue(5, context => ObjectManager.GetObjectsOfType<WoWUnit>(false).Count > 0, new ActionAlwaysSucceed()),
                                                    new Sleep(1000),
                                                    new ActionSetPoi(true, context => new BotPoi(DeathHelpers.FindSafeSpot(), PoiType.Corpse, Flightor.MountHelper.Mounted ? NavType.Fly : NavType.Run)),
                                                    new Action((ActionSucceedDelegate)(context => _corpseRetrievalStopwatch.Start())))),
                                            new Decorator(context => BotPoi.Current.Location == StyxWoW.Me.CorpsePoint,
                                                new Sequence(
                                                    new ActionSetActivity(LevelbotResources.LevelBot_CreateDeathBehavior_Safespot_is_invalid__Grabbing_corpse__),
                                                    new Action((ActionSucceedDelegate)(context => Navigator.PlayerMover.MoveStop())),
                                                    new Action((ActionDelegate)GrabCorpse),
                                                    new Action((ActionSucceedDelegate)(context => _corpseRetrievalStopwatch.Reset())),
                                                    new ActionClearPoi("Grabbed our corpse."))),
                                            new Decorator(context => _corpseRetrievalStopwatch.Elapsed.TotalSeconds > 40.0,
                                                new Sequence(
                                                    new ActionSetActivity(LevelbotResources.LevelBot_CreateDeathBehavior_SafeRes_timer_expired___Grabbing_our_corpse_where_we_are_),
                                                    new ActionSetActivity(LevelbotResources.LevelBot_CreateDeathBehavior_Grabbing_corpse),
                                                    new Action((ActionSucceedDelegate)(context => Navigator.PlayerMover.MoveStop())),
                                                    new Action((ActionDelegate)GrabCorpse),
                                                    new Action((ActionSucceedDelegate)(context => _corpseRetrievalStopwatch.Reset())),
                                                    new WaitContinue(5, context => StyxWoW.Me.IsAlive, new ActionAlwaysSucceed()),
                                                    new ActionClearPoi("Res timer expired. Grabbed our corpse."))),
                                            new Decorator(new CanRunDecoratorDelegate(CanGrabCorpseNow),
                                                new Sequence(
                                                    new ActionSetActivity(LevelbotResources.LevelBot_CreateDeathBehavior_Grabbing_corpse),
                                                    new Action((ActionSucceedDelegate)(context => Navigator.PlayerMover.MoveStop())),
                                                    new Action((ActionDelegate)GrabCorpse),
                                                    new Action((ActionSucceedDelegate)(context => _corpseRetrievalStopwatch.Reset())),
                                                    new WaitContinue(5, context => StyxWoW.Me.IsAlive, new ActionAlwaysSucceed()),
                                                    new ActionClearPoi("Grabbed corpse at safe spot"))),
                                            new Decorator(context => BotPoi.Current.Location != WoWPoint.Zero, MoveToPoi))),
                                    new ActionSuceedIfDeadOrGhost()))))));
        }

        private static HookExecutor VendorMainHookExecutor
        {
            get
            {
                return _vendorMainHookExecutor ?? (_vendorMainHookExecutor = new HookExecutor("Vendor_Main",
                    "Vendor: Vendor Main Hook Location - This will call the Vendors's Main Functions such as determining which vendors and mailboxes to go to, moving to the vendors, and handling of the selling.",
                    new ActionRunCoroutine(new Func<object, Task<bool>>(context => RunVendorLogicCoroutine()))));
            }
        }

        public static PrioritySelector CreateVendorBehavior()
        {
            return new PrioritySelector(new ActionRunCoroutine(new Func<object, Task<bool>>(context => MainVendorLogic())));
        }

        private static void BlacklistCurrentPoi(string reason)
        {
            if (BotPoi.Current.Type == PoiType.Fly)
            {
                if (BotPoi.Current.Entry > 0U)
                {
                    FlightPaths.IgnoreFp(BotPoi.Current.Entry);
                    return;
                }
                FlightPaths.IgnoreFp(BotPoi.Current.Location);
                return;
            }
            if (BotPoi.Current.AsVendor != null)
            {
                ProfileManager.CurrentProfile.VendorManager.Blacklist.Add(BotPoi.Current.AsVendor);
                return;
            }
            if (BotPoi.Current.AsMailbox != null)
            {
                ProfileManager.CurrentProfile.MailboxManager.Blacklist.Add(BotPoi.Current.AsMailbox);
                return;
            }
            Blacklist.Add(BotPoi.Current.Entry, BotPoi.Current.Location, BlacklistFlags.Interact | BlacklistFlags.NpcDatabase, TimeSpan.FromMinutes(10.0), reason);
        }

        private static Mount.MountWrapper FindGroundMount()
        {
            return Mount.Mounts.FirstOrDefault(mount => _groundMountSpellIds.Contains(mount.CreatureSpellId));
        }

        private static WoWObject FindNearestVendorMountTarget()
        {
            return ObjectManager.GetObjectsOfType<WoWUnit>(false, false)
                .Where(unit => _vendorMountTargetEntries.Contains(unit.Entry))
                .OrderBy(unit => unit.DistanceSqr)
                .FirstOrDefault();
        }

        private static bool HasVendorNearby(Vendor.VendorType vendorType)
        {
            return ProfileManager.CurrentProfile.VendorManager.GetClosestVendor(vendorType) != null;
        }

        private static bool NeedsToSell()
        {
            if (Vendors.ForceSell)
                return HasVendorNearby(Vendor.VendorType.Sell);

            return ProfileManager.CurrentProfile.MinFreeBagSlots >= 0 && (ulong)StyxWoW.Me.FreeNormalBagSlots <= (ulong)ProfileManager.CurrentProfile.MinFreeBagSlots &&
                   HasVendorNearby(Vendor.VendorType.Sell);
        }

        private static bool NeedsToRepair()
        {
            if (Vendors.RepairDisabled)
                return false;

            if (_repairCostCacheTimer.IsFinished)
            {
                WoWPrice estimatedRepairCost = StyxWoW.Me.GetEstimatedRepairCost();
                if (estimatedRepairCost.TotalCoppers != 0L)
                    Logging.WriteDiagnostic("Updating repair cost for current equipped items. New value: [{0}]", new object[] { estimatedRepairCost });

                _cachedRepairCost = (ulong)estimatedRepairCost.TotalCoppers;
                _repairCostCacheTimer.Reset();
            }
            if (StyxWoW.Me.Copper <= _cachedRepairCost)
            {
                if (Vendors.ForceRepair)
                {
                    Logging.Write(Colors.Red, "WARNING! You have no money to repair! Cancelling forced repair run.");
                    Vendors.ForceRepair = false;
                }
                return false;
            }
            if (Vendors.ForceRepair)
                return HasVendorNearby(Vendor.VendorType.Repair);

            return ProfileManager.CurrentProfile.MinDurability >= 0f && StyxWoW.Me.LowestDurabilityPercent <= (double)ProfileManager.CurrentProfile.MinDurability &&
                   HasVendorNearby(Vendor.VendorType.Repair);
        }

        private static bool NeedsToBuyFood()
        {
            if (StyxWoW.Me.Copper < 10000UL)
            {
                if (Vendors.ForceBuy)
                {
                    Logging.Write(Colors.Red, "WARNING! You have no money to repair! Cancelling forced restock run.");
                    Vendors.ForceBuy = false;
                }
                return false;
            }
            if (Vendors.ForceBuy)
                return HasVendorNearby(Vendor.VendorType.Food);

            if ((StyxWoW.Me.PowerType == WoWPowerType.Mana || StyxWoW.Me.Class == WoWClass.Druid) && Consumable.GetBestDrink(false) == null && CharacterSettings.Instance.DrinkAmount > 0)
                return HasVendorNearby(Vendor.VendorType.Food);

            return Consumable.GetBestFood(false) == null && CharacterSettings.Instance.FoodAmount > 0 && HasVendorNearby(Vendor.VendorType.Food);
        }

        private static bool NeedsToMail()
        {
            LocalPlayer me = StyxWoW.Me;
            Profile currentProfile = ProfileManager.CurrentProfile;
            if (!Vendors.ForceMail && BotPoi.Current.Type != PoiType.Sell)
                return false;

            if (string.IsNullOrEmpty(CharacterSettings.Instance.MailRecipient))
            {
                Logging.WriteDiagnostic("Cannot mail because there is no mail recipient");
                Vendors.ForceMail = false;
                return false;
            }
            if (currentProfile == null)
            {
                Logging.WriteDiagnostic("Cannot mail because current profile is null");
                Vendors.ForceMail = false;
                return false;
            }
            if (ProfileManager.CurrentProfile.MailboxManager == null)
            {
                Logging.WriteDiagnostic("Cannot mail because current profile has no mailboxes");
                Vendors.ForceMail = false;
                return false;
            }
            if (ProfileManager.CurrentProfile.MailboxManager.GetClosestMailbox() == null
                && Query.GetNearestMailbox(StyxWoW.Me.MapId, StyxWoW.Me.Location) == null)
            {
                Logging.WriteDiagnostic("Cannot mail because no nearby mailbox could be found");
                Vendors.ForceMail = false;
                return false;
            }
            if (me.Level < currentProfile.MinMailLevel)
            {
                Logging.WriteDiagnostic("Cannot mail because character is lower level than the MinMailLevel specified in profile");
                Vendors.ForceMail = false;
                return false;
            }
            return true;
        }

        private static void CloseAllVendorFrames()
        {
            using (StyxWoW.Memory.AcquireFrame())
            {
                if (MerchantFrame.Instance.IsVisible)
                    MerchantFrame.Instance.Close();
                if (GossipFrame.Instance.IsVisible)
                    GossipFrame.Instance.Close();
                if (TaxiFrame.Instance.IsVisible)
                    TaxiFrame.Instance.Close();
                if (TrainerFrame.Instance.IsVisible)
                    TrainerFrame.Instance.Close();
                if (MailFrame.Instance.IsVisible)
                    MailFrame.Instance.Close();
            }
        }

        private static bool IsAnyVendorFrameOpen()
        {
            switch (BotPoi.Current.Type)
            {
                case PoiType.Buy:
                case PoiType.Sell:
                case PoiType.Repair:
                    return MerchantFrame.Instance.IsVisible || GossipFrame.Instance.IsVisible;
                case PoiType.Train:
                    return TrainerFrame.Instance.IsVisible || GossipFrame.Instance.IsVisible;
                case PoiType.Mail:
                    return MailFrame.Instance.IsVisible || GossipFrame.Instance.IsVisible;
                case PoiType.Fly:
                    return TaxiFrame.Instance.IsVisible || GossipFrame.Instance.IsVisible;
                default:
                    return false;
            }
        }

        private static bool IsMerchantFrameClosed(object context) { return !MerchantFrame.Instance.IsVisible; }

        private static void LogAndBlacklistNoMerchantFrame(object context)
        {
            Logging.Write(Colors.Red, "Vendor doesn't offer a merchant frame: '{0}' blacklisting!", new object[] { string.Format("{0} [{1}]", BotPoi.Current.Name, BotPoi.Current.Entry) });
            BlacklistCurrentPoi("Vendor doesn't offer merchant frame");
            BotPoi.Clear("Vendor blacklisted");
        }

        private static bool IsCurrentPoiVendor(object context) { return BotPoi.Current.AsObject.ToUnit().IsVendor; }

        private static bool IsCurrentPoiRepairMerchant(object context) { return BotPoi.Current.AsObject.ToUnit().IsRepairMerchant; }

        private static void RepairAllItems(object context) { Vendors.RepairAllItems(); }

        private static void ForceMailNext(object context) { Vendors.ForceMail = true; }

        private static bool StillHasFullBagsAfterSellRun(object context)
        {
            return !Vendors.ForceMail && (ulong)StyxWoW.Me.FreeNormalBagSlots <= (ulong)ProfileManager.CurrentProfile.MinFreeBagSlots;
        }

        private static void LogFullBagsAfterSellRun(object context) { Logging.Write("We have just done a sell run and bags are still full. Stopping the bot."); }

        private static void StopForFullBags(object context) { TreeRoot.Stop("Full bags"); }

        private static bool IsNotMailPoi(object context) { return BotPoi.Current.Type != PoiType.Mail; }

        private static void CloseMerchantFrame(object context) { MerchantFrame.Instance.Close(); }

        private static void ClearMyTarget(object context) { StyxWoW.Me.ClearTarget(); }

        private static void BuyVendorItems(object context) { Vendors.BuyItems(); }

        private static bool IsTrainerFrameVisible(object context) { return TrainerFrame.Instance.IsVisible; }

        private static void TrainSkills(object context) { Vendors.TrainSkills(); }

        private static void CloseTrainer(object context) { Lua.DoString("CloseTrainer()", "WoW.lua"); }

        private static bool CurrentPoiIsNone_1() { return BotPoi.Current.Type == PoiType.None; }
        private static bool CurrentPoiIsNone_2() { return BotPoi.Current.Type == PoiType.None; }
        private static bool CurrentPoiIsNone_3() { return BotPoi.Current.Type == PoiType.None; }

        private static bool NotInSpecialShapeshiftForm(object context) { return !_specialShapeshiftForms.Contains(StyxWoW.Me.Shapeshift); }

        private static bool IsTargetingFirstUnit() { return StyxWoW.Me.CurrentTargetGuid == Targeting.Instance.FirstUnit.Guid; }

        private static bool IsMounted() { return StyxWoW.Me.Mounted; }

        public static async Task<bool> Roam(NavType navType = NavType.Run)
        {
            if (!BehaviorFlags.HasFlag(BehaviorFlags.Roam))
                return false;

            if (await FindTarget(navType))
                return true;

            if (await MoveToKillPoi())
                return true;

            return await MoveToHotspot(navType);
        }

        public static async Task<bool> FindTarget(NavType navType)
        {
            if (BotPoi.Current.Type.IsOneOf(PoiType.Kill, PoiType.Loot, PoiType.Skin, PoiType.Harvest))
                return false;

            if (!NeedToFindTarget)
                return false;

            if (StyxWoW.Me.CurrentTargetGuid != Targeting.Instance.FirstUnit.Guid)
            {
                Targeting.Instance.FirstUnit.Target();
                if (!await Coroutine.Wait(5000, () => StyxWoW.Me.CurrentTargetGuid == Targeting.Instance.FirstUnit.Guid))
                    return false;
            }

            BotPoi.Current = new BotPoi(Targeting.Instance.FirstUnit, PoiType.Kill, navType);
            return true;
        }

        public static async Task<bool> MoveToKillPoi()
        {
            if (BotPoi.Current.Type != PoiType.Kill)
                return false;

            WoWUnit target = BotPoi.Current.AsObject as WoWUnit;
            if (target != null && StyxWoW.Me.CurrentTargetGuid != target.Guid)
            {
                target.Target();
                if (!await Coroutine.Wait(5000, () => StyxWoW.Me.CurrentTargetGuid == target.Guid))
                    return false;
            }

            if (StyxWoW.Me.GotTarget && RoutineManager.Current.MoveToTargetBehavior != null)
            {
                if (await RoutineManager.Current.MoveToTargetBehavior.ExecuteCoroutine())
                    return true;
            }

            if (await CommonCoroutines.LandAndDismount("Landing for kill poi", dismount: false))
                return true;

            MoveResult moveResult = await CommonCoroutines.MoveTo(BotPoi.Current.Location, "kill poi");
            return moveResult.IsSuccessful();
        }

        public static async Task<bool> MoveToHotspot(NavType navType)
        {
            if (!BotPoi.Current.Type.IsOneOf(PoiType.None, PoiType.Hotspot) || !NeedToMoveToHotspot)
                return false;

            Hotspot hotspot = StyxWoW.AreaManager.CurrentGrindArea.CurrentHotSpot;
            if (BotPoi.Current.Type == PoiType.Hotspot && BotPoi.Current.Location == hotspot)
            {
                TreeRoot.StatusText = LevelbotResources.LevelBot_CreateRoamBehavior_Moving_to_hotspot;
                return await MoveToPoi.ExecuteCoroutine();
            }

            BotPoi.Current = new BotPoi(hotspot, PoiType.Hotspot, navType);
            return true;
        }

        public static async Task<bool> MainVendorLogic()
        {
            if (ProfileManager.CurrentProfile == null)
                return false;

            return await VendorMainHookExecutor.ExecuteCoroutine();
        }

        private static readonly Random _vendorRandom = new Random();

        private static async Task<bool> SellAllItemsCoroutine()
        {
            MerchantFrame merchant = MerchantFrame.Instance;
            if (!merchant.IsVisible)
            {
                Vendors.SellAllItems();
                return true;
            }

            Profile currentProfile = ProfileManager.CurrentProfile;
            ItemQuality sellQualities = ItemQuality.None;
            if (currentProfile != null)
            {
                if (currentProfile.SellGrey) sellQualities |= ItemQuality.Poor;
                if (currentProfile.SellWhite) sellQualities |= ItemQuality.Common;
                if (currentProfile.SellGreen) sellQualities |= ItemQuality.Uncommon;
                if (currentProfile.SellBlue) sellQualities |= ItemQuality.Rare;
                if (currentProfile.SellPurple) sellQualities |= ItemQuality.Epic;
            }
            if (sellQualities == ItemQuality.None)
                return true;

            HashSet<string> protectedNames = new HashSet<string>(ProtectedItemsManager.GetAllItemNames().Select(n => n.ToLowerInvariant()));
            HashSet<uint> protectedIds = new HashSet<uint>(ProtectedItemsManager.GetAllItemIds());

            foreach (WoWItem eq in EquipmentManager.EquipmentSets.SelectMany(s => s.Items))
                protectedIds.Add(eq.Entry);
            foreach (Styx.WoWInternals.Quest.QuestObjective obj in StyxWoW.Me.QuestLog.GetAllQuests().SelectMany(q => q.GetObjectives()))
            {
                if (obj.Type == Styx.WoWInternals.Quest.QuestObjectiveType.CollectIntermediateItem || obj.Type == Styx.WoWInternals.Quest.QuestObjectiveType.CollectItem)
                    protectedIds.Add(obj.ID);
            }
            uint foodId;
            if (uint.TryParse(CharacterSettings.Instance.FoodName, out foodId))
                protectedIds.Add(foodId);
            else
                protectedNames.Add(CharacterSettings.Instance.FoodName.ToLowerInvariant());
            uint drinkId;
            if (uint.TryParse(CharacterSettings.Instance.DrinkName, out drinkId))
                protectedIds.Add(drinkId);
            else
                protectedNames.Add(CharacterSettings.Instance.DrinkName.ToLowerInvariant());

            if (Vendors.OnVendorItems != null)
            {
                SellItemsEventArgs sellItemsEventArgs = new SellItemsEventArgs();
                foreach (Delegate d in Vendors.OnVendorItems.GetInvocationList())
                {
                    try
                    {
                        ((VendorItemsEventHandler)d)(sellItemsEventArgs);
                    }
                    catch
                    {
                        sellItemsEventArgs.NameExceptions.Clear();
                        sellItemsEventArgs.IdExceptions.Clear();
                        continue;
                    }
                    foreach (string name in sellItemsEventArgs.NameExceptions)
                        protectedNames.Add(name.ToLowerInvariant());
                    foreach (uint id in sellItemsEventArgs.IdExceptions)
                        protectedIds.Add(id);
                    sellItemsEventArgs.NameExceptions.Clear();
                    sellItemsEventArgs.IdExceptions.Clear();
                }
            }

            List<WoWItem> itemsToSell = new List<WoWItem>();
            foreach (WoWItem item in StyxWoW.Me.BagItems)
            {
                if (protectedIds.Contains(item.Entry) || protectedNames.Contains(item.Name.ToLowerInvariant()))
                    continue;
                if (item.ItemInfo.Bond == WoWItemBondType.Quest || item.ItemInfo.ItemClass == WoWItemClass.Key
                    || item.IsConjured || item.ItemInfo.BeginQuestId != 0 || item.ItemInfo.SellPrice == 0
                    || item.ItemInfo.IsAccountBound || item.ItemInfo.IsBattleNetAccountBound)
                    continue;
                if ((sellQualities & (ItemQuality)(1 << (int)item.Quality)) == 0)
                    continue;
                itemsToSell.Add(item);
            }

            foreach (WoWItem item in itemsToSell)
            {
                if (!merchant.IsVisible)
                    break;
                merchant.SellItem(item);
                await Coroutine.Sleep(_vendorRandom.Next(180, 650));
            }
            await Coroutine.Sleep(_vendorRandom.Next(400, 900));
            Vendors.ForceSell = false;
            return true;
        }

        private static async Task<bool> RunVendorLogicCoroutine()
        {
            if (await ApproachVendorPoiCoroutine())
                return true;

            if (await FindVendorDestinationCoroutine())
            {
                CloseAllVendorFrames();
                return true;
            }

            return false;
        }

        private static async Task<bool> ApproachVendorPoiCoroutine()
        {
            bool isVendorPoi = BotPoi.Current.Type.IsOneOf(PoiType.Sell, PoiType.Repair, PoiType.Mail, PoiType.Buy, PoiType.Train);
            bool isFlyPoi = BotPoi.Current.Type == PoiType.Fly;

            if (!isVendorPoi && !isFlyPoi)
                return false;

            if (isVendorPoi && !BehaviorFlags.HasFlag(BehaviorFlags.Vendor))
                return false;

            if (isFlyPoi && !BehaviorFlags.HasFlag(BehaviorFlags.FlightPath))
                return false;

            if (BotPoi.Current.Type.IsOneOf(PoiType.Sell, PoiType.Repair))
            {
                if (await MountVendor(BotPoi.Current.Type))
                    return true;
            }

            WoWObject poiObject = BotPoi.Current.AsObject;
            if (poiObject != null && !poiObject.WithinInteractRange)
            {
                if (await MoveToPoi.ExecuteCoroutine())
                    return true;
            }
            else if (poiObject == null && BotPoi.Current.Location.DistanceSqr(StyxWoW.Me.Location) > 25f)
            {
                if (await MoveToPoi.ExecuteCoroutine())
                    return true;
            }

            if (BotPoi.Current.Type == PoiType.Fly && BotPoi.Current.Entry == 0U)
            {
                WoWUnit nearestFlightMerchant = FlightPaths.NearestFlightMerchant;
                if (nearestFlightMerchant != null)
                {
                    BotPoi.Current.Entry = nearestFlightMerchant.Entry;
                    BotPoi.Current.Location = nearestFlightMerchant.Location;
                    return true;
                }
            }

            if (poiObject == null)
            {
                await BlacklistUnreachableVendorPoi();
                return true;
            }

            if (!IsAnyVendorFrameOpen())
                await InteractVendorObjectCoroutine();
            else
                await RunVendorHooksCoroutine();

            return true;
        }

        private static async Task<bool> InteractVendorObjectCoroutine()
        {
            WoWObject poiObject = BotPoi.Current.AsObject;
            if (poiObject == null)
                return false;

            if (!_vendorMountTargetEntries.Contains(poiObject.Entry) && poiObject.DistanceSqr < 1.0)
            {
                WoWPoint destination = _random.Next(0, 2) < 1
                    ? WoWMathHelper.CalculatePointInFront(poiObject.Location, poiObject.Rotation, 3f)
                    : WoWMathHelper.CalculatePointBehind(poiObject.Location, poiObject.Rotation, 3f);

                MoveResult moveResult = await CommonCoroutines.MoveTo(destination);
                if (moveResult.IsSuccessful())
                {
                    Logging.WriteDiagnostic("Too close to the vendor. Moving away a bit to avoid getting stuck by WoW interact bug.");
                    return true;
                }
            }

            if (StyxWoW.Me.IsMoving)
                await CommonCoroutines.StopMoving();

            await Coroutine.Sleep(500);

            if (!poiObject.IsValid)
                return false;

            poiObject.Interact();

            if (BotPoi.Current.Type == PoiType.Fly)
            {
                if (await CommonCoroutines.WaitForLuaEvent("TAXIMAP_OPENED", (int)(6000 + StyxWoW.WoWClient.Latency), () => BotPoi.Current.Type == PoiType.None))
                    return true;

                if (BotPoi.Current.Type == PoiType.Fly)
                {
                    if (!IsAnyVendorFrameOpen() && FlightPaths.Reason == FlightPathReason.Learn)
                    {
                        if (!poiObject.IsValid)
                            return false;

                        poiObject.Interact();
                        if (await CommonCoroutines.WaitForLuaEvent("TAXIMAP_OPENED", (int)(2000 + StyxWoW.WoWClient.Latency), () => BotPoi.Current.Type == PoiType.None))
                            return true;

                        return await FinishVendorInteractCoroutine();
                    }

                    return await FinishVendorInteractCoroutine();
                }
            }

            if (!await Coroutine.Wait(5000, IsAnyVendorFrameOpen))
            {
                WoWUnit poiUnit = poiObject as WoWUnit;
                if (poiUnit != null && !poiUnit.CanInteract)
                {
                    string reason = $"{poiUnit.SafeName} cannot be interacted with";
                    BlacklistCurrentPoi(reason);
                    BotPoi.Clear(reason);
                    return true;
                }
                return false;
            }

            await SelectGossipOptionCoroutine(BotPoi.Current.Type.GetGossipType());
            return true;
        }

        private static async Task<bool> FinishVendorInteractCoroutine()
        {
            if (GossipFrame.Instance.IsVisible)
            {
                await SelectGossipOptionCoroutine(BotPoi.Current.Type.GetGossipType());
                if (await CommonCoroutines.WaitForLuaEvent("TAXIMAP_OPENED", (int)(2000 + StyxWoW.WoWClient.Latency), () => BotPoi.Current.Type == PoiType.None))
                    return true;
            }

            Logging.Write(Colors.Red, "Taximap failed to open. Blacklisting the flight master.");
            BlacklistCurrentPoi("Taximap failed to open");
            BotPoi.Clear("Blacklisted flight master");
            return true;
        }

        private static async Task<bool> RunVendorHooksCoroutine()
        {
            if (_vendorRootSelector == null)
            {
                _vendorRootSelector = new PrioritySelector(
                    new DecoratorIsPoiType(new[] { PoiType.Sell, PoiType.Repair },
                        new PrioritySelector(
                            new Decorator(IsMerchantFrameClosed, new Action((ActionSucceedDelegate)LogAndBlacklistNoMerchantFrame)),
                            new Sequence(
                                new HookExecutor("Vendor_Sell", "Vendor: Vendor Sell Hook Location - This will call the Vendors's Sell Functions to just sell items, will not handle opening of the Frame or closing it. This will just sell items.",
                                    new DecoratorContinue(IsCurrentPoiVendor,
                                        new Sequence(
                                            new ActionDebugString("Selling items"),
                                            new ActionSetActivity(LevelbotResources.LevelBot_CreateVendorBehavior_Selling_Items),
                                            new ActionRunCoroutine(new Func<object, Task<bool>>(context => SellAllItemsCoroutine()))))),
                                new HookExecutor("Vendor_Repair", "Vendor: Vendor Repair Hook Location - This will call the Vendors's Repair Functions to just Repair items, will only repair items.",
                                    new DecoratorContinue(IsCurrentPoiRepairMerchant,
                                        new Sequence(
                                            new ActionDebugString("Repairing items"),
                                            new ActionSetActivity(LevelbotResources.LevelBot_CreateVendorBehavior_Repairing_Items),
                                            new Action((ActionSucceedDelegate)RepairAllItems),
                                            new Sleep(4000)))),
                                new DecoratorContinue((object context) => NeedsToMail(), new Action((ActionSucceedDelegate)ForceMailNext)),
                                new DecoratorContinue(StillHasFullBagsAfterSellRun,
                                    new Sequence(
                                        new Action((ActionSucceedDelegate)LogFullBagsAfterSellRun),
                                        new Action((ActionSucceedDelegate)StopForFullBags))),
                                new DecoratorContinue(IsNotMailPoi, new ActionClearPoi("Done selling/repairing")),
                                new Action((ActionSucceedDelegate)CloseMerchantFrame),
                                new Action((ActionSucceedDelegate)ClearMyTarget)))),
                    new HookExecutor("Vendor_Mail", "Vendor: Vendor Mail Hook Location - This will call the Vendors's Mail Functions to just Mail items. Will not control the Frame.",
                        new ActionRunCoroutine(new Func<object, Task<bool>>(context => MailItemsCoroutine()))),
                    new HookExecutor("Vendor_Buy", "Vendor: Vendor Buy Hook Location - This will call the Vendors's Buy Functions to just Buy items. Will not control the Frame.",
                        new DecoratorIsPoiType(PoiType.Buy,
                            new Sequence(
                                new ActionDebugString("Buying items"),
                                new ActionSetActivity(LevelbotResources.LevelBot_CreateVendorBehavior_Buying_Items),
                                new Sleep(1000),
                                new Action((ActionSucceedDelegate)BuyVendorItems),
                                new Sleep(5000),
                                new Action((ActionSucceedDelegate)CloseMerchantFrame),
                                new ActionClearPoi("Done buying")))),
                    new HookExecutor("Vendor_Train", "Vendor: Vendor Train Hook Location - This will call the Vendors's Train Functions to any skill. This includes mount training and profession training. Will not control the Frame.",
                        new DecoratorIsPoiType(PoiType.Train,
                            new Sequence(
                                new Wait(3, IsTrainerFrameVisible, null),
                                new ActionDebugString(LevelbotResources.LevelBot_CreateVendorBehavior_Training_Skills),
                                new ActionSetActivity(LevelbotResources.LevelBot_CreateVendorBehavior_Training_Skills),
                                new Action((ActionSucceedDelegate)TrainSkills),
                                new Sleep(2000),
                                new Action((ActionSucceedDelegate)CloseTrainer),
                                new ActionClearPoi("Done training")))),
                    new DecoratorIsPoiType(PoiType.Fly,
                        new PrioritySelector(
                            new ActionRunCoroutine(new Func<object, Task<bool>>(context => TakeFlightPathCoroutine())),
                            new ActionAlwaysSucceed())));
            }

            return await _vendorRootSelector.ExecuteCoroutine();
        }

        private static async Task<bool> FindVendorDestinationCoroutine()
        {
            if (BotPoi.Current.Type == PoiType.Kill)
                return false;

            if (!BehaviorFlags.HasFlag(BehaviorFlags.Vendor))
                return TryFindFlightPathDestination();

            if (NeedsToMail())
            {
                Mailbox closestMailbox = ProfileManager.CurrentProfile.MailboxManager.GetClosestMailbox();
                if (closestMailbox == null)
                {
                    MailboxResult queryMailbox = Query.GetNearestMailbox(StyxWoW.Me.MapId, StyxWoW.Me.Location);
                    if (queryMailbox != null)
                        closestMailbox = new Mailbox(queryMailbox.Location);
                }
                if (closestMailbox != null)
                {
                    BotPoi.Current = new BotPoi(closestMailbox);
                    return true;
                }
            }

            PoiType? sellOrRepair = NeedsToSell() ? PoiType.Sell : (NeedsToRepair() ? PoiType.Repair : (PoiType?)null);
            if (sellOrRepair.HasValue)
            {
                if (!await MountVendor(sellOrRepair.Value))
                {
                    Vendor closestVendor = ProfileManager.CurrentProfile.VendorManager.GetClosestVendor(Vendor.VendorType.Repair);
                    BotPoi.Current = new BotPoi(closestVendor, sellOrRepair.Value);
                }
                return true;
            }

            if (!NeedsToBuyFood())
                return TryFindFlightPathDestination();

            Vendor closestFoodVendor = ProfileManager.CurrentProfile.VendorManager.GetClosestVendor(Vendor.VendorType.Food);
            BotPoi.Current = new BotPoi(closestFoodVendor, PoiType.Buy);
            return true;
        }

        private static bool TryFindFlightPathDestination()
        {
            if (!BehaviorFlags.HasFlag(BehaviorFlags.FlightPath) ||
                (FlightPaths.Reason != FlightPathReason.Learn && !FlightPaths.NeedFlightPath && !FlightPaths.NeedToGrabNode))
                return false;

            FlightPaths.SetPoi();
            return true;
        }

        private static Task BlacklistUnreachableVendorPoi()
        {
            if (BotPoi.Current.Type == PoiType.Mail)
            {
                string name = string.IsNullOrWhiteSpace(BotPoi.Current.Name)
                    ? "@ " + BotPoi.Current.Location
                    : BotPoi.Current.Name + " @ " + BotPoi.Current.Location;
                Logging.Write(Colors.Red, "Could not find or move to mailbox {0} - blacklisting!", name);
                BlacklistCurrentPoi("Could not find mailbox");
                BotPoi.Clear("Could not find mailbox");
            }
            else
            {
                Logging.Write(Colors.Red, "Could not find or move to vendor: '{0}' blacklisting!", $"{BotPoi.Current.Name} [{BotPoi.Current.Entry}]");
                BlacklistCurrentPoi("Could not find poi");
            }
            BotPoi.Clear("Vendor was blacklisted");
            return Task.CompletedTask;
        }

        private static async Task<bool> SelectGossipOptionCoroutine(GossipEntry.GossipEntryType gossipEntryType)
        {
            GossipFrame gossipFrame = GossipFrame.Instance;
            if (!gossipFrame.IsVisible)
                return false;

            List<GossipEntry> entries = gossipFrame.GossipOptionEntries;
            if (entries != null)
            {
                foreach (GossipEntry entry in entries.Where(e => e.Type == gossipEntryType))
                {
                    gossipFrame.SelectGossipOption(entry.Index);
                    await Coroutine.Sleep(500);
                    await CommonCoroutines.SleepForLagDuration();
                    return true;
                }
            }

            BlacklistCurrentPoi("Could not find gossip option");
            Logging.Write("Failed to select gossip option {0}", gossipEntryType);
            BotPoi.Clear($"Couldn't find {gossipEntryType} in gossip options");
            GossipFrame.Instance.Close();
            return false;
        }

        private static async Task<bool> TakeFlightPathCoroutine()
        {
            if (BotPoi.Current.Type != PoiType.Fly)
                return false;
            if (FlightPaths.Reason != FlightPathReason.Use)
                return false;
            if (!TaxiFrame.Instance.IsVisible)
                return false;

            if (StyxWoW.Me.Mounted)
                await CommonCoroutines.Dismount("Using taxi");

            if (_specialShapeshiftForms.Contains(StyxWoW.Me.Shapeshift))
            {
                Logging.Write("Canceling Shapeshift form: {0}", StyxWoW.Me.Shapeshift);
                Lua.DoString("RunMacroText('/cancelform')");
                if (!await Coroutine.Wait(4000, () => !_specialShapeshiftForms.Contains(StyxWoW.Me.Shapeshift)))
                {
                    Logging.Write("Failed at canceling Shapeshift form");
                    return false;
                }
            }

            await Coroutine.Sleep(500);

            Logging.Write("Taking flight path to {0} from {1}", FlightPaths.TakingPathTo.Name, FlightPaths.TakingPathFrom.Name);
            TaxiFrame.TaxiFrameNode node = TaxiFrame.Instance.Nodes.FirstOrDefault(n => n.Name == FlightPaths.TakingPathTo.Name);
            if (node == null)
            {
                BlacklistCurrentPoi("TakingPathTo node is null");
                BotPoi.Clear("Ooops, TakingPathTo node is null. something is wrong with flight paths.");
                return false;
            }

            node.TakeNode();
            await CommonCoroutines.SleepForLagDuration();
            return true;
        }

        private static async Task<bool> MailItemsCoroutine()
        {
            if (BotPoi.Current.Type != PoiType.Mail)
                return false;

            Logging.WriteDiagnostic("Mailing items");
            TreeRoot.StatusText = LevelbotResources.LevelBot_CreateVendorBehavior_Mailing_Items;
            await Vendors.MailAllItemsCoroutine();
            await Coroutine.Sleep(2000);

            if (StyxWoW.Me.FreeNormalBagSlots <= ProfileManager.CurrentProfile.MinFreeBagSlots)
            {
                Logging.Write("We have just done a mail run and bags are still full. Stopping the bot.");
                TreeRoot.Stop("Full bags");
            }
            else
            {
                BotPoi.Clear("Done mailing");
            }
            return true;
        }

        public static async Task<bool> MountVendor(PoiType poiType)
        {
            Mount.MountWrapper groundMount = FindGroundMount();
            if (groundMount == null || WoWMovement.ActiveMover == null || !WoWMovement.ActiveMover.IsMe || !Mount.CanMount())
                return false;

            if (Mount.Current == groundMount && BotPoi.Current.Type == poiType && BotPoi.Current.AsObject == FindNearestVendorMountTarget())
                return false;

            await CommonCoroutines.StopMoving();

            if (!(StyxWoW.Me.Mounted && Mount.Current == groundMount))
            {
                if (await CommonCoroutines.LandAndDismount("Dismounting in order to sell"))
                    return true;

                Logging.Write("Summoning vendor mount ({0}) for vendor run", groundMount.CreatureSpellId);
                Mount.SummonMount(groundMount.CreatureSpellId);
                await Coroutine.Wait(5000, () => StyxWoW.Me.Mounted);
                await Coroutine.Sleep(1000);
                await CommonCoroutines.SleepForLagDuration();
            }

            WoWObject vendorTarget = FindNearestVendorMountTarget();
            if (vendorTarget == null)
            {
                Logging.WriteDiagnostic("No mount vendor could be found after mounting up");
                return false;
            }

            BotPoi.Current = new BotPoi(vendorTarget, poiType);
            return true;
        }

        private sealed class CombatCoroutine : CoroutineTask<bool>
        {
            public CombatCoroutine()
            {
                _combatMain = new CoroutineHookExecutor("Combat_Main", "The main combat root behavior location. Any combat logic is done within this hook, or child hooks.", CombatMainCoroutine);
                _combatOoc = new CoroutineHookExecutor("Combat_OOC", "Combat: out of combat behavior hook location. Any \"out of combat\" routine related things are done here.", CombatOutOfCombatCoroutine);
                _routineRest = new CoroutineHookExecutor("Routine_Rest", "Routine: Rest Hook Location - This will call the Routine's Resting Functions such as eating and drinking.", RestCoroutine);
                _routineBuff = new CoroutineHookExecutor("Routine_Buff", "Routine: Buff Hook Location - This will call the Routine's Buffing Functions such as 'Power Word: Fortitude', 'MotW' etc.", PreCombatBuffCoroutine);
                _combatPullBuff = new CoroutineHookExecutor("Combat_PullBuff", "Combat: Pull Buff Hook Location - This will call the Routine's Pull Buff Functions such as Power Word: Shield right befor pulling the target.", PullBuffCoroutine);
                _combatPull = new CoroutineHookExecutor("Combat_Pull", "Combat: Pull Hook Location - This will call the Routine's Pull Functions such as Movement, Attacking, and LOS to get to start attacking the target.", PullCoroutine);
                _combatHeal = new CoroutineHookExecutor("Combat_Heal", "Combat: Heal Hook Location - This will call the Routine's Heal Functions such as Healing other party members or even yourself.", HealCoroutine);
                _combatBuff = new CoroutineHookExecutor("Combat_Buff", "Combat: Buff Hook Location - This will call the Routine's Combat Buff Functions such as 'Horn of Winter', 'Power Infusion' etc..", CombatBuffCoroutine);
                _combatOnly = new CoroutineHookExecutor("Combat_Only", "Combat: Combat Only Hook Location - This will only call the Routine's Combat Function such as the main attack, will NOT call buffs, healing, resting, or pull.", CombatOnlyCoroutine);
            }

            public override async Task<bool> Run()
            {
                return await _combatMain;
            }

            private async Task<bool> CombatMainCoroutine()
            {
                if (!BehaviorFlags.HasAny(BehaviorFlags.Combat))
                    return false;

                if (Mount.ShouldDismount(BotPoi.Current.Location))
                {
                    if (await CommonCoroutines.LandAndDismount("Combat"))
                        return true;
                }

                if (BotPoi.Current.Type != PoiType.Skin && StyxWoW.Me.HasPendingSpell("Skinning"))
                {
                    Lua.DoString("SpellStopTargeting()");
                    return true;
                }

                if (BotPoi.Current.Type == PoiType.Kill && ShouldAbandonCombatPoi(out string reason))
                {
                    BotPoi.Clear(reason);
                    return true;
                }

                if (!StyxWoW.Me.IsActuallyInCombat)
                {
                    if (await _combatOoc)
                        return true;
                }

                return await CombatAttackCoroutine();
            }

            private async Task<bool> CombatOutOfCombatCoroutine()
            {
                if (BehaviorFlags.HasAny(BehaviorFlags.Rest))
                {
                    if (await _routineRest)
                        return true;
                    if (await _routineBuff)
                        return true;
                }

                if (!BehaviorFlags.HasAny(BehaviorFlags.Pull))
                    return false;

                if (BotPoi.Current.Type != PoiType.Kill)
                    return false;

                if (Blacklist.Contains(BotPoi.Current.Guid, BlacklistFlags.Pull))
                {
                    BotPoi.Clear("Pull target is blacklisted");
                    if (StyxWoW.Me.CurrentTargetGuid == BotPoi.Current.Guid)
                        StyxWoW.Me.ClearTarget();
                    return true;
                }

                if (Targeting.Instance.FirstUnit != null && BotPoi.Current.AsObject != Targeting.Instance.FirstUnit)
                    BotPoi.Current = new BotPoi(Targeting.Instance.FirstUnit, PoiType.Kill);

                WoWUnit target = BotPoi.Current.AsObject as WoWUnit;
                if (target != null && StyxWoW.Me.CurrentTarget != target)
                {
                    target.Target();
                    return true;
                }

                if (!IsTargetInPullRange())
                    return false;

                if (await _combatPullBuff)
                    return true;

                await _combatPull;
                return true;
            }

            private async Task<bool> CombatAttackCoroutine()
            {
                if (!BehaviorFlags.HasAny(BehaviorFlags.Combat))
                    return false;
                if (StyxWoW.Me.Mounted)
                    return false;
                if (!StyxWoW.Me.IsActuallyInCombat)
                    return false;

                WoWUnit firstUnit = Targeting.Instance.FirstUnit;
                if (firstUnit == null)
                    return false;

                if (BotPoi.Current.Type != PoiType.Kill || (BotPoi.Current.Guid != firstUnit.Guid && firstUnit.IsTargetingMeOrPet))
                    BotPoi.Current = new BotPoi(firstUnit, PoiType.Kill);

                if (await _combatHeal)
                    return true;
                if (await _combatBuff)
                    return true;

                await _combatOnly;
                return true;
            }

            private bool IsTargetInPullRange()
            {
                WoWUnit currentTarget = StyxWoW.Me.CurrentTarget;
                if (currentTarget == null)
                    return false;

                if (StyxWoW.Me.IsFlying)
                {
                    if (StyxWoW.Me.CurrentTarget == null)
                        return false;
                    if (StyxWoW.Me.CurrentTarget.DistanceSqr > 9.0)
                        return false;
                }
                return currentTarget.InLineOfSpellSight && currentTarget.DistanceSqr <= Targeting.PullDistanceSqr;
            }

            private bool ShouldAbandonCombatPoi(out string reason)
            {
                var unit = BotPoi.Current.AsObject as WoWUnit;
                if (unit == null)
                {
                    if (BotPoi.Current.Location != WoWPoint.Zero && StyxWoW.Me.Location.DistanceSqr(BotPoi.Current.Location) >= 100f)
                    {
                        reason = null;
                        return false;
                    }
                    reason = "POI disappeared";
                    return true;
                }

                if (unit.IsDead)
                {
                    reason = "POI is dead from combat";
                    return true;
                }
                if (unit.TaggedByOther && (Targeting.Instance.FirstUnit == null || Targeting.Instance.FirstUnit != unit))
                {
                    reason = "POI is tagged by others";
                    return true;
                }
                if (Blacklist.Contains(unit, BlacklistFlags.Combat))
                {
                    reason = "POI was blacklisted";
                    return true;
                }
                if (!unit.Attackable)
                {
                    reason = "POI is not attackable";
                    return true;
                }
                if (!unit.CanSelect)
                {
                    reason = "POI can't be selected";
                    return true;
                }
                if (unit.IsFriendly)
                {
                    reason = "POI is friendly";
                    return true;
                }
                reason = null;
                return false;
            }

            private static Task<bool> RestCoroutine() { return Routine.RestBehavior.ExecuteCoroutine(null); }
            private static Task<bool> PreCombatBuffCoroutine() { return Routine.PreCombatBuffBehavior.ExecuteCoroutine(null); }
            private static Task<bool> PullBuffCoroutine() { return Routine.PullBuffBehavior.ExecuteCoroutine(null); }
            private static Task<bool> PullCoroutine() { return Routine.PullBehavior.ExecuteCoroutine(null); }
            private static Task<bool> HealCoroutine() { return Routine.HealBehavior.ExecuteCoroutine(null); }
            private static Task<bool> CombatBuffCoroutine() { return Routine.CombatBuffBehavior.ExecuteCoroutine(null); }
            private static Task<bool> CombatOnlyCoroutine() { return Routine.CombatBehavior.ExecuteCoroutine(null); }

            private readonly CoroutineHookExecutor _combatMain;
            private readonly CoroutineHookExecutor _combatOoc;
            private readonly CoroutineHookExecutor _routineRest;
            private readonly CoroutineHookExecutor _routineBuff;
            private readonly CoroutineHookExecutor _combatPullBuff;
            private readonly CoroutineHookExecutor _combatPull;
            private readonly CoroutineHookExecutor _combatHeal;
            private readonly CoroutineHookExecutor _combatBuff;
            private readonly CoroutineHookExecutor _combatOnly;
        }

        private static class DeathHelpers
        {
            internal static WoWPoint FindSafeSpot()
            {
                List<WoWPoint> hostileLocations = ObjectManager.GetObjectsOfType<WoWUnit>(true, false)
                    .Where(unit => !unit.IsDead && unit.IsHostile && unit.Difficulty != DifficultyColor.Gray)
                    .Select(unit => unit.Location)
                    .ToList();

                WoWPoint corpsePoint = StyxWoW.Me.CorpsePoint;
                if (hostileLocations.Count <= 0)
                    return corpsePoint;

                Logging.Write(LevelbotResources.DeathHelpers_FindSafeSpot_There_are__0__hostile_mobs_, new object[] { hostileLocations.Count });

                var candidates = new List<WoWPoint>();
                for (int x = -35; x <= 35; x += 5)
                {
                    for (int y = -35; y <= 35; y += 5)
                    {
                        if (y != 0 || x != 0)
                        {
                            WoWPoint candidate = corpsePoint.Add(y, x, 0f);
                            if (candidate.DistanceSqr(corpsePoint) <= 1444f)
                                candidates.Add(candidate);
                        }
                    }
                }

                candidates = candidates
                    .OrderByDescending(candidate => hostileLocations.OrderBy(hostile => candidate.DistanceSqr(hostile)).FirstOrDefault().DistanceSqr(candidate))
                    .ToList();

                foreach (WoWPoint candidate in candidates)
                {
                    List<float> heights = Navigator.FindHeights(candidate.X, candidate.Y);
                    if (heights == null || heights.Count == 0)
                        continue;
                    float height = heights.OrderBy(h => Math.Abs(h - candidate.Z)).FirstOrDefault();
                    if (height != 0f)
                    {
                        var groundedCandidate = new WoWPoint(candidate.X, candidate.Y, height);
                        if (groundedCandidate.DistanceSqr(corpsePoint) <= 1444f)
                        {
                            WoWPoint[] path = Navigator.GeneratePath(corpsePoint, groundedCandidate);
                            if (path.Length > 0 && Navigator.AtLocation(path[path.Length - 1], groundedCandidate))
                                return groundedCandidate;
                        }
                    }
                }
                return corpsePoint;
            }
        }

        private static BehaviorFlags _behaviorFlags = BehaviorFlags.All;
        private PrioritySelector _root;

        private static PoiType _lastPoiType;
        private static ulong _lastPoiGuid;
        private static bool _lootEventAttached;
        private static int _lootRetryCount;
        private static int _lootBlacklistCount;

        private static int _corpseCampCount;
        private static readonly WaitTimer _corpseCampTimer = new WaitTimer(new TimeSpan(0, 3, 0));
        private static Stopwatch _corpseRetrievalStopwatch = new Stopwatch();
        private static bool _wasIndoorsOnDeath;
        private static readonly WaitTimer _releaseTimer = WaitTimer.FiveSeconds;
        private static ActionMoveToPoi _moveToPoi;
        private static readonly Random _random = new Random();

        private static HookExecutor _vendorMainHookExecutor;
        private static PrioritySelector _vendorRootSelector;
        private static readonly HashSet<int> _groundMountSpellIds = new HashSet<int> { 122708, 61425, 61447 };
        private static readonly ShapeshiftForm[] _specialShapeshiftForms =
        {
            ShapeshiftForm.Bear,
            ShapeshiftForm.Cat,
            ShapeshiftForm.DireBear,
            ShapeshiftForm.Travel,
            ShapeshiftForm.TreeOfLife,
            ShapeshiftForm.GhostWolf
        };
        private static readonly HashSet<uint> _vendorMountTargetEntries = new HashSet<uint> { 32639U, 32641U, 62822U };
        private static WaitTimer _repairCostCacheTimer = new WaitTimer(TimeSpan.FromMinutes(3.0));
        private static ulong _cachedRepairCost;
    }
}
