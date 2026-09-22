using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Bots.Grind;
using Bots.Quest.Resources;
using CommonBehaviors.Actions;
using CommonBehaviors.Decorators;
using Styx;
using Styx.Common;
using Styx.Common.Helpers;
using Styx.CommonBot;
using Styx.CommonBot.AreaManagement;
using Styx.CommonBot.Database;
using Styx.CommonBot.Frames;
using Styx.CommonBot.ObjectDatabase;
using Styx.CommonBot.POI;
using Styx.CommonBot.Profiles;
using Styx.CommonBot.Profiles.Quest;
using Styx.Pathing;
using Styx.TreeSharp;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;
using CreatureCacheEntry = Styx.WoWInternals.WoWCache.WoWCache.CreatureCacheEntry;
using GameObjectCacheEntry = Styx.WoWInternals.WoWCache.WoWCache.GameObjectCacheEntry;
using Vector3 = Tripper.Tools.Math.Vector3;

namespace Bots.Quest.Objectives
{
    public class CollectItemObjective : QuestObjective
    {
        private Composite _branch;
        private WoWPoint? _cachedStepPosition;
        private readonly CollectItemObjectiveInfo _overrideInfo;
        private readonly MerchantFrame _merchantFrame = new MerchantFrame();
        private readonly Stopwatch _buyTimer = new Stopwatch();
        private readonly GossipFrame _gossipFrame = new GossipFrame();
        private readonly Stopwatch _frameTimer = new Stopwatch();
        private WoWPoint? _sellingVendorLocation;
        private uint? _sellingVendorId;
        private readonly Dictionary<uint, bool> _gameObjectDropCache = new Dictionary<uint, bool>();
        private readonly Dictionary<uint, bool> _vendorDropCache = new Dictionary<uint, bool>();
        private readonly Dictionary<uint, bool> _mobDropCache = new Dictionary<uint, bool>();
        private readonly WaitTimer _lootWaitTimer = new WaitTimer(TimeSpan.FromSeconds(1.0));

        public CollectItemObjective(PlayerQuest quest, List<WoWQuestStep> questSteps, Styx.WoWInternals.Quest.QuestObjective questObjective, List<QuestObjective> prerequisites, WoWPoint forcedHotspot, NavType navType)
            : base(quest, questSteps, prerequisites, forcedHotspot, navType)
        {
            Targeting.Instance.WeighTargetsFilter += CollectItemObjective_ObjectiveWeighTargets;
            Targeting.Instance.IncludeTargetsFilter += CollectItemObjective_IncludeTargetsFilter;
            LootTargeting.Instance.IncludeTargetsFilter += CollectItemObjective_IncludeLootsFilter;
            BotEvents.Player.OnObjectHarvested += CollectItemObjective_OnObjectHarvested;
            BotEvents.Player.OnMobLooted += CollectItemObjective_OnMobLooted;
            Objective = questObjective;
            ProtectedItemsManager.Add(Objective.ID);
            if (OverridedQuestInfo != null)
                _overrideInfo = OverridedQuestInfo.FindCollectItem(Objective.ID);
        }

        public Styx.WoWInternals.Quest.QuestObjective Objective { get; private set; }

        public override bool IsCompleted
        {
            get
            {
                return StyxWoW.Me.CarriedItems.Where(item => item.Entry == Objective.ID).Sum(item => item.StackCount) >= Objective.Count;
            }
        }

        public override bool CanComplete { get { return DonePrerequisites && GotCompletionInfo(); } }

        public List<WoWGameObject> QuestObjects
        {
            get
            {
                bool ignoreBlacklist = Objective.Count == 1;
                return (from gameObject in ObjectManager.GetObjectsOfType<WoWGameObject>()
                        where GameObjectDropsItem(gameObject)
                              && (ignoreBlacklist || !Blacklist.Contains(gameObject.Guid, BlacklistFlags.Loot | BlacklistFlags.Node | BlacklistFlags.Interact))
                        orderby gameObject.DistanceSqr
                        select gameObject).ToList();
            }
        }

        public override void Dispose()
        {
            if (BotPoi.Current.Type == PoiType.Hotspot)
                BotPoi.Clear("Done with collecting items.");

            StyxWoW.AreaManager.SetArea(null);
            ProtectedItemsManager.Remove(Objective.ID);
            LootTargeting.Instance.IncludeTargetsFilter -= CollectItemObjective_IncludeLootsFilter;
            Targeting.Instance.IncludeTargetsFilter -= CollectItemObjective_IncludeTargetsFilter;
            Targeting.Instance.WeighTargetsFilter -= CollectItemObjective_ObjectiveWeighTargets;
            BotEvents.Player.OnObjectHarvested -= CollectItemObjective_OnObjectHarvested;
            BotEvents.Player.OnMobLooted -= CollectItemObjective_OnMobLooted;
        }

        public override WoWPoint GetObjectiveLocation()
        {
            List<WoWUnit> units = (from unit in ObjectManager.GetObjectsOfType<WoWUnit>()
                                   where UnitDropsQuestItem(unit) && unit.IsAlive
                                   orderby unit.Distance
                                   select unit).ToList();

            List<WoWGameObject> gameObjects = (from obj in LootTargeting.Instance.LootingList
                                               where obj is WoWGameObject
                                               let gameObject = obj.ToGameObject()
                                               where GameObjectDropsItem(gameObject)
                                               select gameObject).ToList();

            if (units.Count <= 0 && gameObjects.Count <= 0)
            {
                if (!_cachedStepPosition.HasValue)
                {
                    if (_overrideInfo != null && _overrideInfo.OverridedHotspots != null && _overrideInfo.OverridedHotspots.Count > 0)
                    {
                        _cachedStepPosition = _overrideInfo.OverridedHotspots.FindClosestTo(StyxWoW.Me.Location);
                    }
                    else if (ForcedHotspot != WoWPoint.Empty)
                    {
                        _cachedStepPosition = ForcedHotspot;
                    }
                    else
                    {
                        WoWQuestStep questStep = GetClosestQuestStep();
                        var position = new Vector3(questStep.StepPosition.X, questStep.StepPosition.Y, 0f);
                        if (!HeightHelper.FindMeshHeight(ref position))
                        {
                            Logging.Write(QuestBotResources.CollectItemObjective_GetObjectiveLocation_Could_not_find_mesh_height_for_quest__0__on_step__1_, Quest.Name, questStep.PoiID);
                            _cachedStepPosition = WoWPoint.Zero;
                        }
                        else
                        {
                            _cachedStepPosition = position;
                        }
                    }
                }

                return _cachedStepPosition.Value;
            }

            if (units.Count > 0)
            {
                WoWPoint myLocation = StyxWoW.Me.Location;
                WoWPoint closestUnitLocation = units[0].Location;
                float closestUnitDistance = myLocation.DistanceSqr(closestUnitLocation);
                for (int i = 1; i < units.Count; i++)
                {
                    WoWPoint location = units[i].Location;
                    float distance = myLocation.DistanceSqr(location);
                    if (distance < closestUnitDistance)
                    {
                        closestUnitLocation = location;
                        closestUnitDistance = distance;
                    }
                }

                if (gameObjects.Count > 0)
                {
                    WoWPoint gameObjectLocation = gameObjects[0].Location;
                    return closestUnitDistance < myLocation.DistanceSqr(gameObjectLocation) ? closestUnitLocation : gameObjectLocation;
                }

                return closestUnitLocation;
            }

            return gameObjects[0].Location;
        }

        private bool GotCompletionInfo()
        {
            if (_overrideInfo != null && _overrideInfo.OverridedHotspots != null && _overrideInfo.OverridedHotspots.Count > 0)
                return true;

            if (!QuestArea.HotspotsCreated)
                QuestArea.CreateHotspots();

            return QuestArea.Hotspots.Count > 0;
        }

        public override Composite CreateBranch()
        {
            int myLevel = StyxWoW.Me.Level;
            if (_branch == null)
            {
                GrindArea area;
                if (_overrideInfo != null && _overrideInfo.OverridedHotspots != null && _overrideInfo.OverridedHotspots.Count > 0)
                {
                    area = new GrindArea(new HotspotManager(_overrideInfo.OverridedHotspots))
                    {
                        TargetMaxLevel = _overrideInfo.TargetMaxLevel > 0 ? _overrideInfo.TargetMaxLevel : myLevel + 5,
                        TargetMinLevel = _overrideInfo.TargetMinLevel > 0 ? _overrideInfo.TargetMinLevel : Quest.Level - 5
                    };
                }
                else if (ForcedHotspot != WoWPoint.Empty)
                {
                    area = new GrindArea(new HotspotManager(new List<WoWPoint> { ForcedHotspot }));
                }
                else
                {
                    QuestArea.CreateHotspots();
                    QuestArea.TargetMaxLevel = myLevel + 5;
                    QuestArea.TargetMinLevel = Quest.Level - 5;
                    area = QuestArea;
                }

                StyxWoW.AreaManager.SetArea(area);

                List<WoWUnit> units = (from unit in ObjectManager.GetObjectsOfType<WoWUnit>()
                                       where UnitDropsQuestItem(unit)
                                             && unit.IsAlive
                                             && (unit.OwnedByUnit == null || unit.OwnedByUnit != null && !unit.OwnedByUnit.IsPlayer)
                                       orderby unit.Location.DistanceSqr(StyxWoW.Me.Location)
                                       select unit).ToList();
                var mobIds = new List<uint>();
                foreach (WoWUnit unit in units)
                {
                    if (!mobIds.Contains(unit.Entry))
                        mobIds.Add(unit.Entry);
                }

                _branch = new PrioritySelector(
                    new Decorator(
                        context => GetSellingVendorId() != 0,
                        new PrioritySelector(
                            context => GetSellingVendorUnit(),
                            new Decorator(
                                context => context != null && ((WoWUnit)context).WithinInteractRange,
                                new Sequence(
                                    new Styx.TreeSharp.Action(context => StopAndOpenFrame(context)),
                                    new Styx.TreeSharp.Action(context => OpenMerchant(context)),
                                    new Styx.TreeSharp.Action(context => BuyQuestItem()))),
                            new NavigationAction(GetVendorLocation, context => NavType))),
                    new Decorator(
                        RelatedItemCreatesRequiredItem,
                        new Decorator(
                            context => QuestArea.CircledHotspots.Count == 1,
                            new PrioritySelector(
                                new Decorator(
                                    context => QuestArea.CurrentHotSpot.Position.Distance(StyxWoW.Me.Location) <= 5f,
                                    new Styx.TreeSharp.Action(context => UseRelatedItem(null)))))),
                    new DecoratorIsNotPoiType(new[] { PoiType.Loot, PoiType.Skin }, CreateRoamBehavior()));
            }

            return _branch;
        }

        public PrioritySelector CreateRoamBehavior()
        {
            return new PrioritySelector(
                LevelBot.CreateFindTargetBehavior(NavType),
                new DecoratorIsNotPoiType(
                    PoiType.Kill,
                    new PrioritySelector(
                        new Decorator(
                            context => QuestObjects != null && QuestObjects.Count > 0 && StyxWoW.Me.IsFlying && QuestObjects[0].Distance > 3.0,
                            new Sequence(
                                new Wait(1, context => QuestObjects[0].Distance > 3.0,
                                    new Styx.TreeSharp.Action(context => Navigator.MoveTo(QuestObjects[0].Location))),
                                new ActionSetPoi(context => new BotPoi(QuestObjects[0], PoiType.Loot)))))),
                LevelBot.CreateMoveToKillPoiBehavior(),
                LevelBot.CreateMoveToHotspotBehavior(NavType));
        }

        private WoWPoint GetVendorLocation(object context)
        {
            return context == null ? GetSellingVendorLocation() : ((WoWUnit)context).Location;
        }

        private RunStatus OpenMerchant(object context)
        {
            if (_merchantFrame.IsVisible)
                return RunStatus.Success;

            if (_gossipFrame.IsVisible)
            {
                List<GossipEntry> gossipEntries = _gossipFrame.GossipOptionEntries;
                bool found = false;
                foreach (GossipEntry entry in gossipEntries)
                {
                    if (entry.Type == GossipEntry.GossipEntryType.Vendor)
                    {
                        _gossipFrame.SelectGossipOption(entry.Index);
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    Logging.Write("This NPC is not a vendor.");
                    return RunStatus.Failure;
                }

                StyxWoW.Sleep(500);
                return RunStatus.Running;
            }

            return RunStatus.Failure;
        }

        private RunStatus BuyQuestItem()
        {
            if (!_merchantFrame.IsVisible)
            {
                Logging.WriteDiagnostic("Returning failure because MerchantFrame never appeared");
                return RunStatus.Failure;
            }

            if (!_buyTimer.IsRunning)
            {
                MerchantItem itemToBuy = _merchantFrame.GetAllMerchantItems().FirstOrDefault(item => item.ItemId == Objective.ID);
                if (itemToBuy == null)
                {
                    Logging.Write("Vendor does not sell item with ID {0}", Objective.ID);
                    _buyTimer.Stop();
                    return RunStatus.Failure;
                }

                int amount = (int)Math.Max(1L, Objective.Count / itemToBuy.Quantity);
                _merchantFrame.BuyItem(itemToBuy.Index, amount);
                _buyTimer.Reset();
                _buyTimer.Start();
                return RunStatus.Running;
            }

            if (_buyTimer.Elapsed.TotalSeconds > 5.0)
            {
                _buyTimer.Stop();
                return RunStatus.Failure;
            }

            long itemCount = StyxWoW.Me.CarriedItems.Where(item => item.Entry == Objective.ID).Sum(item => item.StackCount);
            if (itemCount >= Objective.Count)
            {
                _buyTimer.Stop();
                return RunStatus.Success;
            }

            return RunStatus.Running;
        }

        private RunStatus StopAndOpenFrame(object context)
        {
            var vendor = context as WoWUnit;
            if (!StyxWoW.Me.IsMoving && !StyxWoW.Me.Mounted)
            {
                using (StyxWoW.Memory.AcquireFrame())
                {
                    if (_merchantFrame.IsVisible || _gossipFrame.IsVisible)
                    {
                        _frameTimer.Stop();
                        return RunStatus.Success;
                    }
                }

                if (!_frameTimer.IsRunning)
                {
                    vendor.Interact();
                    _frameTimer.Reset();
                    _frameTimer.Start();
                }
                else if (_frameTimer.Elapsed.TotalSeconds > 5.0)
                {
                    _frameTimer.Stop();
                    return RunStatus.Failure;
                }

                StyxWoW.Sleep(500);
                return RunStatus.Running;
            }

            if (StyxWoW.Me.Mounted)
                Mount.Dismount("Frame");

            WoWMovement.MoveStop();
            StyxWoW.Sleep(250);
            return RunStatus.Running;
        }

        private WoWUnit GetSellingVendorUnit()
        {
            IEnumerable<WoWUnit> units = ObjectManager.GetObjectsOfType<WoWUnit>().Where(unit => unit.IsAlive && unit.MyReaction >= WoWUnitReaction.Neutral);
            uint sellingVendorId = GetSellingVendorId();
            return units.FirstOrDefault(VendorSellsItem) ?? units.FirstOrDefault(unit => unit.Entry == sellingVendorId);
        }

        private WoWPoint GetSellingVendorLocation()
        {
            if (!_sellingVendorLocation.HasValue)
            {
                if (_overrideInfo != null && _overrideInfo.OverridedHotspots != null && _overrideInfo.OverridedHotspots.Count == 1)
                {
                    _sellingVendorLocation = _overrideInfo.OverridedHotspots[0];
                }
                else
                {
                    NpcResult npc = Query.GetNearestNpcById(GetSellingVendorId(), StyxWoW.Me.Location);
                    if (npc != null)
                        _sellingVendorLocation = npc.Location;
                    else
                    {
                        Logging.Write("Can not find NPC selling item with ID {0} in database. Please add a quest override for quest {1}", Objective.ID, Quest.Name);
                        _sellingVendorLocation = WoWPoint.Zero;
                    }
                }
            }

            return _sellingVendorLocation.Value;
        }

        private uint GetSellingVendorId()
        {
            if (!_sellingVendorId.HasValue)
            {
                if (_overrideInfo != null && _overrideInfo.OverridedCollectFrom != null && _overrideInfo.OverridedCollectFrom.Count > 0)
                {
                    CollectFromCollection collectFrom = _overrideInfo.OverridedCollectFrom;
                    _sellingVendorId = (from item in collectFrom
                                        where item.Type == CollectFromType.Vendor
                                        select item.ID).FirstOrDefault();
                }
                else
                {
                    _sellingVendorId = 0u;
                    foreach (KeyValuePair<uint, HashSet<uint>> vendor in DropDatabase.Vendors)
                    {
                        if (vendor.Value.Contains(Objective.ID))
                            _sellingVendorId = vendor.Key;
                    }
                }
            }

            return _sellingVendorId.Value;
        }

        private bool VendorSellsItem(WoWUnit vendor)
        {
            uint entry = vendor.Entry;
            bool value;
            if (!_vendorDropCache.TryGetValue(entry, out value))
            {
                value = _overrideInfo != null
                        && _overrideInfo.OverridedCollectFrom != null
                        && _overrideInfo.OverridedCollectFrom.Count > 0
                        && _overrideInfo.OverridedCollectFrom.ContainsVendor(entry);
                if (!value)
                    value = DropDatabase.VendorSellsItem(entry, Objective.ID);

                _vendorDropCache[entry] = value;
            }

            return value;
        }

        private bool RelatedItemCreatesRequiredItem(object context)
        {
            uint relatedItemId = Quest.InternalInfo.StartingItemId;
            if (relatedItemId != 0)
            {
                WoWItem relatedItem = ObjectManager.GetObjectsOfType<WoWItem>().FirstOrDefault(item => item.Entry == relatedItemId);
                if (relatedItem == null)
                    return false;

                WoWItem.WoWItemSpell itemSpell = relatedItem.GetSpell(0);
                if (itemSpell != null)
                {
                    WoWSpell spell = WoWSpell.FromId(itemSpell.Id);
                    if (spell.CreatesItemId == Objective.ID)
                        return true;
                }
            }

            return false;
        }

        private RunStatus UseRelatedItem(object context)
        {
            if (StyxWoW.Me.IsMoving)
            {
                WoWMovement.MoveStop();
                return RunStatus.Running;
            }

            if (StyxWoW.Me.IsCasting)
                return RunStatus.Running;

            uint relatedItemId = Quest.InternalInfo.StartingItemId;
            if (relatedItemId != 0)
            {
                WoWItem relatedItem = ObjectManager.GetObjectsOfType<WoWItem>().FirstOrDefault(item => item.Entry == relatedItemId);
                if (relatedItem != null)
                {
                    Lua.DoString("UseItemByName(\"{0}\")", relatedItem.Entry);
                    return RunStatus.Running;
                }

                return RunStatus.Failure;
            }

            return RunStatus.Success;
        }

        private void CollectItemObjective_ObjectiveWeighTargets(List<Targeting.TargetPriority> units)
        {
            for (int i = 0; i < units.Count; i++)
            {
                float score = 0f;
                if (UnitDropsQuestItem(units[i].Object.ToUnit()))
                    score += 40f;

                units[i].Score += score;
            }
        }

        private void CollectItemObjective_IncludeTargetsFilter(List<WoWObject> incomingUnits, HashSet<WoWObject> outgoingUnits)
        {
            if (IsCompleted || StyxWoW.Me.IsActuallyInCombat || !_lootWaitTimer.IsFinished)
                return;

            PoiType type = BotPoi.Current.Type;
            if (type == PoiType.Buy || type == PoiType.Mail || type == PoiType.Repair || type == PoiType.Sell)
                return;

            foreach (WoWObject obj in incomingUnits)
            {
                WoWUnit unit = obj.ToUnit();
                if (unit != null && !unit.TaggedByOther && UnitDropsQuestItem(unit))
                    outgoingUnits.Add(unit);
            }
        }

        private void CollectItemObjective_IncludeLootsFilter(List<WoWObject> incomingObjects, HashSet<WoWObject> outgoingObjects)
        {
            if (IsCompleted || StyxWoW.Me.Combat || !_lootWaitTimer.IsFinished)
                return;

            for (int i = 0; i < incomingObjects.Count; i++)
            {
                if (incomingObjects[i] is WoWGameObject && GameObjectDropsItem(incomingObjects[i].ToGameObject()))
                    outgoingObjects.Add(incomingObjects[i]);

                if (incomingObjects[i] is WoWUnit && UnitDropsQuestItem(incomingObjects[i].ToUnit()))
                    outgoingObjects.Add(incomingObjects[i]);
            }
        }

        private void CollectItemObjective_OnObjectHarvested(BotEvents.Player.ObjectHarvestedEventArgs args)
        {
            if (StyxWoW.Me.CarriedItems.Where(item => item.Entry == Objective.ID).Sum(item => item.StackCount) == Objective.Count - 1)
                _lootWaitTimer.Reset();
        }

        private void CollectItemObjective_OnMobLooted(BotEvents.Player.MobLootedEventArgs args)
        {
            if (StyxWoW.Me.CarriedItems.Where(item => item.Entry == Objective.ID).Sum(item => item.StackCount) == Objective.Count - 1)
                _lootWaitTimer.Reset();
        }

        private bool UnitDropsQuestItem(WoWUnit unit)
        {
            uint entry = unit.Entry;
            bool value;
            if (!_mobDropCache.TryGetValue(entry, out value))
            {
                CreatureCacheEntry info;
                if (!unit.GetCachedInfo(out info))
                    return false;

                CollectFromCollection collectFrom;
                value = _overrideInfo != null
                        && (collectFrom = _overrideInfo.OverridedCollectFrom) != null
                        && collectFrom.Count > 0
                        && collectFrom.ContainsMob(entry)
                    || DropDatabase.UnitDropsItem(entry, Objective.ID)
                    || info.QuestItems.Contains(Objective.ID);
                _mobDropCache[entry] = value;
            }

            return value;
        }

        private bool GameObjectDropsItem(WoWGameObject gameObject)
        {
            uint entry = gameObject.Entry;
            bool value;
            if (!_gameObjectDropCache.TryGetValue(entry, out value))
            {
                GameObjectCacheEntry info;
                if (!gameObject.GetCachedInfo(out info))
                    return false;

                CollectFromCollection collectFrom;
                value = _overrideInfo != null
                        && (collectFrom = _overrideInfo.OverridedCollectFrom) != null
                        && collectFrom.Count > 0
                        && collectFrom.ContainsGameObject(entry)
                    || DropDatabase.GameObjectDropsItem(entry, Objective.ID)
                    || info.QuestItems.Contains(Objective.ID);
                _gameObjectDropCache[entry] = value;
            }

            return value && gameObject.CanLoot;
        }

        public override string ToString()
        {
            return string.Format(QuestBotResources.CollectItemObjective_ToString__CollectItemObjective_ItemID___0___Count___1__, Objective.ID, Objective.Count);
        }
    }
}
