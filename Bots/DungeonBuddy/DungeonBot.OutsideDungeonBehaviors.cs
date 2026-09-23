using System;
using System.Linq;
using Buddy.Coroutines;
using System.Text;
using System.Threading.Tasks;
using Bots.DungeonBuddy.Behaviors;
using Bots.Grind;
using CommonBehaviors.Actions;
using CommonBehaviors.Decorators;
using Styx;
using Styx.Common;
using Styx.Common.Helpers;
using Styx.CommonBot;
using Styx.CommonBot.Coroutines;
using Styx.CommonBot.Frames;
using Styx.CommonBot.Inventory;
using Styx.CommonBot.POI;
using Styx.CommonBot.Profiles;
using Styx.Helpers;
using Styx.Pathing;
using Styx.TreeSharp;
using Styx.WoWInternals;
using Styx.WoWInternals.DBC;
using Styx.WoWInternals.WoWObjects;

using Bots.DungeonBuddy.Enums;
using Bots.DungeonBuddy.Helpers;
using Action = Styx.TreeSharp.Action;

namespace Bots.DungeonBuddy
{
    public partial class DungeonBot
    {
        private static Composite _hearthToHomeZoneBehavior;
        private readonly WaitTimer _inviteTimer = new WaitTimer(TimeSpan.FromSeconds(10));
        private readonly WaitTimer _groupExitWaitTimer = new WaitTimer(TimeSpan.FromSeconds(10));
        private readonly WaitTimer _requeueTimer = new WaitTimer(TimeSpan.FromSeconds(15));

        private static bool IsFarming
        {
            get
            {
                if (DungeonBuddySettings.Instance.DungeonType != DungeonType.Farm)
                    return DungeonBuddySettings.Instance.RaidType == RaidType.Farm;
                return true;
            }
        }

        private bool PartyFarmModeLeaderIsInDungeon
        {
            get
            {
                if (DungeonManager.SelectedDungeon == null)
                    return false;

                if (DungeonBuddySettings.Instance.PartyMode == PartyMode.Leader)
                    return Me.MapId == DungeonManager.SelectedDungeon.LfgDungeon.MapId;

                var leader = GroupMember.GroupMembers.FirstOrDefault(g => g.NameEquals(DungeonBuddySettings.Instance.LeaderName));
                var mapId = leader != null ? leader.MapId : Me.MapId;
                return DungeonManager.SelectedDungeon.LfgDungeon.MapId == mapId;
            }
        }

        private bool HasRoleSelected
        {
            get { return Lua.GetReturnVal<bool>("local l,t,h,d = GetLFGRoles() if t == false and h == false and d == false then return nil end return 1", 0); }
        }

        private bool IsInHearthStoneArea
        {
            get
            {
                var hearthstoneAreaId = StyxWoW.Me.HearthstoneAreaId;
                var zoneId = Me.ZoneId;
                if (hearthstoneAreaId == 0)
                    return false;

                var result = GetTopLevelAreaId(hearthstoneAreaId) == GetTopLevelAreaId(zoneId);
                if (!result)
                    Logger.WriteDebug("Zone: {0}, hearthAreaId: {1}", zoneId, hearthstoneAreaId);
                return result;
            }
        }

        private uint GetTopLevelAreaId(uint areaId)
        {
            var area = AreaTable.FromId(areaId);
            while (area.ParentAreaId != 0)
                area = AreaTable.FromId(area.ParentAreaId);
            return area.AreaId;
        }

        private static bool ShouldBuyConsumables
        {
            get
            {
                if (ShouldBuyConsumablesPreemptively)
                    return true;

                if (Me.PowerType == WoWPowerType.Mana && CharacterSettings.Instance.DrinkAmount > 0)
                    return !Consumable.GetDrinks().Any();

                return false;
            }
        }

        private static bool ShouldRepairGear
        {
            get
            {
                if (ShouldRepairPreemptively)
                    return true;

                return Me.LowestDurabilityPercent <= DungeonBuddySettings.Instance.InsideInstanceRepairPercent / 100f;
            }
        }

        private static bool ShouldSellItems
        {
            get
            {
                if (ShouldSellPreemptively)
                    return true;

                if (Me.FreeNormalBagSlots <= DungeonBuddySettings.Instance.InsideInstanceMinFreeBagSlots)
                    return ItemsToSellCount > 0;

                return false;
            }
        }

        private static bool ShouldMailItems
        {
            get
            {
                if (ShouldMailPreemptively)
                    return true;

                if (Me.FreeNormalBagSlots <= DungeonBuddySettings.Instance.InsideInstanceMinFreeBagSlots)
                    return ItemsToMailCount > 0;

                return false;
            }
        }

        private static bool ShouldVendor
        {
            get
            {
                if (!ShouldSellItems && !ShouldBuyConsumables && !ShouldRepairGear)
                    return ShouldMailItems;

                return true;
            }
        }

        private Composite CreateOutsideDungeonBehavior()
        {
            return new PrioritySelector(
                new Decorator(
                    ctx => !IsInInstance,
                    new PrioritySelector(
                        // Check if a Follower and is party Leader- can happen after a DC
                        new Decorator(
                            ctx =>
                                DungeonBuddySettings.Instance.PartyMode == PartyMode.Follower && Me.IsGroupLeader && CurrentLfgDungeonId == 0 &&
                                GroupMember.GroupMembers.Any(g => g.NameEquals(DungeonBuddySettings.Instance.LeaderName)),
                            new Sequence(
                                new Action(ctx => Logger.Write("I am not supposed to be party leader. Fixing")),
                                new Action(ctx => Lua.DoString($"PromoteToLeader(\"{DungeonBuddySettings.Instance.LeaderName.ToFormatedUTF8()}\")")))),

                        CreateDeathBehavior(),
                        CreateLootBehavior(),

                        new Decorator(
                            ctx => Me.HasAura("Resurrection Sickness") && !Me.Combat,
                            new PrioritySelector(
                                new Decorator(
                                    ctx => DungeonBuddySettings.Instance.PartyMode == PartyMode.Off,
                                    new PrioritySelector(
                                        new Decorator(
                                            ctx => Me.GroupInfo.IsInParty,
                                            new ActionRunCoroutine(ctx => CreateLeavePartyBehavior("Resurrection Sickness"))))))),

                        // kill only stuff that aggroed
                        new Decorator(
                            ctx => Me.Combat && BotPoi.Current.Type != PoiType.Kill && !Targeting.Instance.IsEmpty() && Targeting.Instance.FirstUnit.IsAlive,
                            new ActionSetPoi(true, ctx => new BotPoi(Targeting.Instance.FirstUnit, PoiType.Kill))),
                        CreateCombatBehavior(),
                        // don't proceed down the behavior if in combat.
                        new Decorator(ctx => Me.IsActuallyInCombat, new ActionAlwaysSucceed()),

                        #region Vender Behaviors.
                        new DecoratorIsNotPoiType(
                            new[] {PoiType.Sell, PoiType.Buy, PoiType.Repair, PoiType.Mail},
                            new PrioritySelector(
                                // hearth if i need to do vendor run and I'm not in the zone that hearth is set to
                                new Decorator(ctx => DungeonBuddySettings.Instance.HearthOnVendorRun && ShouldVendor, CreateHearthToHomeZoneBehavior()),
                                // sell items, premptively.
                                new Decorator(
                                    ctx => ShouldMailItems || ShouldSellItems,
                                    new PrioritySelector(
                                        new Decorator(ctx => ShouldSellItems, new ActionRunCoroutine(ctx => LevelBot.MountVendor(PoiType.Sell))),
                                        new PrioritySelector(
                                            ctx =>
                                                {
                                                    if (ShouldMailItems)
                                                    {
                                                        var nearestMailbox = FindNearestMailbox();
                                                        if (nearestMailbox != null)
                                                        {
                                                            Logger.Write("Mailing items before selling");
                                                            return nearestMailbox;
                                                        }
                                                        Logger.Write(
                                                            "Could not find mailbox for {0} at {1} in {2} (Id: {3})",
                                                            Me.IsAlliance ? "Alliance" : "Horde",
                                                            Me.Location,
                                                            Me.CurrentMap.Name,
                                                            Me.CurrentMap.MapId);
                                                    }
                                                    var repairVendor = FindNearestVendor(Vendor.VendorType.Repair);
                                                    if (repairVendor != null)
                                                        Logger.Write("Reparing at {0}", repairVendor.Name);
                                                    else
                                                        Logger.Write(
                                                            "Could not find repair npc for {0} at {1} in {2} (Id: {3})",
                                                            Me.IsAlliance ? "Alliance" : "Horde",
                                                            Me.Location,
                                                            Me.CurrentMap.Name,
                                                            Me.CurrentMap.MapId);
                                                    return repairVendor;
                                                },
                                            new Decorator(ctx => ctx is Mailbox, new ActionSetPoi(true, ctx => new BotPoi((Mailbox) ctx))),
                                            new Decorator(ctx => ctx is Vendor, new ActionSetPoi(true, ctx => new BotPoi((Vendor) ctx, PoiType.Sell)))))),
                                // repair gear, premptively.
                                new Decorator(
                                    ctx => ShouldRepairGear,
                                    new PrioritySelector(
                                        new ActionRunCoroutine(ctx => LevelBot.MountVendor(PoiType.Repair)),
                                        new PrioritySelector(
                                            ctx =>
                                                {
                                                    if (ItemsToMailCount > 0)
                                                    {
                                                        var nearestMailbox = FindNearestMailbox();
                                                        if (nearestMailbox != null)
                                                        {
                                                            Logger.Write("Mailing items before doing a repair run.");
                                                            return nearestMailbox;
                                                        }
                                                        Logger.Write(
                                                            "Could not find mailbox for {0} at {1} in {2} (Id: {3})",
                                                            Me.IsAlliance ? "Alliance" : "Horde",
                                                            Me.Location,
                                                            Me.CurrentMap.Name,
                                                            Me.CurrentMap.MapId);
                                                    }
                                                    var repairVendor = FindNearestVendor(Vendor.VendorType.Repair);
                                                    if (repairVendor != null)
                                                        Logger.Write("Reparing at {0}", repairVendor.Name);
                                                    else
                                                        Logger.Write(
                                                            "Could not find repair npc for {0} at {1} in {2} (Id: {3})",
                                                            Me.IsAlliance ? "Alliance" : "Horde",
                                                            Me.Location,
                                                            Me.CurrentMap.Name,
                                                            Me.CurrentMap.MapId);
                                                    return repairVendor;
                                                },
                                            new Decorator(ctx => ctx is Mailbox, new ActionSetPoi(true, ctx => new BotPoi((Mailbox) ctx))),
                                            new Decorator(ctx => ctx is Vendor, new ActionSetPoi(true, ctx => new BotPoi((Vendor) ctx, PoiType.Repair)))))),
                                new Decorator(
                                    ctx => ShouldBuyConsumables,
                                    new PrioritySelector(
                                        ctx =>
                                            {
                                                var vendor = FindNearestVendor(Vendor.VendorType.Food);
                                                if (vendor != null)
                                                    Logger.Write("Buying at {0}", vendor.Name);
                                                else
                                                    Logger.Write(
                                                        "Could not find food vendor for {0} at {1} in {2} (Id: {3})",
                                                        Me.IsAlliance ? "Alliance" : "Horde",
                                                        Me.Location,
                                                        Me.CurrentMap.Name,
                                                        Me.CurrentMap.MapId);
                                                return vendor;
                                            },
                                        new Decorator(ctx => ctx != null, new ActionSetPoi(true, ctx => new BotPoi((Vendor) ctx, PoiType.Buy))))),
                                // port back in if there is nothing to do outside and we are in a lfgdungeon.
                                new Decorator(
                                    ctx => CurrentLfgDungeonId != 0 && BotPoi.Current.Type == PoiType.None,
                                    new PrioritySelector(
                                        new Decorator(
                                            ctx =>
                                                DungeonBuddySettings.Instance.PartyMode != PartyMode.Off && ISeeAGhost && DungeonBuddySettings.Instance.PortOutsideOnGhostSighting,
                                            new ActionSetActivity("Waiting outide for party members to re-enter dungeon")),
                                        new Sequence(
                                            new Action(ctx => Logger.Write("Zoning into dungeon.")),
                                            new Action(ctx => Lua.DoString("LFGTeleport()")),
                                            new WaitContinue(4, ctx => false, new ActionAlwaysSucceed())))),

                                #region Farm Mode
                                new Decorator(
                                    ctx => DungeonBuddySettings.Instance.DungeonType == DungeonType.Farm && DungeonManager.SelectedDungeon != null,
                                    new PrioritySelector(
                                        // wait for everyone to exit the dungeon.
                                        new Decorator(
                                            ctx =>
                                                {
                                                    if (StyxWoW.Me.GroupInfo.IsInParty && DungeonBuddySettings.Instance.PartyMode == PartyMode.Leader)
                                                    {
                                                        bool flag;
                                                        if (flag = GroupMember.GroupMembers.Any(g => g.MapId == DungeonManager.SelectedDungeon.LfgDungeon.MapId))
                                                            _groupExitWaitTimer.Reset();
                                                        if (!flag)
                                                            return !_groupExitWaitTimer.IsFinished;
                                                        return true;
                                                    }
                                                    return false;
                                                },
                                            new Action(ctx => TreeRoot.StatusText = "Waiting for everyone to get outside of dungeon before resting it.")),
                                        new Decorator(
                                            ctx =>
                                                StyxWoW.Me.GroupInfo.IsInParty && DungeonBuddySettings.Instance.PartyMode == PartyMode.Follower &&
                                                !PartyFarmModeLeaderIsInDungeon && Me.Location.Distance(DungeonManager.SelectedDungeon.Entrance) <= 20,
                                            new PrioritySelector(
                                                new Decorator(ctx => Me.IsMoving, new Action(ctx => WoWMovement.MoveStop())),
                                                new Decorator(ctx => DungeonManager.SelectedDungeon.IsComplete, new Action(ctx => HandleDungeonLeft())),
                                                new Action(ctx => TreeRoot.StatusText = "Waiting for tank to enter dungeon."))),
                                        new Decorator(
                                            ctx => _resetDungeon,
                                            new Sequence(
                                                new Action(ret => Logger.Write("Reseting Instances")),
                                                new Action(ctx => Lua.DoString("ResetInstances();")),
                                                new Action(ctx => HandleDungeonLeft()),
                                                new Action(ctx => _resetDungeon = false))),
                                        new Decorator(
                                            ctx => !DungeonManager.SelectedDungeon.IsFlyingCorpseRun,
                                            new PrioritySelector(
                                                new ActionSetActivity("Moving to Instance Portal on foot"),
                                                new Decorator(
                                                    ctx => Me.Location.DistanceSqr(DungeonManager.SelectedDungeon.Entrance) > 36,
                                                    new Action(ctx => Navigator.MoveTo(DungeonManager.SelectedDungeon.Entrance))),
                                                new Sequence(
                                                    new Action(
                                                        ctx =>
                                                            Navigator.PlayerMover.MoveTowards(
                                                                WoWMathHelper.CalculatePointFrom(Me.Location, DungeonManager.SelectedDungeon.Entrance, -4))),
                                                    new Sleep(TimeSpan.FromMilliseconds(400))))),
                                        new Decorator(
                                            ctx => DungeonManager.SelectedDungeon.IsFlyingCorpseRun,
                                            new PrioritySelector(
                                                new ActionSetActivity("Flying to Instance Portal"),
                                                // no corpse run so we just move to the dungeon entrance..
                                                new Decorator(
                                                    ctx => !DungeonManager.SelectedDungeon.CorpseRunBreadCrumb.Any(),
                                                    new Action(ctx => Flightor.MoveTo(DungeonManager.SelectedDungeon.Entrance))),
                                                new Decorator(
                                                    ctx => DungeonManager.SelectedDungeon.CorpseRunBreadCrumb.Any(),
                                                    new ActionRunCoroutine(ctx => FlyCorpseRunBreadCrumbs(DungeonManager.SelectedDungeon.CorpseRunBreadCrumb)))))))
                                #endregion

                                ))
                        #endregion

                        )));
        }

        private static async Task<bool> FlyCorpseRunBreadCrumbs(CircularQueue<WoWPoint> breadCrumbs)
        {
            if (breadCrumbs == null || !breadCrumbs.Any())
                return false;

            breadCrumbs.CycleTo(breadCrumbs.First);
            var mapId = StyxWoW.Me.MapId;
            var point = breadCrumbs.Dequeue();

            while (Me.MapId == mapId && !Me.IsDead)
            {
                var atFirst = breadCrumbs.Peek() == breadCrumbs.First;
                if (Me.Location.DistanceSqr(point) < 225 && !atFirst)
                    point = breadCrumbs.Dequeue();

                if (atFirst && Me.Location.DistanceSqr(point) < 36)
                {
                    Navigator.PlayerMover.MoveTowards(WoWMathHelper.CalculatePointFrom(Me.Location, point, -4));
                    await Coroutine.Sleep(400);
                }
                else
                {
                    Flightor.MoveTo(point);
                    await Coroutine.Yield();
                }
            }

            return true;
        }

        private Composite CreateHearthToHomeZoneBehavior()
        {
            // Check if we're in a zone that hearth stone is not set to. Can happen when attempting to zone in a dungeon while encounter is in process.
            // We need to make sure bot is in an area that has access to vendors or it can cause issue with bot getting stucked
            return _hearthToHomeZoneBehavior ??
                   (_hearthToHomeZoneBehavior =
                    new Decorator(
                        ctx => !IsFarming && Me.HearthstoneAreaId != 0 && !IsInHearthStoneArea,
                        new PrioritySelector(
                            new Decorator(ctx => Me.Mounted, new ActionRunCoroutine(ctx => CommonCoroutines.Dismount())),
                            new Decorator(ctx => Me.IsMoving, new Action(ctx => WoWMovement.MoveStop())),
                            new Decorator(ctx => MerchantFrame.Instance.IsVisible, new Action(ctx => MerchantFrame.Instance.Close())),
                            new Sequence(
                                new Action(
                                    ctx =>
                                        {
                                            if (Me.IsCasting)
                                                return RunStatus.Success;
                                            Logger.Write("Hearthing because bot is not in zone that hearthstone is set to");

                                            if (SpellManager.HasSpell("Recall") && !SpellManager.Spells["Recall"].Cooldown)
                                            {
                                                SpellManager.Cast("Recall");
                                            }
                                            else
                                            {
                                                var hearthStone =
                                                    Me.BagItems.FirstOrDefault(
                                                        i =>
                                                        i != null && i.ItemInfo != null && (i.Entry == 64488 || i.Entry == 6948) &&
                                                        !i.ItemSpells.FirstOrDefault().ActualSpell.Cooldown);
                                                if (hearthStone == null)
                                                    return RunStatus.Failure;
                                                hearthStone.UseContainerItem();
                                            }
                                            return RunStatus.Success;
                                        })),
                            new WaitContinue(1, ctx => false, new ActionAlwaysSucceed()),
                            new WaitContinue(12, ctx => Me.IsCasting || IsInHearthStoneArea, new ActionAlwaysSucceed()),
                            new DecoratorContinue(
                                ctx => IsInHearthStoneArea && Me.IsCasting,
                                new Sequence(
                                    new ActionLogger("Canceling hearth because I'm in the area that hearthstone is set to"),
                                    new Action(ctx => SpellManager.StopCasting()))),
                            new WaitContinue(1, ctx => false, new ActionAlwaysSucceed()),
                            new WaitContinue(20, ctx => StyxWoW.IsInWorld, new ActionAlwaysSucceed()))));
        }

        private Composite CreateEnqueueBehavior()
        {
            return new ActionRunCoroutine(context => Enqueue());
        }

        private async Task<bool> Enqueue()
        {
            if (await CreatePartyModeBehavior())
                return true;

            if (!_requeueTimer.IsFinished)
                return false;

            _requeueTimer.Reset();
            if (!ShouldRequeue || !CanQueue)
                return false;

            return await LfgQueue.SyncQueue();
        }

        private async Task<bool> CreatePartyModeBehavior()
        {
            if (DungeonBuddySettings.Instance.PartyMode != PartyMode.Leader)
                return false;

            var expectedGroupSize = DungeonBuddySettings.Instance.PartyMembers.Count(s => !string.IsNullOrWhiteSpace(s)) + 1;
            if (GroupMember.GroupMembers.Count == expectedGroupSize || CurrentLfgDungeonId != 0)
                return false;

            if (_inviteTimer.IsFinished)
            {
                Logger.Write("Sending out group invites");
                using (StyxWoW.Memory.AcquireFrame())
                {
                    foreach (var partyMember in
                        from partyMember in DungeonBuddySettings.Instance.PartyMembers
                        where !string.IsNullOrWhiteSpace(partyMember)
                        let escapedPartyMemeber = EncodeToLuaUTF8(partyMember.Trim())
                        let inParty = Lua.GetReturnVal<string>($"return UnitInParty(\"{escapedPartyMemeber}\")", 0)
                        where string.IsNullOrEmpty(inParty) || inParty == "nil"
                        select escapedPartyMemeber)
                    {
                        Lua.DoString($"InviteUnit(\"{partyMember}\")");
                    }
                }
                _inviteTimer.Reset();
            }
            return true;
        }

        private string EncodeToLuaUTF8(string text)
        {
            var buffer = new StringBuilder(Encoding.UTF8.GetByteCount(text) * 2);
            byte[] utf8Encoded = Encoding.UTF8.GetBytes(text);
            foreach (byte b in utf8Encoded)
            {
                buffer.Append(string.Format("\\{0:D3}", b));
            }
            return buffer.ToString();
        }
    }
}
