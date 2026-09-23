using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Buddy.Coroutines;
using CommonBehaviors.Actions;
using Styx;
using Styx.Common.Helpers;
using Styx.CommonBot;
using Styx.CommonBot.Frames;
using Styx.Helpers;
using Styx.Pathing;
using Styx.WoWInternals;
using Styx.WoWInternals.DBC;
using Styx.WoWInternals.WoWObjects;
using Styx.TreeSharp;
using Action = Styx.TreeSharp.Action;
using Bots.DungeonBuddy.Behaviors;
using Bots.DungeonBuddy.Enums;
using Bots.DungeonBuddy.Helpers;
using Bots.DungeonBuddy.Profiles.Handlers;

namespace Bots.DungeonBuddy
{
    public partial class DungeonBot
    {
        private static CompleteReason _dungeonCompleted;
        private static readonly WaitTimer CompleteTimer = new WaitTimer(TimeSpan.FromSeconds(60));
        private readonly WaitTimer _dungeonTimer = new WaitTimer(TimeSpan.FromMinutes(1));
        private readonly Dictionary<int, WaitTimer> _lootCountdownTimers = new Dictionary<int, WaitTimer>();
        private readonly Dictionary<string, string> _globalStringCache = new Dictionary<string, string>();
        private uint _lastLfgDungeonId = Me.GroupInfo.LfgDungeonId;
        private uint _lastMapId;
        private bool _acceptPartyInvite;
        private WaitTimer _acceptReadyCheckTimer;
        private bool _acceptResurrect;
        private bool _attachedEvents;
        private bool _acceptOfferContinue;
        private WaitTimer _kickVoteTimer;
        private bool _performRoleCheck;
        private WaitTimer _acceptQueueTimer;
        private uint _proposalShowTimeMs;
        private bool _proposalSucceeded;
        private static int _queueAttempts;

        public static CompleteReason DungeonCompleted
        {
            get { return _dungeonCompleted; }
            private set
            {
                // We don't want to override this. It will keep reseting the timer.
                if (_dungeonCompleted == value)
                    return;

                _dungeonCompleted = value;
                if (value != CompleteReason.None)
                    CompleteTimer.Reset();
            }
        }

        public static int QueueAttempts => _queueAttempts;

        private bool LootRollInProcess
        {
            get
            {
                RemoveExpiredLootrollEntries();
                if (!_lootCountdownTimers.Any())
                    return Lua.GetReturnVal<bool>("return BonusRollFrame:IsVisible()", 0);
                return true;
            }
        }

        private bool ShouldRequeue
        {
            get
            {
                if (IsFarming || DungeonBuddySettings.Instance.PartyMode == PartyMode.Follower)
                    return false;

                if (Me.IsGroupLeader && CurrentLfgDungeonId != 0)
                    return false;

                bool queue = CurrentLfgDungeonId == 0;
                bool partyModeOffAndNotInParty = DungeonBuddySettings.Instance.PartyMode == PartyMode.Off && !StyxWoW.Me.GroupInfo.IsInParty;
                bool partyModeLeaderAndEveryoneIsAlive = DungeonBuddySettings.Instance.PartyMode == PartyMode.Leader && StyxWoW.Me.GroupInfo.IsInParty &&
                                                          Me.PartyMembers.All(p => p.IsAlive) && !ISeeAGhost;
                bool istank = Me.Specialization.IsTank();
                bool canQueueAsTank = istank && (DungeonBuddySettings.Instance.TankInRandomGroups || InFullGroupOfBots);
                bool shouldVendor = ShouldVendor;
                bool isInNonLfgInstance = IsInNonLfgInstance;

                var retVal = queue && (partyModeOffAndNotInParty || partyModeLeaderAndEveryoneIsAlive) && Me.IsAlive && (!shouldVendor || isInNonLfgInstance) &&
                             (!istank || canQueueAsTank);

                if (queue)
                {
                    if (istank && !canQueueAsTank)
                    {
                        Logger.Write(
                            "You're attempting to queue as tank to random LFG with real players. This is unsafe, dangerous and not supported but if you still wish to do this then you must enable 'Tank In Random Groups' setting under Advanced options.");
                    }

                    Logger.WriteDebug(
                        "[ShouldRequeue: {7}] queue: {0}, PartyMode Off And Not InParty: {1},  PartyMode Leader And Everyone IsAlive: {2}, Do VendorRun: {3}, Cant Do Vendor Run: {4}, IsTank: {5}, Can Queue As Tank: {6}",
                        queue,
                        partyModeOffAndNotInParty,
                        partyModeLeaderAndEveryoneIsAlive,
                        shouldVendor,
                        isInNonLfgInstance,
                        istank,
                        canQueueAsTank,
                        retVal);
                }
                return retVal;
            }
        }

        private bool IsHomePartyLeader => Lua.GetReturnVal<bool>("if UnitIsGroupLeader('player', 1) == true then return 1 end return nil", 0);

        private bool CanQueue
        {
            get
            {
                var isSpecific = DungeonBuddySettings.Instance.DungeonType == DungeonType.Specific || DungeonBuddySettings.Instance.ScenarioType == ScenarioType.Specific;
                var hasDeserter = Me.HasAura(71041);
                var hasDungeonCooldown = Me.HasAura("Dungeon Cooldown") && !isSpecific;
                var hasQueuePermissions = !Me.GroupInfo.IsInParty || Me.GroupInfo.GroupLeaderGuid == StyxWoW.Me.Guid;
                var canQueue = !hasDeserter && !hasDungeonCooldown && hasQueuePermissions;
                Logger.WriteDebug("[CanQueue: {0}]  hasDeserter: {1}, hasDungeonCooldown: {2}, hasQueuePermissions: {3}", canQueue, hasDeserter, hasDungeonCooldown, hasQueuePermissions);
                return canQueue;
            }
        }

        private bool ShouldAcceptProposal
        {
            get
            {
                if (!IsProposalActive || !Me.IsAlive)
                    return false;

                if (_acceptQueueTimer == null || !_acceptQueueTimer.IsFinished)
                    return false;

                return !ShouldVendor || IsInNonLfgInstance;
            }
        }

        private bool IsProposalActive
        {
            get
            {
                if (_proposalShowTimeMs == 0)
                    return false;
                return StyxWoW.WoWClient.PerformanceCounter() - _proposalShowTimeMs < 41000L;
            }
        }

        private bool ShouldContinueLfg
        {
            get
            {
                if (DungeonBuddySettings.Instance.PartyMode == PartyMode.Leader)
                {
                    var allFollowersInGroup = DungeonBuddySettings.Instance.PartyMembers.All(
                        name => string.IsNullOrWhiteSpace(name) || GroupMember.GroupMembers.Any(g => g.NameEquals(name)));
                    Logger.WriteDebug("Should continue LFG because all followers are in group: {0}", allFollowersInGroup);
                    return allFollowersInGroup;
                }

                if (DungeonBuddySettings.Instance.PartyMode == PartyMode.Follower)
                {
                    var leaderInGroup = GroupMember.GroupMembers.Any(g => g.NameEquals(DungeonBuddySettings.Instance.LeaderName));
                    Logger.WriteDebug("Should continue LFG because leader is in group: {0}", leaderInGroup);
                    return leaderInGroup;
                }

                if (Me.IsTank() && !DungeonBuddySettings.Instance.TankInRandomGroups)
                {
                    Logger.WriteDebug("Should not continue LFG because I am tank and I don't tank in randoms");
                    return false;
                }

                Logger.WriteDebug("Should continue LFG: true");
                return true;
            }
        }

        private bool InFullGroupOfBots
        {
            get
            {
                if (DungeonBuddySettings.Instance.DungeonType == DungeonType.Farm || DungeonBuddySettings.Instance.RaidType == RaidType.Farm)
                    return true;

                if (DungeonBuddySettings.Instance.PartyMode != PartyMode.Leader)
                    return false;

                var maxGroupSize = DungeonManager.SelectedDungeon == null
                                        ? (DungeonBuddySettings.Instance.RaidType == RaidType.Specific || DungeonBuddySettings.Instance.RaidType == RaidType.AutoQueueBest
                                               ? 25
                                               : DungeonBuddySettings.Instance.DungeonType != DungeonType.None && DungeonBuddySettings.Instance.DungeonType != DungeonType.Farm
                                                     ? 5
                                                     : DungeonBuddySettings.Instance.ScenarioType == ScenarioType.None ? 1 : 3)
                                        : DungeonManager.SelectedDungeon.LfgDungeon.MaxSize;

                var matchedPartyMembers = DungeonBuddySettings.Instance.PartyMembers.Count(name => GroupMember.GroupMembers.Any(g => g.NameEquals(name)));

                if (GroupMember.GroupMembers.Count != maxGroupSize)
                    return false;

                return maxGroupSize == matchedPartyMembers + 1;
            }
        }

        private static bool IsInNonLfgInstance
        {
            get
            {
                if (!IsInInstance)
                    return false;
                return CurrentLfgDungeonId == 0;
            }
        }

        private static bool IsInInstance
        {
            get
            {
                var currentMap = Me.CurrentMap;
                if (currentMap.IsInstance || currentMap.IsRaid)
                    return true;
                return currentMap.IsScenario;
            }
        }

        /// <summary>
        ///     Attaches all events used by the dungeon bot.
        /// </summary>
        private void AttachEvents()
        {
            if (_attachedEvents)
                return;
            try
            {
                // these events are attached only once
                BotEvents.Player.OnMapChanged += HandleMapChanged;
                LootTargeting.Instance.IncludeTargetsFilter += IncludeLootTargetsFilter;
                LootTargeting.Instance.WeighTargetsFilter += WeighLootTargetsFilter;
                LootTargeting.Instance.RemoveTargetsFilter += RemoveLootTargetsFilter;
                Lua.Events.AttachEvent("LFG_PROPOSAL_SHOW", HandleProposalShow);
                Lua.Events.AttachEvent("LFG_PROPOSAL_SUCCEEDED", HandleProposalSucceeded);
                Lua.Events.AttachEvent("LFG_PROPOSAL_FAILED", HandleProposalFailed);
                Lua.Events.AttachEvent("LFG_OFFER_CONTINUE", HandleOfferContinue);
                Lua.Events.AttachEvent("LFG_ROLE_CHECK_SHOW", HandleRoleCheckShow);
                Lua.Events.AttachEvent("RESURRECT_REQUEST", HandleResurrectRequest);
                Lua.Events.AttachEvent("PARTY_INVITE_REQUEST", HandlePartyInvite);
                Lua.Events.AttachEvent("LOOT_BIND_CONFIRM", (sender, args) => Lua.DoString("ConfirmLootSlot({0})", args.Args[0]));
                Lua.Events.AttachEvent("READY_CHECK", HandleReadyCheck);
                Lua.Events.AttachEvent("LFG_BOOT_PROPOSAL_UPDATE", HandleLfgBootProposal);
                Lua.Events.AttachEvent("START_LOOT_ROLL", HandleStartLootRoll);
                Lua.Events.AttachEvent("LOOT_HISTORY_ROLL_COMPLETE", HandleLootHistoryRollComplete);
                Lua.Events.AttachEvent("UI_INFO_MESSAGE", HandleUiInfoMessage);
                Lua.Events.AttachEvent("QUEST_ACCEPTED", HandleQuestAccepted);
                Lua.Events.AttachEvent("PARTY_LEADER_CHANGED", HandlePartyLeaderChanged);
            }
            finally
            {
                _attachedEvents = true;
            }
            Logger.WriteDebug("Attached event handlers");
        }

        /// <summary>
        ///     Detaches all events used by the dungeon bot.
        /// </summary>
        private void DetachEvents()
        {
            try
            {
                BotEvents.Player.OnMapChanged -= HandleMapChanged;
                LootTargeting.Instance.IncludeTargetsFilter -= IncludeLootTargetsFilter;
                LootTargeting.Instance.WeighTargetsFilter -= WeighLootTargetsFilter;
                LootTargeting.Instance.RemoveTargetsFilter -= RemoveLootTargetsFilter;
                Lua.Events.DetachEvent("LFG_PROPOSAL_SHOW", HandleProposalShow);
                Lua.Events.DetachEvent("LFG_PROPOSAL_SUCCEEDED", HandleProposalSucceeded);
                Lua.Events.DetachEvent("LFG_PROPOSAL_FAILED", HandleProposalFailed);
                Lua.Events.DetachEvent("LFG_OFFER_CONTINUE", HandleOfferContinue);
                Lua.Events.DetachEvent("LFG_ROLE_CHECK_SHOW", HandleRoleCheckShow);
                Lua.Events.DetachEvent("RESURRECT_REQUEST", HandleResurrectRequest);
                Lua.Events.DetachEvent("PARTY_INVITE_REQUEST", HandlePartyInvite);
                Lua.Events.DetachEvent("LOOT_BIND_CONFIRM", (sender, args) => Lua.DoString("ConfirmLootSlot({0})", args.Args[0]));
                Lua.Events.DetachEvent("READY_CHECK", HandleReadyCheck);
                Lua.Events.DetachEvent("LFG_BOOT_PROPOSAL_UPDATE", HandleLfgBootProposal);
                Lua.Events.DetachEvent("START_LOOT_ROLL", HandleStartLootRoll);
                Lua.Events.DetachEvent("LOOT_HISTORY_ROLL_COMPLETE", HandleLootHistoryRollComplete);
                Lua.Events.DetachEvent("UI_INFO_MESSAGE", HandleUiInfoMessage);
                Lua.Events.DetachEvent("QUEST_ACCEPTED", HandleQuestAccepted);
                Lua.Events.DetachEvent("PARTY_LEADER_CHANGED", HandlePartyLeaderChanged);
            }
            finally
            {
                _attachedEvents = false;
            }
            Logger.WriteDebug("Detached event handlers");
        }

        private static void DungeonIsCompleted(CompleteReason reason, int waitTime = 60)
        {
            CompleteTimer.WaitTime = TimeSpan.FromSeconds(waitTime);
            Logger.Write("Dungeon is considered complete in {0} seconds. Reason: {1}", waitTime, reason);
            DungeonCompleted = reason;
        }

        private void HandleMapChanged(BotEvents.Player.MapChangedEventArgs args)
        {
            _queueAttempts = 0;
            Navigator.Clear();
        }

        private void HandleReadyCheck(object sender, LuaEventArgs args)
        {
            _acceptReadyCheckTimer = new WaitTimer(TimeSpan.FromSeconds(new Random().Next(1, 6)));
            _acceptReadyCheckTimer.Reset();
        }

        private void HandleLfgBootProposal(object sender, LuaEventArgs args)
        {
            var needToVote =
                Lua.GetReturnVal<bool>(
                    "local voteInProgress, didVote, _, targetName = GetLFGBootProposal() if ( voteInProgress and not didVote and targetName ) then return 1 end return nil", 0);
            if (needToVote)
            {
                _kickVoteTimer = new WaitTimer(TimeSpan.FromSeconds(new Random().Next(1, 6)));
                _kickVoteTimer.Reset();
            }
        }

        private void HandleStartLootRoll(object sender, LuaEventArgs args)
        {
            RemoveExpiredLootrollEntries();
            var rollId = (int)((double)args.Args[0]);
            var timeMs = (int)((double)args.Args[1]);
            _lootCountdownTimers[rollId] = new WaitTimer(TimeSpan.FromMilliseconds(timeMs));
            _lootCountdownTimers[rollId].Reset();
            Logger.WriteDebug("Loot roll {0} started and {1} seconds left", rollId, _lootCountdownTimers[rollId].TimeLeft.TotalSeconds);
        }

        private void HandleLootHistoryRollComplete(object sender, LuaEventArgs args)
        {
            RemoveExpiredLootrollEntries();
            using (StyxWoW.Memory.AcquireFrame())
            {
                foreach (var key in _lootCountdownTimers.Keys.ToList())
                {
                    var lua = $"return GetLootRollTimeLeft({key})";
                    var timeLeftMs = Lua.GetReturnVal<int>(lua, 0);
                    if (timeLeftMs != 0) continue;
                    Logger.WriteDebug("Roll {0} is over.", key);
                    _lootCountdownTimers.Remove(key);
                }
            }
        }

        private void HandleResurrectRequest(object sender, LuaEventArgs args)
        {
            _acceptResurrect = true;
        }

        private void HandleProposalFailed(object sender, LuaEventArgs args)
        {
            _proposalShowTimeMs = 0;
        }

        private void HandleProposalSucceeded(object sender, LuaEventArgs args)
        {
            _proposalShowTimeMs = 0;
            if (IsProposalActive)
            {
                if (DungeonManager.SelectedDungeon != null)
                    HandleDungeonLeft();
                _proposalSucceeded = true;
            }
        }

        private void HandleProposalShow(object sender, LuaEventArgs args)
        {
            _proposalShowTimeMs = args.FireTimeStamp;
            var seconds = new Random().Next(1, 10);
            _acceptQueueTimer = new WaitTimer(TimeSpan.FromSeconds(seconds));
            _acceptQueueTimer.Reset();
            Logger.Write("Accepting LFG propsosal in {0} seconds.", seconds, 0);
        }

        private void HandleOfferContinue(object sender, LuaEventArgs args)
        {
            _acceptOfferContinue = true;
        }

        private void HandleRoleCheckShow(object sender, LuaEventArgs args)
        {
            _performRoleCheck = true;
        }

        private void HandlePartyInvite(object sender, LuaEventArgs args)
        {
            _acceptPartyInvite = true;
        }

        private void HandleUiInfoMessage(object sender, LuaEventArgs args)
        {
            var text = args.Args[0] as string;
            if (!string.IsNullOrEmpty(text) &&
                (text == GetGlobalString("ERR_LFG_ROLE_CHECK_FAILED_NOT_VIABLE") ||
                 text == GetGlobalString("ERR_LFG_TOO_FEW_MEMBERS") ||
                 text == GetGlobalString("ERR_LFG_TOO_MANY_MEMBERS") ||
                 text == GetGlobalString("ERR_LFG_INVALID_SLOT") ||
                 text == GetGlobalString("ERR_LFG_NO_SLOTS_PLAYER")))
            {
                System.Windows.MessageBox.Show(text);
                TreeRoot.Stop(text);
            }
        }

        private void HandleQuestAccepted(object sender, LuaEventArgs args)
        {
            if (args.Args.Length < 2)
                return;

            var id = (uint)(double)args.Args[1];
            var quest = Styx.WoWInternals.Quest.FromId(id);
            if (quest != null && quest.IsAutoAccepted && QuestFrame.Instance.IsVisible)
            {
                QuestFrame.Instance.Close();
                Logger.Write("Closed quest frame for the quest: {0} after it was automatically accepted.", quest.Name);
            }
        }

        private void HandlePartyLeaderChanged(object sender, LuaEventArgs args)
        {
            if (Me.IsGroupLeader && DungeonBuddySettings.Instance.FreeForAllLoot)
                Lua.DoString("SetLootMethod('freeforall')");
        }

        private void RemoveExpiredLootrollEntries()
        {
            foreach (var key in _lootCountdownTimers.Keys.ToList().Where(key => _lootCountdownTimers[key].IsFinished))
            {
                Logger.WriteDebug("Roll {0} has expired", key);
                _lootCountdownTimers.Remove(key);
            }
        }

        private string GetGlobalString(string name)
        {
            if (!_globalStringCache.TryGetValue(name, out var value))
                value = _globalStringCache[name] = Lua.GetReturnVal<string>($"return {name}", 0);
            return value;
        }

        private void IncludeLootTargetsFilter(List<WoWObject> incomingUnits, HashSet<WoWObject> outgoingUnits)
        {
            var location = Me.Location;
            var lootRadiusSqr = LootTargeting.LootRadius * LootTargeting.LootRadius;
            foreach (var item in incomingUnits)
            {
                var unit = item as WoWUnit;
                WoWGameObject gameObject;
                if (unit != null)
                {
                    if (DungeonBuddySettings.Instance.LootMode == LootMode.Off || !unit.IsDead ||
                        (DungeonBuddySettings.Instance.LootMode == LootMode.BossesOnly && !unit.IsBoss()) ||
                        ((!unit.KilledByMe || !unit.CanLoot) && (!unit.CanSkin || !LootTargeting.SkinMobs || (!CharacterSettings.Instance.NinjaSkin && !unit.KilledByMe))))
                    {
                        continue;
                    }
                }
                else
                {
                    if (!((gameObject = item as WoWGameObject) != null) || !gameObject.CanLoot ||
                        ((!LootTargeting.HarvestHerbs || !gameObject.IsHerb) && (!LootTargeting.HarvestMinerals || !gameObject.IsMineral) && (!LootTargeting.LootChests || !gameObject.IsChest)))
                    {
                        continue;
                    }
                    if (Bots.DungeonBuddy.Profiles.ProfileManager.CurrentProfile != null &&
                        Targeting.IsTooNearBlackspot(Bots.DungeonBuddy.Profiles.ProfileManager.CurrentProfile.Blackspots, gameObject.Location))
                    {
                        Blacklist.Add(gameObject.Guid, BlacklistFlags.Loot, TimeSpan.FromDays(3), "Too close to blackspot");
                        continue;
                    }
                }

                var itemLocation = item.Location;
                if (!(location.DistanceSqr(itemLocation) > lootRadiusSqr) && !ScriptHelpers.WillPullAggroAtLocation(itemLocation))
                    outgoingUnits.Add(item);
            }
        }

        private void WeighLootTargetsFilter(List<Targeting.TargetPriority> priorities)
        {
        }

        private void RemoveLootTargetsFilter(List<WoWObject> units)
        {
            var inCombat = Me.IsActuallyInCombat;
            units.RemoveAll(_ => inCombat);
        }

        private Composite CreateEventBehavior()
        {
            return new PrioritySelector(
                // Do we need to accept resurrect ?
                new Decorator(
                    ret => _acceptResurrect,
                    new PrioritySelector(
                // Idle if there is a corpse recovery delay
                        new Decorator(
                            ret => !Me.IsAlive && Lua.GetReturnVal<int>("return GetCorpseRecoveryDelay()", 0) != 0,
                            new Action(ret => Logger.Write("Waiting for corpse recovery delay to expire"))),
                // Get up damn it
                        new Sequence(
                            new Action(ret => Logger.Write("Accepting resurrect")),
                            new Action(ret => Lua.DoString("AcceptResurrect() StaticPopup_Hide('RESURRECT_NO_TIMER') StaticPopup_Hide('RESURRECT_NO_SICKNESS')")),
                            new Action(ret => CorpseReleaseTimer.Reset()),
                            new WaitContinue(
                                5,
                                ret => Me.IsAlive || Lua.GetReturnVal<bool>("if ResurrectGetOfferer() == nil then return 1 end return nil", 0),
                                new Action(ret => _acceptResurrect = false))))),
                // Do we need to accept a dungeon invite?
                new Decorator(
                    ret => ShouldAcceptProposal,
                    new Action(ret =>
                    {
                        Logger.WriteDebug("Accepting queue");
                        Lua.DoString("AcceptProposal()");
                    })),
                // Proposal succeeded: wait for the zone transition and reset the dungeon timer.
                new Decorator(
                    ret => _proposalSucceeded,
                    new ActionRunCoroutine(ret => HandleProposalTransition())),
                // Do a role check?
                new Decorator(
                    re => _performRoleCheck,
                    new Sequence(
                        new DecoratorContinue(
                            ret =>
                            DungeonCompleted != CompleteReason.Completed ||
                            (DungeonBuddySettings.Instance.PartyMode != PartyMode.Off && DungeonCompleted == CompleteReason.Completed) || !Me.CurrentMap.IsDungeon,
                            new Sequence(
                                new Action(ret => Logger.Write("Role Check is in progress")),
                                new ActionRunCoroutine(ret => LfgQueue.SetLfgRoles()),
                                new Action(ret => Lua.DoString("LFDRoleCheckPopupAcceptButton:Click() StaticPopup1Button1:Click()")))),
                        new Action(ret => _performRoleCheck = false))),
                // someone left instance party, should we continue or quit?
                new Decorator(
                    ctx => _acceptOfferContinue,
                    new Sequence(
                        ctx => ShouldContinueLfg,
                        new DecoratorContinue<bool>(
                            cont => cont,
                            new Sequence(
                                new Sleep(ctx => TimeSpan.FromMilliseconds(ScriptHelpers.Rnd.Next(2000, 4000))),
                                new Action(ctx => Lua.DoString("PartyLFGStartBackfill()")))),
                        new DecoratorContinue<bool>(cont => !cont, new Action(ctx => DungeonIsCompleted(CompleteReason.BackFillNotAccepted, 0))),
                        new Action(ctx => _acceptOfferContinue = false))),
                // Shall accept invites for group mode support
                new Decorator(
                    ret => _acceptPartyInvite,
                    new Sequence(
                        new DecoratorContinue(
                            ret => DungeonBuddySettings.Instance.PartyMode == PartyMode.Follower,
                            new Sequence(
                                ctx => new Random().Next(1, 10),
                                new Action(ctx => Logger.Write("Accepting party invite in {0} seconds.", (int)ctx, 0)),
                                new Sleep(ctx => TimeSpan.FromSeconds((int)ctx)),
                                new Action(ctx => Lua.DoString(
                                    "local f=select(2,StaticPopup_Visible('PARTY_INVITE')) or select(2,StaticPopup_Visible('PARTY_INVITE_XREALM')) if f then StaticPopup_OnClick(f, 1) end")))),
                        new Action(ret => _acceptPartyInvite = false))),
                new Decorator(
                    ctx => _acceptReadyCheckTimer != null && _acceptReadyCheckTimer.IsFinished,
                    new Sequence(
                        new Action(ctx => Logger.Write("Accepting ready check.")),
                        new Action(ctx => Lua.DoString("ConfirmReadyCheck(true) ReadyCheckFrameYesButton:Click()")),
                        new Action(ctx => _acceptReadyCheckTimer = null))),
                new Decorator(
                    ctx => _kickVoteTimer != null && _kickVoteTimer.IsFinished,
                    new Sequence(
                        new Action(ctx => Logger.Write("Accepting kick vote")),
                        new Action(ctx => Lua.DoString("SetLFGBootVote(true)")),
                        new Action(ctx => _kickVoteTimer = null))));
        }

        private async Task HandleProposalTransition()
        {
            await Coroutine.Wait(2000, () => !StyxWoW.IsInWorld);
            await Coroutine.Sleep(2000);
            _dungeonTimer.Reset();
            _proposalSucceeded = false;
        }

        private async Task<bool> CreateLeavePartyBehavior(string reason)
        {
            var isTank = Me.IsTank();
            if (LootRollInProcess)
                return false;

            if (CurrentLfgDungeonId != 0 && Me.Specialization == WoWSpec.PriestHoly)
            {
                var becameSpiritOfRedemption = await Coroutine.Wait((int)(StyxWoW.WoWClient.Latency + 600), () => Me.Shapeshift == ShapeshiftForm.SpiritOfRedemption);
                if (becameSpiritOfRedemption)
                    return false;
            }

            if (isTank && !Targeting.Instance.IsEmpty())
                return false;

            if (DungeonBuddySettings.Instance.PartyMode == PartyMode.Leader &&
                GroupMember.GroupMembers.Any(g => g.IsDead && DungeonBuddySettings.Instance.PartyMembers.Any(g.NameEquals)) &&
                GroupMember.GroupMembers.Any(g => g.Player != null && g.IsAlive && CanResurrect(g.Player)))
            {
                return false;
            }

            Logger.Write("Leaving group. Reason: {0}", reason);
            Lua.DoString("LeaveParty()");
            HandleDungeonLeft();
            await Coroutine.Wait(4000, () => !StyxWoW.IsInWorld);
            return true;
        }
    }
}
