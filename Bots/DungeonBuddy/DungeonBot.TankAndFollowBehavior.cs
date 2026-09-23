using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Buddy.Coroutines;
using Bots.Grind;
using CommonBehaviors.Actions;
using Styx;
using Styx.Common;
using Styx.Common.Helpers;
using Styx.CommonBot;
using Styx.CommonBot.Coroutines;
using Styx.CommonBot.Frames;
using Styx.CommonBot.Inventory;
using Styx.Helpers;
using Styx.CommonBot.POI;
using Styx.Patchables;
using Styx.Pathing;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;
using Styx.TreeSharp;
using Tripper.Navigation;
using Tripper.Tools.Math;
using Action = Styx.TreeSharp.Action;
using Bots.DungeonBuddy.Enums;
using Bots.DungeonBuddy.Helpers;

namespace Bots.DungeonBuddy
{
    partial class DungeonBot
    {
        private const int MaxUnmountedFollowerRangeSqr = 40 * 40;
        private const int MaxMountedFollowerRangeSqr = 65 * 65;

        private readonly WaitTimer _satchelCheckTimer = new WaitTimer(TimeSpan.FromMinutes(1));
        private readonly WaitTimer _unexpectedGroupTimer = WaitTimer.FiveSeconds;
        private bool _wasInUnexpectedGroup;
        private bool _farmResetTriggered;

        private readonly HashSet<uint> _satchelOfHelpfulGoodsList = new HashSet<uint>
                                                                     {
                                                                         51999,
                                                                         52000,
                                                                         52001,
                                                                         52002,
                                                                         52003,
                                                                         52004,
                                                                         52005,
                                                                         67248,
                                                                         67250,
                                                                         92813,
                                                                         95618,
                                                                         95619,
                                                                         95617,
                                                                         95343,
                                                                         98133,
                                                                         98134,
                                                                         104274,
                                                                         104275,
                                                                         104271,
                                                                     };

        private bool ISeeAGhost
        {
            get { return GroupMember.GroupMembers.Any(m => m.IsGhost); }
        }

        private bool InstanceIsComplete
        {
            get
            {
                if (DungeonManager.SelectedDungeon == null || DungeonCompleted == CompleteReason.None)
                    return false;

                return CompleteTimer.IsFinished;
            }
        }

        private static bool IsLeaderInGroup
        {
            get { return GroupMember.GroupMembers.Any(m => m.IsLeader() && m.IsOnline); }
        }

        private bool InHomeGroup
        {
            get { return Lua.GetReturnVal<bool>("local val= IsInGroup(1) if val then return 1 end return nil", 0); }
        }

        private bool HavePhasedGroupMembers
        {
            get
            {
                using (StyxWoW.Memory.AcquireFrame())
                {
                    var myGuid = Me.Guid;
                    foreach (var p in GroupMember.GroupMembers)
                    {
                        if (p.Guid == myGuid) continue;
                        string lua = String.Format("return UnitInPhase('{0}')", p.UnitId);
                        if (Lua.GetReturnVal<bool>(lua, 0) == false)
                            return true;
                    }
                }
                return false;
            }
        }

        private bool InUnexpectedGroup
        {
            get
            {
                var unexpected = false;
                if (DungeonBuddySettings.Instance.PartyMode == PartyMode.Follower)
                    unexpected = !GroupMember.GroupMembers.Any(g => g.NameEquals(DungeonBuddySettings.Instance.LeaderName));
                else if (DungeonBuddySettings.Instance.PartyMode == PartyMode.Leader)
                    unexpected =
                        !DungeonBuddySettings.Instance.PartyMembers.All(
                            name => string.IsNullOrWhiteSpace(name) || GroupMember.GroupMembers.Any(g => g.NameEquals(name)));

                if (unexpected != _wasInUnexpectedGroup && unexpected)
                    _unexpectedGroupTimer.Reset();

                _wasInUnexpectedGroup = unexpected;

                if (unexpected && _unexpectedGroupTimer.IsFinished)
                {
                    _wasInUnexpectedGroup = false;
                    return true;
                }
                return false;
            }
        }

        private bool ShouldExitDungeonToRepair
        {
            get
            {
                if (DungeonManager.CurrentDungeon != null && !DungeonManager.CurrentDungeon.CanExitNow)
                    return false;

                return ShouldRepairPreemptively || ShouldRepairGear;
            }
        }

        private bool ShouldExitDungeonToBuyDrinks
        {
            get
            {
                if (DungeonManager.CurrentDungeon != null && !DungeonManager.CurrentDungeon.CanExitNow)
                    return false;

                if (ShouldBuyConsumablesPreemptively)
                    return true;

                if (!DungeonBuddySettings.Instance.PortOutsideInstanceToBuy)
                    return false;

                return ShouldBuyConsumables;
            }
        }

        private bool ShouldExitDungeonToMailOrSell
        {
            get
            {
                if (DungeonManager.CurrentDungeon != null && !DungeonManager.CurrentDungeon.CanExitNow)
                    return false;

                if (ShouldSellPreemptively || ShouldMailPreemptively)
                    return true;

                if (!DungeonBuddySettings.Instance.PortOutsideInstanceToSell)
                    return false;

                return ShouldSellItems || ShouldMailItems;
            }
        }

        private bool ShouldExitDungeonOnPartyMembersDeath
        {
            get
            {
                if (!DungeonBuddySettings.Instance.PortOutsideOnGhostSighting)
                    return false;

                if (DungeonBuddySettings.Instance.PartyMode == PartyMode.Off)
                    return false;

                if (DungeonManager.SelectedDungeon == null)
                    return false;

                if (DungeonManager.SelectedDungeon.LfgDungeon.SubType != LfgDungeons.LfgType.Dungeon)
                    return false;

                if (!DungeonManager.SelectedDungeon.CanExitNow)
                    return false;

                return _corpseRunTimer.IsFinished && ISeeAGhost && !InstanceIsComplete;
            }
        }

        private Composite CreateTankAndFollowBehavior()
        {
            return new PrioritySelector(
                new ActionRunCoroutine(ctx => CreateSetRoleBehavior()),
                new Decorator(ctx => Me.GroupInfo.IsInParty, new ActionRunCoroutine(ctx => CreateLeaveGroupBehavior())),
                new ActionRunCoroutine(ctx => CreateDungeonCompletionBehavior()),
                new Decorator(
                    ctx => ShouldExitDungeonToRepair,
                    new PrioritySelector(
                        new ActionRunCoroutine(ctx => LevelBot.MountVendor(PoiType.Repair)),
                        new ActionRunCoroutine(ctx => CreateExitDungeonBehavior("Repairing")))),
                new Decorator(ctx => ShouldExitDungeonToBuyDrinks, new ActionRunCoroutine(ctx => CreateExitDungeonBehavior("Buying more drinks"))),
                new Decorator(
                    ctx => ShouldExitDungeonToMailOrSell,
                    new PrioritySelector(
                        new Decorator(ctx => ShouldSellItems, new ActionRunCoroutine(ctx => LevelBot.MountVendor(PoiType.Sell))),
                        new ActionRunCoroutine(ctx => CreateExitDungeonBehavior("Selling items")))),
                new Decorator(
                    ctx => ShouldExitDungeonOnPartyMembersDeath,
                    new ActionRunCoroutine(
                        ctx => CreateExitDungeonBehavior("Someone died and released. Exiting and re-entering dungeon to meet up at entrance"))),
                new Decorator(
                    ctx => CurrentLfgDungeonId > 0 && Me.MapId != Me.CurrentLfgDungeon().MapId,
                    new ActionRunCoroutine(ctx => CreateExitDungeonBehavior("I'm not in the right Instance"))),
                new Decorator(
                    ctx => IsFarming,
                    new PrioritySelector(
                        new Decorator(
                            ctx => InstanceIsComplete && (!Me.GroupInfo.IsInParty || DungeonBuddySettings.Instance.PartyMode == PartyMode.Leader),
                            new PrioritySelector(
                                new Decorator(ctx => !_farmResetTriggered, new Action(ctx => _farmResetTriggered = true)),
                                new ActionRunCoroutine(ctx => CreateExitDungeonBehavior("Resetting Dungeon")))),
                        new Decorator(
                            ctx => DungeonBuddySettings.Instance.PartyMode == PartyMode.Follower && !PartyFarmModeLeaderIsInDungeon,
                            new ActionRunCoroutine(ctx => CreateExitDungeonBehavior("Leader is not in dungeon"))))),
                new Decorator(
                    ctx => _satchelCheckTimer.IsFinished && !Me.Combat,
                    new Sequence(
                        ctx => Me.BagItems.FirstOrDefault(i => i.IsValid && _satchelOfHelpfulGoodsList.Contains(i.Entry)),
                        new DecoratorContinue(
                            ctx => ctx != null,
                            new Sequence(
                                new Bots.DungeonBuddy.Helpers.Action<WoWItem>(item => item.UseContainerItem()),
                                new WaitContinue(3, ctx => LootFrame.Instance.IsVisible, new ActionAlwaysSucceed()),
                                new SleepForLagDuration(),
                                new Sleep(1500),
                                new DecoratorContinue(ctx => LootFrame.Instance.IsVisible, new Action(ctx => LootFrame.Instance.LootAll())))),
                        new Action(ctx => _satchelCheckTimer.Reset()))),
                new Decorator(ctx => Me.IsFollower(), new ActionRunCoroutine(ctx => ExecuteFollowerMovementBehavior())),
                new Decorator(ctx => Me.IsLeader(), new ActionRunCoroutine(ctx => ExecuteTankBehavior())));
        }

        private async Task<bool> CreateSetRoleBehavior()
        {
            if (Me.Role != WoWPartyMember.GroupRole.None || !Me.GroupInfo.IsInParty || Me.CurrentMap.IsScenario)
                return false;

            if (Me.IsTank())
                Lua.DoString("UnitSetRole('player', 'Tank')");
            else if (Me.IsHealer())
                Lua.DoString("UnitSetRole('player', 'Healer')");
            else
                Lua.DoString("UnitSetRole('player', 'Damager')");

            return true;
        }

        private async Task<bool> CreateLeaveGroupBehavior()
        {
            var partyMode = DungeonBuddySettings.Instance.PartyMode;

            if (InUnexpectedGroup)
                return await CreateLeavePartyBehavior(partyMode == PartyMode.Leader ? "Followers are not in group." : "My leader isn't in group.");

            if (InstanceIsComplete && CurrentLfgDungeonId > 0)
            {
                if (QueueAttempts >= 3)
                    return await CreateLeavePartyBehavior("Tried to queue 3 or more times without success");

                if (partyMode == PartyMode.Leader && DungeonBuddySettings.Instance.DungeonType == DungeonType.Specific &&
                    DungeonBuddySettings.Instance.SelectedDungeonIds.Length == 1)
                {
                    if (await CreateLeavePartyBehavior("Leaving dungeon to reset instance"))
                    {
                        await Coroutine.Wait(
                            45000,
                            () => Me.GroupInfo.LfgDungeonId == 0 && GroupMember.GroupMembers.All(m => !m.CurrentMap.IsDungeon && !m.CurrentMap.IsScenario));
                        Logger.Write("Reseting Instances");
                        Lua.DoString("ResetInstances();");
                        return true;
                    }
                }

                if (partyMode != PartyMode.Follower)
                    return await CreateLeavePartyBehavior(DungeonCompleted.ToString());

                return false;
            }

            return false;
        }

        private async Task<bool> CreateDungeonCompletionBehavior()
        {
            if (!_dungeonTimer.IsFinished)
                return false;

            var partyMode = DungeonBuddySettings.Instance.PartyMode;

            if (DungeonManager.SelectedDungeon != null && partyMode != PartyMode.Follower && DungeonCompleted == CompleteReason.None &&
                DungeonManager.SelectedDungeon.IsComplete)
            {
                if (LootRollInProcess)
                    TreeRoot.StatusText = "Loot roll in process";
                else
                    DungeonIsCompleted(CompleteReason.Completed, 10);
                return true;
            }

            if (DungeonCompleted == CompleteReason.None && GroupMember.GroupMembers.Count(p => p.IsOnline) == 1 && partyMode == PartyMode.Off)
            {
                DungeonIsCompleted(CompleteReason.AbandonedInDungeon, 10);
                Logger.Write("No one left in dungeon group. Leaving in {0} seconds", (int) CompleteTimer.WaitTime.TotalSeconds);
                return true;
            }

            if (DungeonCompleted == CompleteReason.None && _inactivityTimer.IsFinished)
            {
                DungeonIsCompleted(CompleteReason.InActive, 10);
                Logger.Write(
                    "No activity was detected for {0} minutes. Leaving in {1} seconds",
                    (int) _inactivityTimer.WaitTime.TotalMinutes,
                    (int) CompleteTimer.WaitTime.TotalSeconds);
                return true;
            }

            if (DungeonCompleted == CompleteReason.None && !IsLeaderInGroup && partyMode != PartyMode.Follower && !HavePhasedGroupMembers &&
                CurrentLfgDungeonId > 0 && !Me.CurrentMap.IsRaid)
            {
                DungeonIsCompleted(CompleteReason.MissingTank, 60 * 5);
                RaFHelper.ClearLeader();
                Logger.Write("There is no tank in the party. Leaving dungeon in {0} seconds", (int) CompleteTimer.WaitTime.TotalSeconds);
                return true;
            }

            if (DungeonCompleted == CompleteReason.MissingTank && IsLeaderInGroup)
            {
                DungeonCompleted = CompleteReason.None;
                Logger.Write("Tank is back. Cancelling dungeon leave");
                return true;
            }

            return false;
        }

        private async Task<bool> CreateExitDungeonBehavior(string reason)
        {
            if (!Me.IsAlive)
                return false;

            if (Me.Specialization == WoWSpec.PriestHoly)
            {
                var becameSpiritOfRedemption = await Coroutine.Wait((int) (StyxWoW.WoWClient.Latency + 600), () => Me.Shapeshift == ShapeshiftForm.SpiritOfRedemption);
                if (becameSpiritOfRedemption)
                    return false;
            }

            if (!IsFarming)
            {
                if (Me.Combat)
                    return false;

                var lastMapId = Me.MapId;
                Logger.Write(reason);
                BotPoi.Clear("Porting outside instance");
                Lua.DoString("LFGTeleport(true)");
                await Coroutine.Sleep(2000);

                if (Me.MapId != lastMapId)
                    return true;

                var hearth =
                    Me.BagItems.FirstOrDefault(
                        i => i != null && i.ItemInfo != null && (i.Entry == 64488 || i.Entry == 6948) && !i.ItemSpells.FirstOrDefault().ActualSpell.Cooldown);
                if (hearth != null || (SpellManager.HasSpell("Recall") && !SpellManager.Spells["Recall"].Cooldown))
                {
                    await ScriptHelpers.StopMovingIfMoving("Hearthing");

                    if (hearth != null)
                        hearth.UseContainerItem();
                    else
                        SpellManager.Cast("Recall");

                    var startedCasting = await Coroutine.Wait(2000, () => Me.IsCasting);
                    if (startedCasting)
                    {
                        await Coroutine.Wait(15000, () => Me.MapId != lastMapId || !StyxWoW.IsInWorld || !Me.IsCasting);
                        if (Me.MapId != lastMapId)
                            return true;
                    }
                }
            }

            if (Me.IsActuallyInCombat)
                return false;

            var exitLocation = DungeonManager.CurrentDungeon != null ? DungeonManager.CurrentDungeon.ExitLocation : WoWPoint.Zero;
            if (exitLocation == WoWPoint.Zero)
                return false;

            if (Me.Location.DistanceSqr(exitLocation) > 6 * 6)
            {
                var moveResult = await CommonCoroutines.MoveTo(exitLocation);
                return moveResult.IsSuccessful();
            }

            Navigator.PlayerMover.MoveTowards(WoWMathHelper.CalculatePointFrom(Me.Location, exitLocation, -4f));
            await Coroutine.Sleep(400);
            return false;
        }
    }
}
